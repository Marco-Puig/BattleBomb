#if DEVELOPMENT_BUILD || UNITY_EDITOR
using BattleBomb.Gameplay.Net;
using BattleBomb.Gameplay.Session;
using UnityEngine;

namespace BattleBomb.UI.Debug
{
    /// <summary>
    /// Development only: host or join over the local socket, with a made-up connection (HANDOFF-M8
    /// Task 90). Two editors under Multiplayer Play Mode: one clicks Host local at the title, the
    /// other Join local. Not a menu — the real front door is Plan 2's lobby and Plan 3's Steam.
    /// Installs itself, so no scene carries it and a release build has none.
    /// </summary>
    public sealed class NetDevOverlay : MonoBehaviour
    {
        private int _lag;
        private bool _folded;
        private GUIStyle _style;
        private GameSession _session;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("Net Dev Overlay");
            DontDestroyOnLoad(go);
            go.AddComponent<NetDevOverlay>();
        }

        private void OnGUI()
        {
            int fontSize = Mathf.Max(12, Screen.height / 60);
            if (_style == null || _style.fontSize != fontSize)
            {
                _style = new GUIStyle(GUI.skin.button) { fontSize = fontSize };
            }

            float width = fontSize * 22f;
            float row = fontSize + 10f;
            var area = new Rect(Screen.width - width - 8f, Screen.height - row * 5f - 8f, width, row * 5f);
            GUILayout.BeginArea(area, GUI.skin.box);

            if (_session == null)
            {
                _session = FindAnyObjectByType<GameSession>();
            }

            GameSession session = _session;
            NetSession net = session != null ? session.Net : null;
            NetRole role = net != null ? net.Role : NetRole.Offline;

            if (GUILayout.Button(_folded ? "Net ▲" : "Net ▼", _style))
            {
                _folded = !_folded;
            }

            if (!_folded)
            {
                GUILayout.Label(net != null ? net.Status : "Offline", _style);
                if (role == NetRole.Offline)
                {
                    if (GUILayout.Button($"Lag: {NetSession.LocalLagNames[_lag]}", _style))
                    {
                        _lag = (_lag + 1) % NetSession.LocalLagNames.Count;
                    }

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Host local", _style))
                    {
                        NetSession.FindOrCreate().HostLocal(_lag);
                    }

                    if (GUILayout.Button("Join local", _style))
                    {
                        NetSession.FindOrCreate().JoinLocal(_lag);
                    }

                    GUILayout.EndHorizontal();
                }
                else
                {
                    if (net.HasProblem)
                    {
                        GUILayout.Label("Connection problem…", _style);
                    }

                    if (GUILayout.Button("Leave", _style))
                    {
                        net.Leave();
                    }
                }
            }

            GUILayout.EndArea();
        }
    }
}
#endif
