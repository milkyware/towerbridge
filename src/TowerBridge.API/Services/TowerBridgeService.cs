using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TowerBridge.API.Models;

namespace TowerBridge.API.Services
{
    public class TowerBridgeService : ITowerBridgeService
    {
        private const string TOWERBRIDGE_URL = "https://www.towerbridge.org.uk/lift-times";

        public async IAsyncEnumerable<BridgeLift> GetAllAsync()
        {
            var doc = new HtmlWeb().Load(TOWERBRIDGE_URL);
            var nodes = doc.DocumentNode
                .SelectNodes("//table/tbody/tr");

            var lifts = new List<BridgeLift>();
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
                yield return lift;
            }
        }

        public async Task<BridgeLift> GetNextAsync()
        {
            return await GetAllAsync()
                .Where(l => l.Date > DateTime.Now)
                .OrderBy(l => l.Date)
                .FirstOrDefaultAsync();
        }
    }
}
