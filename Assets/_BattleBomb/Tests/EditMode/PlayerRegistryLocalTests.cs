using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Players;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>"Local players on this display", never "characters in the world" (HANDOFF-M8 planning decision
    /// 15): a device, a script or a replay is local; a player whose hands are on another machine is not.</summary>
    public sealed class PlayerRegistryLocalTests
    {
        private sealed class Pad : IPlayerCommandSource
        {
            public Pad(int id) => PlayerId = new PlayerId(id);

            public PlayerId PlayerId { get; }

            public PlayerCommand Sample(int frame) => PlayerCommand.Idle(frame);
        }

        private sealed class Wire : IPlayerCommandSource, IRemotePlayerSource
        {
            public Wire(int id) => PlayerId = new PlayerId(id);

            public PlayerId PlayerId { get; }

            public PlayerCommand Sample(int frame) => PlayerCommand.Idle(frame);
        }

        [Test]
        public void A_remote_player_is_in_the_world_but_not_on_this_display()
        {
            var registry = new PlayerRegistry();
            registry.Register(new Pad(0));
            registry.Register(new Wire(1));

            Assert.That(registry.Count, Is.EqualTo(2));
            Assert.That(registry.LocalCount, Is.EqualTo(1));
            Assert.That(registry.IsLocal(new PlayerId(0)), Is.True);
            Assert.That(registry.IsLocal(new PlayerId(1)), Is.False);
            Assert.That(registry.IsLocal(new PlayerId(5)), Is.False, "Nobody is not a local player.");
        }

        [Test]
        public void The_couch_is_two_local_players()
        {
            var registry = new PlayerRegistry();
            registry.Register(new Pad(0));
            registry.Register(new Pad(1));

            Assert.That(registry.LocalCount, Is.EqualTo(2));
        }
    }
}
