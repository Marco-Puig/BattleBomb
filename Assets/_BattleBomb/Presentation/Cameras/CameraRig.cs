using System.Collections.Generic;
using BattleBomb.Core.Cameras;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
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

        [Header("Screen framing (UI Pass 01)")]
        [Tooltip("Pull in on the player when they open a screen and hold the display alone.")]
        [SerializeField] private bool _frameOnScreens = true;

        [Tooltip("Share of the screen's height the framed character should fill. The distance is "
            + "derived from this and the character's own size, so it stays right when a capsule "
            + "is replaced by a sprite of a different height.")]
        [Range(0.2f, 0.95f)]
        [SerializeField] private float _screenFill = 0.72f;

        [Tooltip("Share of the display the screen's panel covers. Must match the chest screen's " +
            "own split, which is half.")]
        [Range(0f, 0.9f)]
        [SerializeField] private float _screenPanelFraction = 0.5f;

        [Tooltip("The panel sits on the left, so the framed character is pushed to the right.")]
        [SerializeField] private bool _screenPanelOnLeft = true;

        [Tooltip("Where the character's middle sits vertically, -1 bottom to 1 top. A little above "
            + "centre, so a character filling the height still clears the stats block.")]
        [Range(-1f, 1f)]
        [SerializeField] private float _screenSubjectY = 0.12f;

        private readonly List<Vector3> _targets = new List<Vector3>();
        private readonly List<Renderer> _renderers = new List<Renderer>();
        private Vector3 _velocity;
        private bool _snapped;

        private Transform _boundsOf;
        private Bounds _subjectBounds;
        private Vector3 _boundsOffset;

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
            CameraFrame frame = TryFrameOnScreen(actors, tuning, out CameraFrame framed)
                ? framed
                : CameraFraming.Compute(_targets, tuning, _driver.Bounds);
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

        /// <summary>
        /// Whether one player is alone on this display with a screen open — and if so, where the
        /// camera should sit so the screen's subject lands in the half the panel is not covering.
        ///
        /// This is read off the driver rather than pushed in by the UI: rule 2 has presentation
        /// observing simulation state, and the UI assembly cannot see this one anyway. The
        /// condition is deliberately the same one the chest screen uses to decide its own layout —
        /// a single character means a single player holds the display, so the camera is theirs to
        /// take. In local co-op it belongs to both and nothing moves.
        /// </summary>
        private bool TryFrameOnScreen(
            IReadOnlyList<CharacterActor> actors, in CameraTuning tuning, out CameraFrame frame)
        {
            frame = default;
            if (!_frameOnScreens || actors.Count != 1 || actors[0] == null)
            {
                return false;
            }

            int playerId = actors[0].PlayerId.Value;
            if (!_driver.TryGetOpenScreen(playerId, out InteractionKind kind))
            {
                return false;
            }

            frame = FrameOnSubject(SubjectOf(playerId, kind, actors[0]), tuning);
            return true;
        }

        /// <summary>
        /// Who the open screen is about. A chest is about the player — the panel beside them is
        /// their sack and their loadout, so they are what the camera holds. A shopkeeper is about
        /// the shopkeeper: the panel is their rack, and framing the player there would fill the
        /// free half with the one character the screen never mentions (Michael, 2026-08-23).
        /// </summary>
        private Transform SubjectOf(int playerId, InteractionKind kind, CharacterActor player)
        {
            if (kind != InteractionKind.Chest
                && _driver.TryGetOpenInteractable(playerId, out WorldInteractable source))
            {
                return source.transform;
            }

            return player.transform;
        }

        /// <summary>
        /// The subject, offset so a panel covering part of the screen does not cover them. The
        /// shift is computed from the camera's own frustum rather than authored, so it stays right
        /// if the field of view or the aspect changes.
        /// </summary>
        private CameraFrame FrameOnSubject(Transform subject, in CameraTuning tuning)
        {
            Bounds bounds = SubjectBounds(subject);

            // Distance follows from how much of the screen the character should fill, rather than
            // being a number tuned against one placeholder capsule.
            float tanHalfFov = Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float halfHeight = bounds.size.y / (2f * Mathf.Max(0.05f, _screenFill));
            float distance = Mathf.Max(_camera.nearClipPlane + 0.5f, halfHeight / Mathf.Max(0.01f, tanHalfFov));
            float halfWidth = halfHeight * Mathf.Max(0.1f, _camera.aspect);

            // A panel covering fraction f of one side leaves its free half centred at normalised
            // x = ±f. The focus moves toward the panel, which pushes the subject away from it: a
            // panel on the left sits the focus left of the subject, so the subject reads right.
            float direction = _screenPanelOnLeft ? -1f : 1f;

            Vector3 focus = bounds.center;
            focus.x += direction * _screenPanelFraction * halfWidth;

            // The subject's middle lands at _screenSubjectY in normalised screen space, so the
            // focus sits that far the other side of it.
            focus.y -= _screenSubjectY * halfHeight;

            // The character's own depth is kept, unlike the shared framing which flattens focus
            // onto the z=0 plane. Flattening it here would leave the subject standing however far
            // off that plane they happen to be — further from the camera than the fill maths
            // assumed, so they come out smaller than asked for.
            return new CameraFrame(focus, distance);
        }

        /// <summary>
        /// The character's rendered extent. Measured rather than assumed so the framing survives
        /// the capsule being replaced by a billboard sprite (D47), which will not be two units
        /// tall. Cached per subject: this runs every LateUpdate while a screen is open.
        /// </summary>
        private Bounds SubjectBounds(Transform subject)
        {
            if (_boundsOf == subject)
            {
                Bounds cached = _subjectBounds;
                cached.center = subject.position + _boundsOffset;
                return cached;
            }

            subject.GetComponentsInChildren(includeInactive: false, _renderers);
            var bounds = new Bounds(subject.position + Vector3.up, new Vector3(1f, 2f, 1f));
            for (int i = 0; i < _renderers.Count; i++)
            {
                if (i == 0)
                {
                    bounds = _renderers[i].bounds;
                }
                else
                {
                    bounds.Encapsulate(_renderers[i].bounds);
                }
            }

            _boundsOf = subject;
            _subjectBounds = bounds;
            _boundsOffset = bounds.center - subject.position;
            return bounds;
        }
    }
}
