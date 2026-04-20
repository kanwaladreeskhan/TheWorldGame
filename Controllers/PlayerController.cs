using Microsoft.AspNetCore.Mvc;
using GlobalTradeSimulator.DataAccess;
using GlobalTradeSimulator.Models;

namespace GlobalTradeSimulator.Web.Controllers
{
    [Route("api/player")]
    [ApiController]
    public class PlayerController : ControllerBase
    {

        private readonly PlayerRepository _repo = new PlayerRepository();

        // GET: api/player → All players (for country selection)
        [HttpGet]
        public IActionResult GetAllPlayers()
        {
            var players = _repo.GetAllPlayers();
            return Ok(players);
        }

        // GET: api/player/{name} → Get single player by name
        [HttpGet("{name}")]
        public IActionResult GetPlayer(string name)
        {
            var player = _repo.GetPlayer(name);
            if (player == null)
                return NotFound(new { message = "Player not found" });

            return Ok(player);
        }

        // NEW: GET: api/player/{id}/inventory → Player Inventory
        [HttpGet("{id}/inventory")]
        public IActionResult GetInventory(int id)
        {
            var inventory = _repo.GetInventory(id);
            return Ok(inventory);
        }

        // NEW: GET: api/player/leaderboard → Show Leaderboard
        [HttpGet("leaderboard")]
        public IActionResult GetLeaderboard()
        {
            try
            {
                var data = _repo.GetLeaderboard(); // SQL View wala data
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}