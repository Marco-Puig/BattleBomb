using System;
using BattleBomb.Core.Combat;
using UnityEngine;

namespace BattleBomb.Gameplay.Data
{
    /// <summary>
    /// One authored chain position: the Light that continues the combo, and optionally a Heavy
    /// ender for this position (D19 — the launcher lives after two Lights).
    /// </summary>
    [Serializable]
    public sealed class ComboStepSpec
    {
        [SerializeField] private AttackSpec _light = new AttackSpec();
        [SerializeField] private bool _hasEnder;
        [SerializeField] private AttackSpec _ender = new AttackSpec();

        public static ComboStepSpec From(in ComboStep step)
        {
            ComboStepSpec spec = new ComboStepSpec
            {
                _light = AttackSpec.From(step.OnLight),
                _hasEnder = step.HasHeavy,
            };
            if (step.HasHeavy)
            {
                spec._ender = AttackSpec.From(step.OnHeavy);
            }

            return spec;
        }

        public ComboStep ToRuntime() => _hasEnder
            ? new ComboStep(_light.ToRuntime(), _ender.ToRuntime())
            : new ComboStep(_light.ToRuntime());
    }
}
