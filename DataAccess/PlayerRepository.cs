using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using GlobalTradeSimulator.Models;

namespace GlobalTradeSimulator.DataAccess
{
    public class PlayerRepository
    {
        // Connection String (Updated to your SQL Express instance)
        private readonly string _connString = "Server=.\\LAB;Database=gameDB;Trusted_Connection=True;TrustServerCertificate=True;";

        // 1. Get Player by Name
        public Player? GetPlayer(string name)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connString))
                {
                    connection.Open();
                    string sql = "SELECT PlayerId, Name, Balance FROM Players WHERE Name = @name";
                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@name", name);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Player
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    Balance = Convert.ToDouble(reader["Balance"])
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Error GetPlayer: " + ex.Message); }
            return null;
        }

        // 2. Get Player by ID
        public Player? GetPlayerById(int id)
        {
            using var connection = new SqlConnection(_connString);
            connection.Open();
            string sql = "SELECT PlayerId, Name, Balance FROM Players WHERE PlayerId = @id";
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return new Player { Id = reader.GetInt32(0), Name = reader.GetString(1), Balance = Convert.ToDouble(reader["Balance"]) };
            return null;
        }

        // 3. Get All Players
        public List<Player> GetAllPlayers()
        {
            var list = new List<Player>();
            using var connection = new SqlConnection(_connString);
            connection.Open();
            string sql = "SELECT PlayerId, Name, Balance FROM Players";
            using var cmd = new SqlCommand(sql, connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new Player { Id = reader.GetInt32(0), Name = reader.GetString(1), Balance = reader.GetDouble(2) });
            return list;
        }

        // 4. Get Inventory
        public List<PlayerResource> GetInventory(int playerId)
        {
            var inv = new List<PlayerResource>();
            using (SqlConnection connection = new SqlConnection(_connString))
            {
                connection.Open();
                string sql = "SELECT r.ResourceId, r.Name, pr.Quantity FROM PlayerResources pr JOIN Resources r ON pr.ResourceId = r.ResourceId WHERE pr.PlayerId = @pid";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@pid", playerId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                        while (reader.Read())
                            inv.Add(new PlayerResource { ResourceId = reader.GetInt32(0), ResourceName = reader.GetString(1), Quantity = reader.GetInt32(2) });
                }
            }
            return inv;
        }

        // 5. Get Leaderboard (With Player Highlight Logic)
        public List<object> GetLeaderboard()
        {
            var list = new List<object>();
            using var connection = new SqlConnection(_connString);
            connection.Open();
            
            // Uses SQL VIEW 'Leaderboard'
            string sql = "SELECT Name, TotalWealth FROM Leaderboard ORDER BY TotalWealth DESC";
            var cmd = new SqlCommand(sql, connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string name = reader.GetString(0);
                list.Add(new { 
                    Name = name, 
                    TotalWealth = Convert.ToDouble(reader["TotalWealth"]),
                    IsPlayer = (name.ToLower() == "aamnah") // Highlights your name in the list
                });
            }
            return list;
        }
    }
}