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
using LazyCache;

namespace TowerBridge.API.Services
{
    public class TowerBridgeService : ITowerBridgeService
    {
        private const string TOWERBRIDGE_CACHE = "TowerBridge";
        private const string TOWERBRIDGE_URL = "https://www.towerbridge.org.uk/lift-times";
        private const string TOWERBRIDGE_NO_LIFTS_PATH = "//div[@class='view-empty']";
        private const string TOWERBRIDGE_TABLE_PATH = "//div[@class='view-content']/table/tbody/tr";
        private IAppCache _appCache;
        private ILogger _logger;
        private TowerBridgeOptions _options;

        public TowerBridgeService(ILogger<TowerBridgeService> logger, IAppCache appCache, IOptions<TowerBridgeOptions> options)
        {
            _appCache = appCache;
            _logger = logger;
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

        public async Task<IEnumerable<BridgeLift>> GetTodayAsync()
        {
            var lifts = await GetLiftsAsync();
            var today = lifts.Where(l => l.Date > DateTime.Today && l.Date < DateTime.Today.AddDays(1))
                .OrderBy(l => l.Date);
            _logger.LogInformation("Returning todays bridge lifts");
            return today;

        }

        private async Task<IEnumerable<BridgeLift>> GetLiftsAsync()
        {
            var result = await _appCache.GetOrAddAsync(TOWERBRIDGE_CACHE, async () =>
            {
                List<BridgeLift> lifts;
                _logger.LogTrace("Getting tower bridge timetable from site");
                var doc = await new HtmlWeb().LoadFromWebAsync(TOWERBRIDGE_URL);
                _logger.LogDebug($"Timetable HTML:{Environment.NewLine}{doc.Text}");

                lifts = new List<BridgeLift>();

                _logger.LogTrace($"Checking if any bridge lifts scheduled");
                if (doc.DocumentNode.SelectNodes(TOWERBRIDGE_NO_LIFTS_PATH).Any())
                {
                    _logger.LogWarning($"No bridge lifts scheduled");
                    return lifts;
                }


                _logger.LogTrace($"Querying timetable nodes");
                var nodes = doc.DocumentNode
                    .SelectNodes(TOWERBRIDGE_TABLE_PATH);
                _logger.LogDebug($"Timetable count: {nodes.Count}", nodes.Count);

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
                    _logger.LogDebug($"BridgeLift: {lift}");
                    lifts.Add(lift);
                }
                _logger.LogInformation("Caching bridge lifts");
                return lifts;
            });

            return result;
        }
    }
}
