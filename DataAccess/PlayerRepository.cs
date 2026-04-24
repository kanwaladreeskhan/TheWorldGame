using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using GlobalTradeSimulator.Models;

namespace GlobalTradeSimulator.DataAccess
{
    public class PlayerRepository
    {
        // Connection String ek hi jagah rakhein
        private readonly string _connString = "Server=.\\SQLEXPRESS;Database=gameDB;Trusted_Connection=True;TrustServerCertificate=True;";

        // 1. Get Player by Name (For Login)
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

        // 2. Get Player by ID (For Trade Logic)
        public Player? GetPlayerById(int id)
        {
            using var connection = new SqlConnection(_connString);
            connection.Open();
            string sql = "SELECT PlayerId, Name, Balance FROM Players WHERE PlayerId = @id";
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Player { Id = reader.GetInt32(0), Name = reader.GetString(1), Balance = Convert.ToDouble(reader["Balance"]) };
            }
            return null;
        }

        // 3. Get All Players (For Country Selection)
        public List<Player> GetAllPlayers()
        {
            var list = new List<Player>();
            using var connection = new SqlConnection(_connString);
            connection.Open();
            string sql = "SELECT PlayerId, Name, Balance FROM Players";
            using var cmd = new SqlCommand(sql, connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Player { Id = reader.GetInt32(0), Name = reader.GetString(1), Balance = reader.GetDouble(2) });
            }
            return list;
        }

        // 4. Get Inventory (Joins with Resources)
        public List<PlayerResource> GetInventory(int playerId)
        {
            List<PlayerResource> inv = new List<PlayerResource>();
            using (SqlConnection connection = new SqlConnection(_connString))
            {
                connection.Open();
                string sql = "SELECT r.ResourceId, r.Name, pr.Quantity FROM PlayerResources pr JOIN Resources r ON pr.ResourceId = r.ResourceId WHERE pr.PlayerId = @pid";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@pid", playerId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            inv.Add(new PlayerResource { ResourceId = reader.GetInt32(0), ResourceName = reader.GetString(1), Quantity = reader.GetInt32(2) });
                        }
                    }
                }
            }
            return inv;
        }

        // 5. Get Leaderboard (Uses SQL VIEW 'Leaderboard')
        public List<object> GetLeaderboard()
        {
            var list = new List<object>();
            using var connection = new SqlConnection(_connString);
            connection.Open();
            var cmd = new SqlCommand("SELECT Name, TotalWealth FROM Leaderboard ORDER BY TotalWealth DESC", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new { Name = reader.GetString(0), TotalWealth = Convert.ToDouble(reader["TotalWealth"]) });
            }
            return list;
        }
    }
}