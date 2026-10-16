using System;

namespace Othello.Services
{
    /// <summary>
    /// Everything the ads and IAP facades read or write: the wallet and the
    /// "Remove Ads" flag. Kept as one object so save/restore (Phase 3,
    /// Firestore/Cloud Save) has a single thing to serialize instead of
    /// several loose fields scattered across services.
    /// <para>
    /// Profile fields outside monetization (display name, avatar, unlocked
    /// and equipped skins) are owned and snapshotted by
    /// <see cref="PlayerSession"/> instead, since that's where they already
    /// live at runtime.
    /// </para>
    /// </summary>
    public sealed class MonetizationState
    {
        public CurrencyWallet Wallet { get; } = new();
        public bool IsNoAdsPurchased { get; private set; }

        /// <summary>Raised when <see cref="IsNoAdsPurchased"/> changes.</summary>
        public event Action Changed;

        public void SetNoAdsPurchased(bool value)
        {
            if (IsNoAdsPurchased == value)
            {
                return;
            }

            IsNoAdsPurchased = value;
            Changed?.Invoke();
        }
    }
}
