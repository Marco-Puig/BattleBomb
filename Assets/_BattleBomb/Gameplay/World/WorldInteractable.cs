using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.Gameplay.World
{
    /// <summary>What walking up to a thing and pressing Light opens (D42/D43).</summary>
    public enum InteractionKind
    {
        /// <summary>The chest: inventory access made physical. Every checkpoint room has one.</summary>
        Chest = 0,

        /// <summary>The shopkeeper: buys anything, sells potions and a small rolled rack.</summary>
        Shopkeeper = 1,
    }

    /// <summary>
    /// A thing in the world a player can open (D42). Registers with the driver so contextual
    /// Light can find it the same way it finds a downed partner or a drop — the interaction is
    /// simulation state, never a trigger volume firing on a Unity callback.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldInteractable : MonoBehaviour
    {
        [Tooltip("What this opens when a player presses Light beside it.")]
        [SerializeField] private InteractionKind _kind = InteractionKind.Chest;

        [Tooltip("Planar reach, in world units — a little more generous than a loot grab.")]
        [SerializeField] private float _radius = 1.4f;

        [Tooltip("Driver this registers with. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        public InteractionKind Kind => _kind;

        public float Radius => Mathf.Max(0.1f, _radius);

        public Vector3 Position => transform.position;

        private void OnEnable()
        {
            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            if (_driver != null)
            {
                _driver.RegisterInteractable(this);
            }
        }

        private void OnDisable()
        {
            if (_driver != null)
            {
                _driver.UnregisterInteractable(this);
            }
        }
    }
}
