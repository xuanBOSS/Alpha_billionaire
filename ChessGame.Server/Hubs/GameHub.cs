using ChessGame.Server.Controllers;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using ChessGame.Database;
using Microsoft.EntityFrameworkCore;
using ChessGame.Server.Services;

namespace ChessGame.Server.Hubs
{
    public class GameHub : Hub
    {
        private readonly RoomManager _roomManager;
        //private readonly ChessDbContext _dbContext;
        private readonly IDbContextFactory<ChessDbContext> _dbContextFactory; // 修改为DbContextFactory
        private readonly PlayerSessionManager _sessionManager;
        private readonly AIService _aiService;

        public GameHub(RoomManager roomManager, IDbContextFactory<ChessDbContext> dbContextFactory, PlayerSessionManager sessionManager, AIService aiService)
        {
            _roomManager = roomManager;
            _dbContextFactory = dbContextFactory; // 使用工厂
            _sessionManager = sessionManager;
            _aiService = aiService;
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

                return new LoginResponse
                {
                    Success = true,
                    UserId = player.UserId,
                    UserName = player.UserName
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

        //public async Task StartMatch()//实现房间匹配
        //{
        //    await _roomManager.MatchPlayer(Context.ConnectionId);
        //}

        //public async Task PlacePiece(int x, int y)//找到房间传入落子信息
        //{
        //    await _roomManager.HandlePiece(Context.ConnectionId, x, y);
        //}

        //public async Task ExitRoom()//离开房间
        //{
        //    await _roomManager.ExitRoom(Context.ConnectionId);
        //}


        ////回复testWindow的函数
        //// 客户端调用这个方法来发送消息
        //public async Task SendMessage(string user, string message)
        //{
        //    // 发送消息到所有连接的客户端
        //    await Clients.All.SendAsync("ReceiveMessage", user, message); // 向所有连接的客户端发送消息
        //}
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
    }

    
    // 响应模型
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
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