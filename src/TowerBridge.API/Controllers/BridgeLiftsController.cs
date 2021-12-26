using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TowerBridge.API.Models;
using TowerBridge.API.Services;

namespace TowerBridge.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BridgeLiftsController : ControllerBase
    {
        private ILogger _logger;
        private ITowerBridgeService _service;

        public BridgeLiftsController(ILogger<BridgeLiftsController> logger, ITowerBridgeService service)
        {
            _logger = logger;
            _service = service;
        }

        [HttpGet]
        public async Task<IEnumerable<BridgeLift>> GetAll()
        {
            return await _service.GetAllAsync();
        }

        [HttpGet("Next")]
        public async Task<BridgeLift> GetNextAsync()
        {
            return await _service.GetNextAsync();
        }
    }
}
