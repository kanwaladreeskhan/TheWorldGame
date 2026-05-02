using Microsoft.AspNetCore.Mvc;
using GlobalTradeSimulator.DataAccess;
using System;
using System.Collections.Generic;

namespace GlobalTradeSimulator.Web.Controllers
{
    [Route("api/market")]
    [ApiController]
    public class MarketController : ControllerBase
    {
        private readonly MarketRepository _marketRepo = new MarketRepository();

        // GET: api/market
        [HttpGet]
        public IActionResult GetAllMarketData()
        {
            try
            {
                var data = _marketRepo.GetMarketPrices();

                if (data == null || data.Count == 0)
                {
                    // Returning empty list instead of message object for frontend map() compatibility
                    return Ok(new List<MarketResource>());
                }

                return Ok(data);
            }
            catch (Exception ex)
            {
                // This will show up in the browser console if backend fails
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: api/market/{id}
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            try 
            {
                var data = _marketRepo.GetMarketPrices();
                var item = data.Find(x => x.Id == id);
                if (item == null) return NotFound(new { message = "Resource not found" });
                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}