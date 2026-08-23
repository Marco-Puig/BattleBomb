using System;
using BattleBomb.Core.Items;
using UnityEngine;

namespace BattleBomb.Gameplay.Data
{
    /// <summary>
    /// Authoring format for D33's ladder and the drop-kind weights — the one loot-tuning asset.
    /// Field defaults mirror Core's paper values, so a fresh asset is already the design.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleBomb/Quality Ladder", fileName = "QualityLadder")]
    public sealed class QualityLadder : ScriptableObject
    {
        [Serializable]
        private struct Row
        {
            [Tooltip("Quality score (from DropRoll) needed to reach this rank.")]
            public float MinScore;

            [Tooltip("Multiplier on the definition's base stat block.")]
            public float StatBudget;

            public int AffixMin;
            public int AffixMax;
            public int UpgradeCapacity;

            public Row(float minScore, float statBudget, int affixMin, int affixMax, int upgradeCapacity)
            {
                MinScore = minScore;
                StatBudget = statBudget;
                AffixMin = affixMin;
                AffixMax = affixMax;
                UpgradeCapacity = upgradeCapacity;
            }
        }

        [Tooltip("Nine rows, Nothing → Mythical. Order IS the ladder. Godly is a later release " +
            "and has no row yet — adding one means growing QualityTable.RankCount too.")]
        [SerializeField] private Row[] _rows =
        {
            new Row(0f, 0.5f, 0, 0, 0),
            new Row(0.7f, 0.7f, 0, 0, 1),
            new Row(0.95f, 0.85f, 0, 1, 1),
            new Row(1.2f, 1f, 1, 1, 2),
            new Row(1.6f, 1.2f, 1, 2, 3),
            new Row(2.1f, 1.45f, 2, 2, 4),
            new Row(2.8f, 1.75f, 2, 3, 5),
            new Row(3.7f, 2.1f, 3, 3, 6),
            new Row(5f, 2.5f, 3, 4, 8),
        };

        [Header("Drop kinds")]
        [Tooltip("Relative weights for what falls: consumables common, pets rarest (task 41 paper).")]
        [SerializeField] private float _helmetWeight = 11f;
        [SerializeField] private float _chestWeight = 11f;
        [SerializeField] private float _bootsWeight = 11f;
        [SerializeField] private float _weaponWeight = 15f;
        [SerializeField] private float _petWeight = 2f;
        [SerializeField] private float _equipmentWeight = 8f;
        [SerializeField] private float _consumableWeight = 30f;

        public QualityTable ToTable()
        {
            if (_rows == null || _rows.Length != QualityTable.RankCount)
            {
                return QualityTable.Default;
            }

            var rows = new QualityRow[QualityTable.RankCount];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = new QualityRow(
                    _rows[i].MinScore, _rows[i].StatBudget,
                    _rows[i].AffixMin, _rows[i].AffixMax, _rows[i].UpgradeCapacity);
            }

            return new QualityTable(rows);
        }

        public DropWeights ToWeights() => new DropWeights(
            _helmetWeight, _chestWeight, _bootsWeight, _weaponWeight,
            _petWeight, _equipmentWeight, _consumableWeight);
    }
}
