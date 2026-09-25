using System;
using System.Collections.Generic;
using System.Text;
using BattleBomb.Core.Items;
using BattleBomb.Core.Saves;
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// An item on the wire, as the save already writes it (D52: identity id plus rolled values, never
    /// by name) — the save's own DTO through <c>JsonUtility</c>, so M8 adds no second item format to
    /// keep in step. Drops are rare, so the size does not matter; Plan 2's inventory state may want a
    /// binary form if a full sack turns out heavy.
    /// </summary>
    public static class ItemWire
    {
        public static void Write(NetWriter w, in ItemInstance item)
        {
            string json = item.IsEmpty ? string.Empty : JsonUtility.ToJson(SaveMapper.ToSave(item));
            if (Encoding.UTF8.GetByteCount(json) > NetProtocol.MaxItemJsonBytes)
            {
                throw new NetFormatException($"An item of {json.Length} characters is too large to send.");
            }

            w.WriteString(json);
        }

        public static ItemInstance Read(NetReader r, IReadOnlyList<ItemSpec> catalog)
        {
            string json = r.ReadString(NetProtocol.MaxItemJsonBytes);
            if (json.Length == 0)
            {
                return default;
            }

            ItemSave save;
            try
            {
                save = JsonUtility.FromJson<ItemSave>(json);
            }
            catch (ArgumentException e)
            {
                throw new NetFormatException($"An item that is not an item: {e.Message}");
            }

            if (save == null)
            {
                throw new NetFormatException("An item that is not an item.");
            }

            return SaveMapper.ToInstance(save, catalog);
        }

        /// <summary>A stand-in that carries only a quality — all an elite's armor tint reads on the
        /// guest (<c>EnemyVisual</c>). Not empty, and never grabbed: the real piece drops from the host.</summary>
        public static ItemInstance StandIn(QualityRank quality) =>
            new ItemInstance(default, quality, default, Array.Empty<AffixRoll>(), 1);
    }
}
