using Microsoft.AspNetCore.Mvc;
using GlobalTradeSimulator.DataAccess;
using GlobalTradeSimulator.Models;
using System.Data.SqlClient;

namespace GlobalTradeSimulator.Web.Controllers
{
    [Route("api/trade")]
    [ApiController]
    public class TradeController : ControllerBase
    {
        private string _connString = "Server=DESKTOP-VEIPHS8\\SQLEXPRESS;Database=gameDB;Trusted_Connection=True;TrustServerCertificate=True;";

        [HttpPost]
        public IActionResult ExecuteTrade([FromBody] TradeRequest request)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    // SQL Procedure select karein base on Action (BUY/SELL)
                    string procName = (request.Action.ToUpper() == "BUY") ? "BuyResource" : "SellResource";

                    using (SqlCommand cmd = new SqlCommand(procName, conn))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@PlayerId", request.PlayerId);
                        cmd.Parameters.AddWithValue("@ResourceId", request.ResourceId);
                        cmd.Parameters.AddWithValue("@Qty", request.Quantity);

                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { message = "Trade successful using SQL Procedure!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "SQL Error: " + ex.Message });
            }
        }
    }
 
     

    public class TradeRequest
    {
        public int PlayerId { get; set; }
        public int ResourceId { get; set; }
        public int Quantity { get; set; }
        public string Action { get; set; } // BUY or SELL
    }
}