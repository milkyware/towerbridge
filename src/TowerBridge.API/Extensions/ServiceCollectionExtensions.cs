using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using System;
using TowerBridge.API.Clients;
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

            services.AddSingleton<HtmlWeb>();
            services.AddLazyCache();
            services.AddTransient<ITowerBridgeClient, TowerBridgeClient>();
            services.AddTransient<ITowerBridgeService, TowerBridgeService>();

            return services;
        }
    }
}
