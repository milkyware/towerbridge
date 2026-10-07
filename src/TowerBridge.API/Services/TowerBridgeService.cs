using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TowerBridge.API.Clients;
using TowerBridge.API.Models;

namespace TowerBridge.API.Services
{
    public class TowerBridgeService : ITowerBridgeService
    {
        // The timetable is one .time-table block per day, each carrying a heading with
        // the full date and containing .bridge-lift-row entries for that day.
        private const string TOWERBRIDGE_DAY_PATH =
            "//div[contains(concat(' ', normalize-space(@class), ' '), ' time-table ')]";
        private const string TOWERBRIDGE_DAY_HEADING_PATH =
            "./div[contains(@class, 'time-table__header')]//h3[contains(@class, 'time-table__heading')]";
        private const string TOWERBRIDGE_ROW_PATH =
            ".//div[contains(@class, 'bridge-lift-row__content')]";

        private const string DATE_FORMAT = "dddd d MMMM yyyy";
        private const string TIME_FORMAT = @"hh\:mm";

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
            var todayLifts = lifts.Where(l => l.Date >= today && l.Date < today.AddDays(1))
                .OrderBy(l => l.Date);
            _logger.LogInformation("Returning todays bridge lifts");
            return todayLifts;
        }

        private async Task<IEnumerable<BridgeLift>> GetLiftsAsync()
        {
            _logger.LogTrace("Getting Tower Bridge lifts page");
            var htmlDoc = await _towerBridgeClient.GetBridgeLiftsPage();

            var lifts = ParseLifts(htmlDoc);

            _logger.LogInformation($"Returning {nameof(GetLiftsAsync)}");
            return lifts;
        }

        private IEnumerable<BridgeLift> ParseLifts(HtmlDocument htmlDoc)
        {
            var lifts = new List<BridgeLift>();

            _logger.LogTrace("Querying timetable days");
            var dayNodes = htmlDoc.DocumentNode.SelectNodes(TOWERBRIDGE_DAY_PATH);

            // No day blocks (or no rows at all) simply means nothing is scheduled.
            if (dayNodes == null)
            {
                _logger.LogWarning("No bridge lifts scheduled");
                return lifts;
            }

            foreach (var dayNode in dayNodes)
            {
                var date = ParseDay(dayNode);
                if (date == null)
                {
                    continue;
                }

                var rowNodes = dayNode.SelectNodes(TOWERBRIDGE_ROW_PATH);
                if (rowNodes == null)
                {
                    continue;
                }

                _logger.LogDebug($"Timetable count for {date.Value:d MMMM yyyy}: {rowNodes.Count}");

                foreach (var rowNode in rowNodes)
                {
                    var lift = ParseLift(rowNode, date.Value);
                    if (lift != null)
                    {
                        _logger.LogDebug($"BridgeLift: {lift}");
                        lifts.Add(lift);
                    }
                }
            }

            return lifts;
        }

        private DateTime? ParseDay(HtmlNode dayNode)
        {
            var heading = dayNode.SelectSingleNode(TOWERBRIDGE_DAY_HEADING_PATH)?.InnerText?.Trim();
            if (string.IsNullOrEmpty(heading) ||
                !DateTime.TryParseExact(heading, DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                _logger.LogWarning($"Could not parse bridge lift day heading '{heading}'");
                return null;
            }

            return date;
        }

        private BridgeLift? ParseLift(HtmlNode rowNode, DateTime date)
        {
            // Each row is three paragraphs: "<time> <direction>", the vessel type and
            // the vessel name.
            var paragraphs = rowNode.SelectNodes("./p");
            if (paragraphs == null || paragraphs.Count < 3)
            {
                _logger.LogWarning($"Skipping malformed bridge lift row '{rowNode.InnerText.Trim()}'");
                return null;
            }

            var timeText = paragraphs[0].SelectSingleNode("./strong")?.InnerText?.Trim();
            if (string.IsNullOrEmpty(timeText) ||
                !TimeSpan.TryParseExact(timeText, TIME_FORMAT, CultureInfo.InvariantCulture, out var time))
            {
                _logger.LogWarning($"Skipping bridge lift row with unparsable time '{timeText}'");
                return null;
            }

            return new BridgeLift
            {
                Date = date.Add(time),
                Direction = ParseDirection(paragraphs[0].InnerText, timeText),
                VesselType = paragraphs[1].InnerText?.Trim() ?? string.Empty,
                Vessel = (paragraphs[2].SelectSingleNode("./strong")?.InnerText ?? paragraphs[2].InnerText)?.Trim() ?? string.Empty
            };
        }

        private static BridgeLiftDirection ParseDirection(string paragraphText, string timeText)
        {
            var direction = paragraphText.Replace(timeText, string.Empty).Trim();
            return direction switch
            {
                "Upriver" or "Up river" => BridgeLiftDirection.UpRiver,
                "Downriver" or "Down river" => BridgeLiftDirection.DownRiver,
                _ => BridgeLiftDirection.Unknown
            };
        }
    }
}
