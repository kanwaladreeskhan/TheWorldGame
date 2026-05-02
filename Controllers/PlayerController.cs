using Microsoft.AspNetCore.Mvc;
using GlobalTradeSimulator.DataAccess;
using GlobalTradeSimulator.Models;
using System;
using System.Collections.Generic;

namespace GlobalTradeSimulator.Web.Controllers
{
    [Route("api/player")]
    [ApiController]
    public class PlayerController : ControllerBase
    {
        // Yahan naam '_repo' hai, toh niche bhi har jagah yahi use hoga
        private readonly PlayerRepository _repo = new PlayerRepository();

        [HttpGet]
        public IActionResult GetAllPlayers() 
        {
            return Ok(_repo.GetAllPlayers());
        }

        [HttpGet("{name}")]
        public IActionResult GetPlayer(string name)
        {
            var player = _repo.GetPlayer(name);
            return player == null ? NotFound(new { message = "Player not found" }) : Ok(player);
        }

        // FIXED: '_playerRepo' ko badal kar '_repo' kar diya hai
        [HttpGet("{id}/inventory")]
        public IActionResult GetInventory(int id)
        {
            try 
            {
                var inventory = _repo.GetInventory(id); 
                return Ok(inventory);
            }
            catch (Exception ex) 
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("leaderboard")]
        public IActionResult GetLeaderboard()
        {
            try 
            { 
                return Ok(_repo.GetLeaderboard()); 
            }
            catch (Exception ex) 
            { 
                return StatusCode(500, new { message = ex.Message }); 
            }
        }
    }
}