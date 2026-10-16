using Othello.Services;
using UnityEngine;

namespace Othello.Unity
{
    /// <summary>
    /// Builds the <see cref="ShopService"/> the first time it's asked for,
    /// not in <see cref="Awake"/> or <see cref="Start"/>: it depends on both
    /// <see cref="MonetizationController"/> and
    /// <see cref="PlayerSessionController"/> having finished their own
    /// <c>Start</c>, and Unity does not guarantee the order <c>Start</c> runs
    /// in across different components on possibly different GameObjects.
    /// Lazily building it on first access — realistically, when the player
    /// opens the shop panel, well after scene load — sidesteps that
    /// ordering question entirely instead of fighting Script Execution Order settings.
    /// </summary>
    public sealed class ShopController : MonoBehaviour
    {
        [SerializeField]
        private MonetizationController _monetization;

        [SerializeField]
        private PlayerSessionController _playerSession;

        private ShopService _shopService;

        public ShopService GetShopService()
        {
            if (_shopService == null)
            {
                var iap = _monetization.Services.Resolve<IIAPService>();
                _shopService = new ShopService(_monetization.State.Wallet, iap, _playerSession.Session);
            }

            return _shopService;
        }
    }
}
