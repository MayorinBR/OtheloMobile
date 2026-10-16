using System;

namespace Othello.Services
{
    /// <summary>
    /// Facade over the native store SDK (Unity IAP). The shop UI and the
    /// inventory only ever call through this interface, so the real
    /// StoreController/receipt-validation plumbing can be added later
    /// without touching any screen that initiates a purchase.
    /// </summary>
    public interface IIAPService
    {
        bool IsInitialized { get; }

        void Initialize(Action onInitialized = null);

        /// <summary>True if a non-consumable product was already bought (e.g. Remove Ads).</summary>
        bool IsProductOwned(string productId);

        /// <summary>
        /// Starts a purchase. <paramref name="onCompleted"/> receives true
        /// once the transaction (and its fulfillment) succeeds.
        /// </summary>
        void Purchase(string productId, Action<bool> onCompleted);
    }
}
