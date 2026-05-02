using Microsoft.AspNetCore.Mvc;
using GlobalTradeSimulator.DataAccess;
using GlobalTradeSimulator.Models; // Agar MarketResource yahan hai toh isko rehne dein
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
                    return Ok(new List<object>()); // Empty list for compatibility
                }
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // NEXT TURN logic - FIXED '_repo' to '_marketRepo'
        [HttpPost("next-turn")]
        public IActionResult NextTurn()
        {
            // Pehle yahan _repo likha tha, jo ghalat tha. Ab theek hai:
            var success = _marketRepo.UpdateMarketTurn(); 
            if (success) return Ok(new { message = "Market updated!" });
            return BadRequest("Update failed.");
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