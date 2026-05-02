using Microsoft.AspNetCore.Mvc;
using GlobalTradeSimulator.Services;
using System;

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

        [HttpGet("state")]
        public IActionResult GetGameState()
        {
            var state = _warService.GetState();
            return Ok(new { turnNumber = state.TurnNumber, mode = state.Mode });
        }

        [HttpPost("start-war")]
        public IActionResult StartWar() => Ok(new { message = _warService.StartWar() });

        [HttpPost("end-war")]
        public IActionResult EndWar() => Ok(new { message = _warService.EndWar() });
    }

    public class TurnRequest { public int PlayerId { get; set; } }
}