using HtmlAgilityPack;
using LazyCache;
using LazyCache.Mocks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using TowerBridge.API.Clients;
using TowerBridge.API.Models;
using TowerBridge.API.Options;
using TowerBridge.API.Services;
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

        [Test()]
        [TestCase(nameof(Resources.BridgeLiftsScheduled),
            6)]
        [TestCase(nameof(Resources.BridgeLiftsNonScheduled),
            0)]
        public async Task GetAllTest(string sampleResource, int expectedLifts)
        {
            var towerBridgeClient = Substitute.For<ITowerBridgeClient>();
            var resourceSample = Resources.ResourceManager.GetString(sampleResource);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(resourceSample);
            towerBridgeClient.GetBridgeLiftsPage()
                .Returns(Task.FromResult(htmlDoc));

            var service = new TowerBridgeService(_dateTimeService, _logger, towerBridgeClient);
            var lifts = await service.GetAllAsync();

            Assert.NotNull(lifts);
            Assert.AreEqual(expectedLifts, lifts.Count());
        }

        [Test()]
        [TestCase(nameof(Resources.BridgeLiftsScheduled), 
            "2023-01-01T00:00:00", 
            false, 
            "2023-01-13T18:30:00", 
            Description = "On 1/1/23 the next lift is 13/1/23 18:30")]
        [TestCase(nameof(Resources.BridgeLiftsScheduled), 
            "2023-01-14T00:00:00",
            false, 
            "2023-01-14T22:15:00", 
            Description = "On 14/1/23 the next lift is 14/1/23 22:15")]
        [TestCase(nameof(Resources.BridgeLiftsNonScheduled), 
            "2023-01-14T00:00:00",
            true, 
            Description = "On 14/1/23 the next lift is 14/1/23 22:15")]
        public async Task GetNextAsyncTest(string sampleResource, string date, bool expectedNull, string expectedDate = default)
        {
            _dateTimeService.GetNow()
                .Returns(DateTime.Parse(date));

            var towerBridgeClient = Substitute.For<ITowerBridgeClient>();
            var resourceSample = Resources.ResourceManager.GetString(sampleResource);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(resourceSample);
            towerBridgeClient.GetBridgeLiftsPage()
                .Returns(Task.FromResult(htmlDoc));

            var service = new TowerBridgeService(_dateTimeService, _logger, towerBridgeClient);
            var lift = await service.GetNextAsync();

            if (expectedNull)
            {
                Assert.IsNull(lift);
            }
            else
            {
                Assert.NotNull(lift);
                Assert.AreEqual(DateTime.Parse(expectedDate), lift.Date);
            }
        }

        [Test()]
        [TestCase(nameof(Resources.BridgeLiftsScheduled), 
            "2023-01-01T00:00:00", 
            0, 
            Description = "Expects 0 bridge lifts for 1/1/23")]
        [TestCase(nameof(Resources.BridgeLiftsScheduled), 
            "2023-01-13T00:00:00", 
            4, 
            Description = "Expects 4 bridge lifts for 13/1/23")]
        [TestCase(nameof(Resources.BridgeLiftsNonScheduled),
            "2023-01-01T00:00:00",
            0,
            Description = "No bridge lifts scheduled for 13/1/23")]
        public async Task GetTodayAsyncTest(string sampleResource, string date, int expectedLifts)
        {
            _dateTimeService.GetToday()
                .Returns(DateTime.Parse(date));

            var towerBridgeClient = Substitute.For<ITowerBridgeClient>();
            var resourceSample = Resources.ResourceManager.GetString(sampleResource);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(resourceSample);
            towerBridgeClient.GetBridgeLiftsPage()
                .Returns(Task.FromResult(htmlDoc));

            var service = new TowerBridgeService(_dateTimeService, _logger, towerBridgeClient);
            var lifts = await service.GetTodayAsync();
            Assert.NotNull(lifts);
            Assert.AreEqual(expectedLifts, lifts.Count());
        }
    }
}