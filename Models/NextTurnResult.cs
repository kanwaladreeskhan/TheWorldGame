using System.Collections.Generic;

namespace GlobalTradeSimulator.Web.Models
{
    public class NextTurnResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TurnNumber { get; set; }
        public string GameMode { get; set; } = string.Empty;
        public double PlayerBalance { get; set; }
        public List<string> Events { get; set; } = new List<string>();
    }
}