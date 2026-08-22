using BattleBomb.Core.Movement;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.Presentation.Characters
{
    /// <summary>
    /// Smooths the fixed-step motion for rendering by interpolating between the actor's previous and
    /// current simulated positions. Reads gameplay state and never writes it — that separation is
    /// the entire point of this assembly (§3, D10).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InterpolatedVisual : MonoBehaviour
    {
        [Tooltip("Actor this visual follows. Leave empty to find one on a parent.")]
        [SerializeField] private CharacterActor _actor;

        [Tooltip("Driver whose interpolation fraction is read. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        private void OnEnable()
        {
            if (_actor == null)
            {
                _actor = GetComponentInParent<CharacterActor>();
            }

            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            if (_actor == null || _driver == null)
            {
                Debug.LogWarning($"{name}: missing actor or driver — the visual will not follow anything.", this);
            }
        }

        private void LateUpdate()
        {
            if (_actor == null || _driver == null)
            {
                return;
            }

            transform.position = Vector3.Lerp(_actor.PreviousPosition, _actor.Position, _driver.Alpha);
            transform.rotation = Quaternion.Euler(0f, _actor.Facing == Facing.Right ? 0f : 180f, 0f);
        }
    }
}
