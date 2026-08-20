using System.Collections.Generic;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// One authored element (D38): its id, the name the player reads, and the mark it leaves on
    /// what it hits.
    /// </summary>
    public readonly struct ElementSpec
    {
        public readonly ElementId Id;
        public readonly string Name;
        public readonly StatusSpec Status;

        public ElementSpec(ElementId id, string name, in StatusSpec status = default)
        {
            Id = id;
            Name = string.IsNullOrEmpty(name) ? id.ToString() : name;
            Status = status;
        }
    }

    /// <summary>
    /// Every element the game knows about right now (D38) — built from authored assets, handed to
    /// the systems that need to name an element or pick one at random. The roster is content: this
    /// class never enumerates a fixed set, so adding an element is adding an asset.
    /// </summary>
    public sealed class ElementCatalog
    {
        private readonly List<ElementSpec> _specs = new List<ElementSpec>();
        private readonly List<ElementId> _ids = new List<ElementId>();
        private readonly Dictionary<int, int> _indexById = new Dictionary<int, int>();

        /// <summary>The no-elements catalog: nothing rolls elemental, everything reads as None.</summary>
        public static ElementCatalog Empty { get; } = new ElementCatalog(null);

        /// <summary>Duplicate ids are dropped — the first authored one wins, deterministically.</summary>
        public ElementCatalog(IReadOnlyList<ElementSpec> specs)
        {
            if (specs == null)
            {
                return;
            }

            for (int i = 0; i < specs.Count; i++)
            {
                ElementSpec spec = specs[i];
                if (spec.Id.IsNone || _indexById.ContainsKey(spec.Id.Value))
                {
                    continue;
                }

                _indexById.Add(spec.Id.Value, _specs.Count);
                _specs.Add(spec);
                _ids.Add(spec.Id);
            }
        }

        public IReadOnlyList<ElementSpec> Specs => _specs;

        /// <summary>Every authored id, in authoring order — the pool an element roll draws from.</summary>
        public IReadOnlyList<ElementId> Ids => _ids;

        public int Count => _specs.Count;

        public bool TryGet(ElementId id, out ElementSpec spec)
        {
            if (!id.IsNone && _indexById.TryGetValue(id.Value, out int index))
            {
                spec = _specs[index];
                return true;
            }

            spec = default;
            return false;
        }

        /// <summary>The player-facing name, or the id's debug text when nothing authored it.</summary>
        public string NameOf(ElementId id) => TryGet(id, out ElementSpec spec) ? spec.Name : id.ToString();
    }
}
