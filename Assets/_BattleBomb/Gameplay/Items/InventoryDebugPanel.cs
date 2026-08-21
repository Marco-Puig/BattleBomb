using System.Collections.Generic;
using BattleBomb.Core.Items;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.Gameplay.Items
{
    /// <summary>
    /// The placeholder equip screen (task 45): per player, the ladder place, the allocate
    /// buttons, the loadout, and the bag with equip actions. Lives in Gameplay because it
    /// mutates player state — the real M6 UI will observe and send commands instead. Strictly
    /// a debug surface: M6 replaces it wholesale (planning decision 11).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryDebugPanel : MonoBehaviour
    {
        [Tooltip("Driver whose players are listed. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        private const int KnifeDefinitionId = 7;
        private const int BowDefinitionId = 8;
        private const int HealthPotionDefinitionId = 9;
        private const int ManaPotionDefinitionId = 12;
        private const int EmberStoneDefinitionId = 13;

        /// <summary>Quality score a granted item rolls at — Shiny territory, not the top of the ladder.</summary>
        private const float DebugGrantQuality = 1.7f;

        private bool _open;
        private Vector2 _scroll;

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

            if (GUI.Button(new Rect(10f, 60f, 100f, 24f), _open ? "Close bags" : "Bags"))
            {
                _open = !_open;
            }

            if (!_open)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(10f, 90f, 360f, Screen.height - 110f), GUI.skin.box);
            _scroll = GUILayout.BeginScrollView(_scroll);

            IReadOnlyList<CharacterActor> players = _driver.Characters.Ordered;
            for (int i = 0; i < players.Count; i++)
            {
                DrawPlayer(players[i]);
                GUILayout.Space(12f);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawPlayer(CharacterActor actor)
        {
            PlayerInventory bag = actor.GetComponent<PlayerInventory>();
            if (bag == null)
            {
                return;
            }

            Inventory inventory = bag.Inventory;
            StatSheet sheet = actor.Sheet;

            GUILayout.Label($"P{actor.PlayerId.Value + 1} — level {bag.Ledger.Level}, "
                + $"points {bag.Ledger.UnspentPoints}, prestige {bag.Ledger.PrestigeCount}, "
                + $"xp {bag.Ledger.XpIntoLevel:F0}/{bag.Curve.XpToNext(bag.Ledger.Level, bag.Ledger.PrestigeCount):F0}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+500 XP (debug)", GUILayout.Width(120f)))
            {
                bag.Earn(500f);
            }

            if (GUILayout.Button("+1000 ⛃", GUILayout.Width(80f)))
            {
                bag.GrantCoins(1000);
            }

            if (bag.Ledger.CanPrestige(bag.Curve) && GUILayout.Button("PRESTIGE", GUILayout.Width(90f)))
            {
                bag.TryPrestige();
            }

            GUILayout.EndHorizontal();
            GUILayout.Label($"Dmg {sheet.WeaponDamage:F0}  Def {sheet.Defence * 100f:F0}%  "
                + $"Crit {sheet.CritChance * 100f:F0}%  Swing x{sheet.SwingSpeedMultiplier:F2}  "
                + $"Speed x{sheet.NetMoveSpeedMultiplier:F2}  Mana {actor.Mana.Current:F0}/{actor.Mana.Max:F0}");
            GUILayout.Label($"Magic +{sheet.MagicDamage:F0} dmg, +{sheet.MagicRange:F1} range");
            GUILayout.Label($"⛃ {bag.Wallet.Balance}   Sack {inventory.SlotsUsed}/{inventory.Rules.Capacity}");

            // Testing magic needs specific loot, and drops are random by design. These hand it
            // over directly so a play session can reach the content it means to judge.
            GUILayout.BeginHorizontal();
            GUILayout.Label("Grant:", GUILayout.Width(44f));
            DrawGrant(actor, bag, KnifeDefinitionId, "Knife");
            DrawGrant(actor, bag, BowDefinitionId, "Bow");
            DrawGrant(actor, bag, EmberStoneDefinitionId, "Stone");
            DrawGrant(actor, bag, ManaPotionDefinitionId, "Mana");
            DrawGrant(actor, bag, HealthPotionDefinitionId, "Health");
            GUILayout.EndHorizontal();

            if (bag.Ledger.UnspentPoints > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Allocate:", GUILayout.Width(60f));
                DrawAllocate(bag, StatId.Strength, "Str");
                DrawAllocate(bag, StatId.Hp, "HP");
                DrawAllocate(bag, StatId.Mana, "Mana");
                DrawAllocate(bag, StatId.Speed, "Spd");
                GUILayout.EndHorizontal();
            }

            bag.SetAutoEquip(GUILayout.Toggle(inventory.AutoEquip, " auto-equip upgrades (D30)"));
            bag.SetAutoSell(GUILayout.Toggle(inventory.AutoSell, " auto-sell at the cap (D43)"));

            GUILayout.Label("Worn:");
            DrawWorn(bag, ItemSlot.Helmet);
            DrawWorn(bag, ItemSlot.Chest);
            DrawWorn(bag, ItemSlot.Boots);
            DrawWorn(bag, ItemSlot.Weapon);
            DrawWorn(bag, ItemSlot.Pet);
            DrawWorn(bag, ItemSlot.Equipment, 0);
            DrawWorn(bag, ItemSlot.Equipment, 1);

            string quick = inventory.QuickKind == QuickSlotKind.Empty
                ? "empty"
                : inventory.QuickKind + (inventory.QuickCooldownRemaining > 0
                    ? $" (cooldown {inventory.QuickCooldownRemaining})"
                    : string.Empty);
            GUILayout.Label($"Quick slot: {quick}");

            GUILayout.Label($"Bag ({inventory.Items.Count}):");
            bool mutated = false;
            for (int i = 0; i < inventory.Items.Count && !mutated; i++)
            {
                ItemStack stack = inventory.Items[i];
                GUILayout.BeginHorizontal();
                string locked = stack.Item.Locked ? " [L]" : string.Empty;
                GUILayout.Label($"{stack.Item.DisplayName} x{stack.Count}{locked}", GUILayout.Width(180f));
                if (stack.Item.IsConsumable)
                {
                    if (GUILayout.Button("Quick", GUILayout.Width(52f)))
                    {
                        bag.RequestQuickConsumable(stack.Item.DefinitionId);
                    }
                }
                else if (stack.Item.Slot == ItemSlot.Equipment)
                {
                    bool first = GUILayout.Button("E1", GUILayout.Width(30f));
                    bool second = GUILayout.Button("E2", GUILayout.Width(30f));
                    mutated = (first && bag.RequestEquip(i, 0)) || (second && bag.RequestEquip(i, 1));
                }
                else if (GUILayout.Button("Equip", GUILayout.Width(52f)))
                {
                    mutated = bag.RequestEquip(i);
                }

                if (!mutated && GUILayout.Button(stack.Item.Locked ? "Unlock" : "Lock", GUILayout.Width(52f)))
                {
                    mutated = bag.RequestLock(i, !stack.Item.Locked);
                }

                if (!mutated && GUILayout.Button($"Sell {inventory.Prices.SellPrice(stack.Item)}", GUILayout.Width(64f)))
                {
                    mutated = bag.RequestSell(i) > 0;
                }

                GUILayout.EndHorizontal();
            }
        }

        private void DrawGrant(CharacterActor actor, PlayerInventory bag, int definitionId, string label)
        {
            if (!GUILayout.Button(label, GUILayout.Width(56f)))
            {
                return;
            }

            // Mid-ladder on purpose: a Godly grant would make every feel judgement about an
            // absurd item rather than about the mechanic being judged.
            ItemInstance rolled = _driver.RollDebugItem(definitionId, DebugGrantQuality);
            if (!rolled.IsEmpty)
            {
                bag.Take(rolled);
            }
        }

        private static void DrawAllocate(PlayerInventory bag, StatId stat, string label)
        {
            if (GUILayout.Button("+" + label, GUILayout.Width(58f)))
            {
                bag.RequestAllocate(stat);
            }
        }

        private static void DrawWorn(PlayerInventory bag, ItemSlot slot, int equipmentIndex = 0)
        {
            ItemInstance worn = bag.Inventory.Loadout.Worn(slot, equipmentIndex);
            string label = slot == ItemSlot.Equipment ? $"Equipment {equipmentIndex + 1}" : slot.ToString();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"  {label}: {(worn.IsEmpty ? "—" : worn.DisplayName)}", GUILayout.Width(270f));
            if (!worn.IsEmpty && GUILayout.Button("Off", GUILayout.Width(40f)))
            {
                bag.RequestUnequip(slot, equipmentIndex);
            }

            GUILayout.EndHorizontal();
        }
    }
}
