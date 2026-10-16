using System;
using System.Collections.Generic;

namespace Othello.Services
{
    /// <summary>
    /// Orchestrates sign-in and profile load/save: signs in anonymously,
    /// pulls the saved profile (if any) into this session's fields, and
    /// pushes the current state back to the cloud on
    /// <see cref="SaveProgress"/>. This is the piece that turns "the player
    /// has a wallet and a no-ads flag" (Phase 2) into "...and it survives
    /// reinstalling the app on a different device" (Phase 3).
    /// <para>
    /// Also owns which owned cosmetics are currently equipped
    /// (<see cref="EquippedPieceSkinId"/>, <see cref="EquippedBoardSkinId"/>),
    /// the avatar, the player's chosen <see cref="LocalizationService.CurrentLanguage"/>,
    /// their online <see cref="Rating"/>, and their play history
    /// (<see cref="Stats"/>) and unlocked achievements, since all of these
    /// are profile-level, not monetization state — <see cref="MonetizationState"/>
    /// only ever tracks the wallet and the no-ads flag.
    /// </para>
    /// </summary>
    public sealed class PlayerSession
    {
        private readonly IAuthService _auth;
        private readonly ICloudSaveService _cloudSave;
        private readonly MonetizationState _monetization;
        private readonly LocalizationService _localization;
        private readonly List<string> _unlockedSkinIds = new();
        private readonly List<string> _unlockedAchievementIds = new();

        /// <summary>
        /// The one item per category every player owns from the start —
        /// see <see cref="InventoryCatalog.PieceSkinDefault"/>,
        /// <see cref="InventoryCatalog.BoardSkinDefault"/> and
        /// <see cref="InventoryCatalog.AvatarDefault"/> — seeded into
        /// <see cref="_unlockedSkinIds"/> both for a brand-new session and,
        /// in <see cref="ApplyProfile"/>, for a save made before this
        /// feature existed.
        /// </summary>
        private static readonly string[] DefaultUnlockedItemIds =
        {
            InventoryCatalog.PieceSkinDefault,
            InventoryCatalog.BoardSkinDefault,
            InventoryCatalog.AvatarDefault
        };

        public string DisplayName { get; private set; } = "Player";
        public string AvatarId { get; private set; } = InventoryCatalog.AvatarDefault;
        public string EquippedPieceSkinId { get; private set; } = InventoryCatalog.PieceSkinDefault;
        public string EquippedBoardSkinId { get; private set; } = InventoryCatalog.BoardSkinDefault;
        public int Rating { get; private set; } = EloRating.DefaultRating;
        public MatchStats Stats { get; } = new();

        /// <summary>
        /// True once at least one permanent identity provider is linked
        /// (see <see cref="IAuthService.LinkedProviders"/>) — the anonymous
        /// sign-in every session starts with (<see cref="Start"/>) doesn't
        /// count on its own. A guest (false) never enters the ranking
        /// system (see <see cref="RecordMatchResult"/>) and never earns the
        /// win bonus on a match reward (see <see cref="GrantMatchReward"/>)
        /// — only the base "you played a match" amount.
        /// </summary>
        public bool HasAccount => _auth.LinkedProviders.Count > 0;
        public IReadOnlyList<string> UnlockedSkinIds => _unlockedSkinIds;
        public IReadOnlyList<string> UnlockedAchievementIds => _unlockedAchievementIds;

        /// <summary>How many consecutive days the player has claimed the daily reward, including today's claim if already made.</summary>
        public int DailyRewardStreak { get; private set; }
        private DateTime? _lastDailyRewardClaimUtc;

        /// <summary>Raised after a saved profile is applied on top of the default state.</summary>
        public event Action ProfileLoaded;

        /// <summary>Raised whenever an unlock, equip, or match result changes what the UI should show.</summary>
        public event Action Changed;

        /// <summary>Raised once per newly-unlocked achievement, after its reward has already been credited to the wallet.</summary>
        public event Action<AchievementDefinition> AchievementUnlocked;

        /// <summary>Raised after a successful <see cref="TryClaimDailyReward"/>, with the tier that was granted.</summary>
        public event Action<DailyRewardTier> DailyRewardClaimed;

        public PlayerSession(
            IAuthService auth, ICloudSaveService cloudSave, MonetizationState monetization, LocalizationService localization)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _cloudSave = cloudSave ?? throw new ArgumentNullException(nameof(cloudSave));
            _monetization = monetization ?? throw new ArgumentNullException(nameof(monetization));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));

            EnsureDefaultItemsUnlocked();
        }

        /// <summary>
        /// Adds any of <see cref="DefaultUnlockedItemIds"/> not already in
        /// <see cref="_unlockedSkinIds"/>. Called at construction for a
        /// brand-new player, and again in <see cref="ApplyProfile"/> so a
        /// save made before this feature existed picks up the same starter
        /// items instead of opening Customization to empty tabs. Adds
        /// directly rather than through <see cref="UnlockSkin"/> — this is
        /// baseline state, not something to raise <see cref="Changed"/> or
        /// re-check achievements for.
        /// </summary>
        private void EnsureDefaultItemsUnlocked()
        {
            foreach (var id in DefaultUnlockedItemIds)
            {
                if (!_unlockedSkinIds.Contains(id))
                {
                    _unlockedSkinIds.Add(id);
                }
            }
        }

        /// <summary>
        /// Signs in anonymously and loads any existing profile.
        /// <paramref name="onReady"/> receives false only if sign-in itself fails —
        /// a brand-new player with no saved profile is still "ready", just with defaults.
        /// </summary>
        public void Start(Action<bool> onReady)
        {
            _auth.SignInAnonymously(signedIn =>
            {
                if (!signedIn)
                {
                    onReady?.Invoke(false);
                    return;
                }

                _cloudSave.Load(_auth.UserId, (profile, found) =>
                {
                    if (found)
                    {
                        ApplyProfile(profile);
                        ProfileLoaded?.Invoke();
                    }

                    onReady?.Invoke(true);
                });
            });
        }

        public void UnlockSkin(string skinId)
        {
            if (string.IsNullOrEmpty(skinId) || _unlockedSkinIds.Contains(skinId))
            {
                return;
            }

            _unlockedSkinIds.Add(skinId);
            UnlockNewlySatisfiedAchievements();
            Changed?.Invoke();
        }

        /// <summary>
        /// Equips an owned <see cref="InventoryCatalog"/> item, routing it to
        /// the right slot by its <see cref="ItemCategory"/>. Returns false
        /// without changing anything if the item doesn't exist or isn't
        /// unlocked yet.
        /// </summary>
        public bool TryEquip(string itemId)
        {
            if (!InventoryCatalog.TryFind(itemId, out var item) || !_unlockedSkinIds.Contains(itemId))
            {
                return false;
            }

            switch (item.Category)
            {
                case ItemCategory.PieceSkin:
                    EquippedPieceSkinId = itemId;
                    break;
                case ItemCategory.BoardSkin:
                    EquippedBoardSkinId = itemId;
                    break;
                case ItemCategory.Avatar:
                    AvatarId = itemId;
                    break;
                default:
                    return false;
            }

            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Renames the player after trimming surrounding whitespace.
        /// Rejects a result that is empty or longer than 20 characters,
        /// leaving <see cref="DisplayName"/> unchanged, and raises
        /// <see cref="Changed"/> only when the rename succeeds.
        /// </summary>
        /// <param name="name">The requested display name, before trimming.</param>
        /// <returns>True if the name was accepted and applied.</returns>
        public bool TrySetDisplayName(string name)
        {
            string trimmed = name?.Trim() ?? string.Empty;
            if (trimmed.Length == 0 || trimmed.Length > 20)
            {
                return false;
            }

            DisplayName = trimmed;
            Changed?.Invoke();
            return true;
        }

        /// <summary>True if the daily reward hasn't been claimed yet today, per <paramref name="utcNow"/>.</summary>
        public bool CanClaimDailyReward(DateTime utcNow) => DailyRewardEvaluator.CanClaim(_lastDailyRewardClaimUtc, utcNow);

        /// <summary>The tier a claim made right now would grant, without claiming it — for previewing in the UI.</summary>
        public DailyRewardTier PeekNextDailyReward(DateTime utcNow) =>
            DailyRewardCatalog.GetTier(DailyRewardEvaluator.GetNextStreakDay(_lastDailyRewardClaimUtc, DailyRewardStreak, utcNow));

        /// <summary>
        /// Claims today's daily reward if it hasn't been claimed yet:
        /// advances <see cref="DailyRewardStreak"/> (or resets it to 1 if a
        /// day was missed since the last claim), credits the tier's reward
        /// to the wallet, and unlocks the streak achievement if it now
        /// qualifies.
        /// </summary>
        /// <param name="utcNow">The current time in UTC, used for the day-boundary check.</param>
        /// <param name="grantedTier">The tier that was granted, if the claim succeeded.</param>
        /// <returns>False, without changing anything, if today's reward was already claimed.</returns>
        public bool TryClaimDailyReward(DateTime utcNow, out DailyRewardTier grantedTier)
        {
            if (!CanClaimDailyReward(utcNow))
            {
                grantedTier = default;
                return false;
            }

            DailyRewardStreak = DailyRewardEvaluator.GetNextStreakDay(_lastDailyRewardClaimUtc, DailyRewardStreak, utcNow);
            _lastDailyRewardClaimUtc = utcNow;
            grantedTier = DailyRewardCatalog.GetTier(DailyRewardStreak);

            _monetization.Wallet.Add(CurrencyType.Soft, grantedTier.SoftReward);
            if (grantedTier.HardReward > 0)
            {
                _monetization.Wallet.Add(CurrencyType.Hard, grantedTier.HardReward);
            }

            UnlockNewlySatisfiedAchievements();
            DailyRewardClaimed?.Invoke(grantedTier);
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Records the result of a finished Vs-AI or online match: folds it
        /// into <see cref="Stats"/>, applies the Elo rating change for
        /// online matches, unlocks any newly-satisfied achievements
        /// (crediting their reward), and raises <see cref="Changed"/>. Not
        /// called for local pass-and-play — there is no single "the player"
        /// perspective to record a result for when both sides are human.
        /// </summary>
        /// <param name="outcome">How the match ended for this player.</param>
        /// <param name="isOnline">Whether this was a ranked online match — pass false for a friendly online match, which plays over the same real connection but is never ranked.</param>
        /// <param name="opponentRating">The opponent's rating; only used when <paramref name="isOnline"/> is true.</param>
        /// <returns>The signed Elo rating change, or 0 for a non-online match or a guest with no linked account (see <see cref="HasAccount"/>) — a guest never enters the ranking system.</returns>
        public int RecordMatchResult(MatchOutcome outcome, bool isOnline, int opponentRating = 0)
        {
            int delta = 0;
            if (isOnline && HasAccount)
            {
                int previousRating = Rating;
                Rating = EloRating.CalculateNewRating(Rating, opponentRating, outcome);
                delta = Rating - previousRating;
            }

            Stats.RecordMatch(outcome, isOnline);
            UnlockNewlySatisfiedAchievements();
            Changed?.Invoke();
            return delta;
        }

        /// <summary>
        /// Links a permanent email+password identity to the current
        /// (anonymous) account, turning this session's guest into a real
        /// account — see <see cref="HasAccount"/>. Progress already made
        /// this session is untouched; from here on it's tied to an identity
        /// that survives reinstalling the app on another device.
        /// </summary>
        public void LinkEmailPasswordAccount(string email, string password, Action<bool> onCompleted)
        {
            _auth.LinkWithEmailPassword(email, password, success =>
            {
                if (success)
                {
                    Changed?.Invoke();
                }
                onCompleted?.Invoke(success);
            });
        }

        /// <summary>
        /// Links a Google identity to the current (anonymous) account — see
        /// <see cref="LinkEmailPasswordAccount"/> for what that means for
        /// <see cref="HasAccount"/>. <see cref="IAuthService.LinkWithGoogle"/>
        /// only succeeds once a native Google Sign-In plugin is wired in on
        /// the Unity side (see that method's own doc comment); until then
        /// this always reports failure.
        /// </summary>
        public void LinkGoogleAccount(Action<bool> onCompleted)
        {
            _auth.LinkWithGoogle(success =>
            {
                if (success)
                {
                    Changed?.Invoke();
                }
                onCompleted?.Invoke(success);
            });
        }

        /// <summary>Links an Apple identity — see <see cref="LinkGoogleAccount"/>, same caveat with <see cref="IAuthService.LinkWithApple"/>.</summary>
        public void LinkAppleAccount(Action<bool> onCompleted)
        {
            _auth.LinkWithApple(success =>
            {
                if (success)
                {
                    Changed?.Invoke();
                }
                onCompleted?.Invoke(success);
            });
        }

        /// <summary>
        /// Permanently erases this player's account: deletes the saved
        /// cloud profile first (while <see cref="IAuthService.UserId"/> is
        /// still valid), then deletes the identity itself. Required by both
        /// storefronts' account-deletion policies since the game creates
        /// accounts via <see cref="IAuthService"/>.
        /// <see cref="ProfileLoaded"/> and <see cref="Changed"/> are
        /// deliberately not raised — the caller is expected to leave this
        /// session behind (e.g. return to the main menu and start a fresh
        /// anonymous one) rather than keep showing it.
        /// </summary>
        /// <returns>False if either the cloud profile or the identity failed to delete — the account may be left partially deleted; safe to call again.</returns>
        public void DeleteAccount(Action<bool> onCompleted)
        {
            string userId = _auth.UserId;
            _cloudSave.Delete(userId, cloudDeleted =>
            {
                _auth.DeleteAccount(authDeleted =>
                {
                    onCompleted?.Invoke(cloudDeleted && authDeleted);
                });
            });
        }

        /// <summary>
        /// Computes and credits the soft-currency reward for a finished
        /// match of any mode — unlike <see cref="RecordMatchResult"/>, this
        /// runs for <see cref="RewardMatchMode.LocalPvP"/> too, since the
        /// reward always lands in this single session's wallet regardless
        /// of which local color technically won. A guest (see
        /// <see cref="HasAccount"/>) still earns the base "you played a
        /// match" amount, just never the win bonus that requires an
        /// account — see <see cref="MatchRewardCalculator"/> for the exact
        /// formula.
        /// </summary>
        /// <returns>The amount of soft currency actually credited.</returns>
        public int GrantMatchReward(
            RewardMatchMode mode, MatchOutcome outcome, bool resigned, Othello.AI.Difficulty difficulty,
            int playerDiscs, int opponentDiscs, bool forcedEnd)
        {
            int reward = MatchRewardCalculator.CalculateSoftReward(mode, outcome, resigned, difficulty, playerDiscs, opponentDiscs, forcedEnd);
            if (!HasAccount)
            {
                reward = Math.Min(reward, MatchRewardCalculator.MinReward);
            }

            if (reward > 0)
            {
                _monetization.Wallet.Add(CurrencyType.Soft, reward);
                Changed?.Invoke();
            }

            return reward;
        }

        /// <summary>
        /// Unlocks every achievement <see cref="AchievementEvaluator"/> now
        /// considers satisfied but that this session hasn't already
        /// credited, granting each one's reward as it unlocks.
        /// </summary>
        private void UnlockNewlySatisfiedAchievements()
        {
            var satisfiedIds = AchievementEvaluator.GetSatisfiedIds(
                Stats, Rating, _unlockedSkinIds.Count, InventoryCatalog.All.Count, DailyRewardStreak);
            foreach (var id in satisfiedIds)
            {
                if (_unlockedAchievementIds.Contains(id) || !AchievementCatalog.TryFind(id, out var achievement))
                {
                    continue;
                }

                _unlockedAchievementIds.Add(id);
                _monetization.Wallet.Add(CurrencyType.Soft, achievement.RewardSoftCurrency);
                AchievementUnlocked?.Invoke(achievement);
            }
        }

        public void SaveProgress(Action<bool> onCompleted = null)
        {
            var snapshot = new PlayerProfileData
            {
                UserId = _auth.UserId,
                DisplayName = DisplayName,
                AvatarId = AvatarId,
                UnlockedSkinIds = new List<string>(_unlockedSkinIds),
                SoftBalance = _monetization.Wallet.SoftBalance,
                HardBalance = _monetization.Wallet.HardBalance,
                IsNoAdsPurchased = _monetization.IsNoAdsPurchased,
                EquippedPieceSkinId = EquippedPieceSkinId,
                EquippedBoardSkinId = EquippedBoardSkinId,
                Language = _localization.CurrentLanguage,
                Rating = Rating,
                MatchesPlayed = Stats.MatchesPlayed,
                Wins = Stats.Wins,
                Losses = Stats.Losses,
                Draws = Stats.Draws,
                OnlineWins = Stats.OnlineWins,
                CurrentWinStreak = Stats.CurrentWinStreak,
                BestWinStreak = Stats.BestWinStreak,
                UnlockedAchievementIds = new List<string>(_unlockedAchievementIds),
                DailyRewardStreak = DailyRewardStreak,
                LastDailyRewardClaimUtc = _lastDailyRewardClaimUtc
            };

            _cloudSave.Save(_auth.UserId, snapshot, onCompleted);
        }

        private void ApplyProfile(PlayerProfileData data)
        {
            DisplayName = data.DisplayName;
            AvatarId = string.IsNullOrEmpty(data.AvatarId) ? InventoryCatalog.AvatarDefault : data.AvatarId;
            EquippedPieceSkinId = string.IsNullOrEmpty(data.EquippedPieceSkinId)
                ? InventoryCatalog.PieceSkinDefault
                : data.EquippedPieceSkinId;
            EquippedBoardSkinId = string.IsNullOrEmpty(data.EquippedBoardSkinId)
                ? InventoryCatalog.BoardSkinDefault
                : data.EquippedBoardSkinId;
            Rating = data.Rating;
            Stats.Restore(data.MatchesPlayed, data.Wins, data.Losses, data.Draws, data.OnlineWins, data.CurrentWinStreak, data.BestWinStreak);
            DailyRewardStreak = data.DailyRewardStreak;
            _lastDailyRewardClaimUtc = data.LastDailyRewardClaimUtc;

            _unlockedSkinIds.Clear();
            if (data.UnlockedSkinIds != null)
            {
                _unlockedSkinIds.AddRange(data.UnlockedSkinIds);
            }
            EnsureDefaultItemsUnlocked();

            _unlockedAchievementIds.Clear();
            if (data.UnlockedAchievementIds != null)
            {
                _unlockedAchievementIds.AddRange(data.UnlockedAchievementIds);
            }

            _monetization.Wallet.SetBalance(CurrencyType.Soft, data.SoftBalance);
            _monetization.Wallet.SetBalance(CurrencyType.Hard, data.HardBalance);
            _monetization.SetNoAdsPurchased(data.IsNoAdsPurchased);
            _localization.SetLanguage(data.Language);

            // Retroactively grants anything the restored progress already
            // satisfies (e.g. a profile saved before an achievement's
            // condition existed, or before this player's account was linked
            // to the one that saved it), instead of requiring a new match
            // or purchase to notice.
            UnlockNewlySatisfiedAchievements();
        }
    }
}
