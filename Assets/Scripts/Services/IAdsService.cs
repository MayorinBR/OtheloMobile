using System;

namespace Othello.Services
{
    /// <summary>
    /// Facade over whichever ad network SDK ends up wired in (AdMob, Unity
    /// Ads). Gameplay code — <see cref="AdTimer"/> included — only ever
    /// talks to this interface, so swapping the real SDK in later is a
    /// one-class change instead of a search-and-replace through the game.
    /// </summary>
    public interface IAdsService
    {
        /// <summary>Shows an interstitial ad. Calls <paramref name="onClosed"/> once the player dismisses it.</summary>
        void ShowInterstitial(Action onClosed = null);

        /// <summary>
        /// Shows a rewarded ad. <paramref name="onCompleted"/> receives true
        /// only if the player watched it through and earned the reward.
        /// </summary>
        void ShowRewardedAd(Action<bool> onCompleted);
    }
}
