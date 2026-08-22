namespace BattleBomb.Core.Items
{
    /// <summary>
    /// D33's display rule: the quality prefixes the authored name ("Shiny Leather Chestplate").
    /// Nothing-rank items wear their bare name — "Nothing Hunting Knife" reads like a bug.
    /// Consumables speak alchemy instead (Michael's mega-pass direction): the rank becomes the
    /// container — "Vial of Health" up to "Elixir of Health" — so consumable definitions are
    /// named by their essence ("Health"), never "Health Potion".
    /// </summary>
    public static class ItemNaming
    {
        public static string Compose(QualityRank rank, string baseName)
        {
            string name = baseName ?? string.Empty;
            return rank == QualityRank.Nothing ? name : rank + " " + name;
        }

        public static string Compose(QualityRank rank, ItemSlot slot, string baseName) =>
            slot == ItemSlot.Consumable
                ? ConsumableTerm(rank) + " of " + (baseName ?? string.Empty)
                : Compose(rank, baseName);

        public static string ConsumableTerm(QualityRank rank)
        {
            switch (rank)
            {
                case QualityRank.Torn:
                case QualityRank.Rusty: return "Flask";
                case QualityRank.Shiny:
                case QualityRank.Pristine: return "Bottle";
                case QualityRank.Legendary: return "Draught";
                case QualityRank.Mythical: return "Philter";
                case QualityRank.Godly: return "Elixir";
                default: return "Vial";
            }
        }
    }
}
