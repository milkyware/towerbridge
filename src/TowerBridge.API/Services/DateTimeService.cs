namespace TowerBridge.API.Services
{
    public class DateTimeService : IDateTimeService
    {
        public DateTime GetNow()
        {
            return DateTime.Now;
        }

        public DateTime GetToday()
        {
            return DateTime.Today;
        }
    }
}
