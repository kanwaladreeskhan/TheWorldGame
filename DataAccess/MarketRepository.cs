using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using GlobalTradeSimulator.Models;

namespace GlobalTradeSimulator.DataAccess
{
    public class MarketRepository
    {
        // Connection String (Aapki Machine ke mutabiq)
        private readonly string _connString = "Server=DESKTOP-VEIPHS8\\SQLEXPRESS;Database=gameDB;Trusted_Connection=True;TrustServerCertificate=True;";

        /// <summary>
        /// Database se saare resources, unki prices, supply aur demand fetch karta hai.
        /// </summary>
        public List<MarketResource> GetMarketPrices()
        {
            var list = new List<MarketResource>();
            // CONNECTION STRING CHECK: Kya server name bilkul yahi hai?
            string _connString = "Server=DESKTOP-VEIPHS8\\SQLEXPRESS;Database=gameDB;Trusted_Connection=True;TrustServerCertificate=True;";

            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    string sql = "SELECT r.ResourceId, r.Name, mp.CurrentPrice, mp.Supply, mp.Demand " +
                        "FROM Resources r JOIN MarketPrices mp ON r.ResourceId = mp.ResourceId order by ResourceId" 
                     ;
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
                // YE LINE DEBUGGING MEIN MADAD KAREGI
                throw new Exception("Database Connection Error: " + ex.Message);
            }
            return list;
        }
    }

    // Model class for Market Data (Ensure this matches your Models folder or keep it here)
    public class MarketResource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double CurrentPrice { get; set; } // SQL column ka naam yahi hai
        public int Supply { get; set; }
        public int Demand { get; set; }
    }
}