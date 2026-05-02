using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
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

        public TradeResult BuyResource(int playerId, int resourceId, int qty)
        {
            var validation = ValidateTrade(playerId, resourceId, qty, "BUY");
            if (!validation.IsValid)
                return new TradeResult { Success = false, Message = validation.ErrorMessage };

            return ExecuteStoredProcedure("BuyResource", playerId, resourceId, qty);
        }

        public TradeResult SellResource(int playerId, int resourceId, int qty)
        {
            var validation = ValidateTrade(playerId, resourceId, qty, "SELL");
            if (!validation.IsValid)
                return new TradeResult { Success = false, Message = validation.ErrorMessage };

            return ExecuteStoredProcedure("SellResource", playerId, resourceId, qty);
        }

        public ValidationResult ValidateTrade(int playerId, int resourceId, int qty, string tradeType)
        {
            if (qty <= 0)
                return new ValidationResult { IsValid = false, ErrorMessage = "Quantity zero ya negative nahi ho sakti." };

            try
            {
                using var conn = new SqlConnection(_conn);
                conn.Open();

                if (tradeType.ToUpper() == "BUY")
                {
                    string sql = @"SELECT p.Balance, mp.CurrentPrice 
                                   FROM Players p, MarketPrices mp 
                                   WHERE p.PlayerId = @PlayerId AND mp.ResourceId = @ResourceId";

                    using var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@PlayerId", playerId);
                    cmd.Parameters.AddWithValue("@ResourceId", resourceId);

                    using var reader = cmd.ExecuteReader();
                    if (!reader.Read())
                        return new ValidationResult { IsValid = false, ErrorMessage = "Market data not found." };

                    // Logic: Using decimal for financial accuracy
                    decimal balance = Convert.ToDecimal(reader["Balance"]);
                    decimal price = Convert.ToDecimal(reader["CurrentPrice"]);
                    decimal totalCost = price * qty;

                    if (balance < totalCost)
                        return new ValidationResult { IsValid = false, ErrorMessage = $"Insufficient Funds! Need: ${totalCost:N2}, Have: ${balance:N2}" };
                }
                else if (tradeType.ToUpper() == "SELL")
                {
                    string sql = "SELECT ISNULL(Quantity, 0) FROM PlayerResources WHERE PlayerId = @PlayerId AND ResourceId = @ResourceId";
                    using var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@PlayerId", playerId);
                    cmd.Parameters.AddWithValue("@ResourceId", resourceId);

                    var result = cmd.ExecuteScalar();
                    int ownedQty = (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);

                    if (ownedQty < qty)
                        return new ValidationResult { IsValid = false, ErrorMessage = $"Stock Shortage! You have {ownedQty}, trying to sell {qty}." };
                }

                return new ValidationResult { IsValid = true };
            }
            catch (Exception ex)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "System Validation Error: " + ex.Message };
            }
        }

        public List<TradeHistory> GetTradeHistory(int playerId)
        {
            var history = new List<TradeHistory>();
            try
            {
                using var conn = new SqlConnection(_conn);
                conn.Open();
                string sql = @"SELECT t.TradeId, t.TradeType, t.Quantity, t.PriceAtTrade, t.TradeDate, r.Name 
                               FROM Trades t JOIN Resources r ON t.ResourceId = r.ResourceId
                               WHERE t.PlayerId = @PlayerId ORDER BY t.TradeDate DESC";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@PlayerId", playerId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    history.Add(new TradeHistory {
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
            catch { throw; }
            return history;
        }

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

                return new TradeResult { Success = true, Message = "Transaction processed successfully." };
            }
            catch (Exception ex)
            {
                return new TradeResult { Success = false, Message = "DB Error: " + ex.Message };
            }
        }
    }

    public class TradeResult { public bool Success { get; set; } public string Message { get; set; } = ""; }
    public class ValidationResult { public bool IsValid { get; set; } public string ErrorMessage { get; set; } = ""; }
    public class TradeHistory {
        public int TradeId { get; set; }
        public string TradeType { get; set; } = "";
        public string ResourceName { get; set; } = "";
        public int Quantity { get; set; }
        public double PriceAtTrade { get; set; }
        public double TotalValue { get; set; }
        public DateTime TradeDate { get; set; }
    }
}