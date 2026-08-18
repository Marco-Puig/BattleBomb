using System.Collections.Generic;
using BattleBomb.Core.Cameras;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.Presentation.Cameras
{
    /// <summary>
    /// Moves the camera toward the frame Core computes. All framing maths lives in
    /// <see cref="CameraFraming"/> — this component only damps and applies it (§3).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraRig : MonoBehaviour
    {
        [Tooltip("Driver whose characters are framed. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        [Tooltip("Camera this rig moves. Leave empty to use the one on this object.")]
        [SerializeField] private Camera _camera;

        [SerializeField] private float _damping = 0.15f;

        [Header("Framing")]
        [SerializeField] private Vector3 _offsetDirection = new Vector3(0f, 0.45f, -1f);
        [SerializeField] private float _baseDistance = 14f;
        [SerializeField] private float _minDistance = 10f;
        [SerializeField] private float _maxDistance = 22f;
        [SerializeField] private float _comfortWidth = 8f;
        [SerializeField] private float _distancePerUnitSpread = 0.8f;
        [SerializeField] private float _focusHeight = 1.4f;
        [SerializeField] private float _edgeInset = 4f;

        private readonly List<Vector3> _targets = new List<Vector3>();
        private Vector3 _velocity;
        private bool _snapped;

        private CameraTuning Tuning => new CameraTuning(
            _offsetDirection, _baseDistance, _minDistance, _maxDistance,
            _comfortWidth, _distancePerUnitSpread, _focusHeight, _edgeInset);

        private void OnEnable()
        {
            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            _snapped = false;
            _velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (_driver == null || _camera == null)
            {
                return;
            }

            _targets.Clear();
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                _targets.Add(actors[i].Position);
            }

            CameraTuning tuning = Tuning;
            CameraFrame frame = CameraFraming.Compute(_targets, tuning, _driver.Bounds);
            Vector3 target = frame.PositionFor(tuning);

            Transform rig = _camera.transform;
            if (_snapped)
            {
                rig.position = Vector3.SmoothDamp(rig.position, target, ref _velocity, _damping);
            }
            else
            {
                rig.position = target;
                _snapped = true;
            }

            rig.LookAt(frame.Focus);
        }
    }
}
