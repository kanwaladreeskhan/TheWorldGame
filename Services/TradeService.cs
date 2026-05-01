using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using GlobalTradeSimulator.Models;

namespace GlobalTradeSimulator.Services
{
    public class TradeService
    {
        private readonly string _conn;

        public TradeService(string connectionString)
        {
            _conn = connectionString;
        }

        // ─────────────────────────────────────────────
        // 1. BUY RESOURCE
        // ─────────────────────────────────────────────
        /// <summary>
        /// Player kisi bhi resource ko market price pe khareedata hai.
        /// SQL Procedure: BuyResource
        /// Validation: balance check, qty check — DB procedure ke andar bhi hoti hai.
        /// </summary>
        public TradeResult BuyResource(int playerId, int resourceId, int qty)
        {
            // Step 1: Validation pehle karo (C# side)
            var validation = ValidateTrade(playerId, resourceId, qty, "BUY");
            if (!validation.IsValid)
                return new TradeResult { Success = false, Message = validation.ErrorMessage };

            // Step 2: SQL Stored Procedure call karo
            return ExecuteStoredProcedure("BuyResource", playerId, resourceId, qty);
        }

        // ─────────────────────────────────────────────
        // 2. SELL RESOURCE
        // ─────────────────────────────────────────────
        /// <summary>
        /// Player apna resource bech sakta hai.
        /// SQL Procedure: SellResource
        /// Validation: resource availability check, qty check.
        /// </summary>
        public TradeResult SellResource(int playerId, int resourceId, int qty)
        {
            // Step 1: Validation
            var validation = ValidateTrade(playerId, resourceId, qty, "SELL");
            if (!validation.IsValid)
                return new TradeResult { Success = false, Message = validation.ErrorMessage };

            // Step 2: SQL Stored Procedure call
            return ExecuteStoredProcedure("SellResource", playerId, resourceId, qty);
        }

        // ─────────────────────────────────────────────
        // 3. TRADE VALIDATION
        // ─────────────────────────────────────────────
        /// <summary>
        /// DB se live data check karke validate karta hai.
        /// BUY ke liye: balance >= cost
        /// SELL ke liye: player ke paas enough resource hai?
        /// </summary>
        public ValidationResult ValidateTrade(int playerId, int resourceId, int qty, string tradeType)
        {
            // Negative quantity check
            if (qty <= 0)
                return new ValidationResult { IsValid = false, ErrorMessage = "Quantity zero ya negative nahi ho sakti." };

            try
            {
                using var conn = new SqlConnection(_conn);
                conn.Open();

                if (tradeType.ToUpper() == "BUY")
                {
                    // Balance vs Cost check
                    string sql = @"
                        SELECT p.Balance, mp.CurrentPrice
                        FROM Players p, MarketPrices mp
                        WHERE p.PlayerId = @PlayerId AND mp.ResourceId = @ResourceId";

                    using var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@PlayerId", playerId);
                    cmd.Parameters.AddWithValue("@ResourceId", resourceId);

                    using var reader = cmd.ExecuteReader();
                    if (!reader.Read())
                        return new ValidationResult { IsValid = false, ErrorMessage = "Player ya Resource nahi mila." };

                    double balance = Convert.ToDouble(reader["Balance"]);
                    double price = Convert.ToDouble(reader["CurrentPrice"]);
                    double totalCost = price * qty;

                    if (balance < totalCost)
                        return new ValidationResult
                        {
                            IsValid = false,
                            ErrorMessage = $"Insufficient balance! Needed: {totalCost:F2}, Available: {balance:F2}"
                        };
                }
                else if (tradeType.ToUpper() == "SELL")
                {
                    // Resource availability check
                    string sql = @"
                        SELECT ISNULL(Quantity, 0) AS Quantity
                        FROM PlayerResources
                        WHERE PlayerId = @PlayerId AND ResourceId = @ResourceId";

                    using var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@PlayerId", playerId);
                    cmd.Parameters.AddWithValue("@ResourceId", resourceId);

                    var result = cmd.ExecuteScalar();
                    int ownedQty = result != null ? Convert.ToInt32(result) : 0;

                    if (ownedQty < qty)
                        return new ValidationResult
                        {
                            IsValid = false,
                            ErrorMessage = $"Insufficient resource! You have: {ownedQty}, Trying to sell: {qty}"
                        };
                }

                return new ValidationResult { IsValid = true };
            }
            catch (SqlException ex)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "DB Error: " + ex.Message };
            }
        }

        // ─────────────────────────────────────────────
        // 4. TRADE HISTORY
        // ─────────────────────────────────────────────
        /// <summary>
        /// Kisi bhi player ki saari trades return karta hai Trades table se.
        /// </summary>
        public List<TradeHistory> GetTradeHistory(int playerId)
        {
            var history = new List<TradeHistory>();

            try
            {
                using var conn = new SqlConnection(_conn);
                conn.Open();

                string sql = @"
                    SELECT t.TradeId, t.TradeType, t.Quantity, t.PriceAtTrade, t.TradeDate,
                           r.Name AS ResourceName
                    FROM Trades t
                    JOIN Resources r ON t.ResourceId = r.ResourceId
                    WHERE t.PlayerId = @PlayerId
                    ORDER BY t.TradeDate DESC";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@PlayerId", playerId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    history.Add(new TradeHistory
                    {
                        TradeId = reader.GetInt32(0),
                        TradeType = reader.GetString(1),
                        Quantity = reader.GetInt32(2),
                        PriceAtTrade = Convert.ToDouble(reader[3]),
                        TradeDate = reader.GetDateTime(4),
                        ResourceName = reader.GetString(5),
                        TotalValue = Convert.ToDouble(reader[3]) * reader.GetInt32(2)
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Trade History fetch error: " + ex.Message);
            }

            return history;
        }

        // ─────────────────────────────────────────────
        // PRIVATE HELPER: Stored Procedure Execute
        // ─────────────────────────────────────────────
        private TradeResult ExecuteStoredProcedure(string procName, int playerId, int resourceId, int qty)
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                conn.Open();

                using var cmd = new SqlCommand(procName, conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PlayerId", playerId);
                cmd.Parameters.AddWithValue("@ResourceId", resourceId);
                cmd.Parameters.AddWithValue("@Qty", qty);

                cmd.ExecuteNonQuery();

                return new TradeResult
                {
                    Success = true,
                    Message = $"{procName} completed successfully! Qty: {qty}"
                };
            }
            catch (SqlException ex)
            {
                return new TradeResult { Success = false, Message = "SQL Error: " + ex.Message };
            }
            catch (Exception ex)
            {
                return new TradeResult { Success = false, Message = "System Error: " + ex.Message };
            }
        }
    }

    // ─────────────────────────────────────────────
    // HELPER MODELS (is file ke andar, ya Models folder mein le jao)
    // ─────────────────────────────────────────────

    public class TradeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = "";
    }

    public class TradeHistory
    {
        public int TradeId { get; set; }
        public string TradeType { get; set; } = "";
        public string ResourceName { get; set; } = "";
        public int Quantity { get; set; }
        public double PriceAtTrade { get; set; }
        public double TotalValue { get; set; }
        public DateTime TradeDate { get; set; }
    }
}