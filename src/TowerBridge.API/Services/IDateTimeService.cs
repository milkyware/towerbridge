namespace TowerBridge.API.Services
{
    public interface IDateTimeService
    {
        public DateTime GetNow();

        public DateTime GetToday();
    }
}
