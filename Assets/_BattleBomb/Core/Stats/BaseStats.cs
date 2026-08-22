using UnityEngine;

namespace BattleBomb.Core.Stats
{
    /// <summary>
    /// A player's allocated base-stat points (D32): Strength scales weapon damage, HP grows the
    /// health pool, Mana grows the mana pool, Speed buys velocity up to the hard cap and slow
    /// resistance past it. Immutable; allocation returns a new value.
    /// </summary>
    public readonly struct BaseStats
    {
        public readonly int Strength;
        public readonly int Hp;
        public readonly int Mana;
        public readonly int Speed;

        public BaseStats(int strength, int hp, int mana, int speed)
        {
            Strength = Mathf.Max(0, strength);
            Hp = Mathf.Max(0, hp);
            Mana = Mathf.Max(0, mana);
            Speed = Mathf.Max(0, speed);
        }

        public static BaseStats Zero => default;

        public int Total => Strength + Hp + Mana + Speed;

        public BaseStats Allocate(StatId stat, int points = 1)
        {
            switch (stat)
            {
                case StatId.Strength: return new BaseStats(Strength + points, Hp, Mana, Speed);
                case StatId.Hp: return new BaseStats(Strength, Hp + points, Mana, Speed);
                case StatId.Mana: return new BaseStats(Strength, Hp, Mana + points, Speed);
                case StatId.Speed: return new BaseStats(Strength, Hp, Mana, Speed + points);
                default: return this;
            }
        }
    }
}
