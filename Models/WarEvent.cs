namespace GlobalTradeSimulator.Models
{
    public class WarEvent
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = "";
        public string Description { get; set; } = "";
        public int AffectedResourceId { get; set; }
        public double PriceMultiplier { get; set; } = 1.0;
        public int SupplyDrop { get; set; }
        public int DemandBoost { get; set; }
        public bool IsActive { get; set; }
    }
}
