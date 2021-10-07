using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TowerBridge.API.Models
{
    public class BridgeLift
    {
        public DateTime Date { get; set; }

        public string Vessel { get; set; }

        public string Direction { get; set; }
    }
}
