using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Items;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.UI.Combat
{
    /// <summary>
    /// Minimal per-player health readout: one bar per registered player in PlayerId order, with
    /// the downed state spelled out. Bar and text sizes are serialized from day one so the future
    /// settings menu can bind them (the DamageNumbers pattern, D20). Purely observational.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHealthBars : MonoBehaviour
    {
        [Tooltip("Driver whose players are read. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        [Tooltip("Bar text size: the settings menu binds this later.")]
        [SerializeField] private int _fontSize = 16;

        [SerializeField] private float _barWidth = 240f;
        [SerializeField] private float _barHeight = 22f;

        private GUIStyle _style;

        private void OnEnable()
        {
            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }
        }

        private void OnGUI()
        {
            if (_driver == null)
            {
                return;
            }

            IReadOnlyList<CharacterActor> players = _driver.Characters.Ordered;
            if (players.Count == 0)
            {
                return;
            }

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                };
            }

            _style.fontSize = Mathf.Max(8, _fontSize);

            float x = (Screen.width - _barWidth) * 0.5f;
            for (int i = 0; i < players.Count; i++)
            {
                PlayerCondition condition = players[i].Condition;
                float fraction = condition.Health.Max > 0f
                    ? Mathf.Clamp01(condition.Health.Current / condition.Health.Max)
                    : 0f;

                PlayerInventory ledgerBag = players[i].GetComponent<PlayerInventory>();

                Rect back = new Rect(x, 10f + i * (_barHeight + 14f), _barWidth, _barHeight);
                Color restore = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.55f);
                GUI.DrawTexture(back, Texture2D.whiteTexture);
                if (fraction > 0f)
                {
                    GUI.color = Color.Lerp(
                        new Color(0.85f, 0.2f, 0.15f), new Color(0.25f, 0.8f, 0.3f), fraction);
                    GUI.DrawTexture(
                        new Rect(back.x + 2f, back.y + 2f, (back.width - 4f) * fraction, back.height - 4f),
                        Texture2D.whiteTexture);
                }

                // The ladder strip (D24) and the mana strip (D32), thin under the health bar.
                if (ledgerBag != null)
                {
                    var ledger = ledgerBag.Ledger;
                    float toNext = ledgerBag.Curve.XpToNext(ledger.Level, ledger.PrestigeCount);
                    float xpFraction = ledger.Level >= ledgerBag.Curve.MaxLevel ? 1f
                        : toNext > 0f ? Mathf.Clamp01(ledger.XpIntoLevel / toNext) : 0f;
                    GUI.color = new Color(0f, 0f, 0f, 0.55f);
                    GUI.DrawTexture(new Rect(back.x, back.yMax + 1f, back.width, 4f), Texture2D.whiteTexture);
                    GUI.color = new Color(0.95f, 0.8f, 0.25f);
                    GUI.DrawTexture(new Rect(back.x, back.yMax + 1f, back.width * xpFraction, 4f), Texture2D.whiteTexture);

                    float manaFraction = Mathf.Clamp01(players[i].Mana.Fraction);
                    GUI.color = new Color(0f, 0f, 0f, 0.55f);
                    GUI.DrawTexture(new Rect(back.x, back.yMax + 6f, back.width, 4f), Texture2D.whiteTexture);
                    GUI.color = new Color(0.3f, 0.55f, 1f);
                    GUI.DrawTexture(new Rect(back.x, back.yMax + 6f, back.width * manaFraction, 4f), Texture2D.whiteTexture);

                    // The splash's price, marked on the pool: what a press costs is visible
                    // before it is pressed, so an empty pool never reads as a broken button (D39).
                    float cost = players[i].SplashManaCost;
                    if (cost > 0f && players[i].Mana.Max > 0f)
                    {
                        float costFraction = Mathf.Clamp01(cost / players[i].Mana.Max);
                        GUI.color = manaFraction >= costFraction
                            ? new Color(1f, 1f, 1f, 0.65f)
                            : new Color(1f, 0.35f, 0.3f, 0.9f);
                        GUI.DrawTexture(
                            new Rect(back.x + back.width * costFraction, back.yMax + 5f, 1.5f, 6f),
                            Texture2D.whiteTexture);
                    }
                }

                GUI.color = restore;

                string prefix = $"P{players[i].PlayerId.Value + 1}";
                if (ledgerBag != null)
                {
                    prefix += ledgerBag.Ledger.PrestigeCount > 0
                        ? $" ★{ledgerBag.Ledger.PrestigeCount} Lv{ledgerBag.Ledger.Level}"
                        : $" Lv{ledgerBag.Ledger.Level}";
                }

                string label = condition.IsDown
                    ? $"{prefix}  DOWN"
                    : $"{prefix}  {Mathf.CeilToInt(condition.Health.Current)}/{Mathf.CeilToInt(condition.Health.Max)}";
                int grabs = _driver.GrabCountFor(players[i].PlayerId.Value);
                if (grabs > 0)
                {
                    label += $"   loot {grabs}";
                }

                if (ledgerBag != null && ledgerBag.Inventory.QuickKind == QuickSlotKind.Consumable)
                {
                    int potions = 0;
                    IReadOnlyList<ItemStack> items = ledgerBag.Inventory.Items;
                    for (int s = 0; s < items.Count; s++)
                    {
                        if (items[s].Item.DefinitionId == ledgerBag.Inventory.QuickConsumableId)
                        {
                            potions += items[s].Count;
                        }
                    }

                    label += ledgerBag.Inventory.QuickCooldownRemaining > 0
                        ? $"   potion x{potions} (cd)"
                        : $"   potion x{potions}";
                }

                _style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
                GUI.Label(new Rect(back.x + 9f, back.y + 1f, back.width, back.height), label, _style);
                _style.normal.textColor = condition.IsDown ? new Color(1f, 0.45f, 0.4f) : Color.white;
                GUI.Label(new Rect(back.x + 8f, back.y, back.width, back.height), label, _style);
            }
        }
    }
}
