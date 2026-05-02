using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using GlobalTradeSimulator.DataAccess;

namespace GlobalTradeSimulator.Services
{
    public class GameEngine
    {
        private readonly AIService _aiService;
        private readonly WarService _warService;
        private readonly GameStateRepository _repo;
        private readonly string _connString = "Server=.\\LAB;Database=gameDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public GameEngine()
        {
            _aiService = new AIService();
            _warService = new WarService();
            _repo = new GameStateRepository();
        }

        /// <summary>
        /// MAIN GAME LOOP — Called when user clicks "Next Turn"
        /// </summary>
        public NextTurnResult NextTurn(int playerId)
        {
            var result = new NextTurnResult();
            var gameState = _repo.GetGameState();

            Console.WriteLine($"\n{'=' * 60}");
            Console.WriteLine($"⏩ NEXT TURN: {gameState.TurnNumber + 1} | Mode: {gameState.Mode}");
            Console.WriteLine($"{'=' * 60}");

            try
            {
                // 1. Increment turn
                _repo.IncrementTurn();
                gameState.TurnNumber++;

                // 2. Process War Scenario (may change mode)
                string warMessage = _warService.ProcessWarScenario();
                if (!string.IsNullOrEmpty(warMessage))
                    result.Events.Add(warMessage);

                // Refresh game state (may have changed due to war)
                gameState = _repo.GetGameState();

                // 3. Process ALL AI countries
                Console.WriteLine("\n🤖 --- AI Phase ---");
                _aiService.ProcessAllAI();
                result.Events.Add("AI countries completed their trades.");

                // 4. Update Market Prices (basic version - Module 3 can enhance)
                Console.WriteLine("\n📊 --- Market Update Phase ---");
                string marketMessage = UpdateMarketPrices(gameState.Mode);
                result.Events.Add(marketMessage);

                // 5. Get updated player data
                result.PlayerBalance = GetPlayerBalance(playerId);
                result.TurnNumber = gameState.TurnNumber;
                result.GameMode = gameState.Mode;

                Console.WriteLine($"\n✅ Turn {gameState.TurnNumber} Complete | Mode: {gameState.Mode}");
            }
            catch (Exception ex)
            {
                result.Events.Add($"❌ Error: {ex.Message}");
                Console.WriteLine($"❌ GameEngine Error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Basic market price update based on supply/demand dynamics
        /// (This is a simplified version — Module 3 can build the full MarketService)
        /// </summary>
        private string UpdateMarketPrices(string gameMode)
        {
            using var conn = new SqlConnection(_connString);
            conn.Open();

            // Price formula: NewPrice = BasePrice + (Demand - Supply) * 0.1
            // War mode: extra 20% price volatility
            double volatilityMultiplier = gameMode == "War" ? 1.2 : 1.0;

            string sql = @"
                UPDATE MarketPrices 
                SET CurrentPrice = CASE 
                    WHEN CurrentPrice + ((Demand - Supply) * 0.1 * @volatility) > 10 
                    THEN CurrentPrice + ((Demand - Supply) * 0.1 * @volatility)
                    ELSE 10 
                END,
                -- Simulate natural supply changes
                Supply = CASE 
                    WHEN Supply + @supplyChange > 0 
                    THEN Supply + @supplyChange 
                    ELSE 1 
                END,
                Demand = CASE 
                    WHEN Demand + @demandChange > 0 
                    THEN Demand + @demandChange 
                    ELSE 1 
                END
            ";

            // Random supply/demand shifts (more volatile in war)
            int supplyChange = gameMode == "War" ? -15 : -5;
            int demandChange = gameMode == "War" ? 20 : 5;

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@volatility", volatilityMultiplier);
            cmd.Parameters.AddWithValue("@supplyChange", supplyChange + new Random().Next(-10, 5));
            cmd.Parameters.AddWithValue("@demandChange", demandChange + new Random().Next(10));
            cmd.ExecuteNonQuery();

            return gameMode == "War"
                ? "🔥 Market updated with WAR volatility!"
                : "📈 Market prices stabilized.";
        }

        /// <summary>
        /// Get player's current balance
        /// </summary>
        private double GetPlayerBalance(int playerId)
        {
            using var conn = new SqlConnection(_connString);
            conn.Open();
            string sql = "SELECT Balance FROM Players WHERE PlayerId = @pid";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pid", playerId);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToDouble(result) : 0;
        }
    }

    /// <summary>
    /// Result object returned after each turn
    /// </summary>
    public class NextTurnResult
    {
        public int TurnNumber { get; set; }
        public string GameMode { get; set; } = "Normal";
        public double PlayerBalance { get; set; }
        public List<string> Events { get; set; } = new List<string>();
    }
}