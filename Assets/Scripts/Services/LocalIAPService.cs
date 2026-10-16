using System;
using System.Collections.Generic;

namespace Othello.Services
{
    /// <summary>
    /// Local stand-in for Unity IAP: no store round-trip, purchases
    /// "succeed" immediately and go straight to
    /// <see cref="PurchaseFulfillment"/>. Lets the shop UI, the wallet, and
    /// the no-ads flag all be exercised together — testable without network,
    /// exactly as the project's service-facade design calls for — before
    /// the real store SDK and its receipt validation are wired in.
    /// </summary>
    public sealed class LocalIAPService : IIAPService
    {
        private readonly PurchaseFulfillment _fulfillment;
        private readonly HashSet<string> _ownedNonConsumables = new();

        public LocalIAPService(PurchaseFulfillment fulfillment)
        {
            _fulfillment = fulfillment ?? throw new ArgumentNullException(nameof(fulfillment));
        }

        public bool IsInitialized { get; private set; }

        public void Initialize(Action onInitialized = null)
        {
            IsInitialized = true;
            onInitialized?.Invoke();
        }

        public bool IsProductOwned(string productId) => _ownedNonConsumables.Contains(productId);

        public void Purchase(string productId, Action<bool> onCompleted)
        {
            if (!ProductCatalog.TryFind(productId, out var product))
            {
                onCompleted?.Invoke(false);
                return;
            }

            _fulfillment.Apply(productId);

            if (product.Type == ProductType.NonConsumable)
            {
                _ownedNonConsumables.Add(productId);
            }

            onCompleted?.Invoke(true);
        }
    }
}
