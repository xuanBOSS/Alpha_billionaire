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
                .WithUrl("https://localhost:7101/gamehub") // SignalR 服务器的 URL
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





        //房间匹配窗口可能需要注册的消息
        //请求进行房间匹配
        public async Task TryMatchRoom()
        {
            try
            {
                await _connection.InvokeAsync("StartMatch");//尝试匹配
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送消息失败: {ex.Message}");//房间匹配失败
            }
        }

        //一种匹配界面的可能逻辑
        //先接收玩家是否需要等待的消息
        //需要等待则显示等待界面
        //等待房间匹配成功的消息

        // 注册等待房间匹配的事件
        public void WaitRoom(Action<bool> waitroom)
        {
            _connection.On<bool>("WaitingForOpponent", waitroom);
        }

        // 注册接收房间匹配结果的事件
        public void MatchResult(Action<string, string> EnterRoom)//EnterRoom在界面类中定义，展示匹配成功后的界面
        {
            _connection.On<string, string>("MatchSuccess", EnterRoom);//没有处理房间匹配失败的情况
        }

        //退出房间
        public async Task ExitRoom()
        {
            try
            {
                await _connection.InvokeAsync("ExitRoom");//退出房间
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送消息失败: {ex.Message}");//退出房间失败
            }
        }






        //对战窗口可能需要注册的消息
        //向服务端传送落子信息
        public async Task TryPlacePiece(int x, int y)//传入落子位置(x,y)
        {
            try
            {
                await _connection.InvokeAsync("PlacePiece", x, y);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送消息失败: {ex.Message}");
            }
        }

        // 注册接收落子信息的事件，在棋盘上显示落子结果
        public void ReceivePiece(Action<string> ShowStep)
        {
            _connection.On<string>("PieceInfo", ShowStep);
        }

        //注册显示胜负的事件
        public void ReachEnd(Action<string> ShowResult)
        {
            _connection.On<string>("GameOver", ShowResult);
        }

        //注册显示当前轮次玩家状态的事件(行动或者等待)
        public void Turn(Action<string> ShowState)
        {
            _connection.On<string>("State", ShowState);
        }









        //testWindow中对服务端和客户端的检查代码

        // 注册接收消息的事件
        public void RegisterReceiveMessage(Action<string, string> callback)
        {
            _connection.On<string, string>("ReceiveMessage", callback);
        }

        // 客户端调用这个方法来发送消息到服务端
        public async Task SendMessageToServer(string user, string message)
        {
            try
            {
                // 调用服务端的 SendMessage 方法，将消息发送到服务端
                //InvokeAsync会确保客户端在服务器执行完后再继续
                await _connection.InvokeAsync("SendMessage", user, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送消息失败: {ex.Message}");
            }
        }

        //如果采用这种方式发送消息，客户端不会等待服务端的回应
        // 发送消息到所有连接的客户端
        /*public async Task SendMessageAsync(string user, string message)
        {
            await _connection.SendAsync("SendMessage", user, message);
        }*/
    }
}
