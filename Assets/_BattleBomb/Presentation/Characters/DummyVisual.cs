using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.Presentation.Characters
{
    /// <summary>
    /// The dummy's counterpart to <c>InterpolatedVisual</c>: follows the simulated body between
    /// fixed steps. Reads and never writes, like everything in this assembly (§3).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DummyVisual : MonoBehaviour
    {
        [Tooltip("Dummy this visual follows. Leave empty to find one on a parent.")]
        [SerializeField] private TrainingDummy _dummy;

        [Tooltip("Driver whose interpolation fraction is read. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        private void OnEnable()
        {
            if (_dummy == null)
            {
                _dummy = GetComponentInParent<TrainingDummy>();
            }

            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            if (_dummy == null || _driver == null)
            {
                Debug.LogWarning($"{name}: missing dummy or driver — the visual will not follow anything.", this);
            }
        }

        private void LateUpdate()
        {
            if (_dummy == null || _driver == null)
            {
                return;
            }

            transform.position = Vector3.Lerp(_dummy.PreviousPosition, _dummy.Position, _driver.Alpha);
        }
    }
}
