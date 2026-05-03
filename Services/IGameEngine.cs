using System.Threading.Tasks;
using GlobalTradeSimulator.Web.Models;

namespace GlobalTradeSimulator.Web.Services
{
    public interface IGameEngine
    {
        Task<NextTurnResult> ProcessTurnAsync();
        NextTurnResult NextTurn(int playerId);       // <-- controller calls this
    }
}