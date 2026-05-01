using Microsoft.AspNetCore.Mvc;
using GlobalTradeSimulator.Services;

namespace GlobalTradeSimulator.Web.Controllers
{
    [Route("api/trade")]
    [ApiController]
    public class TradeController : ControllerBase
    {
        private readonly TradeService _tradeService;
        private readonly MarketService _marketService;

        // appsettings.json se connection string inject hogi
        public TradeController(IConfiguration config)
        {
            string conn = config.GetConnectionString("GameDB")!;
            _tradeService = new TradeService(conn);
            _marketService = new MarketService(conn);
        }

        // ─────────────────────────────────────────────
        // POST api/trade
        // Body: { "playerId": 1, "resourceId": 2, "quantity": 5, "action": "BUY" }
        // ─────────────────────────────────────────────
        [HttpPost]
        public IActionResult ExecuteTrade([FromBody] TradeRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body missing hai." });

            TradeResult result;

            if (request.Action.ToUpper() == "BUY")
                result = _tradeService.BuyResource(request.PlayerId, request.ResourceId, request.Quantity);
            else if (request.Action.ToUpper() == "SELL")
                result = _tradeService.SellResource(request.PlayerId, request.ResourceId, request.Quantity);
            else
                return BadRequest(new { message = "Action sirf BUY ya SELL hona chahiye." });

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            // Trade ke baad demand/supply update karo, prices recalculate karo
            _marketService.AdjustDemandSupply(request.ResourceId, request.Quantity, request.Action);
            _marketService.UpdateAllMarketPrices();

            return Ok(new { message = result.Message });
        }

        // ─────────────────────────────────────────────
        // GET api/trade/history/{playerId}
        // ─────────────────────────────────────────────
        [HttpGet("history/{playerId}")]
        public IActionResult GetHistory(int playerId)
        {
            try
            {
                var history = _tradeService.GetTradeHistory(playerId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }

    // Request Model
    public class TradeRequest
    {
        public int PlayerId { get; set; }
        public int ResourceId { get; set; }
        public int Quantity { get; set; }
        public string Action { get; set; } = ""; // BUY or SELL
    }
}