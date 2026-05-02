using System;
using System.Collections.Generic;
using System.Linq;
using GlobalTradeSimulator.DataAccess;
using GlobalTradeSimulator.Models;

namespace GlobalTradeSimulator.Services
{
    public class AIService
    {
        private readonly GameStateRepository _repo;
        private readonly Random _random = new Random();

        private readonly Dictionary<string, double> _strategyMultipliers = new()
        {
            { "Aggressive", 1.5 },
            { "Balanced", 1.0 },
            { "Conservative", 0.5 }
        };

        private readonly Dictionary<string, int> _randomTradeChance = new()
        {
            { "Aggressive", 35 },
            { "Balanced", 20 },
            { "Conservative", 10 }
        };

        public AIService()
        {
            _repo = new GameStateRepository();
        }

        public void MakeDecision(Player aiPlayer)
        {
            var gameState = _repo.GetGameState();
            var rules = _repo.GetAIRules();
            var inventory = _repo.GetPlayerInventory(aiPlayer.Id);
            bool isWarMode = gameState.Mode == "War";

            // 1. Rule-based decisions
            foreach (var rule in rules)
            {
                EvaluateRule(aiPlayer, rule, inventory, isWarMode);
            }

            // 2. Random behavior
            if (_random.Next(100) < _randomTradeChance[aiPlayer.StrategyType])
            {
                ExecuteRandomTrade(aiPlayer, inventory, isWarMode);
            }

            // 3. War mode aggressive buy
            if (isWarMode && aiPlayer.StrategyType == "Aggressive")
            {
                ExecuteWarAggressiveBuy(aiPlayer, inventory);
            }
        }

        private void EvaluateRule(Player player, AIRule rule, List<(int ResourceId, int Quantity)> inventory, bool isWarMode)
        {
            var playerResource = inventory.FirstOrDefault(r => r.ResourceId == rule.ResourceId);
            int ownedQuantity = playerResource.Quantity;
            double currentPrice = _repo.GetCurrentPrice(rule.ResourceId);

            bool conditionMet = (rule.ConditionType == "LOW" && ownedQuantity < rule.Threshold) ||
                               (rule.ConditionType == "HIGH" && ownedQuantity > rule.Threshold);

            if (!conditionMet || (rule.Action == "BUY" && player.Balance < rule.MinBalance)) return;

            int baseQty = Math.Min(rule.MaxQuantity, rule.Action == "BUY" ? (int)(player.Balance / currentPrice) : ownedQuantity);
            int adjustedQty = (int)(baseQty * _strategyMultipliers[player.StrategyType]);

            if (isWarMode && player.StrategyType == "Aggressive" && rule.Action == "BUY")
                adjustedQty = (int)(adjustedQty * 1.3);

            if (adjustedQty > 0) _repo.ExecuteAITrade(player.Id, rule.ResourceId, adjustedQty, rule.Action);
        }

        private void ExecuteRandomTrade(Player player, List<(int ResourceId, int Quantity)> inventory, bool isWarMode)
        {
            int randomResourceId = _random.Next(1, 9);
            string action = _random.Next(2) == 0 ? "BUY" : "SELL";
            double price = _repo.GetCurrentPrice(randomResourceId);
            var playerResource = inventory.FirstOrDefault(r => r.ResourceId == randomResourceId);

            int qty = action == "BUY" ? Math.Min(10, (int)(player.Balance / price / 3)) : 
                                       (playerResource.Quantity > 0 ? _random.Next(1, playerResource.Quantity + 1) : 0);

            if (qty > 0) _repo.ExecuteAITrade(player.Id, randomResourceId, qty, action);
        }

        private void ExecuteWarAggressiveBuy(Player player, List<(int ResourceId, int Quantity)> inventory)
        {
            int[] criticalResources = { 1, 3, 4, 6 };
            foreach (int resId in criticalResources)
            {
                double price = _repo.GetCurrentPrice(resId);
                if (player.Balance > price * 5)
                {
                    int qty = Math.Clamp((int)(player.Balance * 0.1 / price), 1, 15);
                    _repo.ExecuteAITrade(player.Id, resId, qty, "BUY");
                }
            }
        }

        public void ProcessAllAI()
        {
            var aiPlayers = _repo.GetAIPlayers();
            foreach (var ai in aiPlayers) MakeDecision(ai);
        }
    }
}