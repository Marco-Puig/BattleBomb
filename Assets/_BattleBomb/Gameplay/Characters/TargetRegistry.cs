using System.Collections.Generic;

namespace BattleBomb.Gameplay.Characters
{
    /// <summary>
    /// The scene's hittable targets — dummies and enemies alike — ordered by body name so hit
    /// resolution walks them in an order independent of scene load or spawn order (D10).
    /// </summary>
    public sealed class TargetRegistry
    {
        private static readonly System.Comparison<ISimTarget> ByName =
            (a, b) => string.CompareOrdinal(a.Body.name, b.Body.name);

        private readonly List<ISimTarget> _targets = new List<ISimTarget>();

        public IReadOnlyList<ISimTarget> Ordered
        {
            get
            {
                _targets.Sort(ByName);
                return _targets;
            }
        }

        public void Register(ISimTarget target)
        {
            if (target == null || _targets.Contains(target))
            {
                return;
            }

            _targets.Add(target);
        }

        public void Unregister(ISimTarget target) => _targets.Remove(target);
    }
}
