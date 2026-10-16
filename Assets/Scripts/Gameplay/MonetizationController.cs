using Othello.Core;
using Othello.Services;
using UnityEngine;

namespace Othello.Unity
{
    /// <summary>
    /// Wires the monetization services into the scene: registers the local
    /// (mock) <see cref="IAdsService"/>/<see cref="IIAPService"/> into a
    /// <see cref="ServiceLocator"/>, drives the <see cref="AdTimer"/> every
    /// frame, and shows an interstitial when a match ends and enough active
    /// play time has accumulated — unless <see cref="MonetizationState.IsNoAdsPurchased"/>
    /// is set, per the project's ad-timer design.
    /// <para>
    /// <see cref="IIAPService"/> is wired to its real implementation
    /// (<see cref="UnityIAPService"/>) immediately — nothing in the game
    /// references <see cref="LocalIAPService"/> anymore. <see cref="IAdsService"/>
    /// starts as the <see cref="LocalAdsService"/> mock and is swapped for
    /// the real <see cref="AdMobAdsService"/> once <see cref="AdConsentGate"/>
    /// confirms consent has been handled — see <see cref="Awake"/>.
    /// </para>
    /// </summary>
    public sealed class MonetizationController : MonoBehaviour
    {
        // Real, production AdMob ad unit IDs. Deliberately NOT passed to
        // AdMobAdsService below — every build made from this source,
        // Editor play sessions included, requests Google's own published
        // test ads instead (AdMobAdsService's own default) until this
        // controller passes these two constants explicitly, right before
        // building the release AAB. Testing with the real IDs generates
        // invalid traffic against your own AdMob account and risks
        // suspension before the game even ships.
        private const string ProductionInterstitialAdUnitId = "ca-app-pub-4738008393657320/9790428328";
        private const string ProductionRewardedAdUnitId = "ca-app-pub-4738008393657320/8826042463";

        [SerializeField]
        private GameController _gameController;

        [SerializeField]
        private float _interstitialIntervalMinutes = 3f;

        private AdTimer _adTimer;
        private OthelloGame _subscribedGame;

        public MonetizationState State { get; private set; }
        public ServiceLocator Services { get; private set; }

        private void Awake()
        {
            State = new MonetizationState();
            Services = new ServiceLocator();

            var fulfillment = new PurchaseFulfillment(State);

            // Placeholder until the UMP consent flow below resolves and
            // replaces it — nothing that calls Services.Resolve<IAdsService>()
            // has to wait or null-check for this.
            Services.Register<IAdsService>(new LocalAdsService());

            // AdMobAdsService (see its own doc comment) only compiles when
            // OTHELLO_ADMOB_ENABLED is defined — today that's Android only,
            // since the Google Mobile Ads SDK doesn't target Standalone at
            // all. Editor sessions and Standalone builds keep the mock
            // registered above so the project still compiles and runs there.
#if OTHELLO_ADMOB_ENABLED
            AdConsentGate.Request(() => Services.Register<IAdsService>(new AdMobAdsService()));
#endif
            Services.Register<IIAPService>(new UnityIAPService(fulfillment));

            _adTimer = new AdTimer(_interstitialIntervalMinutes);
        }

        private void Start()
        {
            Services.Resolve<IIAPService>().Initialize();

            // GameController no longer creates its Game at Awake time — a
            // match only exists once the player picks a mode — so this
            // subscribes per match via MatchStarted instead of once here.
            if (_gameController != null)
            {
                _gameController.MatchStarted += OnMatchStarted;
            }
        }

        private void OnMatchStarted()
        {
            if (_subscribedGame != null)
            {
                _subscribedGame.GameEnded -= OnMatchEnded;
            }

            _subscribedGame = _gameController.Game;
            _subscribedGame.GameEnded += OnMatchEnded;
        }

        private void Update()
        {
            if (!State.IsNoAdsPurchased)
            {
                _adTimer.Tick(Time.deltaTime);
            }
        }

        private void OnMatchEnded()
        {
            if (State.IsNoAdsPurchased || !_adTimer.IsDue)
            {
                return;
            }

            Services.Resolve<IAdsService>().ShowInterstitial();
            _adTimer.Reset();
        }
    }
}
