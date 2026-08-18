using System;
using BattleBomb.Core.Combat;
using UnityEngine;

namespace BattleBomb.Gameplay.Data
{
    /// <summary>
    /// One attack's authored frame data (§5 pattern): serialized fields in, a plain
    /// <see cref="AttackTuning"/> out. Defaults mirror the default kit's first Light.
    /// </summary>
    [Serializable]
    public sealed class AttackSpec
    {
        [SerializeField] private int _startupSteps = 5;
        [SerializeField] private int _activeSteps = 4;
        [SerializeField] private int _recoverySteps = 8;
        [SerializeField] private float _damage = 5f;
        [SerializeField] private float _reachX = 1.6f;
        [SerializeField] private float _depthTolerance = 1f;
        [SerializeField] private float _lungeDistance = 1.2f;
        [SerializeField] private int _maxTargets = 2;
        [SerializeField] private float _knockbackSpeed = 5f;
        [SerializeField] private float _launchSpeed;
        [SerializeField] private int _hitstopSteps = 2;
        [SerializeField] private float _moveSpeedScale = 1f;

        public static AttackSpec From(in AttackTuning tuning)
        {
            AttackSpec spec = new AttackSpec
            {
                _startupSteps = tuning.StartupSteps,
                _activeSteps = tuning.ActiveSteps,
                _recoverySteps = tuning.RecoverySteps,
                _damage = tuning.Damage,
                _reachX = tuning.ReachX,
                _depthTolerance = tuning.DepthTolerance,
                _lungeDistance = tuning.LungeDistance,
                _maxTargets = tuning.MaxTargets,
                _knockbackSpeed = tuning.KnockbackSpeed,
                _launchSpeed = tuning.LaunchSpeed,
                _hitstopSteps = tuning.HitstopSteps,
                _moveSpeedScale = tuning.MoveSpeedScale,
            };
            return spec;
        }

        public AttackTuning ToRuntime() => new AttackTuning(
            _startupSteps,
            _activeSteps,
            _recoverySteps,
            _damage,
            _reachX,
            _depthTolerance,
            _lungeDistance,
            _maxTargets,
            _knockbackSpeed,
            _launchSpeed,
            _hitstopSteps,
            _moveSpeedScale);
    }
}
