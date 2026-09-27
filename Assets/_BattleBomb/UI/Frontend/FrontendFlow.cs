using System.Collections.Generic;
using System.Text;
using BattleBomb.Core.Chapters;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.Net;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Session;
using BattleBomb.UI.Chest;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BattleBomb.UI.Frontend
{
    /// <summary>
    /// Title → Character select → Chapter select → launch (D51). Graybox text screens on the
    /// command stream, like every menu in the game. Builds or reuses the <see cref="GameSession"/>,
    /// loads the save, and hands the machine a launch request. A pointer gets the three big
    /// buttons — start, launch, back — so a tap can play (D5); the stick does the rest.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FrontendFlow : MonoBehaviour
    {
        private const string GameplayScene = "Gameplay";
        private const string CanvasName = "Frontend Canvas";

        [Tooltip("Every playable character (D46's roster).")]
        [SerializeField] private CharacterDefinition[] _roster = new CharacterDefinition[0];

        [Tooltip("Every chapter, in order. The fixture until the story lands.")]
        [SerializeField] private ChapterDefinition[] _chapters = new ChapterDefinition[0];

        [Tooltip("Difficulty rows (D50), in order.")]
        [SerializeField] private TierDefinition[] _tiers = new TierDefinition[0];

        [Tooltip("Samples the two devices. Leave empty to find the one in the scene.")]
        [SerializeField] private CommandSampler _input;

        [Tooltip("The same three fonts the chest host loads. The front door comes up first, so it " +
                 "loads them itself; without them it draws in the built-in font.")]
        [SerializeField] private Font _displayFont;

        [SerializeField] private Font _uiFont;

        [SerializeField] private Font _monoFont;

        private readonly StringBuilder _text = new StringBuilder();
        private readonly Vector2[] _lastMove = new Vector2[FrontendState.Slots];
        private readonly List<ChapterDefinition> _live = new List<ChapterDefinition>();
        private readonly List<Prompt> _prompts = new List<Prompt>();

        private GameSession _session;
        private FrontendState _state;
        private StageSelection _selection;
        private PromptRow _promptRow;
        private Text _body;
        private Button _primary;
        private Button _back;
        private bool _launched;

        /// <summary>This machine is a guest: its front door is a lobby (D59), null otherwise.</summary>
        private GuestLobby _lobby;
        private bool _sentAny;
        private int _sentPick;
        private bool _sentReady;

        public FrontendState State => _state;

        public StageSelection Selection => _selection;

        /// <summary>The lobby while this machine is a guest, or null — read by the smoke suite.</summary>
        public GuestLobby Lobby => _lobby;

        private void OnEnable()
        {
            if (_input == null)
            {
                _input = FindAnyObjectByType<CommandSampler>();
            }

            // Nulls dropped once, here, because ChapterDefinition.ToRuntime skips them: a hole in
            // the authored array would shift every runtime index past it, and the picker would
            // quietly launch the chapter next to the one on the screen.
            _live.Clear();
            for (int i = 0; i < _chapters.Length; i++)
            {
                if (_chapters[i] != null)
                {
                    _live.Add(_chapters[i]);
                }
            }

            _session = GameSession.FindOrCreate();
            _session.Chapters = _live.ToArray();
            _session.Tiers = _tiers;
            _session.Roster = _roster;

            LoadSave();
            _selection = new StageSelection(
                ChapterDefinition.ToRuntime(_live), TierDefinition.ToRuntime(_tiers), _session.Progress);
            _state = new FrontendState(_roster.Length, canContinue: _session.LoadedSave != null);
            _launched = false;

            if (_session.Chapter != null)
            {
                RejoinTheCouch();
            }

            UiBuild.UseFonts(_displayFont, _uiFont, _monoFont);
            Build();
            Repaint();
        }

        /// <summary>Back from the pause menu: the couch was chosen before the machine booted, so
        /// put it back as it was and land on chapter select. Every present slot joins before
        /// anyone is readied — readying the last present slot moves the screen on, and a join
        /// arriving after that would land on a screen that has no join.</summary>
        private void RejoinTheCouch()
        {
            _state.Confirm(0);

            for (int i = 1; i < FrontendState.Slots; i++)
            {
                if (_session.Characters[i] != null)
                {
                    _state.Confirm(i);
                }
            }

            for (int i = 0; i < FrontendState.Slots; i++)
            {
                if (_session.Characters[i] != null)
                {
                    RestorePick(i, IndexInRoster(_session.Characters[i]));
                }
            }

            for (int i = 0; i < FrontendState.Slots; i++)
            {
                if (_session.Characters[i] != null)
                {
                    _state.Confirm(i);
                }
            }
        }

        private int IndexInRoster(CharacterDefinition definition)
        {
            for (int i = 0; i < _roster.Length; i++)
            {
                if (_roster[i] == definition)
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>Walks a slot's cursor onto the character it already is. The state machine
        /// only exposes a relative move — the right shape for a player holding a stick, and one
        /// wrap of the roster here.</summary>
        private void RestorePick(int slot, int target)
        {
            for (int guard = 0; guard < _roster.Length && _state.PickOf(slot) != target; guard++)
            {
                _state.MovePick(slot, 1);
            }
        }

        /// <summary>Re-read every time the front door comes up, so returning mid-run picks up
        /// whatever the autosave last wrote. The refusal is recorded on the session, not only
        /// logged: a save this build could not read is a file that must not be written over, and
        /// the autosave (task 81) is what would.</summary>
        private void LoadSave()
        {
            _session.LoadFromStore();
            if (!_session.LoadedCleanly)
            {
                UnityEngine.Debug.LogWarning(
                    $"{name}: save '{_session.SaveName}' refused: {_session.LoadOutcome}. Starting fresh, " +
                    "and the file will be left alone rather than overwritten.", this);
            }
        }

        private void Update()
        {
            if (_input == null || _state == null || _launched || _session == null)
            {
                return;
            }

            NetSession net = _session.Net;
            if (net != null && net.Role == NetRole.Guest)
            {
                UpdateAsGuest(net);
                return;
            }

            _lobby = null;
            SyncRemote(net);

            for (int slot = 0; slot < FrontendState.Slots; slot++)
            {
                if (_state.IsRemote(slot))
                {
                    // The online guest's seat is driven from their own machine (D59).
                    continue;
                }

                PlayerCommand command = _input.CommandFor(slot);
                MenuPress press = MenuPress.From(command);
                Steer(slot, command);

                // Start starts the game from the title and does nothing else here — anywhere else
                // it would be a second A (D57). Escape backs out.
                if (press.Confirm || (slot == 0 && press.Pause && _state.Screen == FrontendScreen.Title))
                {
                    Confirm(slot);
                }
                else if (press.Back)
                {
                    _state.Back(slot);
                }
            }

            _session.Seats.Follow(
                _state.Screen,
                _state.IsJoined(1) && !_state.IsRemote(1),
                _input.Players.LastDeviceOf(new PlayerId(0)),
                _input.Players.LastDeviceOf(new PlayerId(1)));

            if (_state.Screen == FrontendScreen.Launching)
            {
                LaunchNow();
                return;
            }

            Repaint();
        }

        /// <summary>
        /// The host's side of the lobby (D59): a connected guest sits in Player 2's slot with their own pick; a couch
        /// Player 2 makes the game full; and the guest is told where this front door is.
        /// </summary>
        private void SyncRemote(NetSession net)
        {
            bool guest = net != null && net.Role == NetRole.Host && net.IsConnected;
            _state.SetRemote(1, guest, guest ? Mathf.Max(0, net.GuestPick) : 0, guest && net.GuestReady);
            if (net == null || net.Role != NetRole.Host)
            {
                return;
            }

            net.SetFull(!guest && _state.IsJoined(1));
            net.PublishLobby(new LobbyState(_state.Screen, _state.PickOf(0), _state.IsReady(0), false));
        }

        /// <summary>
        /// The front door while this machine is a guest (D59): pick a hero from this machine's own save, ready, and
        /// wait for the host, who chooses the chapter and launches both. Nothing of this machine's own can launch while
        /// connected. Back un-readies, then leaves the game.
        /// </summary>
        private void UpdateAsGuest(NetSession net)
        {
            if (_lobby == null)
            {
                int start = net.LobbyPick >= 0 ? net.LobbyPick : IndexInRoster(_session.Characters[0]);
                _lobby = new GuestLobby(_roster.Length, start, net.LobbyReady);
                _sentAny = false;
            }

            PlayerCommand command = _input.CommandFor(0);
            MenuPress press = MenuPress.From(command);
            Vector2 move = command.Move;
            bool freshX = Mathf.Abs(move.x) > 0.5f && Mathf.Abs(_lastMove[0].x) <= 0.5f;
            _lastMove[0] = move;
            if (freshX)
            {
                _lobby.MovePick(move.x > 0f ? 1 : -1);
            }

            if (press.Confirm)
            {
                _lobby.Confirm();
            }
            else if (press.Back)
            {
                _lobby.Back();
            }

            if (_lobby.LeaveRequested)
            {
                net.Leave();
                _lobby = null;
                Repaint();
                return;
            }

            net.LobbyPick = _lobby.Pick;
            net.LobbyReady = _lobby.Ready;
            if (net.IsConnected && (!_sentAny || _lobby.Pick != _sentPick || _lobby.Ready != _sentReady))
            {
                // Ready, it brings this machine's own save cut down to the hero (D61), re-read whenever this front
                // door comes up — so after a match it brings what the last autosave wrote.
                net.SendLobbyPick(_lobby.Pick, _lobby.Ready);
                _sentAny = true;
                _sentPick = _lobby.Pick;
                _sentReady = _lobby.Ready;
            }

            Repaint();
        }

        /// <summary>This machine is a guest: its front door is the lobby from the moment it joins (D59).</summary>
        private bool IsGuest() => _session.Net != null && _session.Net.Role == NetRole.Guest;

        private void Steer(int slot, in PlayerCommand command)
        {
            Vector2 move = command.Move;
            bool freshX = Mathf.Abs(move.x) > 0.5f && Mathf.Abs(_lastMove[slot].x) <= 0.5f;
            bool freshY = Mathf.Abs(move.y) > 0.5f && Mathf.Abs(_lastMove[slot].y) <= 0.5f;
            _lastMove[slot] = move;
            int dx = freshX ? (move.x > 0f ? 1 : -1) : 0;
            int dy = freshY ? (move.y > 0f ? 1 : -1) : 0;

            switch (_state.Screen)
            {
                case FrontendScreen.Title:
                    if (slot == 0 && dy != 0)
                    {
                        _state.MoveTitle(-dy);
                    }

                    break;

                case FrontendScreen.Characters:
                    if (dx != 0)
                    {
                        _state.MovePick(slot, dx);
                    }

                    break;

                case FrontendScreen.Chapters:
                    if (slot == 0 && dy != 0)
                    {
                        _selection.MoveChapter(-dy);
                    }
                    else if (slot == 0 && dx != 0)
                    {
                        _selection.MoveTier(dx);
                    }

                    break;
            }
        }

        private void Confirm(int slot)
        {
            if (_state.Screen == FrontendScreen.Chapters)
            {
                if (slot == 0)
                {
                    _state.Launch(_selection.CanLaunch);
                }

                return;
            }

            _state.Confirm(slot);
        }

        private void LaunchNow()
        {
            _launched = true;
            _session.Chapter = _live[_selection.ChapterIndex];
            _session.StageIndex = _selection.LaunchStageIndex;
            _session.ResumeCheckpointArena = _selection.LaunchCheckpointArena;
            _session.TierIndex = _selection.TierIndex;
            for (int i = 0; i < FrontendState.Slots; i++)
            {
                // An online guest's hero is theirs, and never enters this machine's session (the binder seats it).
                _session.Characters[i] = _state.IsJoined(i) && !_state.IsRemote(i) && _roster.Length > 0
                    ? _roster[Mathf.Clamp(_state.PickOf(i), 0, _roster.Length - 1)]
                    : null;
            }

            _session.DrawRunSeeds();
            SceneManager.LoadScene(GameplayScene, LoadSceneMode.Single);
        }

        // ── Drawing ──────────────────────────────────────────────────────────────────

        private void Build()
        {
            // A mid-play recompile nulls every reference below while the objects survive, so the
            // old canvas goes rather than being left for the new one to draw on top of.
            Transform stale = transform.Find(CanvasName);
            if (stale != null)
            {
                Destroy(stale.gameObject);
            }

            var canvasGo = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            Camera camera = Camera.main;
            if (camera != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform panel = UiBuild.Place(UiBuild.Rect("Panel", canvas.transform), 0.15f, 0.1f, 0.85f, 0.9f);
            UiBuild.Box("Back", panel, UiBuild.Panel);
            RectTransform pad = UiBuild.Place(UiBuild.Rect("Pad", panel), 0f, 0.12f, 1f, 1f, 24f);
            _body = UiBuild.Label("Text", pad, string.Empty, 18, UiBuild.Ink, TextAnchor.UpperLeft);
            _promptRow = new PromptRow(pad);
            _promptRow.Place(0f, 1f, 0f, 0f, 0f);

            _primary = MakeButton(panel, "Primary", 0.55f, 0.02f, 0.95f, 0.1f, () =>
            {
                if (IsGuest())
                {
                    _lobby?.Confirm();
                }
                else
                {
                    Confirm(0);
                }
            });
            _back = MakeButton(panel, "Back", 0.05f, 0.02f, 0.45f, 0.1f, () =>
            {
                if (IsGuest())
                {
                    _lobby?.Back();
                }
                else
                {
                    _state.Back(0);
                }
            });

            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var es = new GameObject("Frontend Event System", typeof(UnityEngine.EventSystems.EventSystem));
                es.transform.SetParent(transform, false);
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        private static Button MakeButton(
            RectTransform parent, string name, float x0, float y0, float x1, float y1,
            UnityEngine.Events.UnityAction onClick)
        {
            Image box = UiBuild.Box(name, parent, UiBuild.PanelInner);
            UiBuild.Place((RectTransform)box.transform, x0, y0, x1, y1);

            // UiBuild's boxes are decoration and take no raycasts. This one has to: it is the
            // whole of D5's "a tap can play" on a device with no stick.
            box.raycastTarget = true;
            var button = box.gameObject.AddComponent<Button>();
            button.targetGraphic = box;
            button.onClick.AddListener(onClick);
            UiBuild.PointerOnly(button);
            UiBuild.Label("Label", box.transform, name, 16, UiBuild.Ink, TextAnchor.MiddleCenter);
            return button;
        }

        private InputFamily FamilyOf(int slot) =>
            _input != null ? _input.Players.FamilyOf(new PlayerId(slot)) : InputFamily.Keyboard;

        private void Repaint()
        {
            if (_body == null)
            {
                return;
            }

            _text.Clear();
            if (_lobby != null)
            {
                RepaintLobby();
                return;
            }

            switch (_state.Screen)
            {
                case FrontendScreen.Title:
                    _text.Append("BATTLEBOMB\n\n");
                    int row = 0;
                    if (_state.TitleOptions == 2)
                    {
                        AppendRow(row++, "Continue", _state.TitleCursor);
                    }

                    AppendRow(row, "Start", _state.TitleCursor);
                    SetButtonLabel(_primary, "Choose");
                    SetButtonLabel(_back, string.Empty);
                    break;

                case FrontendScreen.Characters:
                    _text.Append("CHOOSE YOUR CHARACTER\n\n");
                    for (int slot = 0; slot < FrontendState.Slots; slot++)
                    {
                        _text.Append("Player ").Append(slot + 1).Append(_state.IsRemote(slot) ? " (online):  " : ":  ");
                        if (_state.IsRemote(slot))
                        {
                            string guestHero = _roster.Length > 0 && _roster[_state.PickOf(slot)] != null
                                ? _roster[_state.PickOf(slot)].DisplayName
                                : "?";
                            _text.Append(UiBuild.Tint(guestHero, UiBuild.Focus))
                                .Append(_state.IsReady(slot) ? "   READY" : "   choosing")
                                .Append('\n');
                            continue;
                        }

                        if (!_state.IsJoined(slot))
                        {
                            // Player 1 keeps the device they came in on (D57): with Player 1 on the
                            // keyboard, Enter is theirs, and only a pad can join.
                            string join = PromptRow.Inline(InputFamily.Gamepad, PromptKey.Confirm);
                            if (FamilyOf(0) != InputFamily.Keyboard)
                            {
                                join += " or " + PromptRow.Inline(InputFamily.Keyboard, PromptKey.Confirm);
                            }

                            _text.Append(UiBuild.Tint($"press {join} to join", UiBuild.InkDim));
                        }
                        else
                        {
                            string name = _roster.Length > 0 && _roster[_state.PickOf(slot)] != null
                                ? _roster[_state.PickOf(slot)].DisplayName
                                : "?";
                            _text.Append("<  ").Append(UiBuild.Tint(name, UiBuild.Focus)).Append("  >");
                            _text.Append(_state.IsReady(slot)
                                ? "   READY"
                                : $"   ({PromptRow.Inline(FamilyOf(slot), PromptKey.Confirm)}: ready)");
                        }

                        _text.Append('\n');
                    }

                    SetButtonLabel(_primary, "Ready");
                    SetButtonLabel(_back, "Back");
                    break;

                case FrontendScreen.Chapters:
                    _text.Append("CHAPTERS\n\n");
                    for (int i = 0; i < _selection.ChapterCount; i++)
                    {
                        bool open = _selection.IsUnlocked(i, 0);
                        string label = (_live[i] != null ? _live[i].DisplayName : "?")
                            + (open ? string.Empty : "   [locked]");
                        AppendRow(i, open ? label : UiBuild.Tint(label, UiBuild.InkDim), _selection.ChapterIndex);
                    }

                    _text.Append("\nTier:  ");
                    for (int t = 0; t < _selection.TierCount; t++)
                    {
                        bool open = _selection.IsUnlocked(_selection.ChapterIndex, t);
                        string name = t < _tiers.Length && _tiers[t] != null ? _tiers[t].DisplayName : "?";
                        string cell = t == _selection.TierIndex ? "[ " + name + " ]" : "  " + name + "  ";
                        _text.Append(open ? cell : UiBuild.Tint(cell, UiBuild.InkDim));
                    }

                    if (_selection.IsResuming)
                    {
                        _text.Append("\n\nContinue from stage ").Append(_selection.LaunchStageIndex + 1);
                    }

                    if (_state.IsRemote(1) && !_state.IsReady(1))
                    {
                        _text.Append("\n\n").Append(UiBuild.Tint("Waiting for Player 2 to be ready.", UiBuild.InkDim));
                    }

                    SetButtonLabel(_primary, _selection.CanLaunch ? "Launch" : "Locked");
                    SetButtonLabel(_back, "Back");
                    break;

                default:
                    _text.Append("Loading...");
                    break;
            }

            _body.text = _text.ToString();

            _prompts.Clear();
            switch (_state.Screen)
            {
                case FrontendScreen.Title:
                    _prompts.Add(new Prompt(PromptKey.Move, "Move"));
                    _prompts.Add(new Prompt(PromptKey.Confirm, "Choose"));
                    break;

                case FrontendScreen.Characters:
                    _prompts.Add(new Prompt(PromptKey.Move, "Pick"));
                    _prompts.Add(new Prompt(PromptKey.Confirm, "Ready"));
                    _prompts.Add(new Prompt(PromptKey.Back, "Back"));
                    break;

                case FrontendScreen.Chapters:
                    _prompts.Add(new Prompt(PromptKey.Move, "Chapter and tier"));
                    _prompts.Add(new Prompt(PromptKey.Confirm, _selection.CanLaunch ? "Launch" : "Locked"));
                    _prompts.Add(new Prompt(PromptKey.Back, "Back"));
                    break;
            }

            _promptRow.Show(_prompts, FamilyOf(0));
        }

        private void RepaintLobby()
        {
            NetSession net = _session.Net;
            _text.Append("ONLINE — PLAYER 2\n\n");
            if (net == null || !net.IsConnected)
            {
                _text.Append(net != null ? net.Status : "Offline");
            }
            else
            {
                string hero = _roster.Length > 0 && _roster[_lobby.Pick] != null ? _roster[_lobby.Pick].DisplayName : "?";
                _text.Append("Your hero:  <  ").Append(UiBuild.Tint(hero, UiBuild.Focus)).Append("  >")
                    .Append(_lobby.Ready ? "   READY" : $"   ({PromptRow.Inline(FamilyOf(0), PromptKey.Confirm)}: ready)")
                    .Append("\n\n").Append(HostLine(net.HostLobby));
            }

            _body.text = _text.ToString();
            SetButtonLabel(_primary, _lobby.Ready ? string.Empty : "Ready");
            SetButtonLabel(_back, _lobby.Ready ? "Not ready" : "Leave");

            _prompts.Clear();
            if (!_lobby.Ready)
            {
                _prompts.Add(new Prompt(PromptKey.Move, "Pick"));
                _prompts.Add(new Prompt(PromptKey.Confirm, "Ready"));
            }

            _prompts.Add(new Prompt(PromptKey.Back, _lobby.Ready ? "Not ready" : "Leave"));
            _promptRow.Show(_prompts, FamilyOf(0));
        }

        private string HostLine(in LobbyState host)
        {
            if (host.InMatch)
            {
                return "The host is playing. You will join at the next checkpoint room.";
            }

            switch (host.HostScreen)
            {
                case FrontendScreen.Chapters:
                    return "The host is choosing a chapter.";
                case FrontendScreen.Launching:
                    return "Starting…";
                default:
                    return "The host is choosing a hero.";
            }
        }

        private void AppendRow(int row, string label, int cursor)
        {
            string line = $"  {(row == cursor ? ">" : " ")} {label}";
            _text.Append(row == cursor ? UiBuild.Tint(line, UiBuild.Focus) : line).Append('\n');
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(label.Length > 0);
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
            }
        }
    }
}
