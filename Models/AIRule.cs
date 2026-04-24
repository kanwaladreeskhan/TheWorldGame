namespace GlobalTradeSimulator.Models
{
    public class AIRule
    {
        public int RuleId { get; set; }
        public int ResourceId { get; set; }
        public string ConditionType { get; set; } = "";  // LOW or HIGH
        public int Threshold { get; set; }
        public string Action { get; set; } = "";          // BUY or SELL
        public double MinBalance { get; set; }
        public int MaxQuantity { get; set; }
    }
}