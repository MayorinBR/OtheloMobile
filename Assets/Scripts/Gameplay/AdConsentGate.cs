using System;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace Othello.Unity
{
    /// <summary>
    /// Runs Google's User Messaging Platform (UMP) consent flow before the
    /// real ad service is allowed to initialize — required by AdMob's own
    /// policies to ask players in the EEA/UK for consent to personalized
    /// ads before requesting one. See <see cref="MonetizationController.Awake"/>,
    /// which registers a mock <see cref="LocalAdsService"/> immediately and
    /// swaps in the real <see cref="AdMobAdsService"/> once <see cref="Request"/>
    /// calls back.
    /// <para>
    /// For most players — outside the EEA/UK, or on a later launch after a
    /// decision was already recorded — <see cref="ConsentForm.LoadAndShowConsentFormIfRequired"/>
    /// determines no form is needed and this resolves within a frame or
    /// two with nothing shown.
    /// </para>
    /// <para>
    /// Fails open: any UMP error is logged, not surfaced as a blocked ad
    /// experience — a consent-service hiccup shouldn't be able to stop the
    /// player from ever seeing an ad again.
    /// </para>
    /// </summary>
    public static class AdConsentGate
    {
        public static void Request(Action onReady)
        {
            var request = new ConsentRequestParameters();

            ConsentInformation.Update(request, updateError =>
            {
                if (updateError != null)
                {
                    Debug.LogWarning($"UMP: falha ao atualizar informações de consentimento: {updateError.Message}");
                    onReady?.Invoke();
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    if (formError != null)
                    {
                        Debug.LogWarning($"UMP: falha ao carregar/exibir o formulário de consentimento: {formError.Message}");
                    }

                    onReady?.Invoke();
                });
            });
        }
    }
}
