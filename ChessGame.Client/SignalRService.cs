using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using ChessGame.GameLogic;
using System.Windows;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Diagnostics;


namespace ChessGame.Client
{
    /*public class PlayerInfoDTO
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int WinTimes { get; set; }
    }*/
    public class SignalRService
    {
        private HubConnection _connection;
        private static SignalRService _instance;

        // 添加用于存储 MatchSuccess 回调和注册状态的字段
        private Action<string, string, PlayerInfo, PlayerInfo> _matchSuccessCallback;
        private bool _isMatchHandlerRegistered = false;

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
                //.WithUrl("http://localhost:5000/gamehub")
                .WithUrl("http://192.168.108.115:5000/gamehub")
                //.WithAutomaticReconnect()
                .Build();

            _connection.Closed += async (error) =>
            {
                Console.WriteLine("SignalR连接已关闭");
                _isMatchHandlerRegistered = false; // 连接关闭时重置注册状态
                OnConnectionStateChanged?.Invoke(false);
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await StartConnectionAsync();
            };

            _connection.Reconnected += async (connectionId) =>
            {
                Console.WriteLine($"SignalR重新连接成功: {connectionId}");
                OnConnectionStateChanged?.Invoke(true);
                // 重新连接后立即重新注册事件处理器
                if (_matchSuccessCallback != null)
                {
                    RegisterMatchSuccessHandler();
                }
                await Task.CompletedTask;
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

                    // 连接成功后立即注册已设置的事件处理器
                    if (_matchSuccessCallback != null && !_isMatchHandlerRegistered)
                    {
                        RegisterMatchSuccessHandler();
                    }
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
                            Message = "无法连接到服务器，请检查网络或稍后重试。"
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
                        UserName = response.UserName,
                        WinTimes = response.WinTimes
                    };

                    return new LoginResult
                    {
                        Success = true,
                        UserId = response.UserId,
                        UserName = response.UserName,
                        WinTimes = response.WinTimes // << 将 WinTimes 放入 LoginResult
                    };
                }
                else
                {
                    return new LoginResult
                    {
                        Success = false,
                        Message = response.Message ?? "登录失败"
                    };
                }
            }
            catch (Exception ex)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = $"登录请求出错: {ex.Message}"
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
                        Message = response.Message ?? "注册失败"
                    };
                }
            }
            catch (Exception ex)
            {
                return new RegisterResult
                {
                    Success = false,
                    Message = $"注册请求出错: {ex.Message}"
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

        //请求玩家身份信息（黑棋或者白棋）
        public async Task GetIdentify()
        {
            try
            {
                await _connection.InvokeAsync("GetIdentify");//尝试请求玩家身份信息
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送消息失败: {ex.Message}");//请求失败
                OnError?.Invoke($"请求玩家身份信息失败: {ex.Message}");
            }
        }

        //请求玩家身份信息（黑棋或者白棋）
        public async Task GetIdentifyInAIMode()
        {
            try
            {
                await _connection.InvokeAsync("GetIdentifyInAIMode");//尝试请求玩家身份信息
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送消息失败: {ex.Message}");//请求失败
                OnError?.Invoke($"请求玩家身份信息失败: {ex.Message}");
            }
        }

        //注册接受玩家身份信息的事件
        public void ReceiveIdentify(Action<string,int,string,int> GetIdentify)
        {
            _connection.On<string,int,string,int>("IdentifyInfo", GetIdentify);
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
        
        public void MatchResult(Action<string, string, PlayerInfo, PlayerInfo> enterRoomCallback)
        {
            Console.WriteLine("设置MatchSuccess回调函数");
            _matchSuccessCallback = enterRoomCallback;

            // 如果连接已建立，立即注册处理器
            if (_connection.State == HubConnectionState.Connected && !_isMatchHandlerRegistered)
            {
                RegisterMatchSuccessHandler();
            }
            else
            {
                Console.WriteLine($"连接状态: {_connection.State}，等待连接建立后注册事件处理器");
            }
        }
        // 独立的事件处理器注册方法
        private void RegisterMatchSuccessHandler()
        {
            if (_matchSuccessCallback == null) return;

            Console.WriteLine("正在注册MatchSuccess事件处理器...");

            // 确保移除旧的处理器
            _connection.Remove("MatchSuccess");

            // 注册新的处理器
            _connection.On<string, string, PlayerInfo, PlayerInfo>("MatchSuccess",
                (roomId, message, player1Info, player2Info) =>
                {
                    Console.WriteLine($"=== 收到MatchSuccess事件 ===");
                    Console.WriteLine($"时间: {DateTime.Now:HH:mm:ss.fff}");
                    Console.WriteLine($"房间: {roomId}, 消息: {message}");
                    Console.WriteLine($"玩家1: {player1Info?.UserName} (ID: {player1Info?.UserId})");
                    Console.WriteLine($"玩家2: {player2Info?.UserName} (ID: {player2Info?.UserId})");

                    try
                    {
                        _matchSuccessCallback(roomId, message, player1Info, player2Info);
                        Console.WriteLine("MatchSuccess回调执行成功");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"MatchSuccess回调执行失败: {ex.Message}");
                    }
                });

            _isMatchHandlerRegistered = true;
            Console.WriteLine("MatchSuccess事件处理器注册完成");
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
            // _connection.On<MineMapDTO>("MapInfo", HandleMap);//没有处理房间匹配失败的情况
            _connection.On<MineMapDTO>("MapInfo", (mapDTO) =>
            {
                // 创建新的 MineMap 实例
                var mineMap = new MineMap();

                // 使用 DTO 数据更新 MineMap
                mineMap.SerializedMines = mapDTO.Mines;
                mineMap.SerializedNumbers = mapDTO.Numbers;

                // 调用回调函数处理转换后的 MineMap
                HandleMap(mineMap);
            });
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

        // 注册接收落子信息的事件，如果传入参数为true，在棋盘上显示落子结果(AI模式下直接调用)
        public void ReceivePiece(Action<bool,int,int,int> ShowStep)//黑1白2
        {
            _connection.On<bool,int,int,int>("PieceInfo", ShowStep);
        }

        // 注册普通游戏模式胜率更新事件
        public void OnWinRateUpdate(Action<double, double> callback)
        {
            _connection.On<double, double>("WinRateUpdate", (blackRate, whiteRate) => callback(blackRate, whiteRate));
        }

        // 请求计算胜率（在每次落子后调用）
        public async Task RequestWinRateCalculation()
        {
            try
            {
                await _connection.InvokeAsync("CalculateWinRate");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"请求胜率计算失败: {ex.Message}");
                OnError?.Invoke($"请求胜率计算失败: {ex.Message}");
            }
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

        
        // 用户信息类
        public class UserInfo
        {
            public string UserId { get; set; }
            public string UserName { get; set; }
            public int WinTimes { get; set; }
        }
    

        // 登录结果类
        public class LoginResult
        {
            public bool Success { get; set; }
            public string UserId { get; set; }
            public string UserName { get; set; }
            public int WinTimes { get; set; } // << ADD THIS
            public string Message { get; set; }
        }

        // 注册结果类
        public class RegisterResult
        {
            public bool Success { get; set; }
            public string UserId { get; set; }
            public string UserName { get; set; }
            public string Message { get; set; }
        }

        // 服务器登录响应类
        public class LoginResponse
        {
            public bool Success { get; set; }
            public string UserId { get; set; }
            public string UserName { get; set; }
            public int WinTimes { get; set; } // << 添加 WinTimes
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
        // 用于从服务器接收对手信息，与 UserInfo 结构相同
        public class PlayerInfo 
        {
            public string UserId { get; set; }
            public string UserName { get; set; }
            public int WinTimes { get; set; }
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

        

        //请求一个带ai的房间
        public async Task StartAIGame()
        {
            try
            {
                await _connection.InvokeAsync("MatchAI");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"开始AI游戏失败: {ex.Message}");
                OnError?.Invoke($"开始AI游戏失败: {ex.Message}");
            }
        }
        public void OnAIRoomCreated(Action<string, PlayerInfo, PlayerInfo> callback) // AI作为PlayerInfo
        {
            _connection.On<string, PlayerInfo, PlayerInfo>("AIRoomCreated", callback);
        }

        //玩家选择自己持有的颜色
        public async Task SelectPlayerColor(PlayerColor playerColor)
        {
            try
            {
                await _connection.InvokeAsync("SelectColor", playerColor);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"选择棋子颜色失败: {ex.Message}");
                OnError?.Invoke($"选择棋子颜色失败: {ex.Message}");
            }
        }

        // 注册接收AI模式下玩家落子信息的事件，如果传入参数为true，在棋盘上显示落子结果
        public void ReceivePlayerPieceInAIMode(Action<bool, int, int, int> ShowStep)//黑1白2
        {
            _connection.On<bool, int, int, int>("PlayerPieceInfoInAIMode", ShowStep);
        }

        //注册接收AI棋子为黑色时的事件
        public void ReceiveBlackAI(Action<string> DealBlackAI)
        {
            _connection.On<string>("AIColorInfo", DealBlackAI);
        }

        //请求AI落子
        public async Task AskAIPiece()
        {
            try
            {
                await _connection.InvokeAsync("AskAIPiece");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AI模式落子失败: {ex.Message}");
                OnError?.Invoke($"AI模式落子失败: {ex.Message}");
            }
        }

        

        //玩家在AI模式下落子
        public async Task TryPlacePieceInAI(int x, int y)//传入落子位置(x,y)
        {
            try
            {
                await _connection.InvokeAsync("PlacePieceInAIRoom", x, y);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送消息失败: {ex.Message}");
                OnError?.Invoke($"发送落子信息失败: {ex.Message}");
            }
        }

        /*----------------------------------------------------------------分割线------------------------------------------------------------------*/

        // 注册AI游戏结束的事件
        public void OnAIGameEnd(Action<string> callback)
        {
            _connection.On<string>("AIGameOver", callback);
        }

        // 注册AI胜率更新的事件
        public void OnAIWinRateUpdate(Action<double, double> callback)
        {
            _connection.On<double, double>("AIWinRateUpdate", (playerRate, aiRate) => callback(playerRate, aiRate));
        }

        // 退出AI游戏
        public async Task ExitAIGame()
        {
            try
            {
                await _connection.InvokeAsync("ExitAIGame");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"退出AI游戏失败: {ex.Message}");
                OnError?.Invoke($"退出AI游戏失败: {ex.Message}");
            }
        }

        
        public event Action<UserInfo> OnUserInfoUpdated;

        // 添加更新用户信息的方法
        public async Task UpdateUserInfo()
        {
            try
            {
                if (_connection.State != HubConnectionState.Connected)
                {
                    await StartConnectionAsync();
                }

                var response = await _connection.InvokeAsync<LoginResponse>("GetUserInfo", CurrentUser.UserId);
                if (response.Success)
                {
                    CurrentUser = new UserInfo
                    {
                        UserId = response.UserId,
                        UserName = response.UserName,
                        WinTimes = response.WinTimes
                    };
                    OnUserInfoUpdated?.Invoke(CurrentUser);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新用户信息失败: {ex.Message}");
            }
        }

    }
}
