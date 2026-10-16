using System.Collections.Generic;

namespace Othello.Services
{
    /// <summary>Whether a product can be bought once (kept forever) or repeatedly (consumed on use).</summary>
    public enum ProductType
    {
        Consumable,
        NonConsumable
    }

    /// <summary>One purchasable product, independent of any store SDK's own product model.</summary>
    public readonly struct ProductDefinition
    {
        public string Id { get; }
        public ProductType Type { get; }
        public string DisplayName { get; }

        public ProductDefinition(string id, ProductType type, string displayName)
        {
            Id = id;
            Type = type;
            DisplayName = displayName;
        }
    }

    /// <summary>
    /// The product list as the game defines it. Store-specific setup (App
    /// Store Connect, Google Play Console) has to mirror these IDs, but the
    /// game itself only ever refers to a product by the constants here —
    /// never a hard-coded string sprinkled through the codebase.
    /// </summary>
    public static class ProductCatalog
    {
        public const string RemoveAds = "remove_ads";
        public const string GemsPackSmall = "gems_pack_small";
        public const string GemsPackLarge = "gems_pack_large";

        /// <summary>
        /// An inventory skin sold directly for real money instead of through
        /// the in-game currency shop — see <see cref="InventoryCatalog.BoardSkinGold"/>.
        /// </summary>
        public const string SkinGoldBoard = "skin_gold_board";

        public static readonly IReadOnlyList<ProductDefinition> All = new[]
        {
            new ProductDefinition(RemoveAds, ProductType.NonConsumable, "Remover anúncios"),
            new ProductDefinition(GemsPackSmall, ProductType.Consumable, "Pacote de 100 gemas"),
            new ProductDefinition(GemsPackLarge, ProductType.Consumable, "Pacote de 550 gemas"),
            new ProductDefinition(SkinGoldBoard, ProductType.NonConsumable, "Tabuleiro Dourado (exclusivo)")
        };

        public static bool TryFind(string productId, out ProductDefinition product)
        {
            foreach (var candidate in All)
            {
                if (candidate.Id == productId)
                {
                    product = candidate;
                    return true;
                }
            }

            product = default;
            return false;
        }
    }
}
