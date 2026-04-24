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

        // Strategy multipliers for trade quantity
        private readonly Dictionary<string, double> _strategyMultipliers = new()
        {
            { "Aggressive", 1.5 },    // Buys/sells 50% more
            { "Balanced", 1.0 },      // Normal
            { "Conservative", 0.5 }   // Buys/sells 50% less
        };

        // Strategy: random trade chance
        private readonly Dictionary<string, int> _randomTradeChance = new()
        {
            { "Aggressive", 35 },     // 35% chance of random trade
            { "Balanced", 20 },       // 20% chance
            { "Conservative", 10 }    // 10% chance
        };

        public AIService()
        {
            _repo = new GameStateRepository();
        }

        /// <summary>
        /// Main AI decision method — called per AI country
        /// </summary>
        public void MakeDecision(Player aiPlayer)
        {
            var gameState = _repo.GetGameState();
            var rules = _repo.GetAIRules();
            var inventory = _repo.GetPlayerInventory(aiPlayer.Id);
            bool isWarMode = gameState.Mode == "War";

            Console.WriteLine($"🤖 AI Decision: {aiPlayer.Name} ({aiPlayer.StrategyType}) | Mode: {gameState.Mode}");

            // 1. Rule-based decisions
            foreach (var rule in rules)
            {
                EvaluateRule(aiPlayer, rule, inventory, isWarMode);
            }

            // 2. Random behavior (20-30% chance based on strategy)
            if (_random.Next(100) < _randomTradeChance[aiPlayer.StrategyType])
            {
                ExecuteRandomTrade(aiPlayer, inventory, isWarMode);
            }

            // 3. War mode: AI gets more aggressive
            if (isWarMode && aiPlayer.StrategyType == "Aggressive")
            {
                ExecuteWarAggressiveBuy(aiPlayer, inventory);
            }
        }

        /// <summary>
        /// Evaluate a single AI rule
        /// </summary>
        private void EvaluateRule(Player player, AIRule rule, List<(int ResourceId, int Quantity)> inventory, bool isWarMode)
        {
            var playerResource = inventory.FirstOrDefault(r => r.ResourceId == rule.ResourceId);
            int ownedQuantity = playerResource.Quantity;
            double currentPrice = _repo.GetCurrentPrice(rule.ResourceId);

            bool conditionMet = false;

            // Check condition
            if (rule.ConditionType == "LOW" && ownedQuantity < rule.Threshold)
                conditionMet = true;
            else if (rule.ConditionType == "HIGH" && ownedQuantity > rule.Threshold)
                conditionMet = true;

            if (!conditionMet) return;

            // Check balance for BUY
            if (rule.Action == "BUY" && player.Balance < rule.MinBalance)
                return;

            // Calculate quantity with strategy multiplier
            int baseQty = Math.Min(rule.MaxQuantity,
                rule.Action == "BUY" ? (int)(player.Balance / currentPrice) : ownedQuantity);

            int adjustedQty = (int)(baseQty * _strategyMultipliers[player.StrategyType]);

            // War mode: aggressive strategies trade more
            if (isWarMode && player.StrategyType == "Aggressive" && rule.Action == "BUY")
                adjustedQty = (int)(adjustedQty * 1.3);

            if (adjustedQty <= 0) adjustedQty = 1;

            // Execute trade
            try
            {
                _repo.ExecuteAITrade(player.Id, rule.ResourceId, adjustedQty, rule.Action);
                Console.WriteLine($"   📊 Rule: {rule.Action} {adjustedQty}x Resource {rule.ResourceId} (Owned: {ownedQuantity}, Threshold: {rule.Threshold})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️ Rule failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute a random trade for unpredictability
        /// </summary>
        private void ExecuteRandomTrade(Player player, List<(int ResourceId, int Quantity)> inventory, bool isWarMode)
        {
            int randomResourceId = _random.Next(1, 9); // Resource IDs 1-8
            string action = _random.Next(2) == 0 ? "BUY" : "SELL";
            double price = _repo.GetCurrentPrice(randomResourceId);
            var playerResource = inventory.FirstOrDefault(r => r.ResourceId == randomResourceId);

            int qty;
            if (action == "BUY")
            {
                qty = (int)(player.Balance / price / 3); // Spend ~1/3 of balance max
                if (qty <= 0) qty = 1;
                if (qty > 10) qty = 10; // Cap random buy
            }
            else
            {
                qty = playerResource.Quantity > 0 ? _random.Next(1, playerResource.Quantity + 1) : 0;
                if (qty <= 0) return;
            }

            try
            {
                _repo.ExecuteAITrade(player.Id, randomResourceId, qty, action);
                Console.WriteLine($"   🎲 Random: {action} {qty}x Resource {randomResourceId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️ Random trade failed: {ex.Message}");
            }
        }

        /// <summary>
        /// War mode: Aggressive panic buying of critical resources
        /// </summary>
        private void ExecuteWarAggressiveBuy(Player player, List<(int ResourceId, int Quantity)> inventory)
        {
            // Critical war resources: Oil(1), Gas(6), Food(3), Steel(4)
            int[] criticalResources = { 1, 3, 4, 6 };

            foreach (int resId in criticalResources)
            {
                double price = _repo.GetCurrentPrice(resId);
                if (player.Balance > price * 2)
                {
                    int qty = (int)(player.Balance * 0.1 / price); // Spend 10% balance
                    if (qty < 1) qty = 1;
                    if (qty > 15) qty = 15;

                    try
                    {
                        _repo.ExecuteAITrade(player.Id, resId, qty, "BUY");
                        Console.WriteLine($"   ⚔️ War Aggressive: BUY {qty}x Resource {resId} (panic buying!)");
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Run AI decisions for ALL non-player countries
        /// </summary>
        public void ProcessAllAI()
        {
            var aiPlayers = _repo.GetAIPlayers();
            Console.WriteLine($"\n🧠 Processing {aiPlayers.Count} AI players...");

            foreach (var ai in aiPlayers)
            {
                // Refresh player data (balance may have changed)
                MakeDecision(ai);
            }
        }
    }
}