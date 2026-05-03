using Microsoft.AspNetCore.SignalR;

public class GameHub : Hub
{
    public async Task SendTrade(int from, int to, string resource)
        => await Clients.All.SendAsync("TradeOccurred", from, to, resource);

    public async Task SendWar(int attacker, int defender, int mapX, int mapY)
        => await Clients.All.SendAsync("WarStarted", attacker, defender, mapX, mapY);
}