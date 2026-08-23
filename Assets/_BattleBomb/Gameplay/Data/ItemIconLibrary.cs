using System.Collections.Generic;
using UnityEngine;

namespace BattleBomb.Gameplay.Data
{
    /// <summary>
    /// Definition id → icon, built from the same authored catalog the driver turns into
    /// <c>ItemSpec</c>s. This exists so presentation can put a face on an item it only knows by
    /// id: an <c>ItemInstance</c> carries its <c>DefinitionId</c> everywhere, including out of a
    /// save, but it can never carry a Sprite (rule 1) — so the id is the join.
    ///
    /// A missing icon is a normal state, not an error. Most items have no art yet, and the sack
    /// draws a deliberately unfinished placeholder plate for those rather than an empty cell.
    /// </summary>
    public sealed class ItemIconLibrary
    {
        private readonly Dictionary<int, Sprite> _icons = new Dictionary<int, Sprite>();

        public int Count => _icons.Count;

        /// <summary>Rebuilds from the authored catalog. Definitions without an icon are skipped.</summary>
        public void Rebuild(IReadOnlyList<ItemDefinition> definitions)
        {
            _icons.Clear();
            if (definitions == null)
            {
                return;
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                ItemDefinition definition = definitions[i];
                if (definition == null || definition.Icon == null)
                {
                    continue;
                }

                // First one wins, matching how the element catalog resolves a duplicate id.
                if (!_icons.ContainsKey(definition.Id))
                {
                    _icons.Add(definition.Id, definition.Icon);
                }
            }
        }

        /// <summary>The item's icon, or null when it has none yet.</summary>
        public Sprite For(int definitionId) =>
            _icons.TryGetValue(definitionId, out Sprite icon) ? icon : null;
    }
}
