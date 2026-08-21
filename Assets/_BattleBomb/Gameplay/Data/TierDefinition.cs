using BattleBomb.Core.Chapters;
using UnityEngine;

namespace BattleBomb.Gameplay.Data
{
    /// <summary>One difficulty row (D50), authored. The player sees the name only.</summary>
    [CreateAssetMenu(menuName = "BattleBomb/Tier Definition", fileName = "Tier")]
    public sealed class TierDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Normal";
        [SerializeField] private float _healthMultiplier = 1f;
        [SerializeField] private float _damageMultiplier = 1f;
        [SerializeField] private int _levelBump;
        [SerializeField] private float _lootMultiplier = 1f;

        public string DisplayName => _displayName;

        public TierSpec ToRuntime() =>
            new TierSpec(_displayName, _healthMultiplier, _damageMultiplier, _levelBump, _lootMultiplier);

        public static TierSpec[] ToRuntime(TierDefinition[] definitions)
        {
            if (definitions == null || definitions.Length == 0)
            {
                return TierSpec.Defaults;
            }

            var specs = new TierSpec[definitions.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                specs[i] = definitions[i] != null ? definitions[i].ToRuntime() : TierSpec.Defaults[0];
            }

            return specs;
        }
    }
}
