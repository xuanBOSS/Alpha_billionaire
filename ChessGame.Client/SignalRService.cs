using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChessGame.Client
{
    public class SignalRService
    {
        private HubConnection _connection;

        public SignalRService()
        {
            // 创建与 SignalR Hub 的连接
            _connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7101/alphahub") // SignalR 服务器的 URL
                .Build();
        }

        // 启动 SignalR 连接
        public async Task StartConnectionAsync()
        {
            try
            {
                await _connection.StartAsync();
                Console.WriteLine("SignalR连接成功!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"连接失败: {ex.Message}");
            }
        }

        // 注册接收消息的事件
        public void RegisterReceiveMessage(Action<string, string> callback)
        {
            _connection.On<string, string>("ReceiveMessage", callback);
        }

        // 发送消息到所有连接的客户端
        public async Task SendMessageAsync(string user, string message)
        {
            await _connection.SendAsync("SendMessage", user, message);
        }

        // 客户端调用这个方法来发送消息到服务端
        public async Task SendMessageToServer(string user, string message)
        {
            try
            {
                // 调用服务端的 SendMessage 方法，将消息发送到服务端
                await _connection.InvokeAsync("SendMessage", user, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送消息失败: {ex.Message}");
            }
        }
    }
}
