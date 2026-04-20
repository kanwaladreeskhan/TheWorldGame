namespace GlobalTradeSimulator.Models {
    public class Player {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int CountryId { get; set; }
        public double Balance { get; set; }
    }
}