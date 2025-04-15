using ChessGame.Server.Controllers;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ChessGame.Server.Hubs
{
    public class GameHub : Hub
    {
        private readonly RoomManager _roomManager;

        public GameHub(RoomManager roomManager)
        {
            _roomManager = roomManager;
        }

        public async Task StartMatch()//实现房间匹配
        {
            await _roomManager.MatchPlayer(Context.ConnectionId);
        }

        public async Task PlacePiece(int x, int y)//找到房间传入落子信息
        {
            await _roomManager.HandlePiece(Context.ConnectionId, x, y);
        }

        public async Task ExitRoom()//离开房间
        {
            await _roomManager.ExitRoom(Context.ConnectionId);
        }


        //回复testWindow的函数
        // 客户端调用这个方法来发送消息
        public async Task SendMessage(string user, string message)
        {
            // 发送消息到所有连接的客户端
            await Clients.All.SendAsync("ReceiveMessage", user, message); // 向所有连接的客户端发送消息
        }
    }
}