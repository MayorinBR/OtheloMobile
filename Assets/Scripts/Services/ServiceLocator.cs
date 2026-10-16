using System;
using System.Collections.Generic;

namespace Othello.Services
{
    /// <summary>
    /// Small registry mapping a service interface to its current
    /// implementation — the Service Locator half of the project's
    /// Facade/Service Locator design. One instance is created at app start
    /// (see the Unity-side <c>MonetizationController</c>), registered with
    /// the local (mock) services, and later with real ones once the AdMob
    /// and Unity IAP SDKs are wired in — no caller needs to change either time.
    /// </summary>
    public sealed class ServiceLocator
    {
        private readonly Dictionary<Type, object> _services = new();

        public void Register<TService>(TService instance) where TService : class
        {
            _services[typeof(TService)] = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        public TService Resolve<TService>() where TService : class
        {
            if (_services.TryGetValue(typeof(TService), out object service))
            {
                return (TService)service;
            }

            throw new InvalidOperationException($"Nenhum serviço registrado para {typeof(TService).Name}.");
        }

        public bool TryResolve<TService>(out TService service) where TService : class
        {
            if (_services.TryGetValue(typeof(TService), out object raw))
            {
                service = (TService)raw;
                return true;
            }

            service = null;
            return false;
        }
    }
}
