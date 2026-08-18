using BattleBomb.Gameplay.Characters;
using UnityEngine;

namespace BattleBomb.Gameplay.Simulation
{
    /// <summary>
    /// One landed hit, announced for Presentation and UI to observe — flashes, hitstop framing,
    /// and damage numbers (D20). Partner shoves carry zero damage and show no number (D21).
    /// </summary>
    public readonly struct HitEvent
    {
        public readonly CharacterActor Attacker;
        public readonly Component Target;
        public readonly float Damage;
        public readonly Vector3 Position;
        public readonly bool IsPartner;

        public HitEvent(CharacterActor attacker, Component target, float damage, Vector3 position, bool isPartner)
        {
            Attacker = attacker;
            Target = target;
            Damage = damage;
            Position = position;
            IsPartner = isPartner;
        }
    }
}
