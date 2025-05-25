using ChessGame.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ChessGame.Server.Controllers
{
    public class GameController
    {
        private readonly IHubContext<GameHub> _hubContext;//允许在Hub类外执行与客户端的交互

    }
}