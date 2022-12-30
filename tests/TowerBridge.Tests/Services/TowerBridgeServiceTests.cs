using HtmlAgilityPack;
using LazyCache;
using LazyCache.Mocks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using TowerBridge.API.Clients;
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
            _logger = Substitute.For<ILogger<TowerBridgeService>>();
        }

        [Test()]
        public async Task GetAllTest()
        {
            var towerBridgeClient = Substitute.For<ITowerBridgeClient>();
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(Resources.BridgeLiftsScheduled);
            towerBridgeClient.GetBridgeLiftsPage()
                .Returns(Task.FromResult(htmlDoc));
            var service = new TowerBridgeService(_dateTimeService, _logger, towerBridgeClient);
            var lifts = await service.GetAllAsync();
            Assert.Greater(lifts.Count(), 0);
        }

        [Test()]
        public async Task GetNextAsyncTest(DateTime date)
        {
            var dateTimeService = Substitute.For<IDateTimeService>();
            dateTimeService.GetNow()
                .Returns(new DateTime(2023, 1, 1));

            var towerBridgeClient = Substitute.For<ITowerBridgeClient>();
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(Resources.BridgeLiftsScheduled);
            towerBridgeClient.GetBridgeLiftsPage()
                .Returns(Task.FromResult(htmlDoc));

            var service = new TowerBridgeService(dateTimeService, _logger, towerBridgeClient);
            var lift = await service.GetNextAsync();
            Assert.NotNull(lift);
        }

        [Test()]
        [TestCase("2023-01-01T00:00:00", 0, TestName = "GetTodayAsyncTest_Lifts0")]
        [TestCase("2023-01-13T00:00:00", 4, TestName = "GetTodayAsyncTest_Lifts4")]
        public async Task GetTodayAsyncTest(string date, int expectedLifts)
        {
            var dateTimeService = Substitute.For<IDateTimeService>();
            dateTimeService.GetToday()
                .Returns(DateTime.Parse(date));

            var towerBridgeClient = Substitute.For<ITowerBridgeClient>();
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(Resources.BridgeLiftsScheduled);
            towerBridgeClient.GetBridgeLiftsPage()
                .Returns(Task.FromResult(htmlDoc));

            var service = new TowerBridgeService(dateTimeService, _logger, towerBridgeClient);
            var lifts = await service.GetTodayAsync();
            Assert.NotNull(lifts);
            Assert.AreEqual(expectedLifts, lifts.Count());
        }
    }
}