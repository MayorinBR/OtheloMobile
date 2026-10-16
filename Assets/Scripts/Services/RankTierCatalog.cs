namespace Othello.Services
{
    /// <summary>
    /// The player-facing rank ladder, from lowest to highest:
    /// <see cref="Aprendiz"/> through <see cref="GraoMestre"/> are rating
    /// bands, while <see cref="Champion"/> is reserved for whoever
    /// currently holds rank #1 on the leaderboard — a position, not a
    /// rating threshold, so it moves as the standings do rather than
    /// staying with whoever first crossed some score.
    /// </summary>
    public enum RankTier
    {
        Aprendiz,
        Tatico,
        Estrategista,
        MestreDeTabuleiro,
        GraoMestre,
        Champion
    }

    /// <summary>
    /// Resolves a rating/rank pair to the <see cref="RankTier"/> it
    /// displays as, and the display name/icon that go with each tier.
    /// <see cref="LeaderboardEntry"/> is the only caller that needs to
    /// compute this — everything else just reads <see cref="LeaderboardEntry.Tier"/>.
    /// </summary>
    public static class RankTierCatalog
    {
        // Ordered low to high; the tier is the highest band whose
        // MinRating the player's rating clears. Champion isn't listed
        // here — see GetTier.
        private static readonly (int MinRating, RankTier Tier)[] RatingBands =
        {
            (0, RankTier.Aprendiz),
            (1300, RankTier.Tatico),
            (1500, RankTier.Estrategista),
            (1700, RankTier.MestreDeTabuleiro),
            (1900, RankTier.GraoMestre)
        };

        /// <summary>
        /// Rank #1 is always <see cref="RankTier.Champion"/>, regardless of
        /// rating — every other rank falls into the highest rating band it
        /// clears.
        /// </summary>
        public static RankTier GetTier(int rating, int rank)
        {
            if (rank == 1)
            {
                return RankTier.Champion;
            }

            RankTier tier = RankTier.Aprendiz;
            foreach (var (minRating, candidate) in RatingBands)
            {
                if (rating >= minRating)
                {
                    tier = candidate;
                }
            }

            return tier;
        }

        /// <summary>Resource-name id for <see cref="CosmeticArt.LoadRankIcon"/>, e.g. "rank_champion".</summary>
        public static string GetIconId(RankTier tier) => tier switch
        {
            RankTier.Aprendiz => "rank_aprendiz",
            RankTier.Tatico => "rank_tatico",
            RankTier.Estrategista => "rank_estrategista",
            RankTier.MestreDeTabuleiro => "rank_mestre_tabuleiro",
            RankTier.GraoMestre => "rank_grao_mestre",
            RankTier.Champion => "rank_champion",
            _ => "rank_aprendiz"
        };

        public static LocalizationKey GetNameKey(RankTier tier) => tier switch
        {
            RankTier.Aprendiz => LocalizationKey.RankTierAprendiz,
            RankTier.Tatico => LocalizationKey.RankTierTatico,
            RankTier.Estrategista => LocalizationKey.RankTierEstrategista,
            RankTier.MestreDeTabuleiro => LocalizationKey.RankTierMestreDeTabuleiro,
            RankTier.GraoMestre => LocalizationKey.RankTierGraoMestre,
            RankTier.Champion => LocalizationKey.RankTierChampion,
            _ => LocalizationKey.RankTierAprendiz
        };
    }
}
