using Microsoft.AspNetCore.Mvc;
using GlobalTradeSimulator.DataAccess;
using System;

namespace GlobalTradeSimulator.Web.Controllers
{
    [Route("api/market")]
    [ApiController]
    public class MarketController : ControllerBase
    {
        private readonly MarketRepository _marketRepo = new MarketRepository();

        // SIRF EK HI [HttpGet] HONA CHAHIYE
        [HttpGet]
        public IActionResult GetAllMarketData()
        {
            try
            {
                var data = _marketRepo.GetMarketPrices();

                if (data == null || data.Count == 0)
                    return Ok(new { message = "Market is empty" });

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Agar specific ID chahiye ho toh rasta badalna parta hai: [HttpGet("{id}")]
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var data = _marketRepo.GetMarketPrices();
            var item = data.Find(x => x.Id == id);
            if (item == null) return NotFound();
            return Ok(item);
        }
    }
}