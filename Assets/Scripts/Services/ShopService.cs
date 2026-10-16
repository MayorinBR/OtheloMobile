using System;
using System.Linq;

namespace Othello.Services
{
    /// <summary>
    /// The one place that turns "buy this item" into either a wallet spend
    /// or a native IAP purchase, depending on <see cref="ItemPrice.Kind"/> —
    /// exactly the check-balance-or-trigger-IAP flow the project's inventory
    /// design calls for. Ownership itself is tracked in
    /// <see cref="PlayerSession.UnlockedSkinIds"/> regardless of which path
    /// paid for the item, so the shop UI only ever needs one ownership check.
    /// </summary>
    public sealed class ShopService
    {
        private readonly CurrencyWallet _wallet;
        private readonly IIAPService _iap;
        private readonly PlayerSession _session;

        public ShopService(CurrencyWallet wallet, IIAPService iap, PlayerSession session)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _iap = iap ?? throw new ArgumentNullException(nameof(iap));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public bool IsOwned(string itemId) => _session.UnlockedSkinIds.Contains(itemId);

        /// <summary>
        /// True when the wallet currently holds enough of the right currency
        /// to buy <paramref name="itemId"/> — meaningless (returns false) for
        /// an item priced via direct IAP, since that path doesn't touch the wallet.
        /// </summary>
        public bool CanAffordWithWallet(string itemId)
        {
            if (!InventoryCatalog.TryFind(itemId, out var item) || item.Price.Kind != PriceKind.Currency)
            {
                return false;
            }

            return _wallet.HasEnough(item.Price.CurrencyType, item.Price.Amount);
        }

        /// <summary>
        /// Buys <paramref name="itemId"/> if not already owned. Spends from
        /// the wallet or starts a store purchase depending on how the item is
        /// priced; <paramref name="onCompleted"/> reports the final result
        /// either way, synchronously for a wallet purchase or asynchronously
        /// once the store responds for a direct one.
        /// </summary>
        public void Purchase(string itemId, Action<bool> onCompleted)
        {
            if (IsOwned(itemId))
            {
                onCompleted?.Invoke(true);
                return;
            }

            if (!InventoryCatalog.TryFind(itemId, out var item))
            {
                onCompleted?.Invoke(false);
                return;
            }

            if (item.Price.Kind == PriceKind.Currency)
            {
                bool spent = _wallet.TrySpend(item.Price.CurrencyType, item.Price.Amount);
                if (spent)
                {
                    _session.UnlockSkin(itemId);
                }
                onCompleted?.Invoke(spent);
                return;
            }

            if (item.Price.Kind == PriceKind.Free)
            {
                // Never reachable through ShopView, which excludes Free
                // items from its list — every player already owns them via
                // PlayerSession's default unlock seeding. Handled anyway so
                // this never falls through to the IAP path below with a
                // null product id.
                _session.UnlockSkin(itemId);
                onCompleted?.Invoke(true);
                return;
            }

            _iap.Purchase(item.Price.ProductId, purchased =>
            {
                if (purchased)
                {
                    _session.UnlockSkin(itemId);
                }
                onCompleted?.Invoke(purchased);
            });
        }
    }
}
