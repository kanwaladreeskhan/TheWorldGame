using GlobalTradeSimulator.Models;

namespace GlobalTradeSimulator.Web.Services
{
    public interface IWarService
    {
        string ProcessWarScenario();
        string StartWar();
        string EndWar();
        GameState GetState();
    }
}