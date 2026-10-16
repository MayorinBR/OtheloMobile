using System.Collections.Generic;

namespace Othello.Services
{
    /// <summary>What an inventory item cosmetically applies to.</summary>
    public enum ItemCategory
    {
        PieceSkin,
        BoardSkin,
        Avatar
    }

    /// <summary>How an item is paid for.</summary>
    public enum PriceKind
    {
        /// <summary>Spent from the in-game <see cref="CurrencyWallet"/>.</summary>
        Currency,

        /// <summary>Bought directly through the store, bypassing the wallet entirely.</summary>
        DirectPurchase,

        /// <summary>
        /// Not for sale — bundled with the game and unlocked for every
        /// player from the start (see <see cref="PlayerSession"/>'s default
        /// unlock seeding). Never shown in <see cref="ShopService"/>.
        /// </summary>
        Free
    }

    /// <summary>
    /// An item's price, in exactly one of the two forms the design calls
    /// for: an amount of Soft/Hard currency, or a direct real-money IAP
    /// product. <see cref="ShopService"/> is the only place that inspects
    /// <see cref="Kind"/> to decide which purchase path to take.
    /// </summary>
    public readonly struct ItemPrice
    {
        public PriceKind Kind { get; }
        public CurrencyType CurrencyType { get; }
        public int Amount { get; }
        public string ProductId { get; }

        private ItemPrice(PriceKind kind, CurrencyType currencyType, int amount, string productId)
        {
            Kind = kind;
            CurrencyType = currencyType;
            Amount = amount;
            ProductId = productId;
        }

        public static ItemPrice InCurrency(CurrencyType type, int amount) =>
            new(PriceKind.Currency, type, amount, null);

        public static ItemPrice ViaIAP(string productId) =>
            new(PriceKind.DirectPurchase, default, 0, productId);

        public static ItemPrice Included() =>
            new(PriceKind.Free, default, 0, null);
    }

    /// <summary>
    /// One purchasable cosmetic item. Carries a <see cref="LocalizationKey"/>
    /// instead of a raw display name, since the name has to change with the
    /// player's chosen language — resolving it is the UI's job, via
    /// <c>LocalizationService.Get(item.NameKey)</c>.
    /// </summary>
    public readonly struct ItemDefinition
    {
        public string Id { get; }
        public ItemCategory Category { get; }
        public LocalizationKey NameKey { get; }
        public ItemPrice Price { get; }

        public ItemDefinition(string id, ItemCategory category, LocalizationKey nameKey, ItemPrice price)
        {
            Id = id;
            Category = category;
            NameKey = nameKey;
            Price = price;
        }
    }

    /// <summary>
    /// The shop's item list. Most items are bought with in-game currency;
    /// <see cref="BoardSkinGold"/> demonstrates the other path the design
    /// calls for — an exclusive skin sold directly for real money instead.
    /// </summary>
    public static class InventoryCatalog
    {
        /// <summary>
        /// The one starter item per category, unlocked for every player
        /// from the start (see <see cref="PlayerSession"/>) so Customization
        /// never opens on an empty tab. <see cref="AvatarDefault"/> is the
        /// literal "default", matching <see cref="PlayerSession.AvatarId"/>'s
        /// own default value from before this feature existed.
        /// </summary>
        public const string PieceSkinDefault = "piece_skin_default";
        public const string BoardSkinDefault = "board_skin_default";
        public const string AvatarDefault = "default";

        public const string PieceSkinWood = "piece_skin_wood";
        public const string PieceSkinMarble = "piece_skin_marble";
        public const string PieceSkinNeon = "piece_skin_neon";
        public const string PieceSkinObsidian = "piece_skin_obsidian";
        public const string BoardSkinWalnut = "board_skin_walnut";
        public const string BoardSkinGold = "board_skin_gold";
        public const string BoardSkinForest = "board_skin_forest";
        public const string BoardSkinOcean = "board_skin_ocean";
        public const string AvatarFox = "avatar_fox";
        public const string AvatarOwl = "avatar_owl";
        public const string AvatarCat = "avatar_cat";
        public const string AvatarPanda = "avatar_panda";

        public static readonly IReadOnlyList<ItemDefinition> All = new[]
        {
            new ItemDefinition(PieceSkinDefault, ItemCategory.PieceSkin, LocalizationKey.ItemPieceSkinDefault,
                ItemPrice.Included()),
            new ItemDefinition(BoardSkinDefault, ItemCategory.BoardSkin, LocalizationKey.ItemBoardSkinDefault,
                ItemPrice.Included()),
            new ItemDefinition(AvatarDefault, ItemCategory.Avatar, LocalizationKey.ItemAvatarDefault,
                ItemPrice.Included()),
            new ItemDefinition(PieceSkinWood, ItemCategory.PieceSkin, LocalizationKey.ItemPieceSkinWood,
                ItemPrice.InCurrency(CurrencyType.Soft, 80)),
            new ItemDefinition(PieceSkinMarble, ItemCategory.PieceSkin, LocalizationKey.ItemPieceSkinMarble,
                ItemPrice.InCurrency(CurrencyType.Soft, 150)),
            new ItemDefinition(PieceSkinNeon, ItemCategory.PieceSkin, LocalizationKey.ItemPieceSkinNeon,
                ItemPrice.InCurrency(CurrencyType.Soft, 250)),
            new ItemDefinition(PieceSkinObsidian, ItemCategory.PieceSkin, LocalizationKey.ItemPieceSkinObsidian,
                ItemPrice.InCurrency(CurrencyType.Hard, 60)),
            new ItemDefinition(BoardSkinWalnut, ItemCategory.BoardSkin, LocalizationKey.ItemBoardSkinWalnut,
                ItemPrice.InCurrency(CurrencyType.Hard, 200)),
            new ItemDefinition(BoardSkinGold, ItemCategory.BoardSkin, LocalizationKey.ItemBoardSkinGold,
                ItemPrice.ViaIAP(ProductCatalog.SkinGoldBoard)),
            new ItemDefinition(BoardSkinForest, ItemCategory.BoardSkin, LocalizationKey.ItemBoardSkinForest,
                ItemPrice.InCurrency(CurrencyType.Soft, 100)),
            new ItemDefinition(BoardSkinOcean, ItemCategory.BoardSkin, LocalizationKey.ItemBoardSkinOcean,
                ItemPrice.InCurrency(CurrencyType.Hard, 180)),
            new ItemDefinition(AvatarFox, ItemCategory.Avatar, LocalizationKey.ItemAvatarFox,
                ItemPrice.InCurrency(CurrencyType.Soft, 50)),
            new ItemDefinition(AvatarOwl, ItemCategory.Avatar, LocalizationKey.ItemAvatarOwl,
                ItemPrice.InCurrency(CurrencyType.Soft, 50)),
            new ItemDefinition(AvatarCat, ItemCategory.Avatar, LocalizationKey.ItemAvatarCat,
                ItemPrice.InCurrency(CurrencyType.Soft, 50)),
            new ItemDefinition(AvatarPanda, ItemCategory.Avatar, LocalizationKey.ItemAvatarPanda,
                ItemPrice.InCurrency(CurrencyType.Soft, 70))
        };

        public static bool TryFind(string itemId, out ItemDefinition item)
        {
            foreach (var candidate in All)
            {
                if (candidate.Id == itemId)
                {
                    item = candidate;
                    return true;
                }
            }

            item = default;
            return false;
        }
    }
}
