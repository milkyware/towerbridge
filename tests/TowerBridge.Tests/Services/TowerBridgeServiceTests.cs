using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TowerBridge.API.Options;
using TowerBridge.API.Services;

namespace TowerBridge.API.Services.Tests
{
    [TestFixture()]
    public class TowerBridgeServiceTests
    {
        private ITowerBridgeService _service;

        [SetUp()]
        public void Setup()
        {
            var logger = Substitute.For<ILogger<TowerBridgeService>>();
            var memoryCache = Substitute.For<IMemoryCache>();
            memoryCache.TryGetValue(Arg.Any<string>(), out Arg.Any<object>())
                .Returns(false);
            var options = Substitute.For<IOptions<TowerBridgeOptions>>();
            options.Value.Returns(new TowerBridgeOptions());
            _service = new TowerBridgeService(logger, memoryCache, options);
        }

        [Test()]
        public async Task GetAllTest()
        {
            var lifts = await _service.GetAllAsync();
            Assert.Greater(lifts.Count(), 0);
        }

        [Test()]
        public async Task GetNextAsyncTest()
        {
            var lift = await _service.GetNextAsync();
            Assert.NotNull(lift);
        }
    }
}