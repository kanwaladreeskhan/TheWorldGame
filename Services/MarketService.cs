using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace GlobalTradeSimulator.Services
{
    public class MarketService
    {
        private readonly string _conn;

        public MarketService(string connectionString)
        {
            _conn = connectionString;
        }

        public List<MarketData> GetAllMarketPrices()
        {
            var list = new List<MarketData>();
            using var conn = new SqlConnection(_conn);
            conn.Open();
            string sql = @"SELECT r.ResourceId, r.Name, r.BasePrice, r.Category, mp.CurrentPrice, mp.Demand, mp.Supply
                           FROM Resources r JOIN MarketPrices mp ON r.ResourceId = mp.ResourceId";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new MarketData {
                    ResourceId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    BasePrice = Convert.ToDouble(reader[2]),
                    Category = reader.GetString(3),
                    CurrentPrice = Convert.ToDouble(reader[4]),
                    Demand = reader.GetInt32(5),
                    Supply = reader.GetInt32(6)
                });
            }
            return list;
        }

        public double CalculateNewPrice(double basePrice, int demand, int supply)
        {
            // Logic: Dynamic gap calculation
            double factor = (double)(demand - supply) / (Math.Max(supply, 1));
            
            // Volatility logic: 15% price sensitivity
            double newPrice = basePrice * (1 + factor * 0.15);

            // Constraints: Keep price between 50% and 300% of base
            return Math.Clamp(newPrice, basePrice * 0.50, basePrice * 3.00);
        }

        public void UpdateAllMarketPrices()
        {
            var marketData = GetAllMarketPrices();
            using var conn = new SqlConnection(_conn);
            conn.Open();

            foreach (var item in marketData)
            {
                double newPrice = CalculateNewPrice(item.BasePrice, item.Demand, item.Supply);
                string sql = "UPDATE MarketPrices SET CurrentPrice = @NewPrice WHERE ResourceId = @ResourceId";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@NewPrice", newPrice);
                cmd.Parameters.AddWithValue("@ResourceId", item.ResourceId);
                cmd.ExecuteNonQuery();
            }
        }

        public void AdjustDemandSupply(int resourceId, int qty, string tradeType)
        {
            string sql = tradeType.ToUpper() == "BUY"
                ? "UPDATE MarketPrices SET Demand = Demand + @Qty, Supply = Supply - @Qty WHERE ResourceId = @ResourceId"
                : "UPDATE MarketPrices SET Supply = Supply + @Qty, Demand = Demand - @Qty WHERE ResourceId = @ResourceId";

            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Qty", qty);
            cmd.Parameters.AddWithValue("@ResourceId", resourceId);
            cmd.ExecuteNonQuery();
        }

        public List<LeaderboardEntry> GetLeaderboard()
        {
            var board = new List<LeaderboardEntry>();
            using var conn = new SqlConnection(_conn);
            conn.Open();
            string sql = "SELECT Name, TotalWealth FROM Leaderboard ORDER BY TotalWealth DESC";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            int rank = 1;
            while (reader.Read())
            {
                board.Add(new LeaderboardEntry {
                    Rank = rank++,
                    PlayerName = reader.GetString(0),
                    TotalWealth = Convert.ToDouble(reader[1])
                });
            }
            return board;
        }
    }

    public class MarketData {
        public int ResourceId { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public double BasePrice { get; set; }
        public double CurrentPrice { get; set; }
        public int Demand { get; set; }
        public int Supply { get; set; }
    }

    public class LeaderboardEntry {
        public int Rank { get; set; }
        public string PlayerName { get; set; } = "";
        public double TotalWealth { get; set; }
    }
}