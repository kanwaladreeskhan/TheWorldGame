// MarketService.cs

using System;
using System.Collections.Generic;
using System.Linq;

namespace TheWorldGame.Services
{
    public class MarketService
    {
        private List<MarketItem> _marketItems;
        private Leaderboard _leaderboard;

        public MarketService()
        {
            _marketItems = new List<MarketItem>();
            _leaderboard = new Leaderboard();
        }

        // Retrieve market info based on demand and supply
        public void UpdateMarketPrices()
        {
            foreach (var item in _marketItems)
            {
                item.CurrentPrice = CalculatePrice(item);
            }
        }

        // A simple algorithm to calculate price based on demand and supply
        private double CalculatePrice(MarketItem item)
        {
            var demandFactor = item.Demand / (item.Supply + 1);  // Avoid division by zero
            return item.BasePrice * (1 + demandFactor);
        }

        // Generate leaderboard based on market transactions
        public Leaderboard GetLeaderboard()
        {
            return _leaderboard;
        }

        // Manage market prices
        public void ManageMarketPrice(string itemId, double newPrice)
        {
            var item = _marketItems.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                item.CurrentPrice = newPrice;
            }
        }

        // Market item class
        public class MarketItem
        {
            public string Id { get; set; }
            public double BasePrice { get; set; }
            public double CurrentPrice { get; set; }
            public double Demand { get; set; }
            public double Supply { get; set; }
        }

        // Leaderboard class
        public class Leaderboard
        {
            public List<string> Players { get; set; }
            public Leaderboard()
            {
                Players = new List<string>();
            }
        }
    }
}