using System.Collections.Generic;
using System.Text;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Items;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using BattleBomb.UI.Chest;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BattleBomb.UI.Frontend
{
    /// <summary>
    /// Chapter complete (D49's end of the line): what each player grabbed and reached, what the
    /// clear just unlocked (D50), and one button back to chapter select. Holds the world paused
    /// while it is up, because the chapter's last stage is still under the players' feet and
    /// nothing else is going to stop them walking around in it.
    /// </summary>
    /// <remarks>
    /// It repaints every step rather than once at open, and that is deliberate: the unlock lines
    /// come from <see cref="SaveService"/>, which fills them in its own handler for the same
    /// <see cref="StageRunner.ChapterCompleted"/> event. Whichever of the two subscribed first
    /// runs first, and subscription order is hierarchy order — a fact that survives exactly
    /// until somebody reorders two roots in the scene. Repainting continuously costs a
    /// StringBuilder per step and makes the question stop mattering.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ResultsScreen : MonoBehaviour
    {
        [Tooltip("Driver whose players these results are. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        [Tooltip("The runner whose chapter completion opens this. Leave empty to find it.")]
        [SerializeField] private StageRunner _runner;

        private readonly StringBuilder _text = new StringBuilder();

        private Canvas _canvas;
        private GameObject _panel;
        private Text _body;
        private SaveService _saves;
        private bool _open;
        private bool _holdingPause;
        private bool _leaving;

        public bool IsOpen => _open;

        private void OnEnable()
        {
            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            if (_runner == null)
            {
                _runner = FindAnyObjectByType<StageRunner>();
            }

            _saves = FindAnyObjectByType<SaveService>();

            if (_runner != null)
            {
                _runner.ChapterCompleted += Open;
            }

            if (_driver != null)
            {
                _driver.MenuStepped += Tick;
                _driver.Stepped += OnStepped;
            }

            // Disabled mid-results and enabled again: the panel is still on screen and the world
            // is running behind it, because OnDisable gave the pause back. Take it again rather
            // than leaving a screen up that nobody can dismiss over a world nobody stopped.
            if (_open && !_leaving)
            {
                HoldPause(true);
            }
        }

        private void OnDisable()
        {
            if (_runner != null)
            {
                _runner.ChapterCompleted -= Open;
            }

            if (_driver != null)
            {
                _driver.MenuStepped -= Tick;
                _driver.Stepped -= OnStepped;
            }

            // The hold is a count on the driver and this object may never come back. Releasing it
            // here is the only thing that keeps a disabled results screen from pausing the game
            // forever; OnEnable takes it again if the screen is still up.
            HoldPause(false);
        }

        private void OnStepped(int frame) => Tick();

        private void Open()
        {
            if (_open || _driver == null)
            {
                return;
            }

            Build();
            _open = true;
            _panel.SetActive(true);
            HoldPause(true);
            Repaint();
        }

        private void Tick()
        {
            if (!_open || _leaving)
            {
                return;
            }

            Repaint();

            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                if (_driver.CommandFor(actors[i].PlayerId.Value).WasPressed(CommandButtons.Light))
                {
                    Leave();
                    return;
                }
            }
        }

        private void Leave()
        {
            if (_leaving)
            {
                return;
            }

            _leaving = true;
            HoldPause(false);
            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
        }

        /// <summary>Held as a bool of our own, not inferred: the driver counts holders, so
        /// releasing one we never took would un-pause somebody else's menu.</summary>
        private void HoldPause(bool held)
        {
            if (_driver == null || held == _holdingPause)
            {
                return;
            }

            _holdingPause = held;
            _driver.HoldMenuPause(held);
        }

        private void Build()
        {
            if (_panel != null)
            {
                return;
            }

            var canvasGo = new GameObject(
                "Results Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
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

            _canvas.sortingOrder = 300;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform panel = UiBuild.Place(
                UiBuild.Rect("Results", _canvas.transform), 0.25f, 0.2f, 0.75f, 0.8f);
            UiBuild.Box("Back", panel, UiBuild.Panel);
            RectTransform pad = UiBuild.Place(UiBuild.Rect("Pad", panel), 0f, 0.15f, 1f, 1f, 20f);
            _body = UiBuild.Label("Text", pad, string.Empty, 18, UiBuild.Ink, TextAnchor.UpperLeft);

            Image box = UiBuild.Box("Continue", panel, UiBuild.PanelInner);
            UiBuild.Place((RectTransform)box.transform, 0.3f, 0.03f, 0.7f, 0.12f);
            box.raycastTarget = true;
            box.gameObject.AddComponent<Button>().onClick.AddListener(Leave);
            UiBuild.Label("Label", box.transform, "Continue", 16, UiBuild.Ink, TextAnchor.MiddleCenter);
            _panel = panel.gameObject;
        }

        private void Repaint()
        {
            if (_body == null)
            {
                return;
            }

            _text.Clear();
            _text.Append("CHAPTER COMPLETE\n\n");
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                PlayerInventory bag = actors[i].GetComponent<PlayerInventory>();
                _text.Append("Player ").Append(i + 1)
                    .Append("   grabbed ").Append(_driver.GrabCountFor(actors[i].PlayerId.Value))
                    .Append("   level ").Append(bag != null ? bag.Level : 0)
                    .Append('\n');
            }

            if (_saves == null)
            {
                _saves = FindAnyObjectByType<SaveService>();
            }

            if (_saves != null)
            {
                if (_saves.UnlockedChapter.Length > 0)
                {
                    _text.Append('\n').Append(UiBuild.Tint("Unlocked: " + _saves.UnlockedChapter, UiBuild.Better));
                }

                if (_saves.UnlockedTier.Length > 0)
                {
                    _text.Append('\n').Append(UiBuild.Tint("Unlocked tier: " + _saves.UnlockedTier, UiBuild.Better));
                }

                if (_saves.WritingBlocked)
                {
                    // The one thing worse than not saving is not saying so on the screen that
                    // exists to tell the player what they just earned.
                    _text.Append('\n').Append(UiBuild.Tint(
                        "NOT SAVED - an existing save file could not be read by this build.", UiBuild.Worse));
                }
            }

            _text.Append("\n\nLight: back to chapters");
            _body.text = _text.ToString();
        }
    }
}
