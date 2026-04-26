using System;
using System.Collections.Generic;

public class ValidationService
{
    private Dictionary<string, int> playerBalances;
    private Dictionary<string, List<string>> playerResources;
    
    public ValidationService(Dictionary<string, int> balances, Dictionary<string, List<string>> resources)
    {
        playerBalances = balances;
        playerResources = resources;
    }

    public bool ValidateTrade(string playerId, string resource, int quantity)
    {
        return IsPlayerExists(playerId) &&
               IsBalanceSufficient(playerId, quantity) &&
               IsResourceAvailable(playerId, resource, quantity);
    }

    private bool IsPlayerExists(string playerId)
    {
        return playerBalances.ContainsKey(playerId);
    }

    private bool IsBalanceSufficient(string playerId, int quantity)
    {
        return playerBalances[playerId] >= quantity;
    }

    private bool IsResourceAvailable(string playerId, string resource, int quantity)
    {
        return playerResources.ContainsKey(playerId) && 
               playerResources[playerId].Count >= quantity; // Adjust according to resource logic
    }
}