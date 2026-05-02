using System;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using GlobalTradeSimulator.Models;

namespace GlobalTradeSimulator.DataAccess
{
    public class GameStateRepository
    {
        private readonly string _connString = "Server=.\\LAB;Database=gameDB;Trusted_Connection=True;TrustServerCertificate=True;";

        // Get current game state
        public GameState GetGameState()
        {
            using var conn = new SqlConnection(_connString);
            conn.Open();
            string sql = "SELECT Id, Mode, TurnNumber FROM GameState WHERE Id = 1";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new GameState
                {
                    Id = reader.GetInt32(0),
                    Mode = reader.GetString(1),
                    TurnNumber = reader.GetInt32(2)
                };
            }
            return new GameState { Mode = "Normal", TurnNumber = 0 };
        }

        // Toggle War/Normal mode (Admin trigger)
        public void SetGameMode(string mode)
        {
            using var conn = new SqlConnection(_connString);
            conn.Open();
            string sql = "UPDATE GameState SET Mode = @mode WHERE Id = 1";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@mode", mode);
            cmd.ExecuteNonQuery();
        }

        // Increment turn number
        public void IncrementTurn()
        {
            using var conn = new SqlConnection(_connString);
            conn.Open();
            string sql = "UPDATE GameState SET TurnNumber = TurnNumber + 1 WHERE Id = 1";
            using var cmd = new SqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        // Get all war events
        public List<WarEvent> GetWarEvents()
        {
            var events = new List<WarEvent>();
            using var conn = new SqlConnection(_connString);
            conn.Open();
            string sql = "SELECT EventId, EventName, Description, AffectedResourceId, PriceMultiplier, SupplyDrop, DemandBoost, IsActive FROM WarEvents";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                events.Add(new WarEvent
                {
                    EventId = reader.GetInt32(0),
                    EventName = reader.GetString(1),
                    Description = reader.GetString(2),
                    AffectedResourceId = reader.GetInt32(3),
                    PriceMultiplier = reader.GetDouble(4),
                    SupplyDrop = reader.GetInt32(5),
                    DemandBoost = reader.GetInt32(6),
                    IsActive = reader.GetBoolean(7)
                });
            }
            return events;
        }

        // Apply war event to market
        public void ApplyWarEvent(WarEvent warEvent)
        {
            using var conn = new SqlConnection(_connString);
            conn.Open();
            string sql = @"
                UPDATE MarketPrices 
                SET CurrentPrice = CurrentPrice * @multiplier,
                    Supply = CASE WHEN Supply - @supplyDrop > 0 THEN Supply - @supplyDrop ELSE 1 END,
                    Demand = Demand + @demandBoost
                WHERE ResourceId = @resourceId;
                
                UPDATE WarEvents SET IsActive = 1 WHERE EventId = @eventId;
            ";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@multiplier", warEvent.PriceMultiplier);
            cmd.Parameters.AddWithValue("@supplyDrop", warEvent.SupplyDrop);
            cmd.Parameters.AddWithValue("@demandBoost", warEvent.DemandBoost);
            cmd.Parameters.AddWithValue("@resourceId", warEvent.AffectedResourceId);
            cmd.Parameters.AddWithValue("@eventId", warEvent.EventId);
            cmd.ExecuteNonQuery();
        }

        // Reset war events (when war ends)
        public void ResetWarEvents()
        {
            using var conn = new SqlConnection(_connString);
            conn.Open();
            string sql = "UPDATE WarEvents SET IsActive = 0";
            using var cmd = new SqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        // Get all AI rules
        public List<AIRule> GetAIRules()
        {
            var rules = new List<AIRule>();
            using var conn = new SqlConnection(_connString);
            conn.Open();
            string sql = "SELECT RuleId, ResourceId, ConditionType, Threshold, Action, MinBalance, MaxQuantity FROM AIRules";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                rules.Add(new AIRule
                {
                    RuleId = reader.GetInt32(0),
                    ResourceId = reader.GetInt32(1),
                    ConditionType = reader.GetString(2),
                    Threshold = reader.GetInt32(3),
                    Action = reader.GetString(4),
                    MinBalance = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                    MaxQuantity = reader.IsDBNull(6) ? 10 : reader.GetInt32(6)
                });
            }
            return rules;
        }

        // Get AI players (non-human)
        public List<Player> GetAIPlayers()
        {
            var players = new List<Player>();
            using var conn = new SqlConnection(_connString);
            conn.Open();
            string sql = @"
                SELECT p.PlayerId, p.Name, p.CountryId, p.Balance, c.Name, c.StrategyType 
                FROM Players p 
                JOIN Countries c ON p.CountryId = c.CountryId 
                WHERE p.Name LIKE '%AI%'";  // AI players have 'AI' in name
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                players.Add(new Player
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    CountryId = reader.GetInt32(2),
                    Balance = reader.GetDouble(3),
                    CountryName = reader.GetString(4),
                    StrategyType = reader.GetString(5)
                });
            }
            return players;
        }

        // Get player inventory for a specific player
        public List<(int ResourceId, int Quantity)> GetPlayerInventory(int playerId)
        {
            var inventory = new List<(int ResourceId, int Quantity)>();
            using var conn = new SqlConnection(_connString);
            conn.Open();
            string sql = "SELECT ResourceId, Quantity FROM PlayerResources WHERE PlayerId = @pid";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pid", playerId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                inventory.Add((reader.GetInt32(0), reader.GetInt32(1)));
            }
            return inventory;
        }

        // Get current market price of a resource
        public double GetCurrentPrice(int resourceId)
        {
            using var conn = new SqlConnection(_connString);
            conn.Open();
            string sql = "SELECT CurrentPrice FROM MarketPrices WHERE ResourceId = @rid";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@rid", resourceId);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToDouble(result) : 0;
        }

        // Execute trade (BUY/SELL) for AI player
        public void ExecuteAITrade(int playerId, int resourceId, int qty, string action)
        {
            // Reuse existing stored procedures
            using var conn = new SqlConnection(_connString);
            conn.Open();
            string proc = (action.ToUpper() == "BUY") ? "BuyResource" : "SellResource";
            using var cmd = new SqlCommand(proc, conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PlayerId", playerId);
            cmd.Parameters.AddWithValue("@ResourceId", resourceId);
            cmd.Parameters.AddWithValue("@Qty", qty);
            cmd.ExecuteNonQuery();
        }
    }
}