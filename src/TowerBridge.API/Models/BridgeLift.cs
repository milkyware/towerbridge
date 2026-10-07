using System;

namespace TowerBridge.API.Models
{
    public class BridgeLift
    {
        public DateTime Date { get; set; }

        public string Vessel { get; set; }

        public string VesselType { get; set; }

        public BridgeLiftDirection Direction { get; set; }
    }
}
