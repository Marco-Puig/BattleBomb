using System.Collections.Generic;
using BattleBomb.Core.Chapters;
using BattleBomb.Core.Items;
using BattleBomb.Core.Players;
using BattleBomb.Core.Progression;
using BattleBomb.Core.Saves;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.Items;
using BattleBomb.Gameplay.Net;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.Gameplay.Session
{
    /// <summary>
    /// Applies the <see cref="GameSession"/> to the machine as it boots: which characters the
    /// player objects are, which of them exist at all, and — once every inventory is up — the
    /// saved sack, wallet, and per-character state (D51/D52). With no session, it does nothing
    /// and the scene runs as authored, so pressing Play here still works.
    /// </summary>
    /// <remarks>
    /// The couch is bound in <c>Awake</c>, not <c>OnEnable</c>, and the execution order is the
    /// reason: a slot nobody sits in has to be deactivated <em>before</em> its own components
    /// wake, or its command source registers with the driver and every forward gate waits
    /// forever for a player who has no device to walk with. The save is restored in
    /// <c>Start</c> instead, because it needs every <see cref="PlayerInventory"/> to have built
    /// its inventory over the shared sack first, and that happens in their <c>OnEnable</c>.
    /// </remarks>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class SessionBinder : MonoBehaviour
    {
        [Tooltip("The machine's player objects, in slot order.")]
        [SerializeField] private CharacterActor[] _players = new CharacterActor[0];

        [SerializeField] private SimulationDriver _driver;
        [SerializeField] private SharedStash _stash;

        private GameSession _session;

        /// <summary>The seat a connected guest plays from this machine, or -1.</summary>
        private int _remoteSeat = -1;

        private void Awake()
        {
            _session = GameSession.Find();
            if (_session == null)
            {
                return;
            }

            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            if (_session.Seeds.HasValue && _driver != null)
            {
                _driver.UseSeeds(_session.Seeds.Value);
            }

            if (_players.Length == 0)
            {
                Debug.LogError(
                    $"{name}: a session launched this scene but no player objects are wired — nobody " +
                    "will be who the front door chose, and an empty slot will still hold every gate shut.",
                    this);
                return;
            }

            NetSession net = _session.Net;
            bool hosting = net != null && net.Role == NetRole.Host && net.IsConnected;
            bool guesting = net != null && net.Role == NetRole.Guest && net.IsConnected;

            for (int i = 0; i < _players.Length; i++)
            {
                if (_players[i] == null)
                {
                    continue;
                }

                // Plan 1's stand-in until the lobby (HANDOFF-M8 Task 102): a connected guest takes the
                // second seat as the host's own hero — over a local Player 2, because D59 lets a couch
                // pair and an online guest never both be here. Chosen here and never written into the
                // session, which outlives the match and would seat the stand-in back at the front door.
                int seat = hosting && i == net.GuestPlayerId.Value ? 0 : i;
                CharacterDefinition definition = seat < _session.Characters.Length ? _session.Characters[seat] : null;
                if (definition == null)
                {
                    // Nobody sits here: the object never wakes, so the driver sees one player.
                    _players[i].gameObject.SetActive(false);
                    continue;
                }

                _players[i].gameObject.SetActive(true);
                _players[i].SetDefinition(definition);
            }

            if (hosting)
            {
                BindHost(net);
            }
            else if (guesting)
            {
                BindGuest(net);
            }
        }

        private void Start()
        {
            if (_session == null || _session.LoadedSave == null)
            {
                return;
            }

            // The guest's gear is the host's to hold during a match (D61); nothing of this machine's
            // save is restored into a world it only draws.
            if (NetSession.RoleOf(_session) == NetRole.Guest)
            {
                return;
            }

            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            if (_stash == null)
            {
                _stash = FindAnyObjectByType<SharedStash>();
            }

            if (_driver == null || _stash == null)
            {
                // Loud rather than silent: a save that does not land looks exactly like a fresh
                // game until the first autosave writes the emptiness back over it.
                Debug.LogError(
                    $"{name}: no {(_driver == null ? "SimulationDriver" : "SharedStash")} in the scene — " +
                    "the save was read but nothing could be restored into.", this);
                return;
            }

            SaveGame save = _session.LoadedSave;
            IReadOnlyList<ItemSpec> catalog = _driver.ItemSpecs;
            SaveMapper.RestoreSack(save, _stash.Sack, catalog);

            // The same sack instance back in, deliberately: every PlayerInventory built its
            // Inventory over it at OnEnable, and handing back a different one would leave two
            // loadouts pointing at a bag nothing else can see. Restore raises Changed, which is
            // what makes both chest screens and both stat sheets redraw from the loaded state.
            _stash.Restore(_stash.Sack, SaveMapper.RestoreWallet(save));

            for (int i = 0; i < _players.Length; i++)
            {
                // The guest's stand-in wears none of the host's saved gear: a copy of it could reach
                // the shared sack and be saved back as a second of each item.
                if (_players[i] == null || !_players[i].gameObject.activeSelf || i == _remoteSeat)
                {
                    continue;
                }

                PlayerInventory bag = _players[i].GetComponent<PlayerInventory>();
                CharacterSave saved = FindCharacter(save, _players[i].Element.Value);
                if (bag == null || saved == null)
                {
                    continue;
                }

                XpLedger ledger = SaveMapper.RestoreCharacter(saved, bag.Inventory, catalog);
                bag.SetLedger(ledger);
            }
        }

        private void BindHost(NetSession net)
        {
            int slot = net.GuestPlayerId.Value;
            if (slot >= _players.Length || _players[slot] == null)
            {
                Debug.LogError($"{name}: a guest is connected but there is no player object for seat {slot}.", this);
                return;
            }

            // Every local device drives the host's own player now: the second seat is the guest's.
            // Without this, a couch Player 2 who joined at character select keeps their pad from Player 1.
            _session.Seats.Follow(FrontendScreen.Title, false, SeatAssignment.NoDevice, SeatAssignment.NoDevice);
            _remoteSeat = slot;
            RemoteCommandSource remote = NetSeats.MakeRemote(_players[slot], net.GuestPlayerId);
            gameObject.AddComponent<NetHost>().Begin(
                net, _session, _driver, FindAnyObjectByType<World.StageRunner>(), remote);
        }

        private void BindGuest(NetSession net)
        {
            // This machine has one player, and every local device is theirs (D57's seat 0 rule with
            // nobody joining). The front door may have left seat 0 held to one device — the guest may
            // have joined from character select — so the seats start clean for the match.
            _session.Seats.Follow(FrontendScreen.Title, false, SeatAssignment.NoDevice, SeatAssignment.NoDevice);

            for (int i = 0; i < _players.Length; i++)
            {
                if (_players[i] == null || !_players[i].gameObject.activeSelf)
                {
                    continue;
                }

                if (i == net.GuestPlayerId.Value)
                {
                    NetSeats.MakeLocalGuest(_players[i], net.GuestPlayerId);
                }
                else
                {
                    NetSeats.MakeReplica(_players[i]);
                }
            }

            _driver.EnterReplicaMode();
            gameObject.AddComponent<NetGuest>().Begin(net, _driver, net.GuestPlayerId);
        }

        private static CharacterSave FindCharacter(SaveGame save, int elementId)
        {
            CharacterSave[] characters = save.Characters;
            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i] != null && characters[i].ElementId == elementId)
                {
                    return characters[i];
                }
            }

            return null;
        }
    }
}
