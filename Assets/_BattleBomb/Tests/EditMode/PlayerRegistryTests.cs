using System.Collections.Generic;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Players;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class PlayerRegistryTests
    {
        /// <summary>A command source with no device behind it — proof the simulation cannot tell (§4).</summary>
        private sealed class StubSource : IPlayerCommandSource
        {
            private readonly Vector2 _move;

            public StubSource(PlayerId playerId, Vector2 move)
            {
                PlayerId = playerId;
                _move = move;
            }

            public PlayerId PlayerId { get; }

            public PlayerCommand Sample(int frame) =>
                PlayerCommand.FromState(frame, _move, CommandButtons.None, CommandButtons.None);
        }

        [Test]
        public void Samples_every_registered_player_for_one_frame()
        {
            PlayerRegistry registry = new PlayerRegistry();
            registry.Register(new StubSource(PlayerId.One, Vector2.right));
            registry.Register(new StubSource(PlayerId.Two, Vector2.left));

            Dictionary<int, PlayerCommand> commands = new Dictionary<int, PlayerCommand>();
            registry.SampleAll(frame: 5, into: commands);

            Assert.That(commands.Count, Is.EqualTo(2));
            Assert.That(commands[PlayerId.One.Value].Move, Is.EqualTo(Vector2.right));
            Assert.That(commands[PlayerId.Two.Value].Move, Is.EqualTo(Vector2.left));
            Assert.That(commands[PlayerId.Two.Value].Frame, Is.EqualTo(5));
        }

        [Test]
        public void Registering_the_same_player_twice_replaces_the_source()
        {
            PlayerRegistry registry = new PlayerRegistry();
            registry.Register(new StubSource(PlayerId.One, Vector2.right));
            registry.Register(new StubSource(PlayerId.One, Vector2.up));

            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.TryGet(PlayerId.One, out IPlayerCommandSource source), Is.True);
            Assert.That(source.Sample(0).Move, Is.EqualTo(Vector2.up));
        }

        [Test]
        public void Unregistering_removes_the_player()
        {
            PlayerRegistry registry = new PlayerRegistry();
            registry.Register(new StubSource(PlayerId.Two, Vector2.one));

            Assert.That(registry.Unregister(PlayerId.Two), Is.True);
            Assert.That(registry.IsRegistered(PlayerId.Two), Is.False);
            Assert.That(registry.Unregister(PlayerId.Two), Is.False);
        }

        [Test]
        public void SampleAll_clears_stale_commands_from_a_previous_frame()
        {
            PlayerRegistry registry = new PlayerRegistry();
            registry.Register(new StubSource(PlayerId.One, Vector2.right));

            Dictionary<int, PlayerCommand> commands = new Dictionary<int, PlayerCommand>
            {
                [PlayerId.Two.Value] = PlayerCommand.Idle(0)
            };

            registry.SampleAll(frame: 1, into: commands);

            Assert.That(commands.ContainsKey(PlayerId.Two.Value), Is.False,
                "A dropped player must not keep contributing last frame's command.");
        }
    }
}
