using System.Collections.Generic;
using BattleBomb.Core.Combat;
using UnityEngine;

namespace BattleBomb.Gameplay.Data
{
    /// <summary>
    /// A character's authored combo table (D19): a data asset in, a plain <see cref="CombatKit"/>
    /// out. A new combo is a new chain entry here — never new code. Fresh assets mirror
    /// <see cref="CombatKit.Default"/> so authoring defaults and code defaults cannot drift.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleBomb/Combat Kit", fileName = "CombatKit")]
    public sealed class CombatKitDefinition : ScriptableObject
    {
        [Tooltip("The Light chain in order. Each position may carry a Heavy ender (L-L-H lives at position three).")]
        [SerializeField] private List<ComboStepSpec> _chain = DefaultChain();

        [Header("Heavy")]
        [SerializeField] private AttackSpec _heavy = AttackSpec.From(CombatKit.Default.Heavy);
        [SerializeField] private AttackSpec _chargedHeavy = AttackSpec.From(CombatKit.Default.ChargedHeavy);

        [Tooltip("Steps Heavy must be held before release fires the charged variant.")]
        [SerializeField] private int _chargeThresholdSteps = CombatKit.Default.ChargeThresholdSteps;

        [Header("Windows")]
        [Tooltip("Steps after recovery in which a follow-up press still continues the chain.")]
        [SerializeField] private int _comboWindowSteps = CombatKit.Default.ComboWindowSteps;

        [Tooltip("Steps a press is remembered while an attack is still busy.")]
        [SerializeField] private int _inputBufferSteps = CombatKit.Default.InputBufferSteps;

        public CombatKit ToRuntime()
        {
            if (_chain == null || _chain.Count == 0)
            {
                Debug.LogWarning($"{name}: empty combo chain — falling back to the default kit.", this);
                return CombatKit.Default;
            }

            ComboStep[] steps = new ComboStep[_chain.Count];
            for (int i = 0; i < _chain.Count; i++)
            {
                steps[i] = _chain[i].ToRuntime();
            }

            return new CombatKit(
                steps,
                _heavy.ToRuntime(),
                _chargedHeavy.ToRuntime(),
                Mathf.Max(1, _chargeThresholdSteps),
                Mathf.Max(1, _comboWindowSteps),
                Mathf.Max(1, _inputBufferSteps));
        }

        private static List<ComboStepSpec> DefaultChain()
        {
            CombatKit kit = CombatKit.Default;
            List<ComboStepSpec> chain = new List<ComboStepSpec>(kit.ChainLength);
            for (int i = 0; i < kit.ChainLength; i++)
            {
                chain.Add(ComboStepSpec.From(kit.StepAt(i)));
            }

            return chain;
        }
    }
}
