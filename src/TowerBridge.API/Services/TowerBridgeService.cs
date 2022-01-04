using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TowerBridge.API.Models;
using TowerBridge.API.Options;
using Microsoft.Extensions.Options;

namespace TowerBridge.API.Services
{
    public class TowerBridgeService : ITowerBridgeService
    {
        private const string TOWERBRIDGE_CACHE = "TowerBridge";
        private const string TOWERBRIDGE_URL = "https://www.towerbridge.org.uk/lift-times";
        private ILogger _logger;
        private IMemoryCache _memoryCache;
        private TowerBridgeOptions _options;

        public TowerBridgeService(ILogger<TowerBridgeService> logger, IMemoryCache memoryCache, IOptions<TowerBridgeOptions> options)
        {
            _logger = logger;
            _memoryCache = memoryCache;
            _options = options.Value;
        }

        public async Task<IEnumerable<BridgeLift>> GetAllAsync()
        {
            var lifts = await GetLiftsAsync();
            _logger.LogInformation("Returning bridge lifts");
            return lifts;
        }

        public async Task<BridgeLift> GetNextAsync()
        {
            var lifts = await GetLiftsAsync();
            var nextLift = lifts.Where(l => l.Date > DateTime.Now)
                .OrderBy(l => l.Date)
                .FirstOrDefault();
            _logger.LogInformation("Returning next bridge lift");
            return nextLift;
        }

        private async Task<IEnumerable<BridgeLift>> GetLiftsAsync()
        {
            List<BridgeLift> lifts;
            if (!_memoryCache.TryGetValue(TOWERBRIDGE_CACHE, out lifts))
            {
                _logger.LogDebug("Getting tower bridge timetable from site");
                var doc = await new HtmlWeb().LoadFromWebAsync(TOWERBRIDGE_URL);
                _logger.LogTrace($"Timetable HTML:{Environment.NewLine}{doc}", doc.DocumentNode.OuterHtml);


                var nodes = doc.DocumentNode
                    .SelectNodes("//table/tbody/tr");
                _logger.LogDebug($"Timetable count: {{timetableCount}}", nodes.Count);

                lifts = new List<BridgeLift>();
                foreach (var n in nodes)
                {
                    var date = n.SelectSingleNode("./td[@class='views-field views-field-field-date-time-1']/time")
                        .Attributes["datetime"]
                        .Value;
                    var direction = n.SelectSingleNode("./td[@headers='view-field-direction-table-column']")
                        .InnerText.Trim();
                    var vessel = n.SelectSingleNode("./td[@headers='view-field-vessel-table-column']")
                        .InnerText.Trim();

                    var lift = new BridgeLift()
                    {
                        Date = DateTime.Parse(date),
                        Direction = direction,
                        Vessel = vessel
                    };
                    _logger.LogTrace("BridgeLift: {bridgeLift}", lift);
                    lifts.Add(lift);
                }
                _logger.LogDebug("Caching bridge lifts");
                _memoryCache.Set(TOWERBRIDGE_CACHE, lifts, _options.CachingExpiration);
            }
            else
            {
                _logger.LogDebug("Returning cached bridge lifts");
            }
            _logger.LogDebug("Returning all bridge lifts");
            return lifts;
        }
    }
}
