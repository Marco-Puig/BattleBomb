using BattleBomb.Gameplay.Net;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleBomb.UI.Combat
{
    /// <summary>
    /// One line at the top of the screen when the connection has something to say (HANDOFF-M8 Stage D): a guest
    /// dropping in waits for a checkpoint room here (Task 103). Placeholder IMGUI like the rest of the HUD — M9's HUD
    /// replaces its look, not what it says. Installs itself, so no scene carries it; with nothing to say it draws nothing.
    /// </summary>
    public sealed class NetBanner : MonoBehaviour
    {
        private GUIStyle _style;
        private NetGuest _guest;
        private float _nextLook;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("Net Banner");
            DontDestroyOnLoad(go);
            go.AddComponent<NetBanner>();
        }

        private void OnEnable() => SceneManager.activeSceneChanged += OnSceneChanged;

        private void OnDisable() => SceneManager.activeSceneChanged -= OnSceneChanged;

        private void OnSceneChanged(Scene from, Scene to)
        {
            _guest = null;
            _nextLook = 0f;
        }

        /// <summary>What the banner says now, or null. Public for the smoke suite.</summary>
        public string Line
        {
            get
            {
                // Looked for at most twice a second, never every frame (Plan 1's note about Find in OnGUI).
                if (_guest == null && Time.unscaledTime >= _nextLook)
                {
                    _guest = FindAnyObjectByType<NetGuest>();
                    _nextLook = Time.unscaledTime + 0.5f;
                }

                if (_guest != null && _guest.WaitingToAppear)
                {
                    return "Waiting for the host to reach a checkpoint room.";
                }

                return null;
            }
        }

        private void OnGUI()
        {
            string line = Line;
            if (line == null)
            {
                return;
            }

            int fontSize = Mathf.Max(14, Screen.height / 40);
            if (_style == null || _style.fontSize != fontSize)
            {
                _style = new GUIStyle(GUI.skin.box) { fontSize = fontSize, alignment = TextAnchor.MiddleCenter };
            }

            float width = Mathf.Min(Screen.width - 32f, fontSize * 30f);
            GUI.Box(new Rect((Screen.width - width) * 0.5f, 16f, width, fontSize * 2.2f), line, _style);
        }
    }
}
