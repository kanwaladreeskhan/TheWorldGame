namespace GlobalTradeSimulator.Models
{
    public class GameState
    {
        public int Id { get; set; }
        public string Mode { get; set; } = "Normal";  // Normal or War
        public int TurnNumber { get; set; }
    }
}
