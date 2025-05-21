using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using ChessGame.GameLogic;
using System.Windows;

namespace ChessGame.Client
{
    public class SignalRService
    {
        private HubConnection _connection;
        private static SignalRService _instance;

        // 用户信息
        public UserInfo CurrentUser { get; private set; }
        public bool IsLoggedIn => CurrentUser != null;

        // 事件
        public event Action<string> OnError;
        public event Action<bool> OnConnectionStateChanged;

        // 单例模式
        public static SignalRService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SignalRService();
                }
                return _instance;
            }
        }

        public SignalRService()
        {
            // 创建与 SignalR Hub 的连接
            _connection = new HubConnectionBuilder()
                //.WithUrl("https://localhost:7101/gamehub") // SignalR 服务器的 URL
                .WithUrl("http://localhost:5000/gamehub")
                .WithAutomaticReconnect()
                .Build();

            _connection.Closed += async (error) =>
            {
                OnConnectionStateChanged?.Invoke(false);
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await StartConnectionAsync();
            };

            _connection.Reconnected += (connectionId) =>
            {
                OnConnectionStateChanged?.Invoke(true);
                return Task.CompletedTask;
            };
        }

        // 启动 SignalR 连接
        public async Task StartConnectionAsync()
        {
            
            try
            {
                if (_connection.State == HubConnectionState.Disconnected)
                {
                    Console.WriteLine("正在连接到服务器...");
                    Console.WriteLine($"连接URL: {_connection.ConnectionId}");

                    await _connection.StartAsync();
                    Console.WriteLine("SignalR连接成功!");
                    OnConnectionStateChanged?.Invoke(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"连接失败: {ex.Message}");

                // 输出更详细的错误信息
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部错误: {ex.InnerException.Message}");
                }

                // 提供更友好的错误提示
                string errorMessage = $"连接服务器失败: {ex.Message}\n\n" +
                    "可能的原因:\n" +
                    "1. 服务器未运行\n" +
                    "2. 连接URL配置错误\n" +
                    "3. 网络连接问题";

                OnError?.Invoke(errorMessage);
                OnConnectionStateChanged?.Invoke(false);
            }
        }

        // 用户登录
        public async Task<LoginResult> LoginAsync(string userId, string password)
        {
            try
            {
                // 确保已连接
                if (_connection.State == HubConnectionState.Disconnected)
                {
                    await StartConnectionAsync();
                    // 再次检查连接状态
                    if (_connection.State != HubConnectionState.Connected)
                    {
                        return new LoginResult
                        {
                            Success = false,
                            ErrorMessage = "无法连接到服务器，请检查网络或稍后重试。"
                        };
                    }
                }

                // 调用服务器的登录方法
                var response = await _connection.InvokeAsync<LoginResponse>("Login", userId, password);

                if (response.Success)
                {
                    // 保存当前用户信息
                    CurrentUser = new UserInfo
                    {
                        UserId = response.UserId,
                        UserName = response.UserName
                    };

                    return new LoginResult { Success = true };
                }
                else
                {
                    return new LoginResult
                    {
                        Success = false,
                        ErrorMessage = response.Message ?? "登录失败"
                    };
                }
            }
            catch (Exception ex)
            {
                return new LoginResult
                {
                    Success = false,
                    ErrorMessage = $"登录请求出错: {ex.Message}"
                };
            }
        }

        // 用户注册
        public async Task<RegisterResult> RegisterAsync(string userId, string password, string userName)
        {
            try
            {
                // 确保已连接
                if (_connection.State == HubConnectionState.Disconnected)
                {
                    await StartConnectionAsync();
                }

                // 调用服务器的注册方法
                var response = await _connection.InvokeAsync<RegisterResponse>("Register", userId, password, userName);

                if (response.Success)
                {
                    return new RegisterResult { Success = true };
                }
                else
                {
                    return new RegisterResult
                    {
                        Success = false,
                        ErrorMessage = response.Message ?? "注册失败"
                    };
                }
            }
            catch (Exception ex)
            {
                return new RegisterResult
                {
                    Success = false,
                    ErrorMessage = $"注册请求出错: {ex.Message}"
                };
            }
        }

        // 注销
        public void Logout()
        {
            CurrentUser = null;
        }

        // 获取Hub连接以供其他需要直接访问Hub的方法使用
        public HubConnection GetHubConnection()
        {
            return _connection;
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
                OnError?.Invoke($"房间匹配失败: {ex.Message}");
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
                OnError?.Invoke($"退出房间失败: {ex.Message}");
            }
        }

        //注册接受地图信息的事件
        public void GetMap(Action<MineMap> HandleMap)//EnterRoom在界面类中定义，展示匹配成功后的界面
        {
            _connection.On<MineMap>("MapInfo", HandleMap);//没有处理房间匹配失败的情况
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
                OnError?.Invoke($"发送落子信息失败: {ex.Message}");
            }
        }

        // 注册接收落子信息的事件，如果传入参数为true，在棋盘上显示落子结果
        public void ReceivePiece(Action<bool,int,int,int> ShowStep)//黑1白2
        {
            _connection.On<bool,int,int,int>("PieceInfo", ShowStep);
        }

        //注册使游戏暂停的事件
        public void PauseGame(Action<string> ShowResult)
        {
            _connection.On<string>("PauseGame", ShowResult);
        }

        //注册游戏结束的事件
        public void GameEnd(Action<string> GameOver)
        {
            _connection.On<string>("GameOver", GameOver);
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
                OnError?.Invoke($"发送消息失败: {ex.Message}");
            }
        }

        //注册AI胜率变化的事件

        //如果采用这种方式发送消息，客户端不会等待服务端的回应
        // 发送消息到所有连接的客户端
        /*public async Task SendMessageAsync(string user, string message)
        {
            await _connection.SendAsync("SendMessage", user, message);
        }*/
        // 用户信息类
        public class UserInfo
        {
            public string UserId { get; set; }
            public string UserName { get; set; }
        }

        // 登录结果类
        public class LoginResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
        }

        // 注册结果类
        public class RegisterResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
        }

        // 服务器登录响应类
        public class LoginResponse
        {
            public bool Success { get; set; }
            public string UserId { get; set; }
            public string UserName { get; set; }
            public string Message { get; set; }
        }

        // 服务器注册响应类
        public class RegisterResponse
        {
            public bool Success { get; set; }
            public string UserId { get; set; }
            public string UserName { get; set; }
            public string Message { get; set; }
        }

        public async Task<List<LeaderboardEntry>> GetLeaderboardAsync()
        {
            try
            {
                if (_connection.State != HubConnectionState.Connected)
                {
                    await StartConnectionAsync();
                    if (_connection.State != HubConnectionState.Connected)
                    {
                        Console.WriteLine("无法连接到服务器获取排行榜数据");
                        return new List<LeaderboardEntry>();
                    }
                }

                // 调用服务端的GetLeaderboard方法
                var leaderboard = await _connection.InvokeAsync<List<LeaderboardEntry>>("GetLeaderboard");
                Console.WriteLine($"成功获取排行榜数据，共{leaderboard.Count}条记录");
                return leaderboard;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取排行榜数据失败: {ex.Message}");
                OnError?.Invoke($"获取排行榜数据失败: {ex.Message}");
                return new List<LeaderboardEntry>();
            }
        }

        // 添加LeaderboardEntry类
        public class LeaderboardEntry
        {
            public string UserId { get; set; }
            public string UserName { get; set; }
            public int WinTimes { get; set; }
            public int Rank { get; set; }
        }

        // 注册排行榜更新事件
        public void RegisterLeaderboardUpdatedEvent(Action<List<LeaderboardEntry>> onLeaderboardUpdated)
        {
            _connection.On<List<LeaderboardEntry>>("LeaderboardUpdated", (leaderboard) =>
            {
                onLeaderboardUpdated?.Invoke(leaderboard);
            });
        }
    }
}
