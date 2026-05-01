using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace GlobalTradeSimulator.Services
{
    public class MarketService
    {
        private readonly string _conn;

        public MarketService(string connectionString)
        {
            _conn = connectionString;
        }

        // ─────────────────────────────────────────────
        // 1. MARKET PRICES — DB se fetch karo
        // ─────────────────────────────────────────────
        /// <summary>
        /// Saare resources ki current market info return karta hai.
        /// </summary>
        public List<MarketData> GetAllMarketPrices()
        {
            var list = new List<MarketData>();

            try
            {
                using var conn = new SqlConnection(_conn);
                conn.Open();

                string sql = @"
                    SELECT r.ResourceId, r.Name, r.BasePrice, r.Category,
                           mp.CurrentPrice, mp.Demand, mp.Supply
                    FROM Resources r
                    JOIN MarketPrices mp ON r.ResourceId = mp.ResourceId
                    ORDER BY r.ResourceId";

                using var cmd = new SqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new MarketData
                    {
                        ResourceId = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        BasePrice = Convert.ToDouble(reader[2]),
                        Category = reader.GetString(3),
                        CurrentPrice = Convert.ToDouble(reader[4]),
                        Demand = reader.GetInt32(5),
                        Supply = reader.GetInt32(6)
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Market data fetch error: " + ex.Message);
            }

            return list;
        }

        // ─────────────────────────────────────────────
        // 2. MARKET PRICE CALCULATION FORMULA
        // NewPrice = BasePrice + (Demand - Supply) * factor
        // ─────────────────────────────────────────────
        /// <summary>
        /// Formula:
        ///   Factor = (Demand - Supply) / (Supply + 1)   ← division by zero se bacho
        ///   NewPrice = BasePrice * (1 + Factor * 0.1)
        ///
        /// Demand ↑ → Price ↑
        /// Supply ↑ → Price ↓
        /// Minimum price = BasePrice ka 50% (price zero nahi jayegi)
        /// </summary>
        public double CalculateNewPrice(double basePrice, int demand, int supply)
        {
            // Division by zero se bachao
            double factor = (double)(demand - supply) / (supply + 1);

            // Adjustment: 10% per unit of factor
            double newPrice = basePrice * (1 + factor * 0.10);

            // Price floor: BasePrice ka aadha se kam nahi jayegi
            double minPrice = basePrice * 0.50;
            return Math.Max(newPrice, minPrice);
        }

        // ─────────────────────────────────────────────
        // 3. UPDATE ALL PRICES IN DATABASE
        // ─────────────────────────────────────────────
        /// <summary>
        /// Saare resources ki price recalculate karke DB mein update karta hai.
        /// Game round ke baad call karo.
        /// </summary>
        public void UpdateAllMarketPrices()
        {
            var marketData = GetAllMarketPrices();

            try
            {
                using var conn = new SqlConnection(_conn);
                conn.Open();

                foreach (var item in marketData)
                {
                    double newPrice = CalculateNewPrice(item.BasePrice, item.Demand, item.Supply);

                    string sql = @"
                        UPDATE MarketPrices
                        SET CurrentPrice = @NewPrice
                        WHERE ResourceId = @ResourceId";

                    using var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@NewPrice", newPrice);
                    cmd.Parameters.AddWithValue("@ResourceId", item.ResourceId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Price update error: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────
        // 4. UPDATE DEMAND/SUPPLY AFTER TRADE
        // ─────────────────────────────────────────────
        /// <summary>
        /// Har trade ke baad demand aur supply adjust hoti hai:
        ///   BUY  → Demand ↑, Supply ↓
        ///   SELL → Supply ↑, Demand ↓
        /// </summary>
        public void AdjustDemandSupply(int resourceId, int qty, string tradeType)
        {
            string sql = tradeType.ToUpper() == "BUY"
                ? "UPDATE MarketPrices SET Demand = Demand + @Qty, Supply = Supply - @Qty WHERE ResourceId = @ResourceId"
                : "UPDATE MarketPrices SET Supply = Supply + @Qty, Demand = Demand - @Qty WHERE ResourceId = @ResourceId";

            try
            {
                using var conn = new SqlConnection(_conn);
                conn.Open();

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Qty", qty);
                cmd.Parameters.AddWithValue("@ResourceId", resourceId);
                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                throw new Exception("Demand/Supply update error: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────
        // 5. LEADERBOARD — total wealth by player
        // ─────────────────────────────────────────────
        /// <summary>
        /// DB View "Leaderboard" se player rankings return karta hai.
        /// TotalWealth = Balance + (Resource Quantity × Current Price)
        /// </summary>
        public List<LeaderboardEntry> GetLeaderboard()
        {
            var board = new List<LeaderboardEntry>();

            try
            {
                using var conn = new SqlConnection(_conn);
                conn.Open();

                // DB.sql mein Leaderboard VIEW already bani hui hai
                string sql = "SELECT Name, TotalWealth FROM Leaderboard ORDER BY TotalWealth DESC";

                using var cmd = new SqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();

                int rank = 1;
                while (reader.Read())
                {
                    board.Add(new LeaderboardEntry
                    {
                        Rank = rank++,
                        PlayerName = reader.GetString(0),
                        TotalWealth = Convert.ToDouble(reader[1])
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Leaderboard fetch error: " + ex.Message);
            }

            return board;
        }
    }

    // ─────────────────────────────────────────────
    // HELPER MODELS
    // ─────────────────────────────────────────────

    public class MarketData
    {
        public int ResourceId { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public double BasePrice { get; set; }
        public double CurrentPrice { get; set; }
        public int Demand { get; set; }
        public int Supply { get; set; }
    }

    public class LeaderboardEntry
    {
        public int Rank { get; set; }
        public string PlayerName { get; set; } = "";
        public double TotalWealth { get; set; }
    }
}