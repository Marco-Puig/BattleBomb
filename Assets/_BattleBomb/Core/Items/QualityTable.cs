using UnityEngine;

namespace BattleBomb.Core.Items
{
    /// <summary>
    /// One row of D33's ladder: the score threshold that reaches the rank, and the three things
    /// the rank grants — a stat budget, an affix count range, and upgrade capacity.
    /// </summary>
    public readonly struct QualityRow
    {
        public readonly float MinScore;
        public readonly float StatBudget;
        public readonly int AffixMin;
        public readonly int AffixMax;
        public readonly int UpgradeCapacity;

        public QualityRow(float minScore, float statBudget, int affixMin, int affixMax, int upgradeCapacity)
        {
            MinScore = minScore;
            StatBudget = statBudget;
            AffixMin = Mathf.Max(0, affixMin);
            AffixMax = Mathf.Max(AffixMin, affixMax);
            UpgradeCapacity = Mathf.Max(0, upgradeCapacity);
        }
    }

    /// <summary>
    /// The nine authored rows, indexed by <see cref="QualityRank"/>, plus the mapping from
    /// <c>DropRoll</c>'s continuous quality score onto a rank — M3's tested roll feeds the
    /// ladder unchanged. Paper values in <see cref="Default"/>; the authoring asset overrides.
    /// </summary>
    public readonly struct QualityTable
    {
        public const int RankCount = 9;

        private readonly QualityRow[] _rows;

        public QualityTable(QualityRow[] rows)
        {
            _rows = rows != null && rows.Length == RankCount ? rows : DefaultRows();
        }

        public QualityRow For(QualityRank rank)
        {
            QualityRow[] rows = _rows ?? DefaultRows();
            int index = Mathf.Clamp((int)rank, 0, RankCount - 1);
            return rows[index];
        }

        /// <summary>The highest rank whose threshold the score reaches; never below Nothing.</summary>
        public QualityRank RankFor(float score)
        {
            QualityRow[] rows = _rows ?? DefaultRows();
            for (int rank = RankCount - 1; rank > 0; rank--)
            {
                if (score >= rows[rank].MinScore)
                {
                    return (QualityRank)rank;
                }
            }

            return QualityRank.Nothing;
        }

        public static QualityTable Default => new QualityTable(DefaultRows());

        private static QualityRow[] DefaultRows() => new[]
        {
            new QualityRow(0f, 0.5f, 0, 0, 0),
            new QualityRow(0.7f, 0.7f, 0, 0, 1),
            new QualityRow(0.95f, 0.85f, 0, 1, 1),
            new QualityRow(1.2f, 1f, 1, 1, 2),
            new QualityRow(1.6f, 1.2f, 1, 2, 3),
            new QualityRow(2.1f, 1.45f, 2, 2, 4),
            new QualityRow(2.8f, 1.75f, 2, 3, 5),
            new QualityRow(3.7f, 2.1f, 3, 3, 6),
            new QualityRow(5f, 2.5f, 3, 4, 8),
        };
    }
}
