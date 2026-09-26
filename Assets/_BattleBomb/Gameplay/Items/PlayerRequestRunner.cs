using BattleBomb.Core.Items;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;

namespace BattleBomb.Gameplay.Items
{
    /// <summary>
    /// The one place a menu action becomes a change (HANDOFF-M8 planning decision 11). Every verb is the
    /// same <see cref="PlayerInventory"/> call the chest screen made before M8, so the couch is untouched;
    /// what is added are the host's checks for a request that may have been sent from a stale screen: an
    /// inventory verb needs this player's chest or shop open, and a verb that names a place in the sack
    /// needs the revision its player was looking at.
    /// </summary>
    public static class PlayerRequestRunner
    {
        public static RequestOutcome Run(in PlayerRequest request, PlayerInventory bag, SimulationDriver driver)
        {
            if (bag == null || driver == null)
            {
                return RequestOutcome.No(RequestRefusal.Refused);
            }

            switch (request.Kind)
            {
                case PlayerRequestKind.CloseScreen:
                    driver.CloseScreen(request.PlayerId);
                    return RequestOutcome.Done();

                case PlayerRequestKind.SetAutoEquip:
                    bag.SetAutoEquip(request.A != 0);
                    return RequestOutcome.Done();

                case PlayerRequestKind.SetAutoSell:
                    bag.SetAutoSell(request.A != 0);
                    return RequestOutcome.Done();
            }

            if (!driver.TryGetOpenScreen(request.PlayerId, out InteractionKind screen))
            {
                return RequestOutcome.No(RequestRefusal.NoScreen);
            }

            if (request.NamesASackPlace && request.Revision != bag.Inventory.Sack.Revision)
            {
                return RequestOutcome.No(RequestRefusal.StaleSack);
            }

            switch (request.Kind)
            {
                case PlayerRequestKind.Equip:
                    return RequestOutcome.From(bag.RequestEquip(request.A));

                case PlayerRequestKind.Unequip:
                    return RequestOutcome.From(bag.RequestUnequip((ItemSlot)request.A, request.B));

                case PlayerRequestKind.Sell:
                    int coins = bag.RequestSell(request.A);
                    return coins > 0 ? RequestOutcome.Done(coins) : RequestOutcome.No(RequestRefusal.Refused);

                case PlayerRequestKind.SellJunk:
                    JunkSale sale = bag.RequestSellJunk((QualityRank)request.A);
                    return sale.IsEmpty
                        ? RequestOutcome.No(RequestRefusal.Refused)
                        : RequestOutcome.Done(sale.Stacks, sale.Pieces, sale.Coins);

                case PlayerRequestKind.Lock:
                    return RequestOutcome.From(bag.RequestLock(request.A, request.B != 0));

                case PlayerRequestKind.LockWorn:
                    return RequestOutcome.From(bag.RequestLockWorn((ItemSlot)request.A, request.B, request.C != 0));

                case PlayerRequestKind.Upgrade:
                    return Upgrade(bag, request);

                case PlayerRequestKind.UpgradeWorn:
                    return UpgradeWorn(bag, request);

                case PlayerRequestKind.Combine:
                    return bag.RequestCombine(request.A, request.B, out CombineResult combined)
                        ? RequestOutcome.Done(combined.Promoted ? 1 : 0)
                        : RequestOutcome.No(RequestRefusal.Refused);

                case PlayerRequestKind.CombineAll:
                    return bag.RequestCombineAll(request.A, out CombineRun run)
                        ? RequestOutcome.Done(run.Combines, run.Promotions)
                        : RequestOutcome.No(RequestRefusal.Refused);

                case PlayerRequestKind.QuickConsumable:
                    return RequestOutcome.From(bag.RequestQuickConsumable(request.A));

                case PlayerRequestKind.Allocate:
                    return screen == InteractionKind.Chest
                        ? RequestOutcome.From(bag.RequestAllocate((StatId)request.A))
                        : RequestOutcome.No(RequestRefusal.NoScreen);

                default:
                    // Buy arrives with the rack in the simulation (Task 98), DebugGrant with the settings
                    // online (Task 100). Until then, asked for either, the host does nothing.
                    return RequestOutcome.No(RequestRefusal.Refused);
            }
        }

        private static RequestOutcome Upgrade(PlayerInventory bag, in PlayerRequest request)
        {
            int index = request.A;
            if (index < 0 || index >= bag.Inventory.Items.Count)
            {
                return RequestOutcome.No(RequestRefusal.Refused);
            }

            int price = bag.Inventory.Prices.UpgradeCost(bag.Inventory.Items[index].Item);
            return bag.RequestUpgrade(index, request.Target)
                ? RequestOutcome.Done(price)
                : RequestOutcome.No(RequestRefusal.Refused);
        }

        private static RequestOutcome UpgradeWorn(PlayerInventory bag, in PlayerRequest request)
        {
            var slot = (ItemSlot)request.A;
            ItemInstance worn = bag.Inventory.Loadout.Worn(slot, request.B);
            int price = worn.IsEmpty ? 0 : bag.Inventory.Prices.UpgradeCost(worn);
            return bag.RequestUpgradeWorn(slot, request.B, request.Target)
                ? RequestOutcome.Done(price)
                : RequestOutcome.No(RequestRefusal.Refused);
        }
    }
}
