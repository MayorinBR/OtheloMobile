using System;
using UnityEngine;

namespace Othello.Services
{
    /// <summary>
    /// Local stand-in for a real ad network: no network call, no SDK,
    /// instant "success". Exists so the game, the AI, and the AdTimer can
    /// all be exercised end to end — including in this session's own
    /// automated checks — before a real AdMob/Unity Ads plugin is installed.
    /// Swap this for the real implementation behind <see cref="IAdsService"/>
    /// when that SDK is wired in; nothing else in the game needs to change.
    /// <para>
    /// Logs instead of showing anything on screen — there is no real ad
    /// creative to render here — so testing <see cref="AdTimer"/>/
    /// <see cref="MonetizationController"/> end to end means watching the
    /// Console for these lines rather than expecting a popup in Play mode.
    /// </para>
    /// </summary>
    public sealed class LocalAdsService : IAdsService
    {
        public void ShowInterstitial(Action onClosed = null)
        {
            Debug.Log("[LocalAdsService] Interstitial (mock) exibido.");
            onClosed?.Invoke();
        }

        public void ShowRewardedAd(Action<bool> onCompleted)
        {
            Debug.Log("[LocalAdsService] Rewarded ad (mock) exibido — recompensa concedida.");
            onCompleted?.Invoke(true);
        }
    }
}
