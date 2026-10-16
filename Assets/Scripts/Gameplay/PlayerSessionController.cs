using Othello.Services;
using UnityEngine;

namespace Othello.Unity
{
    /// <summary>
    /// Signs the player in and restores their saved profile on top of
    /// <see cref="MonetizationController"/>'s state. Registers
    /// <see cref="IAuthService"/>/<see cref="ICloudSaveService"/> into the
    /// same <see cref="ServiceLocator"/> the monetization services use, so
    /// there is exactly one locator per app rather than one per system.
    /// <para>
    /// Runs its setup in <see cref="Start"/>, not <see cref="Awake"/>: Unity
    /// only guarantees every object's Awake has run before any object's
    /// Start runs, and this component depends on
    /// <see cref="MonetizationController"/> having already created its
    /// <see cref="MonetizationController.State"/> and
    /// <see cref="MonetizationController.Services"/>, and on
    /// <see cref="LocalizationController"/> having already created its
    /// <see cref="LocalizationController.Service"/>, in their own Awake.
    /// </para>
    /// </summary>
    public sealed class PlayerSessionController : MonoBehaviour
    {
        [SerializeField]
        private MonetizationController _monetization;

        [SerializeField]
        private LocalizationController _localization;

        public PlayerSession Session { get; private set; }

        private void Start()
        {
            if (_monetization == null || _localization == null)
            {
                Debug.LogError("PlayerSessionController precisa de _monetization e _localization atribuídos no Inspector.");
                return;
            }

            _monetization.Services.Register<IAuthService>(new FirebaseAuthService());
            _monetization.Services.Register<ICloudSaveService>(new FirebaseCloudSaveService());

            var auth = _monetization.Services.Resolve<IAuthService>();
            var cloudSave = _monetization.Services.Resolve<ICloudSaveService>();

            Session = new PlayerSession(auth, cloudSave, _monetization.State, _localization.Service);
            Session.Start(ready =>
            {
                Debug.Log(ready
                    ? $"Sessão pronta. UserId: {auth.UserId}, moedas: soft={_monetization.State.Wallet.SoftBalance} hard={_monetization.State.Wallet.HardBalance}"
                    : "Falha ao entrar (sign-in anônimo não completou).");
            });
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                Session?.SaveProgress();
            }
        }

        private void OnApplicationQuit()
        {
            Session?.SaveProgress();
        }
    }
}
