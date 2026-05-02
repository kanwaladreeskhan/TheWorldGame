using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using GlobalTradeSimulator.Models;

namespace GlobalTradeSimulator.DataAccess
{
    public class MarketRepository
    {
<<<<<<< HEAD
        // Connection String: Ensure this matches your SSMS server name
        private readonly string _connString = "Server=.\\LAB;Database=gameDB;Trusted_Connection=True;TrustServerCertificate=True;";
=======
        private readonly string _connString = "Server=DESKTOP-R9F65GH\\SQLEXPRESS03;Database=gameDB;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;";
>>>>>>> 4e2f0faf0438fb51dc4b7dc630b478a10e1f9d7b

        public List<MarketResource> GetMarketPrices()
        {
            var list = new List<MarketResource>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
<<<<<<< HEAD
                    // Accurate Query joining Resources and MarketPrices
                    string sql = @"SELECT r.ResourceId, r.Name, mp.CurrentPrice, mp.Supply, mp.Demand 
                                 FROM Resources r 
                                 JOIN MarketPrices mp ON r.ResourceId = mp.ResourceId 
                                 ORDER BY r.ResourceId";
=======
                    string sql = "SELECT r.ResourceId, r.Name, mp.CurrentPrice, mp.Supply, mp.Demand " +
                                 "FROM Resources r JOIN MarketPrices mp ON r.ResourceId = mp.ResourceId ORDER BY r.ResourceId";
>>>>>>> 4e2f0faf0438fb51dc4b7dc630b478a10e1f9d7b

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
<<<<<<< HEAD
                // Detailed error for debugging in terminal
                throw new Exception("SQL Error in GetMarketPrices: " + ex.Message);
=======
                throw new Exception("Database Connection Error: " + ex.Message);
>>>>>>> 4e2f0faf0438fb51dc4b7dc630b478a10e1f9d7b
            }

            return list;
        }
     //prices ko randomly change karega.   
public bool UpdateMarketTurn()
{
    // Connection string lazmi check karein
    string connStr = "Server=.\\LAB;Database=gameDB;Trusted_Connection=True;TrustServerCertificate=True;";
    
    try
    {
        using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connStr))
        {
            connection.Open();
            // Price fluctuation logic
            string sql = @"UPDATE MarketPrices 
                           SET CurrentPrice = CurrentPrice * (1 + (ABS(CHECKSUM(NEWID())) % 11 - 5) / 100.0)
                           WHERE CurrentPrice > 0";
            
            using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, connection))
            {
                int rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
        }
    }
    catch (Exception ex) 
    { 
        Console.WriteLine("Turn Update Error: " + ex.Message);
        return false; 
    }
}
    }
    

<<<<<<< HEAD
    // Model class defined here to ensure compatibility
    public class MarketResource
    {
        public int Id { get; set; }
       public string Name { get; set; } = "";
=======
    public class MarketResource
    {
        public int Id { get; set; }
        public string Name { get; set; }
>>>>>>> 4e2f0faf0438fb51dc4b7dc630b478a10e1f9d7b
        public double CurrentPrice { get; set; }
        public int Supply { get; set; }
        public int Demand { get; set; }
    }
}