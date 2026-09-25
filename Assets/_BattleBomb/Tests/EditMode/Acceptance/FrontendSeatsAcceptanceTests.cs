using System.Linq;
using BattleBomb.Gameplay.Players;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BattleBomb.Tests.EditMode.Acceptance
{
    /// <summary>The front door's two command sources: one per seat, both reading the project's
    /// controls, neither carrying a PlayerInput (D57).</summary>
    public sealed class FrontendSeatsAcceptanceTests
    {
        private const string FrontendScenePath = "Assets/_BattleBomb/Scenes/Frontend.unity";

        private Scene _scene;
        private bool _openedByTest;

        [OneTimeSetUp]
        public void OpenScene() =>
            _scene = AcceptanceFixture.OpenForInspection(FrontendScenePath, out _openedByTest);

        [OneTimeTearDown]
        public void CloseScene() => AcceptanceFixture.CloseAfterInspection(_scene, _openedByTest);

        [Test]
        public void The_front_door_has_one_source_per_seat()
        {
            InputSystemCommandSource[] sources = AcceptanceFixture.FindAll<InputSystemCommandSource>(_scene).ToArray();

            Assert.That(sources.Select(s => s.PlayerId.Value).OrderBy(v => v).ToArray(), Is.EqualTo(new[] { 0, 1 }));
            foreach (InputSystemCommandSource source in sources)
            {
                Assert.That(source.GetComponent<PlayerInput>(), Is.Null, $"'{source.name}' still has a PlayerInput.");
                Assert.That(new SerializedObject(source).FindProperty("_controls").objectReferenceValue,
                    Is.Not.Null, $"'{source.name}' has no controls asset.");
            }
        }
    }
}
