using System.Collections.Generic;
using BattleBomb.Core.Items;
using BattleBomb.Core.Progression;
using BattleBomb.Core.Saves;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.Items;
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

            for (int i = 0; i < _players.Length; i++)
            {
                if (_players[i] == null)
                {
                    continue;
                }

                CharacterDefinition definition = i < _session.Characters.Length ? _session.Characters[i] : null;
                if (definition == null)
                {
                    // Nobody sits here: the object never wakes, so the driver sees one player.
                    _players[i].gameObject.SetActive(false);
                    continue;
                }

                _players[i].gameObject.SetActive(true);
                _players[i].SetDefinition(definition);
            }
        }

        private void Start()
        {
            if (_session == null || _session.LoadedSave == null)
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
                if (_players[i] == null || !_players[i].gameObject.activeSelf)
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
