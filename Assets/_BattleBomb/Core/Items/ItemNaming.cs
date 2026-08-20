namespace BattleBomb.Core.Items
{
    /// <summary>
    /// D33's display rule: the quality prefixes the authored name ("Shiny Leather Chestplate").
    /// Nothing-rank items wear their bare name — "Nothing Hunting Knife" reads like a bug.
    /// </summary>
    public static class ItemNaming
    {
        public static string Compose(QualityRank rank, string baseName)
        {
            string name = baseName ?? string.Empty;
            return rank == QualityRank.Nothing ? name : rank + " " + name;
        }
    }
}
