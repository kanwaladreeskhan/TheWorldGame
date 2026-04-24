using Microsoft.AspNetCore.Mvc;
using GlobalTradeSimulator.Services;

namespace GlobalTradeSimulator.Web.Controllers
{
    [Route("api/game")]
    [ApiController]
    public class GameController : ControllerBase
    {
        private readonly GameEngine _gameEngine;
        private readonly WarService _warService;

        public GameController()
        {
            _gameEngine = new GameEngine();
            _warService = new WarService();
        }

        // POST: api/game/next-turn
        [HttpPost("next-turn")]
        public IActionResult NextTurn([FromBody] TurnRequest request)
        {
            try
            {
                var result = _gameEngine.NextTurn(request.PlayerId);
                return Ok(new
                {
                    message = $"Turn {result.TurnNumber} completed!",
                    turnNumber = result.TurnNumber,
                    gameMode = result.GameMode,
                    playerBalance = result.PlayerBalance,
                    events = result.Events
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Game engine error: " + ex.Message });
            }
        }

        // GET: api/game/state
        [HttpGet("state")]
        public IActionResult GetGameState()
        {
            var state = _warService.GetState();
            return Ok(new
            {
                turnNumber = state.TurnNumber,
                mode = state.Mode
            });
        }

        // POST: api/game/start-war (Admin trigger)
        [HttpPost("start-war")]
        public IActionResult StartWar()
        {
            string message = _warService.StartWar();
            return Ok(new { message });
        }

        // POST: api/game/end-war (Admin trigger)
        [HttpPost("end-war")]
        public IActionResult EndWar()
        {
            string message = _warService.EndWar();
            return Ok(new { message });
        }
    }

    public class TurnRequest
    {
        public int PlayerId { get; set; }
    }
}