using System.Collections.Generic;
using Othello.Core;
using Othello.Services;
using UnityEngine;

namespace Othello.Unity
{
    /// <summary>
    /// Loads the cosmetic art (piece skins, board skins, avatars) and shared
    /// UI sprites shipped under <c>Assets/Resources/Art</c>, caching each
    /// lookup so rebuilding a shop or customization row list doesn't hit
    /// <see cref="Resources.Load{T}"/> again for the same sprite.
    /// <para>
    /// Every lookup can legitimately return null — the player hasn't
    /// equipped a skin, or an asset hasn't been imported into the project
    /// yet — so every caller falls back to the existing procedural look
    /// (a flat color, or <see cref="CircleSpriteFactory"/>'s disc) instead
    /// of assuming a sprite exists.
    /// </para>
    /// </summary>
    public static class CosmeticArt
    {
        private static readonly Dictionary<string, Sprite> Cache = new();

        public static Sprite LoadPiece(string skinId, Player player)
        {
            if (string.IsNullOrEmpty(skinId))
            {
                return null;
            }

            return Load($"Art/Pieces/{skinId}_{(player == Player.Black ? "black" : "white")}");
        }

        public static Sprite LoadBoardSquare(string skinId, bool isDarkSquare)
        {
            if (string.IsNullOrEmpty(skinId))
            {
                return null;
            }

            return Load($"Art/Boards/{skinId}_{(isDarkSquare ? "dark" : "light")}");
        }

        /// <summary>
        /// Loads the equipped avatar's imported art, falling back to a
        /// procedurally generated placeholder (see <see cref="AvatarSpriteFactory"/>)
        /// for <see cref="InventoryCatalog.AvatarDefault"/> specifically —
        /// the one avatar every player starts with, so it needs to actually
        /// be visible without waiting on real art to be imported, the same
        /// way the default piece skin and board skin already are.
        /// </summary>
        public static Sprite LoadAvatar(string avatarId)
        {
            if (string.IsNullOrEmpty(avatarId))
            {
                return null;
            }

            Sprite sprite = Load($"Art/Avatars/{avatarId}");
            if (sprite == null && avatarId == InventoryCatalog.AvatarDefault)
            {
                return AvatarSpriteFactory.GetDefaultAvatarSprite();
            }

            return sprite;
        }

        public static Sprite LoadRankIcon(RankTier tier) => Load($"Art/Ranks/{RankTierCatalog.GetIconId(tier)}");

        /// <summary>
        /// One representative sprite for a shop/customization row: the
        /// black-side piece for a piece skin, the dark square for a board
        /// skin, or the avatar itself.
        /// </summary>
        public static Sprite LoadItemThumbnail(ItemDefinition item) => item.Category switch
        {
            ItemCategory.PieceSkin => LoadPiece(item.Id, Player.Black),
            ItemCategory.BoardSkin => LoadBoardSquare(item.Id, isDarkSquare: true),
            ItemCategory.Avatar => LoadAvatar(item.Id),
            _ => null
        };

        public static Sprite LoadButton() => Load("Art/UI/button");

        public static Sprite LoadPanelBackground() => Load("Art/UI/panel_background");

        public static Sprite LoadSoftCurrencyIcon() => Load("Art/UI/icon_soft_currency");

        public static Sprite LoadHardCurrencyIcon() => Load("Art/UI/icon_hard_currency");

        private static Sprite Load(string resourcePath)
        {
            if (Cache.TryGetValue(resourcePath, out var cached))
            {
                return cached;
            }

            var sprite = Resources.Load<Sprite>(resourcePath);
            Cache[resourcePath] = sprite;
            return sprite;
        }
    }
}
