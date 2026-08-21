using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleBomb.Tests.EditMode.Acceptance
{
    /// <summary>
    /// Shared plumbing for the acceptance fixtures.
    /// </summary>
    /// <remarks>
    /// These fixtures are the written-down definition of "done" for work that has no other automated
    /// gate — scenes, prefabs and authored assets. They are written before the work and are expected
    /// to fail until it lands. **Do not edit them to make them pass.** If one is wrong, say so and
    /// leave it red.
    /// </remarks>
    internal static class AcceptanceFixture
    {
        public const string GameplayScenePath = "Assets/_BattleBomb/Scenes/Gameplay.unity";
        public const string PlayerPrefabPath = "Assets/_BattleBomb/Prefabs/Player.prefab";
        public const string ControlsAssetPath = "Assets/_BattleBomb/Input/BattleBombControls.inputactions";
        public const string TemplateControlsPath = "Assets/InputSystem_Actions.inputactions";

        /// <summary>GUID of the URP template's input actions asset, which must no longer be referenced.</summary>
        public const string TemplateControlsGuid = "052faaac586de48259a63d0c4782560b";

        /// <summary>The URP template's starter scene. Deleted in M7 (task 80), when the front door
        /// took index 0 in build settings and the game stopped booting into a template.</summary>
        public const string TemplateScenePath = "Assets/Scenes/SampleScene.unity";

        /// <summary>GUID of that scene, which must no longer be referenced anywhere.</summary>
        public const string TemplateSceneGuid = "99c9720ab356a0642a771bea13969a05";

        /// <summary>
        /// Opens a scene for inspection, reusing it if the editor already has it open so a test run
        /// never closes something the user was working in.
        /// </summary>
        public static Scene OpenForInspection(string path, out bool openedByTest)
        {
            Scene existing = SceneManager.GetSceneByPath(path);
            if (existing.IsValid() && existing.isLoaded)
            {
                openedByTest = false;
                return existing;
            }

            openedByTest = true;
            return EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        public static void CloseAfterInspection(Scene scene, bool openedByTest)
        {
            if (openedByTest && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }

        /// <summary>Every component of type T in the scene, including ones on inactive objects.</summary>
        public static List<T> FindAll<T>(Scene scene) where T : Component
        {
            List<T> found = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                found.AddRange(root.GetComponentsInChildren<T>(includeInactive: true));
            }

            return found;
        }

        /// <summary>Every component in the scene whose type has the given full name.</summary>
        public static List<Component> FindAllByTypeName(Scene scene, string fullTypeName)
        {
            return FindAll<Component>(scene)
                .Where(c => c != null && c.GetType().FullName == fullTypeName)
                .ToList();
        }

        public static string[] RootNames(Scene scene) =>
            scene.GetRootGameObjects().Select(o => o.name).ToArray();
    }
}
