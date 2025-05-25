using ChessGame.Server.Controllers;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using ChessGame.Database;
using Microsoft.EntityFrameworkCore;
using ChessGame.Server.Services;
using ChessGame.AI;
using ChessGame.GameLogic;

namespace ChessGame.Server.Hubs
{
    public class GameHub : Hub
    {
        private readonly RoomManager _roomManager;
        private readonly AIRoomManager _airoomManager;
        //private readonly ChessDbContext _dbContext;
        private readonly IDbContextFactory<ChessDbContext> _dbContextFactory; // 修改为DbContextFactory
        private readonly PlayerSessionManager _sessionManager;
        private readonly AIService _aiService;

        //private static AIHelper aiHelper;
        //public static GameManager gameManager;

        public GameHub(RoomManager roomManager, IDbContextFactory<ChessDbContext> dbContextFactory, PlayerSessionManager sessionManager, AIService aiService, AIRoomManager airoomManager)
        {
            _roomManager = roomManager;
            _dbContextFactory = dbContextFactory; // 使用工厂
            _sessionManager = sessionManager;
            _aiService = aiService;
            _airoomManager = airoomManager;
            //aiHelper = ai;
            //gameManager = game;
        }

        // 登录方法
        public async Task<LoginResponse> Login(string userId, string password)
        {
            // 使用工厂创建DbContext
            using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
            {
                var player = await dbContext.Players.SingleOrDefaultAsync(p => p.UserId == userId);

                if (player == null || player.PassWord != password)
                {
                    return new LoginResponse { Success = false, Message = "用户名或密码错误" };
                }

                // 登录成功，记录用户会话
                _sessionManager.AddSession(userId, Context.ConnectionId);

                // 获取用户的胜场次数
                var gameRecord = await dbContext.GameRecords.FirstOrDefaultAsync(gr => gr.UserId == player.UserId);
                int winTimes = gameRecord?.WinTimes ?? 0;

                return new LoginResponse
                {
                    Success = true,
                    UserId = player.UserId,
                    UserName = player.UserName,
                    WinTimes = winTimes, // << POPULATE WinTimes
                    Message = "登录成功"
                };
            }
        }

        // 注册方法
        public async Task<RegisterResponse> Register(string userId, string password, string userName)
        {
            // 使用工厂创建DbContext
            using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
            {
                // 检查用户是否已存在
                var existingPlayer = await dbContext.Players.FindAsync(userId);
                if (existingPlayer != null)
                {
                    return new RegisterResponse { Success = false, Message = "用户ID已存在" };
                }

                // 添加新用户
                var player = new Player
                {
                    UserId = userId,
                    PassWord = password,
                    UserName = userName
                };
                dbContext.Players.Add(player);
                await dbContext.SaveChangesAsync();

                // 添加默认游戏记录
                var gameRecord = new GameRecord
                {
                    UserId = player.UserId,
                    UserName = player.UserName,
                    WinTimes = 0
                };
                dbContext.GameRecords.Add(gameRecord);
                await dbContext.SaveChangesAsync();

                return new RegisterResponse { Success = true, UserId = userId, UserName = userName };
            }
        }

        // 获取排行榜
        public async Task<List<LeaderboardEntry>> GetLeaderboard()
        {
            // 使用工厂创建DbContext
            using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
            {
                var leaderboard = await dbContext.GameRecords
                    .OrderByDescending(g => g.WinTimes)
                    .Take(10) // 限制返回前10名
                    .ToListAsync();

                var result = new List<LeaderboardEntry>();
                int rank = 1;
                int lastScore = -1;
                int lastRank = 0;

                foreach (var record in leaderboard)
                {
                    if (record.WinTimes != lastScore)
                    {
                        lastRank = rank;
                        lastScore = record.WinTimes;
                    }

                    result.Add(new LeaderboardEntry
                    {
                        UserId = record.UserId,
                        UserName = record.UserName,
                        WinTimes = record.WinTimes,
                        Rank = lastRank
                    });

                    rank++;
                }

                return result;
            }
        }

        // 房间匹配方法
        public async Task StartMatch()
        {
            // 检查用户是否已登录
            string userId = _sessionManager.GetUserId(Context.ConnectionId);
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("MatchError", "请先登录");
                return;
            }

            // 继续使用原有的匹配逻辑，但传入用户ID
            await _roomManager.MatchPlayer(Context.ConnectionId, userId);
        }

        public async Task GetIdentify()
        {
            // 检查用户是否已登录
            string userId = _sessionManager.GetUserId(Context.ConnectionId);
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("MatchError", "请先登录");
                return;
            }

            //查询玩家身份
            await _roomManager.GetIdentify(Context.ConnectionId);
        }



        //------------------------------------AI房间的新方法----------------------------------------------------

        public async Task GetIdentifyInAIMode()
        {
            // 检查用户是否已登录
            string userId = _sessionManager.GetUserId(Context.ConnectionId);
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("MatchError", "请先登录");
                return;
            }

            //查询玩家身份
            await _airoomManager.GetIdentify(Context.ConnectionId);
        }

        // AI房间匹配方法
        public async Task MatchAI()
        {
            // 检查用户是否已登录
            string userId = _sessionManager.GetUserId(Context.ConnectionId);
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("MatchError", "请先登录");
                return;
            }

            // 继续使用原有的匹配逻辑，但传入用户ID
            await _airoomManager.MatchAI(Context.ConnectionId, userId);
        }

        // AI对局玩家棋子颜色选择方法
        public async Task SelectColor(PlayerColor playerColor)
        {
            // 检查用户是否已登录
            string userId = _sessionManager.GetUserId(Context.ConnectionId);
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("MatchError", "请先登录");
                return;
            }

            // 继续使用原有的匹配逻辑，但传入用户ID
            await _airoomManager.SelectColor(Context.ConnectionId, userId, playerColor);
        }

        // AI房间处理玩家落子
        public async Task PlacePieceInAIRoom(int x, int y)
        {
            // 检查用户是否已登录
            string userId = _sessionManager.GetUserId(Context.ConnectionId);
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            await _airoomManager.HandlePiece(Context.ConnectionId, x, y);
        }

        // AI房间处理AI落子
        public async Task AskAIPiece()
        {
            // 检查用户是否已登录
            string userId = _sessionManager.GetUserId(Context.ConnectionId);
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            await _airoomManager.HandleAIPiece(Context.ConnectionId);
        }

        // 退出AI房间
        public async Task ExitAIRoom()
        {
            await _airoomManager.ExitRoom(Context.ConnectionId);
        }

        //------------------------------------------------------------------------------------------------


        // 处理落子
        public async Task PlacePiece(int x, int y)
        {
            // 检查用户是否已登录
            string userId = _sessionManager.GetUserId(Context.ConnectionId);
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            await _roomManager.HandlePiece(Context.ConnectionId, x, y);
        }


        // 添加计算胜率的方法
        public async Task CalculateWinRate()
        {
            // 获取当前用户ID
            string userId = _sessionManager.GetUserId(Context.ConnectionId);
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            // 获取用户当前房间
            var room = _roomManager.FindRoomByPlayer(Context.ConnectionId);
            if (room == null)
            {
                // 检查是否在AI房间
                var aiRoom = _airoomManager.FindRoomByPlayer(Context.ConnectionId);
                if (aiRoom != null)
                {
                    // 使用AIHelper计算胜率
                    var aiHelper = new AIHelper();

                    // 确定玩家和AI的颜色
                    int playerColorValue = aiRoom.PlayerColor == PlayerColor.Black ? 1 : 2;
                    int aiColorValue = aiRoom.PlayerColor == PlayerColor.Black ? 2 : 1;

                    double playerWinRate = aiHelper.CalculateWinProbability(aiRoom.GameManager.Board, aiRoom.GameManager.MineMap, playerColorValue);
                    double aiWinRate = aiHelper.CalculateWinProbability(aiRoom.GameManager.Board, aiRoom.GameManager.MineMap, aiColorValue);

                    // AI模式下使用 AIWinRateUpdate 事件
                    await Clients.Client(Context.ConnectionId).SendAsync("AIWinRateUpdate", playerWinRate, aiWinRate);
                    return;
                }
                return;
            }

            // 使用AIHelper计算胜率
            var helper = new AIHelper();
            double blackWinRate = helper.CalculateWinProbability(room.GameManager.Board, room.GameManager.MineMap, 1);
            double whiteWinRate = helper.CalculateWinProbability(room.GameManager.Board, room.GameManager.MineMap, 2);

            // 发送胜率信息给房间内所有玩家
            await Clients.Group(room.RoomID).SendAsync("WinRateUpdate", blackWinRate, whiteWinRate);

        }



        // 退出房间
        public async Task ExitRoom()
        {
            await _roomManager.ExitRoom(Context.ConnectionId);
        }

        // 客户端断开连接
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string userId = _sessionManager.GetUserId(Context.ConnectionId);
            if (!string.IsNullOrEmpty(userId))
            {
                await ExitRoom();
                _sessionManager.RemoveSession(Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // 客户端调用这个方法来发送消息
        public async Task SendMessage(string user, string message)
        {
            // 发送消息到所有连接的客户端
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        // 获取用户信息方法
        public async Task<LoginResponse> GetUserInfo(string userId)
        {
            using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
            {
                var player = await dbContext.Players.FindAsync(userId);
                if (player == null)
                {
                    return new LoginResponse { Success = false, Message = "用户不存在" };
                }

                var gameRecord = await dbContext.GameRecords.FirstOrDefaultAsync(gr => gr.UserId == player.UserId);
                int winTimes = gameRecord?.WinTimes ?? 0;

                return new LoginResponse
                {
                    Success = true,
                    UserId = player.UserId,
                    UserName = player.UserName,
                    WinTimes = winTimes,
                    Message = "获取用户信息成功"
                };
            }
        }

    }


    // 响应模型
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int WinTimes { get; set; }
        public string Message { get; set; }
    }

    public class RegisterResponse
    {
        public bool Success { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Message { get; set; }
    }

    public class LeaderboardEntry
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int WinTimes { get; set; }
        public int Rank { get; set; }
    }

}