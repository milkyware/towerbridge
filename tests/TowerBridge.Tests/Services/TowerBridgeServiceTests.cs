using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using TowerBridge.API.Clients;
using TowerBridge.API.Models;
using TowerBridge.Tests.Properties;

namespace TowerBridge.API.Services.Tests
{
    [TestFixture()]
    public class TowerBridgeServiceTests
    {
        private IDateTimeService _dateTimeService;
        private ILogger<TowerBridgeService> _logger;

        [SetUp()]
        public void Setup()
        {
            _dateTimeService = Substitute.For<IDateTimeService>();
            _logger = Substitute.For<ILogger<TowerBridgeService>>();
        }

        private TowerBridgeService CreateService(string sampleResource)
        {
            var towerBridgeClient = Substitute.For<ITowerBridgeClient>();
            var resourceSample = Resources.ResourceManager.GetString(sampleResource);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(resourceSample);
            towerBridgeClient.GetBridgeLiftsPage()
                .Returns(Task.FromResult(htmlDoc));

            return new TowerBridgeService(_dateTimeService, _logger, towerBridgeClient);
        }

        [Test()]
        [TestCase(nameof(Resources.BridgeLiftsScheduled),
            6)]
        [TestCase(nameof(Resources.BridgeLiftsNonScheduled),
            0)]
        public async Task GetAllTest(string sampleResource, int expectedLifts)
        {
            var service = CreateService(sampleResource);
            var lifts = await service.GetAllAsync();

            Assert.That(lifts, Is.Not.Null);
            Assert.That(lifts.Count(), Is.EqualTo(expectedLifts));
        }

        [Test()]
        public async Task GetAllParsesFieldsTest()
        {
            var service = CreateService(nameof(Resources.BridgeLiftsScheduled));
            var lifts = (await service.GetAllAsync()).ToList();

            // 00:00 on 8/10/26 — date composed from the day heading plus the row time.
            var midnight = lifts[0];
            Assert.That(midnight.Date, Is.EqualTo(new DateTime(2026, 10, 8, 0, 0, 0)));
            Assert.That(midnight.Direction, Is.EqualTo(BridgeLiftDirection.DownRiver));
            Assert.That(midnight.VesselType, Is.EqualTo("Paddle Steamer"));
            Assert.That(midnight.Vessel, Is.EqualTo("Dixie Queen"));

            // Row with no published direction.
            var noDirection = lifts.Single(l => l.Date == new DateTime(2026, 10, 8, 16, 15, 0));
            Assert.That(noDirection.Direction, Is.EqualTo(BridgeLiftDirection.Unknown));
            Assert.That(noDirection.VesselType, Is.EqualTo("Marine Towage and Salvage/Tugboat"));
            Assert.That(noDirection.Vessel, Is.EqualTo("VB Boluman"));

            // Row with an empty type and the literal "Vessel" placeholder name.
            var placeholder = lifts.Single(l => l.Date == new DateTime(2026, 10, 10, 14, 45, 0));
            Assert.That(placeholder.Direction, Is.EqualTo(BridgeLiftDirection.UpRiver));
            Assert.That(placeholder.VesselType, Is.Empty);
            Assert.That(placeholder.Vessel, Is.EqualTo("Vessel"));
        }

        [Test()]
        [TestCase(nameof(Resources.BridgeLiftsScheduled),
            "2026-10-07T00:00:00",
            false,
            "2026-10-08T00:00:00",
            Description = "Before the first lift the next lift is the 00:00 on 8/10/26")]
        [TestCase(nameof(Resources.BridgeLiftsScheduled),
            "2026-10-08T01:00:00",
            false,
            "2026-10-08T14:30:00",
            Description = "After the midnight lift the next lift is 14:30 on 8/10/26")]
        [TestCase(nameof(Resources.BridgeLiftsScheduled),
            "2026-10-09T00:00:00",
            false,
            "2026-10-10T09:40:00",
            Description = "Between days the next lift is 09:40 on 10/10/26")]
        [TestCase(nameof(Resources.BridgeLiftsNonScheduled),
            "2026-10-09T00:00:00",
            true,
            Description = "No lifts scheduled")]
        public async Task GetNextAsyncTest(string sampleResource, string date, bool expectedNull, string expectedDate = default)
        {
            _dateTimeService.GetNow()
                .Returns(DateTime.Parse(date));

            var service = CreateService(sampleResource);
            var lift = await service.GetNextAsync();

            if (expectedNull)
            {
                Assert.That(lift, Is.Null);
            }
            else
            {
                Assert.That(lift, Is.Not.Null);
                Assert.That(DateTime.Parse(expectedDate), Is.EqualTo(lift.Date));
            }
        }

        [Test()]
        [TestCase(nameof(Resources.BridgeLiftsScheduled),
            "2026-10-08T00:00:00",
            3,
            Description = "Expects 3 bridge lifts on 8/10/26 including the 00:00 lift")]
        [TestCase(nameof(Resources.BridgeLiftsScheduled),
            "2026-10-09T00:00:00",
            0,
            Description = "Expects 0 bridge lifts on 9/10/26")]
        [TestCase(nameof(Resources.BridgeLiftsScheduled),
            "2026-10-10T00:00:00",
            3,
            Description = "Expects 3 bridge lifts on 10/10/26")]
        [TestCase(nameof(Resources.BridgeLiftsNonScheduled),
            "2026-10-08T00:00:00",
            0,
            Description = "No bridge lifts scheduled")]
        public async Task GetTodayAsyncTest(string sampleResource, string date, int expectedLifts)
        {
            _dateTimeService.GetToday()
                .Returns(DateTime.Parse(date));

            var service = CreateService(sampleResource);
            var lifts = await service.GetTodayAsync();

            Assert.That(lifts, Is.Not.Null);
            Assert.That(expectedLifts, Is.EqualTo(lifts.Count()));
        }
    }
}
