using System;
using System.Collections.Generic;
using Othello.Services;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Othello.Unity
{
    /// <summary>
    /// Real <see cref="IIAPService"/> backed by Unity IAP. Drop-in
    /// replacement for <see cref="LocalIAPService"/> — register this
    /// instead, in <see cref="MonetizationController.Awake"/>. Nothing else
    /// in the game changes.
    /// <para>
    /// Also implements <see cref="IStoreListener"/>, since Unity IAP's own
    /// initialization callback needs one — every product in
    /// <see cref="ProductCatalog"/> is registered with it automatically, so
    /// adding a product to that catalog is still the only step needed on
    /// the game side; App Store Connect / Google Play Console still need
    /// their own matching product listing under the same id.
    /// </para>
    /// <para>
    /// Ownership of "what a purchase actually does" stays with
    /// <see cref="PurchaseFulfillment"/>, called only after Unity IAP
    /// confirms the purchase in <see cref="ProcessPurchase"/> — this class
    /// itself never decides that.
    /// </para>
    /// </summary>
    public sealed class UnityIAPService : IIAPService, IStoreListener
    {
        private readonly PurchaseFulfillment _fulfillment;
        private readonly HashSet<string> _ownedNonConsumables = new();
        private readonly Dictionary<string, Action<bool>> _pendingCallbacks = new();

        private IStoreController _storeController;

        public UnityIAPService(PurchaseFulfillment fulfillment)
        {
            _fulfillment = fulfillment ?? throw new ArgumentNullException(nameof(fulfillment));
        }

        public bool IsInitialized { get; private set; }

        public void Initialize(Action onInitialized = null)
        {
            if (IsInitialized)
            {
                onInitialized?.Invoke();
                return;
            }

            _pendingOnInitialized = onInitialized;

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (var product in Othello.Services.ProductCatalog.All)
            {
                UnityEngine.Purchasing.ProductType storeType = product.Type == Othello.Services.ProductType.Consumable
                    ? UnityEngine.Purchasing.ProductType.Consumable
                    : UnityEngine.Purchasing.ProductType.NonConsumable;
                builder.AddProduct(product.Id, storeType);
            }

            UnityPurchasing.Initialize(this, builder);
        }

        private Action _pendingOnInitialized;

        public bool IsProductOwned(string productId) => _ownedNonConsumables.Contains(productId);

        public void Purchase(string productId, Action<bool> onCompleted)
        {
            if (!IsInitialized || !Othello.Services.ProductCatalog.TryFind(productId, out var product))
            {
                onCompleted?.Invoke(false);
                return;
            }

            if (product.Type == Othello.Services.ProductType.NonConsumable && IsProductOwned(productId))
            {
                onCompleted?.Invoke(true);
                return;
            }

            Product storeProduct = _storeController.products.WithID(productId);
            if (storeProduct == null || !storeProduct.availableToPurchase)
            {
                onCompleted?.Invoke(false);
                return;
            }

            _pendingCallbacks[productId] = onCompleted;
            _storeController.InitiatePurchase(storeProduct);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;
            IsInitialized = true;

            foreach (Product product in controller.products.all)
            {
                if (product.definition.type == UnityEngine.Purchasing.ProductType.NonConsumable && product.hasReceipt)
                {
                    _ownedNonConsumables.Add(product.definition.id);
                }
            }

            _pendingOnInitialized?.Invoke();
            _pendingOnInitialized = null;
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"Unity IAP falhou ao inicializar: {error} — {message}");
            _pendingOnInitialized = null;
        }

        public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, null);

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
        {
            string productId = purchaseEvent.purchasedProduct.definition.id;

            if (!Othello.Services.ProductCatalog.TryFind(productId, out var product))
            {
                Debug.LogWarning($"Compra confirmada para um produto fora do ProductCatalog: {productId}");
                return PurchaseProcessingResult.Complete;
            }

            _fulfillment.Apply(productId);
            if (product.Type == Othello.Services.ProductType.NonConsumable)
            {
                _ownedNonConsumables.Add(productId);
            }

            if (_pendingCallbacks.TryGetValue(productId, out var callback))
            {
                _pendingCallbacks.Remove(productId);
                callback?.Invoke(true);
            }

            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.LogWarning($"Compra falhou para {product.definition.id}: {failureReason}");

            if (_pendingCallbacks.TryGetValue(product.definition.id, out var callback))
            {
                _pendingCallbacks.Remove(product.definition.id);
                callback?.Invoke(false);
            }
        }
    }
}
