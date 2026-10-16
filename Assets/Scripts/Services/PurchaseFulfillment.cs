using System;

namespace Othello.Services
{
    /// <summary>
    /// Translates a completed purchase into an effect on
    /// <see cref="MonetizationState"/>. Keeping this separate from
    /// <see cref="IIAPService"/> means the store facade only has to know how
    /// to complete a transaction — not what any given product actually does
    /// once bought, which is a game-design decision, not a storefront one.
    /// </summary>
    public sealed class PurchaseFulfillment
    {
        private readonly MonetizationState _state;

        public PurchaseFulfillment(MonetizationState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void Apply(string productId)
        {
            switch (productId)
            {
                case ProductCatalog.RemoveAds:
                    _state.SetNoAdsPurchased(true);
                    break;
                case ProductCatalog.GemsPackSmall:
                    _state.Wallet.Add(CurrencyType.Hard, 100);
                    break;
                case ProductCatalog.GemsPackLarge:
                    _state.Wallet.Add(CurrencyType.Hard, 550);
                    break;
                case ProductCatalog.SkinGoldBoard:
                    // No wallet/flag effect — unlocking the item itself is
                    // ShopService's job once the purchase completes, the same
                    // as it is for a currency-bought item. This case exists
                    // only so an unrecognized product id still fails loudly.
                    break;
                default:
                    throw new ArgumentException($"Produto desconhecido: {productId}", nameof(productId));
            }
        }
    }
}
