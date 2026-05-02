using Microsoft.AspNetCore.Mvc;
using GlobalTradeSimulator.Services;
using GlobalTradeSimulator.Models; // Models ka namespace zaroori hai

namespace GlobalTradeSimulator.Web.Controllers
{
    [Route("api/trade")] // Frontend '/api/trade' par POST kar raha hai
    [ApiController]
    public class TradeController : ControllerBase
    {
        private readonly TradeService _tradeService;
        private readonly MarketService _marketService;

        // Dependency Injection use karein (Best Practice)
        public TradeController(IConfiguration config)
        {
            string conn = config.GetConnectionString("gameDB")!;
            _tradeService = new TradeService(conn);
            _marketService = new MarketService(conn);
        }

        /// <summary>
        /// POST: api/trade
        /// Frontend Payload: { "PlayerId": 1, "ResourceId": 2, "Quantity": 5, "Action": "BUY" }
        /// </summary>
        [HttpPost]
        public IActionResult ExecuteTrade([FromBody] TradeRequest request)
        {
            // 1. Basic Validation
            if (request == null)
                return BadRequest(new { message = "Invalid trade data." });

            if (request.Quantity <= 0)
                return BadRequest(new { message = "Quantity must be greater than zero." });

            TradeResult result;

            try 
            {
                // 2. Logic execution based on Action
                if (request.Action.ToUpper() == "BUY")
                {
                    result = _tradeService.BuyResource(request.PlayerId, request.ResourceId, request.Quantity);
                }
                else if (request.Action.ToUpper() == "SELL")
                {
                    result = _tradeService.SellResource(request.PlayerId, request.ResourceId, request.Quantity);
                }
                else
                {
                    return BadRequest(new { message = "Action must be BUY or SELL." });
                }

                // 3. Result Check
                if (!result.Success)
                    return BadRequest(new { message = result.Message });

                // 4. Market Impact (Economic System)
                // Trade hone ke baad demand/supply adjust karein taake next turn mein prices change hon
                _marketService.AdjustDemandSupply(request.ResourceId, request.Quantity, request.Action.ToUpper());
                
                // Prices foran update karne ke liye (Optional: Next turn par bhi kar sakte hain)
                _marketService.UpdateAllMarketPrices();

                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Critical Server Error: " + ex.Message });
            }
        }

        /// <summary>
        /// GET: api/trade/history/{playerId}
        /// </summary>
        [HttpGet("history/{playerId}")]
        public IActionResult GetHistory(int playerId)
        {
            try
            {
                var history = _tradeService.GetTradeHistory(playerId);
                if (history == null) return Ok(new List<object>()); // Khali list bhejein agar history na ho
                
                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching history: " + ex.Message });
            }
        }
    }

    // Request Model: Frontend ke JSON keys se match karne ke liye
    public class TradeRequest
    {
        public int PlayerId { get; set; }
        public int ResourceId { get; set; }
        public int Quantity { get; set; }
        public string Action { get; set; } = string.Empty;
    }
}