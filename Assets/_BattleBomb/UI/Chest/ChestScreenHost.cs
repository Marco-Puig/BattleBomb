using System.Collections.Generic;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Items;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BattleBomb.UI.Chest
{
    /// <summary>
    /// Owns the chest screens (D42). Listens for the driver's open and close events, builds one
    /// screen per player who has one open, and lays them out per mode: alone it fills the
    /// display, in couch co-op it takes that player's half and leaves the other half playing.
    ///
    /// The whole screen is placeholder UI — it says what it needs to say and nothing more, and
    /// the art pass replaces it (D47).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChestScreenHost : MonoBehaviour
    {
        [Tooltip("Driver whose screens this follows. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        [Header("Fonts (UI Pass 01)")]
        [Tooltip("Passion One — headings and numbers. Any left empty falls back to the built-in font.")]
        [SerializeField] private Font _displayFont;

        [Tooltip("Archivo — body copy and labels.")]
        [SerializeField] private Font _uiFont;

        [Tooltip("Space Mono — small caps labels and placeholder plates.")]
        [SerializeField] private Font _monoFont;

        private readonly Dictionary<int, ChestScreen> _screens = new Dictionary<int, ChestScreen>();
        private readonly List<int> _scratch = new List<int>();
        private Canvas _canvas;

        internal void RequestClose(int playerId) => _driver?.RequestClose(playerId);

        /// <summary>Where this player's menu actions go: here and now on the couch, across the wire on a
        /// guest (HANDOFF-M8 planning decision 11).</summary>
        internal IPlayerRequests RequestsFor(int playerId) => _driver != null ? _driver.RequestsFor(playerId) : null;

        /// <summary>Which button pictures this player's screen shows (D57).</summary>
        internal InputFamily FamilyFor(int playerId) =>
            _driver != null ? _driver.Players.FamilyOf(new PlayerId(playerId)) : InputFamily.Keyboard;

        /// <summary>The shopkeeper's rack for this player's visit (D43) — the simulation's; the screen draws a copy.</summary>
        internal IReadOnlyList<Core.Items.ItemInstance> RackFor(int playerId) =>
            _driver != null ? _driver.RackFor(playerId) : (IReadOnlyList<Core.Items.ItemInstance>)System.Array.Empty<Core.Items.ItemInstance>();

        /// <summary>
        /// The item's face, or null when it has none authored yet — which is the common case, and
        /// what the sack's placeholder plate is for. Keyed by definition id because that is the
        /// only handle on an item that survives a save.
        /// </summary>
        internal Sprite IconFor(int definitionId) => _driver?.ItemIcons?.For(definitionId);

        private void OnEnable()
        {
            UiBuild.UseFonts(_displayFont, _uiFont, _monoFont);

            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            if (_driver == null)
            {
                // Fully qualified: this assembly has its own Debug namespace for the HUD overlays.
                UnityEngine.Debug.LogError(
                    $"{name}: no SimulationDriver — chests will open onto nothing.", this);
                return;
            }

            EnsureCanvas();
            _driver.ScreenChanged += OnScreenChanged;
            _driver.Stepped += OnStepped;
            _driver.MenuStepped += OnMenuStepped;
            _driver.RackChanged += OnRackChanged;
        }

        private void OnDisable()
        {
            if (_driver != null)
            {
                _driver.ScreenChanged -= OnScreenChanged;
                _driver.Stepped -= OnStepped;
                _driver.MenuStepped -= OnMenuStepped;
                _driver.RackChanged -= OnRackChanged;
            }

            CloseAll();
        }

        private void EnsureCanvas()
        {
            if (_canvas != null)
            {
                return;
            }

            var go = new GameObject("Chest Canvas", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            _canvas = go.GetComponent<Canvas>();

            // Camera space rather than overlay: an overlay canvas is composited outside the
            // camera, so it is invisible to every screenshot the editor can take — which would
            // make this screen unverifiable by anything but a human staring at the monitor.
            Camera camera = Camera.main;
            if (camera != null)
            {
                _canvas.renderMode = RenderMode.ScreenSpaceCamera;
                _canvas.worldCamera = camera;
                _canvas.planeDistance = 1f;
            }
            else
            {
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            // Above the placeholder IMGUI HUD, which draws in its own pass anyway.
            _canvas.sortingOrder = 100;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Pointer input only. Navigation stays on the command stream — this exists so a
            // click or a touch can hit the close button, which is the route out that does not
            // require knowing which gamepad button means "back" (D5's mobile viability, and
            // Michael's M6 pass).
            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
        }

        /// <summary>
        /// One event system for pointer hits. The project has none otherwise, because the menus
        /// are driven by commands — so this is created rather than assumed, and only once.
        /// </summary>
        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var go = new GameObject("Chest Event System", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>();
        }

        private void OnScreenChanged(int playerId, InteractionKind kind, bool opened)
        {
            if (!opened)
            {
                Close(playerId);
                return;
            }

            Open(playerId, kind);
        }

        /// <summary>A purchase emptied a slot, or — on a guest — the host's rack arrived.</summary>
        private void OnRackChanged(int playerId)
        {
            if (_screens.TryGetValue(playerId, out ChestScreen screen) && screen != null)
            {
                screen.OnRackChanged();
            }
        }

        private void Open(int playerId, InteractionKind kind)
        {
            Close(playerId);

            PlayerInventory bag = null;
            CharacterActor actor = null;
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                if (actors[i].PlayerId.Value != playerId)
                {
                    continue;
                }

                actor = actors[i];
                bag = actors[i].GetComponent<PlayerInventory>();
                break;
            }

            // A remote player's screen is on their own machine (HANDOFF-M8 planning decision 15): the host holds
            // it open — that is what keeps their body standing idle — and draws nothing here.
            if (bag == null || !_driver.Players.IsLocal(new PlayerId(playerId)))
            {
                return;
            }

            // One player on this display — solo, or online co-op where the partner has their own
            // screen — means the sack takes the left half and the camera pushes the hero into the
            // right. Two players sharing a display means the camera belongs to both, so the screen
            // takes that player's half and tabs instead. CameraRig reads the same condition off
            // the driver rather than being told: UI does not reach into Presentation.
            bool split = _driver.Players.LocalCount > 1;

            var go = new GameObject($"Chest Screen P{playerId + 1}", typeof(RectTransform));
            go.transform.SetParent(_canvas.transform, false);
            var screen = go.AddComponent<ChestScreen>();
            screen.Host = this;
            LayOut((RectTransform)go.transform, playerId, split, _driver.Players.LocalCount);
            screen.Bind(bag, actor, playerId, kind, split);
            _screens[playerId] = screen;

            // Redrawn on every bag change, so a sale or an upgrade shows immediately.
            bag.Changed += screen.OnBagChanged;
        }

        /// <summary>
        /// Solo the screen fills the display; in couch co-op it takes the opener's half so the
        /// partner keeps playing on the other (D42). Player order decides which half.
        /// </summary>
        private static void LayOut(RectTransform rect, int playerId, bool split, int playerCount)
        {
            if (!split)
            {
                UiBuild.Place(rect, 0.06f, 0.06f, 0.94f, 0.94f);
                return;
            }

            bool leftHalf = playerId % 2 == 0;
            UiBuild.Place(
                rect,
                leftHalf ? 0.02f : 0.52f, 0.06f,
                leftHalf ? 0.48f : 0.98f, 0.94f);
        }

        private void Close(int playerId)
        {
            if (!_screens.TryGetValue(playerId, out ChestScreen screen))
            {
                return;
            }

            if (screen != null)
            {
                PlayerInventory bag = screen.Bag;
                if (bag != null)
                {
                    bag.Changed -= screen.OnBagChanged;
                }

                Destroy(screen.gameObject);
            }

            _screens.Remove(playerId);
        }

        private void CloseAll()
        {
            _scratch.Clear();
            foreach (KeyValuePair<int, ChestScreen> entry in _screens)
            {
                _scratch.Add(entry.Key);
            }

            for (int i = 0; i < _scratch.Count; i++)
            {
                Close(_scratch[i]);
            }
        }

        private void OnStepped(int frame) => TickScreens();

        private void OnMenuStepped() => TickScreens();

        private void TickScreens()
        {
            if (_screens.Count == 0)
            {
                return;
            }

            _scratch.Clear();
            foreach (KeyValuePair<int, ChestScreen> entry in _screens)
            {
                _scratch.Add(entry.Key);
            }

            for (int i = 0; i < _scratch.Count; i++)
            {
                int playerId = _scratch[i];
                if (!_screens.TryGetValue(playerId, out ChestScreen screen) || screen == null)
                {
                    continue;
                }

                PlayerCommand command = _driver.CommandFor(playerId);
                screen.Tick(command, Time.unscaledDeltaTime);
            }
        }
    }
}
