#if OTHELLO_ADMOB_ENABLED
using System;
using GoogleMobileAds.Api;
using Othello.Services;
using UnityEngine;

namespace Othello.Unity
{
    /// <summary>
    /// Real <see cref="IAdsService"/> backed by the Google Mobile Ads Unity
    /// Plugin (AdMob). Drop-in replacement for <see cref="LocalAdsService"/>
    /// — register this instead, in <see cref="MonetizationController.Awake"/>.
    /// Nothing else in the game changes.
    /// <para>
    /// Defaults to Google's own published test ad unit IDs (they always
    /// serve test creatives, never real ones) so the game can be built and
    /// run immediately, before your real AdMob account and its ad units
    /// exist. Pass your real ids to the constructor once you have them —
    /// never ship a build with the test ids, AdMob will flag the account.
    /// </para>
    /// <para>
    /// Loads a fresh ad ahead of time after every show, so the next call to
    /// <see cref="ShowInterstitial"/>/<see cref="ShowRewardedAd"/> already
    /// has one ready instead of loading (and failing silently if the
    /// network is briefly unavailable) at the moment it's needed.
    /// </para>
    /// <para>
    /// This whole file only compiles when <c>OTHELLO_ADMOB_ENABLED</c> is
    /// defined (Edit → Project Settings → Player → Other Settings →
    /// Scripting Define Symbols). It references the Google Mobile Ads SDK,
    /// which isn't imported into the project yet — add the define symbol
    /// once the SDK is imported and the AdMob phase is ready to wire in.
    /// </para>
    /// </summary>
    public sealed class AdMobAdsService : IAdsService
    {
#if UNITY_ANDROID
        private const string DefaultTestInterstitialId = "ca-app-pub-3940256099942544/1033173712";
        private const string DefaultTestRewardedId = "ca-app-pub-3940256099942544/5224354917";
#elif UNITY_IOS
        private const string DefaultTestInterstitialId = "ca-app-pub-3940256099942544/4411468910";
        private const string DefaultTestRewardedId = "ca-app-pub-3940256099942544/1712485313";
#else
        private const string DefaultTestInterstitialId = "unused-editor-platform";
        private const string DefaultTestRewardedId = "unused-editor-platform";
#endif

        private readonly string _interstitialAdUnitId;
        private readonly string _rewardedAdUnitId;

        private InterstitialAd _interstitialAd;
        private RewardedAd _rewardedAd;
        private bool _initialized;

        public AdMobAdsService(string interstitialAdUnitId = DefaultTestInterstitialId, string rewardedAdUnitId = DefaultTestRewardedId)
        {
            _interstitialAdUnitId = interstitialAdUnitId;
            _rewardedAdUnitId = rewardedAdUnitId;

            MobileAds.Initialize(_ =>
            {
                _initialized = true;
                LoadInterstitial();
                LoadRewarded();
            });
        }

        public void ShowInterstitial(Action onClosed = null)
        {
            if (_interstitialAd == null || !_interstitialAd.CanShowAd())
            {
                // No ad ready (still loading, or the previous one failed) —
                // don't block the player waiting for one.
                onClosed?.Invoke();
                return;
            }

            _interstitialAd.OnAdFullScreenContentClosed += HandleClosed;
            _interstitialAd.OnAdFullScreenContentFailed += HandleFailed;
            _interstitialAd.Show();

            void HandleClosed()
            {
                Cleanup();
                LoadInterstitial();
                onClosed?.Invoke();
            }

            void HandleFailed(AdError error)
            {
                Debug.LogWarning($"Interstitial falhou ao exibir: {error}");
                Cleanup();
                LoadInterstitial();
                onClosed?.Invoke();
            }

            void Cleanup()
            {
                _interstitialAd.OnAdFullScreenContentClosed -= HandleClosed;
                _interstitialAd.OnAdFullScreenContentFailed -= HandleFailed;
            }
        }

        public void ShowRewardedAd(Action<bool> onCompleted)
        {
            if (_rewardedAd == null || !_rewardedAd.CanShowAd())
            {
                onCompleted?.Invoke(false);
                return;
            }

            bool earnedReward = false;

            _rewardedAd.OnAdFullScreenContentClosed += HandleClosed;
            _rewardedAd.OnAdFullScreenContentFailed += HandleFailed;
            _rewardedAd.Show(_ => earnedReward = true);

            void HandleClosed()
            {
                Cleanup();
                LoadRewarded();
                onCompleted?.Invoke(earnedReward);
            }

            void HandleFailed(AdError error)
            {
                Debug.LogWarning($"Rewarded ad falhou ao exibir: {error}");
                Cleanup();
                LoadRewarded();
                onCompleted?.Invoke(false);
            }

            void Cleanup()
            {
                _rewardedAd.OnAdFullScreenContentClosed -= HandleClosed;
                _rewardedAd.OnAdFullScreenContentFailed -= HandleFailed;
            }
        }

        private void LoadInterstitial()
        {
            if (!_initialized)
            {
                return;
            }

            InterstitialAd.Load(_interstitialAdUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"Falha ao carregar interstitial: {error}");
                    return;
                }

                _interstitialAd = ad;
            });
        }

        private void LoadRewarded()
        {
            if (!_initialized)
            {
                return;
            }

            RewardedAd.Load(_rewardedAdUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"Falha ao carregar rewarded ad: {error}");
                    return;
                }

                _rewardedAd = ad;
            });
        }
    }
}
#endif
