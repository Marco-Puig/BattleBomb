using System.Linq;
using BattleBomb.UI.Frontend;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace BattleBomb.Tests.EditMode.Acceptance
{
    /// <summary>The front door comes up before any chest host, so it loads the project's fonts
    /// itself (UI Pass 01); without them its text and badge row fall back to the built-in font.</summary>
    public sealed class FrontendFontsAcceptanceTests
    {
        private const string FrontendScenePath = "Assets/_BattleBomb/Scenes/Frontend.unity";
        private const string Fonts = "Assets/_BattleBomb/Art/UI/Fonts/";

        private Scene _scene;
        private bool _openedByTest;

        [OneTimeSetUp]
        public void OpenScene() =>
            _scene = AcceptanceFixture.OpenForInspection(FrontendScenePath, out _openedByTest);

        [OneTimeTearDown]
        public void CloseScene() => AcceptanceFixture.CloseAfterInspection(_scene, _openedByTest);

        [Test]
        public void The_front_door_loads_the_three_project_fonts()
        {
            FrontendFlow flow = AcceptanceFixture.FindAll<FrontendFlow>(_scene).Single();
            var fields = new SerializedObject(flow);

            Assert.That(PathOf(fields, "_displayFont"), Is.EqualTo(Fonts + "PassionOne-Bold.ttf"));
            Assert.That(PathOf(fields, "_uiFont"), Is.EqualTo(Fonts + "Archivo-Variable.ttf"));
            Assert.That(PathOf(fields, "_monoFont"), Is.EqualTo(Fonts + "SpaceMono-Regular.ttf"));
        }

        private static string PathOf(SerializedObject fields, string name)
        {
            SerializedProperty property = fields.FindProperty(name);
            Assert.That(property, Is.Not.Null, $"FrontendFlow has no '{name}' field.");
            return AssetDatabase.GetAssetPath(property.objectReferenceValue);
        }
    }
}
