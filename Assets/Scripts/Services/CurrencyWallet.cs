using System;

namespace Othello.Services
{
    /// <summary>
    /// Holds the player's Soft and Hard balances. This is the single place
    /// that checks "does the player have enough of the right currency" —
    /// the inventory/shop UI calls <see cref="HasEnough"/> before offering an
    /// item, and <see cref="TrySpend"/> at the moment of purchase, instead of
    /// each screen reading and comparing the balances itself.
    /// </summary>
    public sealed class CurrencyWallet
    {
        public int SoftBalance { get; private set; }
        public int HardBalance { get; private set; }

        /// <summary>Raised whenever either balance changes.</summary>
        public event Action Changed;

        public void Add(CurrencyType type, int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Não é possível adicionar um valor negativo.");
            }

            if (type == CurrencyType.Soft)
            {
                SoftBalance += amount;
            }
            else
            {
                HardBalance += amount;
            }

            Changed?.Invoke();
        }

        public bool HasEnough(CurrencyType type, int amount) => Balance(type) >= amount;

        /// <summary>
        /// Attempts to deduct <paramref name="amount"/> of <paramref name="type"/>.
        /// </summary>
        /// <returns>False, without changing any balance, if funds are insufficient.</returns>
        public bool TrySpend(CurrencyType type, int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Não é possível gastar um valor negativo.");
            }

            if (!HasEnough(type, amount))
            {
                return false;
            }

            if (type == CurrencyType.Soft)
            {
                SoftBalance -= amount;
            }
            else
            {
                HardBalance -= amount;
            }

            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Overwrites a balance outright. Not for gameplay — that's what
        /// <see cref="Add"/> and <see cref="TrySpend"/> are for — this exists
        /// for restoring a wallet from a saved <see cref="PlayerProfileData"/>.
        /// </summary>
        public void SetBalance(CurrencyType type, int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Saldo não pode ser negativo.");
            }

            if (type == CurrencyType.Soft)
            {
                SoftBalance = amount;
            }
            else
            {
                HardBalance = amount;
            }

            Changed?.Invoke();
        }

        private int Balance(CurrencyType type) => type == CurrencyType.Soft ? SoftBalance : HardBalance;
    }
}
