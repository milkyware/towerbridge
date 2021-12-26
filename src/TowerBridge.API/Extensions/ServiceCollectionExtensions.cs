using Microsoft.Extensions.DependencyInjection;
using System;
using TowerBridge.API.Options;
using TowerBridge.API.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTowerBridgeService(this IServiceCollection services, Action<TowerBridgeOptions> configureOptions = default)
        {
            if (configureOptions != null)
                services.AddOptions<TowerBridgeOptions>().Configure(configureOptions);

            services.AddMemoryCache();
            services.AddTransient<ITowerBridgeService, TowerBridgeService>();

            return services;
        }
    }
}
