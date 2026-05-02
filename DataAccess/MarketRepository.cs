using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using GlobalTradeSimulator.Models;

namespace GlobalTradeSimulator.DataAccess
{
    public class MarketRepository
    {
        private readonly string _connString = "Server=DESKTOP-R9F65GH\\SQLEXPRESS03;Database=gameDB;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;";

        public List<MarketResource> GetMarketPrices()
        {
            var list = new List<MarketResource>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    string sql = "SELECT r.ResourceId, r.Name, mp.CurrentPrice, mp.Supply, mp.Demand " +
                                 "FROM Resources r JOIN MarketPrices mp ON r.ResourceId = mp.ResourceId ORDER BY r.ResourceId";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
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
            catch (Exception ex)
            {
                throw new Exception("Database Connection Error: " + ex.Message);
            }

            return list;
        }
    }

    public class MarketResource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double CurrentPrice { get; set; }
        public int Supply { get; set; }
        public int Demand { get; set; }
    }
}