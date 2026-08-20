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
                + $"points {bag.Ledger.UnspentPoints}, prestige {bag.Ledger.PrestigeCount}");
            GUILayout.Label($"Dmg {sheet.WeaponDamage:F0}  Def {sheet.Defence * 100f:F0}%  "
                + $"Crit {sheet.CritChance * 100f:F0}%  Swing x{sheet.SwingSpeedMultiplier:F2}  "
                + $"Speed x{sheet.NetMoveSpeedMultiplier:F2}  Mana {actor.Mana.Current:F0}/{actor.Mana.Max:F0}");

            if (bag.Ledger.UnspentPoints > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Allocate:", GUILayout.Width(60f));
                DrawAllocate(actor, bag, StatId.Strength, "Str");
                DrawAllocate(actor, bag, StatId.Hp, "HP");
                DrawAllocate(actor, bag, StatId.Mana, "Mana");
                DrawAllocate(actor, bag, StatId.Speed, "Spd");
                GUILayout.EndHorizontal();
            }

            bool autoEquip = GUILayout.Toggle(inventory.AutoEquip, " auto-equip upgrades (D30)");
            if (autoEquip != inventory.AutoEquip)
            {
                inventory.AutoEquip = autoEquip;
            }

            GUILayout.Label("Worn:");
            DrawWorn(actor, inventory, ItemSlot.Helmet);
            DrawWorn(actor, inventory, ItemSlot.Chest);
            DrawWorn(actor, inventory, ItemSlot.Boots);
            DrawWorn(actor, inventory, ItemSlot.Weapon);
            DrawWorn(actor, inventory, ItemSlot.Pet);
            DrawWorn(actor, inventory, ItemSlot.Equipment, 0);
            DrawWorn(actor, inventory, ItemSlot.Equipment, 1);

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
                GUILayout.Label($"{stack.Item.DisplayName} x{stack.Count}", GUILayout.Width(210f));
                if (stack.Item.IsConsumable)
                {
                    if (GUILayout.Button("Quick", GUILayout.Width(52f)))
                    {
                        inventory.AssignQuickConsumable(stack.Item.DefinitionId);
                    }
                }
                else if (stack.Item.Slot == ItemSlot.Equipment)
                {
                    bool first = GUILayout.Button("E1", GUILayout.Width(34f));
                    bool second = GUILayout.Button("E2", GUILayout.Width(34f));
                    if (first && inventory.TryEquip(i, bag.Level, 0))
                    {
                        actor.RefreshStats();
                        mutated = true;
                    }
                    else if (second && inventory.TryEquip(i, bag.Level, 1))
                    {
                        actor.RefreshStats();
                        mutated = true;
                    }
                }
                else if (GUILayout.Button("Equip", GUILayout.Width(52f))
                    && inventory.TryEquip(i, bag.Level))
                {
                    actor.RefreshStats();
                    mutated = true;
                }

                GUILayout.EndHorizontal();
            }
        }

        private static void DrawAllocate(CharacterActor actor, PlayerInventory bag, StatId stat, string label)
        {
            if (GUILayout.Button("+" + label, GUILayout.Width(58f)))
            {
                bag.SetLedger(bag.Ledger.Spend(stat));
                actor.RefreshStats();
            }
        }

        private static void DrawWorn(CharacterActor actor, Inventory inventory, ItemSlot slot, int equipmentIndex = 0)
        {
            ItemInstance worn = inventory.Loadout.Worn(slot, equipmentIndex);
            string label = slot == ItemSlot.Equipment ? $"Equipment {equipmentIndex + 1}" : slot.ToString();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"  {label}: {(worn.IsEmpty ? "—" : worn.DisplayName)}", GUILayout.Width(270f));
            if (!worn.IsEmpty && GUILayout.Button("Off", GUILayout.Width(40f))
                && inventory.Unequip(slot, equipmentIndex))
            {
                actor.RefreshStats();
            }

            GUILayout.EndHorizontal();
        }
    }
}
