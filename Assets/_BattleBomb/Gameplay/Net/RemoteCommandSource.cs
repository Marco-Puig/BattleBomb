using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.Gameplay.Net
{
    /// <summary>
    /// A player on another machine, as the host's simulation hears them: one more
    /// <see cref="IPlayerCommandSource"/>, indistinguishable from a local pad (D10's promise, D58).
    /// Added at runtime by <see cref="NetSeats"/>, and registers only once it is bound — adding a
    /// component to an active object runs <c>OnEnable</c> immediately, before any id is known.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RemoteCommandSource : MonoBehaviour, IPlayerCommandSource, IRemotePlayerSource
    {
        private IPlayerRegistryHost _host;
        private PlayerId _id;
        [System.NonSerialized] private bool _bound;

        public PlayerId PlayerId => _id;

        public RemoteCommandStream Stream { get; } = new RemoteCommandStream();

        public PlayerCommand Sample(int frame) => Stream.Next(frame);

        internal void Bind(PlayerId id)
        {
            Unregister();
            _id = id;
            _bound = true;
            if (isActiveAndEnabled)
            {
                Register();
            }
        }

        private void OnEnable()
        {
            if (_bound)
            {
                Register();
            }
        }

        private void OnDisable() => Unregister();

        private void Register()
        {
            _host = FindAnyObjectByType<SimulationDriver>();
            if (_host == null)
            {
                Debug.LogError($"{name}: no SimulationDriver — the remote player will never be sampled.", this);
                return;
            }

            _host.Players.Register(this);
        }

        private void Unregister()
        {
            _host?.Players.Unregister(_id);
            _host = null;
        }
    }
}
