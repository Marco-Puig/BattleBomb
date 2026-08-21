using System;

namespace BattleBomb.Core.Progression
{
    /// <summary>
    /// The couch's money (D51, amending D43): one purse for the machine, not one per player —
    /// like the sack it buys into, spending is a couch decision, not a personal one. It only
    /// ever enters through selling and leaves through the shopkeeper and the upgrade sink;
    /// nothing else touches it.
    /// </summary>
    public readonly struct Wallet
    {
        public readonly int Balance;

        public Wallet(int balance)
        {
            Balance = Math.Max(0, balance);
        }

        public static Wallet Empty => default;

        public bool CanAfford(int price) => price >= 0 && price <= Balance;

        public Wallet Earned(int amount) => new Wallet(Balance + Math.Max(0, amount));

        /// <summary>Spending clamps at zero — callers gate on <see cref="CanAfford"/> first, and
        /// a race that slipped past never mints negative money.</summary>
        public Wallet Spent(int amount) => new Wallet(Balance - Math.Max(0, amount));
    }
}
