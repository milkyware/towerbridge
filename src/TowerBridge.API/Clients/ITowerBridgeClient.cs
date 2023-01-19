using HtmlAgilityPack;

namespace TowerBridge.API.Clients
{
    public interface ITowerBridgeClient
    {
        public Task<HtmlDocument> GetBridgeLiftsPage();
    }
}
