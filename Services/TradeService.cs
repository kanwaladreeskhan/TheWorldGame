using System.Data.SqlClient;
using System.Data;

namespace GlobalTradeSimulator.Services
{
    public class TradeService
    {
        private string _conn = "Server=DESKTOP-VEIPHS8\\SQLEXPRESS;Database=gameDB;Trusted_Connection=True;";

        public string ExecuteTrade(int playerId, int resourceId, int qty, string type)
        {
            try
            {
                using var connection = new SqlConnection(_conn);
                connection.Open();

                // Procedure names must match your SQL exactly: 'BuyResource' and 'SellResource'
                string proc = (type.ToUpper() == "BUY") ? "BuyResource" : "SellResource";

                using var cmd = new SqlCommand(proc, connection);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@PlayerId", playerId);
                cmd.Parameters.AddWithValue("@ResourceId", resourceId);
                cmd.Parameters.AddWithValue("@Qty", qty);

                cmd.ExecuteNonQuery();
                return "Trade Successful!";
            }
            catch (SqlException ex)
            {
                return "SQL Error: " + ex.Message;
            }
            catch (Exception ex)
            {
                return "System Error: " + ex.Message;
            }
        }
    }
}