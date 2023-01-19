using HtmlAgilityPack;
using LazyCache;
using Microsoft.Extensions.Options;
using TowerBridge.API.Options;
using TowerBridge.API.Services;

namespace TowerBridge.API.Clients
{
    public class TowerBridgeClient : ITowerBridgeClient
    {
        private const string TOWERBRIDGE_CACHE = "TowerBridge";
        private const string TOWERBRIDGE_URL = "https://www.towerbridge.org.uk/lift-times";

        private IAppCache _cache;
        private ILogger _logger;
        private IOptions<TowerBridgeOptions> _options;

        public TowerBridgeClient(IAppCache cache, ILogger<TowerBridgeClient> logger, IOptions<TowerBridgeOptions> options)
        {
            _cache = cache;
            _logger = logger;
            _options = options;
        }

        public Task<HtmlDocument> GetBridgeLiftsPage()
        {
            var result = _cache.GetOrAddAsync(TOWERBRIDGE_CACHE, async () =>
            {
                _logger.LogTrace("Getting tower bridge timetable from site");
                var htmlDoc = await new HtmlWeb().LoadFromWebAsync(TOWERBRIDGE_URL);
                _logger.LogDebug($"htmlDoc={Environment.NewLine}{htmlDoc.Text}");


                _logger.LogInformation("Caching bridge lifts");
                return htmlDoc;
            }, _options.Value.CachingExpiration);

            _logger.LogInformation($"Returning {nameof(GetBridgeLiftsPage)}");
            return result;
        }
    }
}
