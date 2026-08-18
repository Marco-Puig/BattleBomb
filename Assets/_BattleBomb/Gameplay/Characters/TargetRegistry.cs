using System.Collections.Generic;

namespace BattleBomb.Gameplay.Characters
{
    /// <summary>
    /// The scene's hittable dummies, ordered by name so hit resolution walks them in an order
    /// independent of scene load order (D10). M3's enemies will register here too.
    /// </summary>
    public sealed class TargetRegistry
    {
        private static readonly System.Comparison<TrainingDummy> ByName =
            (a, b) => string.CompareOrdinal(a.name, b.name);

        private readonly List<TrainingDummy> _dummies = new List<TrainingDummy>();

        public IReadOnlyList<TrainingDummy> Ordered
        {
            get
            {
                _dummies.Sort(ByName);
                return _dummies;
            }
        }

        public void Register(TrainingDummy dummy)
        {
            if (dummy == null || _dummies.Contains(dummy))
            {
                return;
            }

            _dummies.Add(dummy);
        }

        public void Unregister(TrainingDummy dummy) => _dummies.Remove(dummy);
    }
}
