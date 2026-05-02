using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using GlobalTradeSimulator.DataAccess;
using Microsoft.Extensions.Configuration; // Configuration ke liye add karein

namespace GlobalTradeSimulator.Services
{
    public class GameEngine : IGameEngine // Interface implement karna behtar hai
    {
        private readonly AIService _aiService;
        private readonly WarService _warService;
        private readonly GameStateRepository _repo;
        private readonly string _connString;

        // Constructor mein connection string ko configuration se lein
        public GameEngine()
        {
            _aiService = new AIService();
            _warService = new WarService();
            _repo = new GameStateRepository();
            // Connection string ko update karein ya appsettings.json se lein
            _connString = "Server=.\\LAB;Database=gameDB;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        public NextTurnResult NextTurn(int playerId)
        {
            var result = new NextTurnResult();
            
            try
            {
                var gameState = _repo.GetGameState();

                // 1. Increment turn in DB
                _repo.IncrementTurn();
                int newTurn = gameState.TurnNumber + 1;

                // 2. War Scenario
                string warMessage = _warService.ProcessWarScenario();
                if (!string.IsNullOrEmpty(warMessage))
                    result.Events.Add(warMessage);

                // Fresh state fetch karein mode check karne ke liye
                var updatedState = _repo.GetGameState();

                // 3. AI Processing (Ensure this doesn't crash)
                _aiService.ProcessAllAI();
                result.Events.Add("AI units have adjusted their portfolios.");

                // 4. Market Update - Yahan masla ho sakta hai agar SQL fail ho
                string marketMessage = UpdateMarketPrices(updatedState.Mode);
                result.Events.Add(marketMessage);

                // 5. Finalize Results
                result.PlayerBalance = GetPlayerBalance(playerId);
                result.TurnNumber = newTurn;
                result.GameMode = updatedState.Mode;

                return result;
            }
            catch (Exception ex)
            {
                // Detailed error logging
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                result.Events.Add($"❌ Engine Failure: {errorMsg}");
                return result;
            }
        }

        private string UpdateMarketPrices(string gameMode)
        {
            using (var conn = new SqlConnection(_connString))
            {
                conn.Open();

                double volatilityMultiplier = (gameMode == "War") ? 1.5 : 1.0; // War mein zyada volatility
                
                // Random changes generate karein
                var rand = new Random();
                int supplyChange = (gameMode == "War") ? rand.Next(-20, -5) : rand.Next(-10, 10);
                int demandChange = (gameMode == "War") ? rand.Next(10, 30) : rand.Next(-5, 15);

                string sql = @"
                    UPDATE MarketPrices 
                    SET CurrentPrice = CASE 
                        WHEN CurrentPrice + ((Demand - Supply) * 0.05 * @volatility) > 5 
                        THEN CurrentPrice + ((Demand - Supply) * 0.05 * @volatility)
                        ELSE 5 -- Price floor
                    END,
                    Supply = CASE WHEN Supply + @sChg > 0 THEN Supply + @sChg ELSE 10 END,
                    Demand = CASE WHEN Demand + @dChg > 0 THEN Demand + @dChg ELSE 10 END";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@volatility", volatilityMultiplier);
                    cmd.Parameters.AddWithValue("@sChg", supplyChange);
                    cmd.Parameters.AddWithValue("@dChg", demandChange);
                    cmd.ExecuteNonQuery();
                }
            }

            return gameMode == "War" ? "🔥 High volatility market update!" : "📈 Regular market adjustment.";
        }

        private double GetPlayerBalance(int playerId)
        {
            try {
                using var conn = new SqlConnection(_connString);
                conn.Open();
                string sql = "SELECT Balance FROM Players WHERE PlayerId = @pid";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@pid", playerId);
                var res = cmd.ExecuteScalar();
                return res != null ? Convert.ToDouble(res) : 0;
            } catch { return 0; }
        }
    }
}