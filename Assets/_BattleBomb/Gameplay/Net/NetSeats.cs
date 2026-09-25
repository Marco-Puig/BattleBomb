using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Players;
using UnityEngine;

namespace BattleBomb.Gameplay.Net
{
    /// <summary>
    /// Who answers to what, online (HANDOFF-M8 planning decision 18). Called by the binder in
    /// <c>Awake</c>, before any player object's sources have enabled, so a disabled device never
    /// registers and a re-seated one builds its controls with the right seat.
    /// </summary>
    internal static class NetSeats
    {
        /// <summary>Host: the guest's body takes orders from the wire and from no local device.</summary>
        internal static RemoteCommandSource MakeRemote(CharacterActor actor, PlayerId id)
        {
            DisableDevices(actor);
            RemoteCommandSource remote = actor.GetComponent<RemoteCommandSource>();
            if (remote == null)
            {
                remote = actor.gameObject.AddComponent<RemoteCommandSource>();
            }

            remote.Bind(id);
            actor.BindSource(remote);
            return remote;
        }

        /// <summary>Guest: the host's body is drawn from snapshots and takes orders from nobody here.
        /// Its disabled device still answers the actor's <c>PlayerId</c> with its authored seat.</summary>
        internal static void MakeReplica(CharacterActor actor) => DisableDevices(actor);

        /// <summary>Guest: this machine's one player owns every local device and speaks as the guest.
        /// Relies on the binder having reset the seats first, so seat 0 is not held to one device.</summary>
        internal static void MakeLocalGuest(CharacterActor actor, PlayerId speakAs)
        {
            InputSystemCommandSource device = actor.GetComponent<InputSystemCommandSource>();
            if (device == null)
            {
                Debug.LogError($"{actor.name}: no InputSystemCommandSource — the guest cannot play.", actor);
                return;
            }

            device.UseSeat(0, speakAs.Value);
        }

        private static void DisableDevices(CharacterActor actor)
        {
            foreach (InputSystemCommandSource device in actor.GetComponents<InputSystemCommandSource>())
            {
                device.enabled = false;
            }
        }
    }
}
