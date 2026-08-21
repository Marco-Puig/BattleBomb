using System.Collections.Generic;
using System.Text;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Items;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace BattleBomb.UI.Chest
{
    /// <summary>
    /// The global settings menu (Michael, M6 design session: "All settings will be in a global
    /// settings menu" — never on the chest screen). Minimal by design: it exists because D30's
    /// auto-equip and D43's auto-sell need a home, and it grows as real settings arrive.
    ///
    /// Opened with Pause by any player, closed the same way. D51 made the couch share one save,
    /// so the two toggles are one row each for the whole machine, not one pair per player.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SettingsMenu : MonoBehaviour
    {
        [Tooltip("Driver whose players own these settings. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        private readonly StringBuilder _text = new StringBuilder();
        private readonly List<PlayerInventory> _bags = new List<PlayerInventory>();

        private Canvas _canvas;
        private GameObject _panel;
        private Text _body;
        private bool _open;
        private int _cursor;
        private int _owner = -1;
        private Vector2 _lastMove;

        /// <summary>Open right now — the driver asks, because an open menu pauses a solo game.</summary>
        public bool IsOpen => _open;

        private void OnEnable()
        {
            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            if (_driver == null)
            {
                UnityEngine.Debug.LogError($"{name}: no SimulationDriver — settings cannot open.", this);
                return;
            }

            _driver.Stepped += OnTick;
            _driver.MenuStepped += OnMenuTick;
        }

        private void OnDisable()
        {
            if (_driver != null)
            {
                Close();
                _driver.Stepped -= OnTick;
                _driver.MenuStepped -= OnMenuTick;
            }
        }

        private void OnTick(int frame) => Tick();

        private void OnMenuTick() => Tick();

        private void Tick()
        {
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;

            if (!_open)
            {
                for (int i = 0; i < actors.Count; i++)
                {
                    int id = actors[i].PlayerId.Value;

                    // A player browsing a chest is busy; Pause belongs to whoever is playing.
                    if (_driver.TryGetOpenScreen(id, out _))
                    {
                        continue;
                    }

                    if (_driver.CommandFor(id).WasPressed(CommandButtons.Pause))
                    {
                        Open(id);
                        break;
                    }
                }

                return;
            }

            PlayerCommand command = _driver.CommandFor(_owner);
            if (command.WasPressed(CommandButtons.Pause) || command.WasPressed(CommandButtons.Heavy))
            {
                Close();
                return;
            }

            StepCursor(command);
            if (command.WasPressed(CommandButtons.Light))
            {
                Toggle();
            }

            Repaint();
        }

        private void StepCursor(in PlayerCommand command)
        {
            Vector2 move = command.Move;
            bool fresh = Mathf.Abs(move.y) > 0.5f && Mathf.Abs(_lastMove.y) <= 0.5f;
            _lastMove = move;
            if (!fresh)
            {
                return;
            }

            CollectBags();
            const int rows = 3;
            _cursor = (_cursor - (move.y > 0f ? 1 : -1) + rows) % rows;
        }

        private void Toggle()
        {
            CollectBags();
            if (_bags.Count == 0)
            {
                return;
            }

            if (_cursor == 2)
            {
                GrantTestLoot();
                return;
            }

            bool autoSell = _cursor == 1;
            bool current = autoSell ? _bags[0].Inventory.AutoSell : _bags[0].Inventory.AutoEquip;
            for (int i = 0; i < _bags.Count; i++)
            {
                PlayerInventory bag = _bags[i];
                if (autoSell)
                {
                    bag.SetAutoSell(!current);
                }
                else
                {
                    bag.SetAutoEquip(!current);
                }
            }
        }

        /// <summary>
        /// Debug only, and it dies when real content arrives: hands every player one of each
        /// starter definition *twice* plus spending money. Combining (D44) needs two identical
        /// items at the same rank, which random drops almost never hand you — without this the
        /// gamble is untestable by hand, which is exactly how the reaction table ended up
        /// shipping unexercised in M5.
        /// </summary>
        private void GrantTestLoot()
        {
            int[] definitions = { 7, 8, 9, 12, 13, 1, 2, 3 };
            for (int b = 0; b < _bags.Count; b++)
            {
                for (int i = 0; i < definitions.Length; i++)
                {
                    // Twice each, at a fixed quality, so the pair really is combinable.
                    for (int copy = 0; copy < 2; copy++)
                    {
                        Core.Items.ItemInstance item =
                            _driver.RollDebugItem(definitions[i], DebugGrantQuality);
                        if (!item.IsEmpty)
                        {
                            _bags[b].Take(item);
                        }
                    }
                }

                _bags[b].GrantCoins(5000);
                _bags[b].Earn(2000f);
            }
        }

        /// <summary>Mid-ladder, so a feel judgement is never about an absurd item.</summary>
        private const float DebugGrantQuality = 2.2f;

        private void CollectBags()
        {
            _bags.Clear();
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                PlayerInventory bag = actors[i].GetComponent<PlayerInventory>();
                if (bag != null)
                {
                    _bags.Add(bag);
                }
            }
        }

        private void Open(int playerId)
        {
            _owner = playerId;
            _open = true;
            _cursor = 0;
            Build();
            _panel.SetActive(true);
            _driver.HoldMenuPause(true);
            Repaint();
        }

        private void Close()
        {
            if (!_open)
            {
                return;
            }

            _open = false;
            _owner = -1;
            if (_panel != null)
            {
                _panel.SetActive(false);
            }

            _driver.HoldMenuPause(false);
        }

        private void Build()
        {
            if (_panel != null)
            {
                return;
            }

            var canvasGo = new GameObject("Settings Canvas", typeof(Canvas), typeof(CanvasScaler));
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

            _canvas.sortingOrder = 200;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform panel = UiBuild.Place(
                UiBuild.Rect("Settings", _canvas.transform), 0.3f, 0.3f, 0.7f, 0.7f);
            UiBuild.Box("Back", panel, UiBuild.Panel);
            RectTransform pad = UiBuild.Place(UiBuild.Rect("Pad", panel), 0f, 0f, 1f, 1f, 18f);
            _body = UiBuild.Label("Text", pad, string.Empty, 15, UiBuild.Ink, TextAnchor.UpperLeft);
            _panel = panel.gameObject;
        }

        private void Repaint()
        {
            if (_body == null)
            {
                return;
            }

            CollectBags();
            _text.Clear();
            _text.Append("SETTINGS\n\n");

            bool autoEquip = _bags.Count > 0 && _bags[0].Inventory.AutoEquip;
            bool autoSell = _bags.Count > 0 && _bags[0].Inventory.AutoSell;
            AppendToggle(0, "Auto-equip upgrades", autoEquip);
            AppendToggle(1, "Auto-sell at the cap", autoSell);
            _text.Append('\n');

            int debugRow = 2;
            string debugLine = $"  {(debugRow == _cursor ? ">" : " ")} [ DEBUG ] grant test loot, coin and XP";
            _text.Append(debugRow == _cursor ? UiBuild.Tint(debugLine, UiBuild.Coin) : debugLine);

            _text.Append("\n\nStick: move   Light: toggle   Pause or Heavy: close");
            _body.text = _text.ToString();
        }

        private void AppendToggle(int row, string label, bool value)
        {
            string line = $"  {(row == _cursor ? ">" : " ")} [{(value ? "x" : " ")}] {label}";
            _text.Append(row == _cursor ? UiBuild.Tint(line, UiBuild.Focus) : line).Append('\n');
        }
    }
}
