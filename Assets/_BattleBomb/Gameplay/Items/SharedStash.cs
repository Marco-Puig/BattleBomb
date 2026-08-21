using System;
using BattleBomb.Core.Items;
using BattleBomb.Core.Progression;
using UnityEngine;

namespace BattleBomb.Gameplay.Items
{
    /// <summary>
    /// The couch's one sack and one wallet (D51). Lives on the Simulation object; every
    /// <see cref="PlayerInventory"/> builds its loadout over this sack and spends from this
    /// wallet. It raises <see cref="Changed"/> for anything money- or bag-shaped so both chest
    /// screens repaint when either player sells — the partner's sale is a change to your view.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SharedStash : MonoBehaviour
    {
        private Sack _sack;
        private Wallet _wallet;

        /// <summary>Raised after the wallet moved or the sack was replaced wholesale (a load).</summary>
        public event Action Changed;

        public Sack Sack => _sack ?? (_sack = new Sack());

        public Wallet Wallet => _wallet;

        /// <summary>Moves coins in. Does not raise <see cref="Changed"/> — the caller is the one
        /// request that knows whether anything else changed alongside it, so it announces once.</summary>
        public void Earn(int coins)
        {
            if (coins <= 0)
            {
                return;
            }

            _wallet = _wallet.Earned(coins);
        }

        public bool CanAfford(int price) => _wallet.CanAfford(price);

        /// <summary>Takes the money if it is there. False means nothing left the wallet. Does
        /// not raise <see cref="Changed"/> — the caller announces once, after everything its
        /// request touched has settled.</summary>
        public bool TrySpend(int price)
        {
            if (!_wallet.CanAfford(price))
            {
                return false;
            }

            _wallet = _wallet.Spent(price);
            return true;
        }

        /// <summary>A load (D52) or the debug grant: the whole stash replaced at once.</summary>
        public void Restore(Sack sack, in Wallet wallet)
        {
            _sack = sack ?? new Sack();
            _wallet = wallet;
            Changed?.Invoke();
        }

        /// <summary>The sack or wallet changed and every listener must hear about it once. Every
        /// <see cref="PlayerInventory"/> request that actually changed something calls this
        /// exactly once, at the end, on success — never <see cref="Earn"/> or
        /// <see cref="TrySpend"/> directly, or the couch's two screens would each redraw twice
        /// or three times for one action.</summary>
        internal void NotifyChanged() => Changed?.Invoke();

        private void OnEnable()
        {
            if (_sack == null)
            {
                _sack = new Sack();
            }
        }
    }
}
