using System;
using System.Linq;
using GlobalTradeSimulator.DataAccess;
using GlobalTradeSimulator.Models;

namespace GlobalTradeSimulator.Services
{
    public class WarService
    {
        private readonly GameStateRepository _repo;
        private readonly Random _random = new Random();

        // War triggers after this many turns
        private const int WAR_POSSIBLE_AFTER_TURN = 5;

        // Chance of random war event per turn (when at war)
        private const int WAR_EVENT_CHANCE = 25; // 25%

        // Chance of war STARTING (when in Normal mode)
        private const int WAR_START_CHANCE = 15; // 15% per turn after threshold

        // Chance of war ENDING (when in War mode)
        private const int WAR_END_CHANCE = 10; // 10% per turn

        public WarService()
        {
            _repo = new GameStateRepository();
        }

        /// <summary>
        /// Called each turn to evaluate war events
        /// </summary>
        public string ProcessWarScenario()
        {
            var gameState = _repo.GetGameState();
            string message = "";

            // Check for war start/end
            if (gameState.Mode == "Normal")
            {
                if (gameState.TurnNumber >= WAR_POSSIBLE_AFTER_TURN && _random.Next(100) < WAR_START_CHANCE)
                {
                    _repo.SetGameMode("War");
                    message = "⚠️ WAR HAS BROKEN OUT! Global tension rises... Prices will surge!";
                    Console.WriteLine(message);
                    ApplyWarStartEffects();
                    return message;
                }
            }
            else if (gameState.Mode == "War")
            {
                if (_random.Next(100) < WAR_END_CHANCE)
                {
                    _repo.SetGameMode("Normal");
                    _repo.ResetWarEvents();
                    message = "🕊️ PEACE RESTORED! Markets stabilizing...";
                    Console.WriteLine(message);
                    return message;
                }

                // Random war events while at war
                if (_random.Next(100) < WAR_EVENT_CHANCE)
                {
                    message = TriggerRandomWarEvent();
                }
            }

            return message;
        }

        /// <summary>
        /// Apply immediate effects when war starts
        /// </summary>
        private void ApplyWarStartEffects()
        {
            // Oil, Gas, Food, Steel, Technology - all spike
            var criticalEvents = _repo.GetWarEvents()
                .Where(e => new[] { 1, 3, 4, 5, 6 }.Contains(e.AffectedResourceId))
                .ToList();

            foreach (var evt in criticalEvents.Take(3)) // Apply 3 random critical events
            {
                _repo.ApplyWarEvent(evt);
                Console.WriteLine($"   💥 War Event: {evt.EventName} - {evt.Description}");
            }
        }

        /// <summary>
        /// Trigger a random war event from the database
        /// </summary>
        private string TriggerRandomWarEvent()
        {
            var events = _repo.GetWarEvents().Where(e => !e.IsActive).ToList();
            if (events.Count == 0) return "";

            var selectedEvent = events[_random.Next(events.Count)];
            _repo.ApplyWarEvent(selectedEvent);

            string message = $"🌍 EVENT: {selectedEvent.EventName} - {selectedEvent.Description}. Prices affected!";
            Console.WriteLine($"   {message}");
            return message;
        }

        /// <summary>
        /// Manual toggle war ON (via API)
        /// </summary>
        public string StartWar()
        {
            _repo.SetGameMode("War");
            ApplyWarStartEffects();
            return "⚠️ WAR manually triggered! Resource prices spiking...";
        }

        /// <summary>
        /// Manual toggle war OFF (via API)
        /// </summary>
        public string EndWar()
        {
            _repo.SetGameMode("Normal");
            _repo.ResetWarEvents();
            return "🕊️ Peace manually restored. Markets returning to normal.";
        }

        /// <summary>
        /// Get current game state for frontend
        /// </summary>
        public GameState GetState()
        {
            return _repo.GetGameState();
        }
    }
}