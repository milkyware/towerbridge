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
using TowerBridge.API.Clients;

namespace TowerBridge.API.Services
{
    public class TowerBridgeService : ITowerBridgeService
    {
        private const string TOWERBRIDGE_NO_LIFTS_PATH = "//div[@class='view-empty']";
        private const string TOWERBRIDGE_TABLE_PATH = "//div[@class='view-content']/table/tbody/tr";

        private IDateTimeService _dateTimeService;
        private ILogger _logger;
        private ITowerBridgeClient _towerBridgeClient;

        public TowerBridgeService(IDateTimeService dateTimeService, ILogger<TowerBridgeService> logger, ITowerBridgeClient towerBridgeClient)
        {
            _dateTimeService = dateTimeService;
            _logger = logger;
            _towerBridgeClient = towerBridgeClient;
        }

        public async Task<IEnumerable<BridgeLift>> GetAllAsync()
        {
            var lifts = await GetLiftsAsync();
            _logger.LogInformation("Returning all bridge lifts");
            return lifts;
        }

        public async Task<BridgeLift> GetNextAsync()
        {
            var lifts = await GetLiftsAsync();
            var now = _dateTimeService.GetNow();
            var nextLift = lifts.Where(l => l.Date > now)
                .OrderBy(l => l.Date)
                .FirstOrDefault();
            _logger.LogInformation("Returning next bridge lift");
            return nextLift;
        }

        public async Task<IEnumerable<BridgeLift>> GetTodayAsync()
        {
            var lifts = await GetLiftsAsync();
            var today = _dateTimeService.GetToday();
            var todayLifts = lifts.Where(l => l.Date > DateTime.Today && l.Date < DateTime.Today.AddDays(1))
                .OrderBy(l => l.Date);
            _logger.LogInformation("Returning todays bridge lifts");
            return todayLifts;

        }

        private async Task<IEnumerable<BridgeLift>> GetLiftsAsync()
        {
            var htmlDoc = await _towerBridgeClient.GetBridgeLiftsPage();

            var lifts = new List<BridgeLift>();

            _logger.LogTrace($"Checking if any bridge lifts scheduled");
            if (htmlDoc.DocumentNode.SelectNodes(TOWERBRIDGE_NO_LIFTS_PATH) != null)
            {
                _logger.LogWarning($"No bridge lifts scheduled");
                return lifts;
            }

            _logger.LogTrace($"Querying timetable nodes");
            var nodes = htmlDoc.DocumentNode
                .SelectNodes(TOWERBRIDGE_TABLE_PATH);
            _logger.LogDebug($"Timetable count: {nodes.Count}");

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
            return lifts;
        }
    }
}
