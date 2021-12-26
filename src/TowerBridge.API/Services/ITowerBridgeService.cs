using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TowerBridge.API.Models;

namespace TowerBridge.API.Services
{
    public interface ITowerBridgeService
    {
        public Task<BridgeLift> GetNextAsync();

        public Task<IEnumerable<BridgeLift>> GetAllAsync();
    }
}
