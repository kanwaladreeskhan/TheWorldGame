using System;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using GlobalTradeSimulator.Models;
using System.Data;

namespace GlobalTradeSimulator.DataAccess
{
    public class GameStateRepository
    {
        // 1. Connection string ko mazeed secure banaya gaya hai (TrustServerCertificate=True zaroori hai)
        private readonly string _connString = "Server=.\\LAB;Database=gameDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public GameState GetGameState()
        {
            try 
            {
                using var conn = new SqlConnection(_connString);
                conn.Open();
                
                // 2. dbo schema use karna behtar hai taake 'Invalid Object Name' ka error na aaye
                string sql = "SELECT Id, Mode, TurnNumber FROM dbo.GameState WHERE Id = 1";
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
                
                // Agar row 1 nahi milti to default return karein
                return new GameState { Mode = "Normal", TurnNumber = 1 };
            }
            catch (SqlException ex)
            {
                // Error handling for debugging
                Console.WriteLine("DB Error: " + ex.Message);
                throw; 
            }
        }

        public void SetGameMode(string mode)
        {
            using var conn = new SqlConnection(_connString);
            conn.Open();
            // Id=1 ki check lagayi hai taake hamesha current game update ho
            string sql = "UPDATE dbo.GameState SET Mode = @mode WHERE Id = 1";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@mode", mode);
            cmd.ExecuteNonQuery();
        }

        public void IncrementTurn()
        {
            using var conn = new SqlConnection(_connString);
            conn.Open();
            // Atomic update: Direct SQL mein increment karna performance ke liye behtar hai
            string sql = "UPDATE dbo.GameState SET TurnNumber = TurnNumber + 1 WHERE Id = 1";
            using var cmd = new SqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        public List<WarEvent> GetWarEvents()
        {
            var events = new List<WarEvent>();
            using var conn = new SqlConnection(_connString);
            conn.Open();
            string sql = "SELECT EventId, EventName, Description, AffectedResourceId, PriceMultiplier, SupplyDrop, DemandBoost, IsActive FROM dbo.WarEvents";
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

        public void ApplyWarEvent(WarEvent warEvent)
        {
            using var conn = new SqlConnection(_connString);
            conn.Open();
            // Transaction use karna behtar hai jab multiple tables update ho rahe hon
            using var transaction = conn.BeginTransaction();
            try {
                string sql = @"
                    UPDATE dbo.MarketPrices 
                    SET CurrentPrice = CurrentPrice * @multiplier,
                        Supply = CASE WHEN Supply - @supplyDrop > 0 THEN Supply - @supplyDrop ELSE 1 END,
                        Demand = Demand + @demandBoost
                    WHERE ResourceId = @resourceId;
                    
                    UPDATE dbo.WarEvents SET IsActive = 1 WHERE EventId = @eventId;";

                using var cmd = new SqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("@multiplier", warEvent.PriceMultiplier);
                cmd.Parameters.AddWithValue("@supplyDrop", warEvent.SupplyDrop);
                cmd.Parameters.AddWithValue("@demandBoost", warEvent.DemandBoost);
                cmd.Parameters.AddWithValue("@resourceId", warEvent.AffectedResourceId);
                cmd.Parameters.AddWithValue("@eventId", warEvent.EventId);
                cmd.ExecuteNonQuery();
                
                transaction.Commit();
            }
            catch {
                transaction.Rollback();
                throw;
            }
        }

        public void ResetWarEvents()
        {
            using var conn = new SqlConnection(_connString);
            conn.Open();
            string sql = "UPDATE dbo.WarEvents SET IsActive = 0";
            using var cmd = new SqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        public List<AIRule> GetAIRules()
        {
            var rules = new List<AIRule>();
            using var conn = new SqlConnection(_connString);
            conn.Open();
            string sql = "SELECT RuleId, ResourceId, ConditionType, Threshold, Action, MinBalance, MaxQuantity FROM dbo.AIRules";
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

        public List<Player> GetAIPlayers()
        {
            var players = new List<Player>();
            using var conn = new SqlConnection(_connString);
            conn.Open();
            string sql = @"
                SELECT p.PlayerId, p.Name, p.CountryId, p.Balance, c.Name, c.StrategyType 
                FROM dbo.Players p 
                JOIN dbo.Countries c ON p.CountryId = c.CountryId 
                WHERE p.Name LIKE '%AI%'"; 
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

        public List<(int ResourceId, int Quantity)> GetPlayerInventory(int playerId)
        {
            var inventory = new List<(int ResourceId, int Quantity)>();
            using var conn = new SqlConnection(_connString);
            conn.Open();
            string sql = "SELECT ResourceId, Quantity FROM dbo.PlayerResources WHERE PlayerId = @pid";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pid", playerId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                inventory.Add((reader.GetInt32(0), reader.GetInt32(1)));
            }
            return inventory;
        }

        public double GetCurrentPrice(int resourceId)
        {
            using var conn = new SqlConnection(_connString);
            conn.Open();
            string sql = "SELECT CurrentPrice FROM dbo.MarketPrices WHERE ResourceId = @rid";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@rid", resourceId);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToDouble(result) : 0;
        }

        public void ExecuteAITrade(int playerId, int resourceId, int qty, string action)
        {
            using var conn = new SqlConnection(_connString);
            conn.Open();
            // Stored procedure call with proper command type
            string proc = (action.ToUpper() == "BUY") ? "dbo.BuyResource" : "dbo.SellResource";
            using var cmd = new SqlCommand(proc, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PlayerId", playerId);
            cmd.Parameters.AddWithValue("@ResourceId", resourceId);
            cmd.Parameters.AddWithValue("@Qty", qty);
            cmd.ExecuteNonQuery();
        }
    }
}