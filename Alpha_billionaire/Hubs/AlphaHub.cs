using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Alpha_billionaire.Hubs
{
    public class AlphaHub:Hub
    {
        //回复testWindow的函数
        // 客户端调用这个方法来发送消息
        public async Task SendMessage(string user, string message)
        {
            // 发送消息到所有连接的客户端
            await Clients.All.SendAsync("ReceiveMessage", user, message); // 向所有连接的客户端发送消息
        }

        public async Task PlacePiece(int x,int y)
        {

        }
    }
}
