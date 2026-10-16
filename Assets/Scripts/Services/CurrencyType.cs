namespace Othello.Services
{
    /// <summary>
    /// The two currencies the design calls for: Soft is earned by playing or
    /// watching a rewarded ad, Hard is bought with real money. Keeping them
    /// as one enum instead of two separate balances is what lets
    /// <see cref="CurrencyWallet"/> and inventory checks share one code path
    /// instead of duplicating add/spend logic per currency.
    /// </summary>
    public enum CurrencyType
    {
        Soft,
        Hard
    }
}
