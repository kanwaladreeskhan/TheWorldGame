using Microsoft.AspNetCore.Mvc;
using GlobalTradeSimulator.Services;
using System;
using System.Threading.Tasks;

namespace GlobalTradeSimulator.Web.Controllers
{
    [Route("api/[controller]")] // Ye automatically "api/game" ban jayega
    [ApiController]
    public class GameController : ControllerBase
    {
        // Jani, in services ko Program.cs mein builder.Services.AddScoped mein register lazmi karna
        private readonly IGameEngine _gameEngine; 
        private readonly IWarService _warService;

        // Constructor mein DI use karein taake state maintain rahe
        public GameController(IGameEngine gameEngine, IWarService warService)
        {
            _gameEngine = gameEngine;
            _warService = warService;
        }

        [HttpPost("next-turn")]
        public IActionResult NextTurn([FromBody] TurnRequest request)
        {
            // Null check taake crash na ho
            if (request == null || request.PlayerId <= 0)
            {
                return BadRequest(new { message = "Invalid Player ID. Please select a nation first." });
            }

            try
            {
                // Game Engine ko trigger karein
                var result = _gameEngine.NextTurn(request.PlayerId);

                if (result == null) 
                    return NotFound(new { message = "Player not found in database." });

                return Ok(new
                {
                    message = $"Turn {result.TurnNumber} processed successfully!",
                    turnNumber = result.TurnNumber,
                    gameMode = result.GameMode,
                    playerBalance = result.PlayerBalance,
                    events = result.Events
                });
            }
            catch (Exception ex)
            {
                // Inner exception check karein agar error depth mein hai
                var errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { message = "Game engine error: " + errorMsg });
            }
        }

        [HttpGet("state")]
        public IActionResult GetGameState()
        {
            try 
            {
                var state = _warService.GetState();
                return Ok(new { turnNumber = state.TurnNumber, mode = state.Mode });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("start-war")]
        public IActionResult StartWar() 
        {
            var msg = _warService.StartWar();
            return Ok(new { message = msg });
        }

        [HttpPost("end-war")]
        public IActionResult EndWar() 
        {
            var msg = _warService.EndWar();
            return Ok(new { message = msg });
        }
    }

    public class TurnRequest 
    { 
        public int PlayerId { get; set; } 
    }
}