using Microsoft.AspNetCore.Mvc;
using GlobalTradeSimulator.DataAccess;
using GlobalTradeSimulator.Models;
using System;

namespace GlobalTradeSimulator.Web.Controllers
{
    [Route("api/player")]
    [ApiController]
    public class PlayerController : ControllerBase
    {
        private readonly PlayerRepository _repo = new PlayerRepository();

        [HttpGet]
        public IActionResult GetAllPlayers() => Ok(_repo.GetAllPlayers());

        [HttpGet("{name}")]
        public IActionResult GetPlayer(string name)
        {
            var player = _repo.GetPlayer(name);
            return player == null ? NotFound(new { message = "Player not found" }) : Ok(player);
        }

        [HttpGet("{id}/inventory")]
        public IActionResult GetInventory(int id) => Ok(_repo.GetInventory(id));

        [HttpGet("leaderboard")]
        public IActionResult GetLeaderboard()
        {
            try { return Ok(_repo.GetLeaderboard()); }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }
    }
}