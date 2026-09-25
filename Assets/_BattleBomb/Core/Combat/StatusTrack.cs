using System.Collections.Generic;
using UnityEngine;

namespace BattleBomb.Core.Combat
{
    /// <summary>One element's mark on one combatant, already priced when it landed.</summary>
    public readonly struct StatusInstance
    {
        public readonly ElementId Element;
        public readonly int RemainingSteps;
        public readonly int StepsToTick;
        public readonly int TickSteps;
        public readonly float DamagePerTick;

        /// <summary>Movement multiplier while this mark lasts (D46); 1 for a pure damage mark.</summary>
        public readonly float MoveScale;

        public StatusInstance(
            ElementId element, int remainingSteps, int stepsToTick, int tickSteps,
            float damagePerTick, float moveScale = 1f)
        {
            Element = element;
            RemainingSteps = remainingSteps;
            StepsToTick = stepsToTick;
            TickSteps = tickSteps;
            DamagePerTick = damagePerTick;
            MoveScale = Mathf.Clamp01(moveScale);
        }

        public bool IsExpired => RemainingSteps <= 0;

        /// <summary>0–1 through the mark's life, for the tint and the bar to read.</summary>
        public float Remaining01(int fullDuration) =>
            fullDuration <= 0 ? 0f : Mathf.Clamp01(RemainingSteps / (float)fullDuration);
    }

    /// <summary>
    /// Every element currently marking one combatant (D40) — players and enemies alike, because
    /// statuses run both ways. One mark per element: reapplying refreshes rather than stacking, so
    /// a crowd of burning enemies can never multiply into a hidden damage spike.
    /// </summary>
    /// <remarks>
    /// Damage over time is reported out and applied by the caller, never subtracted here: the
    /// track owns the timing, the combatant owns its health. Ticks deliberately deal damage
    /// without staggering — a burn that interrupted the player's rhythm would be a punishment the
    /// combat model never agreed to (§2.1's responsiveness).
    /// </remarks>
    public sealed class StatusTrack
    {
        private readonly List<StatusInstance> _active = new List<StatusInstance>();

        public IReadOnlyList<StatusInstance> Active => _active;

        public int Count => _active.Count;

        public bool IsEmpty => _active.Count == 0;

        /// <summary>
        /// The strongest active slow (D46) — 1 when nothing slows. Concurrent slows never stack
        /// multiplicatively: the worst one wins, so two chills can never freeze anyone solid.
        /// </summary>
        public float MoveScale
        {
            get
            {
                float scale = 1f;
                for (int i = 0; i < _active.Count; i++)
                {
                    if (_active[i].MoveScale < scale)
                    {
                        scale = _active[i].MoveScale;
                    }
                }

                return scale;
            }
        }

        public bool Has(ElementId element)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].Element == element)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGet(ElementId element, out StatusInstance status)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].Element == element)
                {
                    status = _active[i];
                    return true;
                }
            }

            status = default;
            return false;
        }

        /// <summary>Every mark, in order — the first is the one presentation tints with.</summary>
        public StatusInstance[] ToArray() => _active.ToArray();

        /// <summary>Replaces every mark with another machine's (M8's replica), order kept.</summary>
        public void Restore(IReadOnlyList<StatusInstance> statuses)
        {
            _active.Clear();
            if (statuses == null)
            {
                return;
            }

            for (int i = 0; i < statuses.Count; i++)
            {
                _active.Add(statuses[i]);
            }
        }

        /// <summary>
        /// Marks the combatant, or renews an existing mark. Renewal never makes a mark worse: the
        /// longer remaining time and the stronger tick both survive, so a weak weapon infusion
        /// keeps a strong cast's burn alive instead of overwriting it — the self-combo D19 wants.
        /// </summary>
        public bool Apply(ElementId element, int durationSteps, int tickSteps, float damagePerTick,
            float moveScale = 1f)
        {
            if (element.IsNone || durationSteps <= 0)
            {
                return false;
            }

            tickSteps = Mathf.Max(1, tickSteps);
            moveScale = Mathf.Clamp01(moveScale);
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].Element != element)
                {
                    continue;
                }

                StatusInstance existing = _active[i];
                _active[i] = new StatusInstance(
                    element,
                    Mathf.Max(existing.RemainingSteps, durationSteps),
                    Mathf.Min(existing.StepsToTick, tickSteps),
                    tickSteps,
                    Mathf.Max(existing.DamagePerTick, damagePerTick),
                    Mathf.Min(existing.MoveScale, moveScale));
                return true;
            }

            _active.Add(new StatusInstance(
                element, durationSteps, tickSteps, tickSteps, damagePerTick, moveScale));
            return true;
        }

        /// <summary>
        /// One fixed step of every mark, returning the damage they owe together. Expired marks
        /// leave; a mark's final tick lands on the step it runs out, never after it.
        /// </summary>
        public float Step()
        {
            float damage = 0f;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                StatusInstance status = _active[i];
                int remaining = status.RemainingSteps - 1;
                int toTick = status.StepsToTick - 1;

                if (toTick <= 0)
                {
                    damage += status.DamagePerTick;
                    toTick = status.TickSteps;
                }

                if (remaining <= 0)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                _active[i] = new StatusInstance(
                    status.Element, remaining, toTick, status.TickSteps, status.DamagePerTick,
                    status.MoveScale);
            }

            return damage;
        }

        /// <summary>A reaction spending the mark it consumed (D41).</summary>
        public bool Remove(ElementId element)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].Element == element)
                {
                    _active.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>Death, a downed player, and the attempt reset all start clean.</summary>
        public void Clear() => _active.Clear();
    }
}
