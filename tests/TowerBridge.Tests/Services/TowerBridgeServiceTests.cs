using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TowerBridge.API.Services;

namespace TowerBridge.API.Services.Tests
{
    [TestFixture()]
    public class TowerBridgeServiceTests
    {
        private ITowerBridgeService service;

        [SetUp()]
        public void Setup()
        {
            service = new TowerBridgeService();
        }

        [Test()]
        public async Task GetAllTest()
        {
            var lifts = await service.GetAllAsync()
                .ToListAsync();
            Assert.Greater(lifts.Count(), 0);
        }

        [Test()]
        public async Task GetNextAsyncTest()
        {
            var lift = await service.GetNextAsync();
            Assert.NotNull(lift);
        }
    }
}