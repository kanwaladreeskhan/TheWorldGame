using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using GlobalTradeSimulator.Models;

namespace GlobalTradeSimulator.DataAccess
{
    public class MarketRepository
    {
        // Connection String: Ensure this matches your SSMS server name
        private readonly string _connString = "Server=.\\LAB;Database=gameDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public List<MarketResource> GetMarketPrices()
        {
            var list = new List<MarketResource>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    // Accurate Query joining Resources and MarketPrices
                    string sql = @"SELECT r.ResourceId, r.Name, mp.CurrentPrice, mp.Supply, mp.Demand 
                                 FROM Resources r 
                                 JOIN MarketPrices mp ON r.ResourceId = mp.ResourceId 
                                 ORDER BY r.ResourceId";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new MarketResource
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    CurrentPrice = Convert.ToDouble(reader[2]),
                                    Supply = reader.GetInt32(3),
                                    Demand = reader.GetInt32(4)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Detailed error for debugging in terminal
                throw new Exception("SQL Error in GetMarketPrices: " + ex.Message);
            }
            return list;
        }
    }

    // Model class defined here to ensure compatibility
    public class MarketResource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double CurrentPrice { get; set; }
        public int Supply { get; set; }
        public int Demand { get; set; }
    }
}