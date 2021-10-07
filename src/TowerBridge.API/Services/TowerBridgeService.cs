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
        private string url = "https://www.towerbridge.org.uk/lift-times";

        public async IAsyncEnumerable<BridgeLift> GetAllAsync()
        {
            var doc = new HtmlWeb().Load("https://www.towerbridge.org.uk/lift-times");
            var nodes = doc.DocumentNode
                .SelectNodes("//table/tbody/tr");

            var lifts = new List<BridgeLift>();
            foreach (var n in nodes)
            {
                var date = n.SelectSingleNode("./td[@class='views-field views-field-field-date-time-1']/time")
                    .Attributes["datetime"]
                    .Value;
                var direction = n.SelectSingleNode("./td[@headers='view-field-direction-table-column']")
                    .InnerText;
                var vessel = n.SelectSingleNode("./td[@headers='view-field-vessel-table-column']")
                    .InnerText;

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
                .OrderBy(l => l.Date)
                .FirstOrDefaultAsync();
        }
    }
}
