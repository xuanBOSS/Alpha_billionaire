using ChessGame.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using ChessGame.Database;
using ChessGame.Server.Services;
using ChessGame.GameLogic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ChessGame.AI;
//using static ChessGame.Client.SignalRService;

namespace ChessGame.Server.Controllers
{
    // 定义一个简单的 DTO 来传递玩家信息
    public class PlayerInfoDTO
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int WinTimes { get; set; }
    }
    public class RoomManager
    {
        private readonly List<Room> _rooms = new();//维护房间信息的链表
        private readonly IHubContext<GameHub> _hubContext;//允许在Hub类外执行与客户端的交互
        private readonly IDbContextFactory<ChessDbContext> _dbContextFactory;
        private readonly PlayerSessionManager _sessionManager;

        public RoomManager(IHubContext<GameHub> hubContext, IDbContextFactory<ChessDbContext> dbContextFactory, PlayerSessionManager sessionManager)
        {
            _hubContext = hubContext;
            _dbContextFactory = dbContextFactory;
            _sessionManager = sessionManager;
        }

        private MineMap CreateDeepCopy(MineMap original)
        {
            MineMap copy = new MineMap();

            // 复制数组内容而不仅仅是引用
            for (int i = 0; i < MineMap.Size; i++)
            {
                for (int j = 0; j < MineMap.Size; j++)
                {
                    copy.mines[i, j] = original.mines[i, j];
                    copy.numbers[i, j] = original.numbers[i, j];
                }
            }

            return copy;
        }

        //----------------------------------------------------------------------查找用户名----------------------------------------------------------------------
        private async Task<PlayerInfoDTO> GetPlayerInfoDTOAsync(string userId)
        {
            using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
            {
                var player = await dbContext.Players.FindAsync(userId);
                var gameRecord = await dbContext.GameRecords.FirstOrDefaultAsync(gr => gr.UserId == userId);
                return new PlayerInfoDTO
                {
                    UserId = userId,
                    UserName = player?.UserName ?? "未知玩家",
                    WinTimes = gameRecord?.WinTimes ?? 0
                };
            }
        }
        //----------------------------------------------------------------------查找用户名----------------------------------------------------------------------

        // 房间匹配，增加用户ID参数
        public async Task MatchPlayer(string connectionId, string userId)
        {
            // 查找是否有等待中的房间
            var waitingRoom = _rooms.FirstOrDefault(r => !r.IsFull);

            //判断当前请求玩家是否需要等待
            bool WaitPartner = (waitingRoom == null);//如果没有等待中的房间，则玩家需要等待
            await _hubContext.Clients.Client(connectionId)
                   .SendAsync("WaitingForOpponent", WaitPartner);

           

            if (waitingRoom != null)
            {
                try
                {
                    // 1. 先将玩家加入房间
                    waitingRoom.AddPlayer(connectionId, userId);
                    await _hubContext.Groups.AddToGroupAsync(connectionId, waitingRoom.RoomID);

                    // 2. 获取两个玩家的完整信息
                    var player1Info = await GetPlayerInfoDTOAsync(waitingRoom.Player1UserId);
                    var player2Info = await GetPlayerInfoDTOAsync(userId);
                    Console.WriteLine($"已获取玩家信息:");
                    Console.WriteLine($"玩家1: ID={player1Info.UserId}, 名称={player1Info.UserName}, 胜场={player1Info.WinTimes}");
                    Console.WriteLine($"玩家2: ID={player2Info.UserId}, 名称={player2Info.UserName}, 胜场={player2Info.WinTimes}");

                    // 3. 发送匹配成功消息，包含两个玩家的信息
                    Console.WriteLine("准备发送MatchSuccess消息给双方玩家");
                            var matchTasks = new[]
                            {
                        _hubContext.Clients.Client(waitingRoom.Player1)
                            .SendAsync("MatchSuccess", waitingRoom.RoomID, "你是黑方", player1Info, player2Info),
                        _hubContext.Clients.Client(waitingRoom.Player2)
                            .SendAsync("MatchSuccess", waitingRoom.RoomID, "你是白方", player1Info, player2Info)
                    };

                            // 等待匹配消息发送完成
                            await Task.WhenAll(matchTasks);
                            Console.WriteLine("MatchSuccess消息已发送给双方玩家");
                            // 5. 短暂延迟确保客户端准备就绪
                            await Task.Delay(100);
                    

                    // 6. 发送地图数据
                    /* await _hubContext.Clients.Group(waitingRoom.RoomID).SendAsync("MapInfo", transferData);
                     Console.WriteLine($"地图数据已发送到房间 {waitingRoom.RoomID}");*/

                    // 7. 打印调试信息
                    waitingRoom.GameManager.MineMap.PrintDebugBoard();

   
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"房间 {waitingRoom.RoomID} 处理过程出错:");
                    Console.WriteLine($"错误信息: {ex.Message}");
                    Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");

                    // 通知客户端发生错误
                    await _hubContext.Clients.Clients(waitingRoom.Player1, waitingRoom.Player2)
                        .SendAsync("MatchError", "配对过程中发生错误，请重试");
                }
            }
            else
            {
                try
                {
                    // 创建新房间
                    var roomName = Guid.NewGuid().ToString();
                    var newRoom = new Room(roomName, connectionId, userId);
                    _rooms.Add(newRoom);

                    await _hubContext.Groups.AddToGroupAsync(connectionId, roomName);
                    Console.WriteLine($"创建新房间 {roomName}，等待其他玩家加入");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"创建房间时出错: {ex.Message}");
                    await _hubContext.Clients.Client(connectionId)
                        .SendAsync("MatchError", "创建房间失败，请重试");
                }
            }
        
        }

        // 获取玩家名称
        private async Task<string> GetPlayerName(string userId)
        {
            using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
            {
                var player = await dbContext.Players.FirstOrDefaultAsync(p => p.UserId == userId);
                return player?.UserName ?? userId;
            }
        }

        // 离开房间
        public async Task ExitRoom(string connectionId)
        {
            Room room = FindRoomByPlayer(connectionId);
            if (room == null) return;

            // 通知对方玩家
            string otherPlayerConnectionId = room.Player1 == connectionId ? room.Player2 : room.Player1;
            if (!string.IsNullOrEmpty(otherPlayerConnectionId))
            {
                await _hubContext.Clients.Client(otherPlayerConnectionId)
                    .SendAsync("PauseGame", "对方已离开游戏");
            }

            // 从SignalR组中移除
            if (!string.IsNullOrEmpty(room.Player1)) await _hubContext.Groups.RemoveFromGroupAsync(room.Player1, room.RoomID);
            if (!string.IsNullOrEmpty(room.Player2)) await _hubContext.Groups.RemoveFromGroupAsync(room.Player2, room.RoomID);

            // 删除房间
            _rooms.Remove(room);
        }

        // 根据 Player 查找房间
        public Room FindRoomByPlayer(string connectionId)
        {
            // 使用 List.Find 方法根据连接ID查找房间
            return _rooms.Find(room =>
                (room.Player1 == connectionId || room.Player2 == connectionId));
        }

        public async Task GetIdentify(string connectionId)
        {   
            // 查找玩家所在的房间
            Room room = FindRoomByPlayer(connectionId);
            if (room == null) return;

            PlayerInfoDTO playerInfo1 = await GetPlayerInfoDTOAsync(room.Player1UserId);
            PlayerInfoDTO playerInfo2 = await GetPlayerInfoDTOAsync(room.Player2UserId);

            await _hubContext.Clients.Client(connectionId)
                    .SendAsync("IdentifyInfo", playerInfo1.UserName, playerInfo1.WinTimes, playerInfo2.UserName, playerInfo2.WinTimes);
            // 发送玩家身份信息
            /*if (room.Player1 == connectionId)
            {
                await _hubContext.Clients.Client(connectionId)
                    .SendAsync("IdentifyInfo", playerInfo1.UserName, playerInfo1.WinTimes, playerInfo2.UserName, playerInfo2.WinTimes);
            }
            else if (room.Player2 == connectionId)
            {
                PlayerInfoDTO playerInfo = await GetPlayerInfoDTOAsync(room.Player1UserId);
                await _hubContext.Clients.Client(connectionId)
                    .SendAsync("IdentifyInfo", playerInfo2.UserName, playerInfo2.WinTimes, playerInfo1.UserName, playerInfo1.WinTimes);
            }*/
        }

        // 处理落子
        public async Task HandlePiece(string connectionId, int x, int y)
        {
            Room room = FindRoomByPlayer(connectionId);
            if (room == null) return;


            var transferData = room.GameManager.MineMap.GetTransferData();
            if (transferData == null)
            {
                throw new InvalidOperationException("地图数据为空");
            }

            Console.WriteLine($"准备发送地图数据到房间 {room.RoomID}");
            Console.WriteLine($"地图数据大小: {transferData.Size}");
            Console.WriteLine($"Mines数组长度: {transferData.Mines?.Length ?? 0}");
            Console.WriteLine($"Numbers数组长度: {transferData.Numbers?.Length ?? 0}");


            await _hubContext.Clients.Group(room.RoomID).SendAsync("MapInfo", transferData);
            Console.WriteLine($"地图数据已发送到房间 {room.RoomID}");

            string msg = "";
            bool result = room.DealPiece(x, y, connectionId, out msg);

            int color = 0;
            //game manager已经switchturn,传相反的数据
            if (room.GameManager.CurrentPlayer == GameLogic.PlayerColor.Black) color = 2;
            if (room.GameManager.CurrentPlayer == GameLogic.PlayerColor.White) color = 1;

            await _hubContext.Clients.Group(room.RoomID)
                   .SendAsync("PieceInfo", result,x,y,color);

            //---------------------------------------------------------------------双方玩家的胜率---------------------------------------------------------------------------
            //double blackRate = 0, whiteRate = 0;
            ////向双方玩家传递胜率
            //await _hubContext.Clients.Group(room.RoomID)
            //       .SendAsync("RateInfo", blackRate,whiteRate);
            // 修改这部分，计算并发送胜率
            if (result)
            {
                // 使用AIHelper计算胜率
                var aiHelper = new AIHelper();
                double blackRate = aiHelper.CalculateWinProbability(room.GameManager.Board, room.GameManager.MineMap, 1);
                double whiteRate = aiHelper.CalculateWinProbability(room.GameManager.Board, room.GameManager.MineMap, 2);

                // 发送胜率数据给房间内所有玩家
                await _hubContext.Clients.Group(room.RoomID)
                        .SendAsync("WinRateUpdate", blackRate, whiteRate);
            }
            //---------------------------------------------------------------------双方玩家的胜率---------------------------------------------------------------------------

            if (result && msg != "获胜") // 如果落子成功，向双方展示落子并切换轮次
            {
                if (connectionId == room.Player1)
                {
                    await _hubContext.Clients.Client(room.Player1)
                        .SendAsync("State", "Wait");

                    await _hubContext.Clients.Client(room.Player2)
                        .SendAsync("State", "Act");
                }
                else
                {
                    await _hubContext.Clients.Client(room.Player1)
                        .SendAsync("State", "Act");

                    await _hubContext.Clients.Client(room.Player2)
                        .SendAsync("State", "Wait");
                }
            }
            else if (result) // 落子成功且对局结束
            {
                // 获取胜利者和失败者的用户ID
                string winnerConnectionId = connectionId;
                string winnerUserId = connectionId == room.Player1 ? room.Player1UserId : room.Player2UserId;
                string loserUserId = connectionId == room.Player1 ? room.Player2UserId : room.Player1UserId;

                // 更新胜利次数
                await UpdateWinRecord(winnerUserId);

                // 发送游戏结束消息
                /*await _hubContext.Clients.Group(room.RoomID)
                    .SendAsync("GameOver", msg);*/

                if(room.GameManager.CurrentPlayer == PlayerColor.Black)
                {
                    await _hubContext.Clients.Client(room.Player1)
                    .SendAsync("GameOver", "胜利");
                    await _hubContext.Clients.Client(room.Player2)
                        .SendAsync("GameOver", "失败");
                }
                else
                {
                    await _hubContext.Clients.Client(room.Player1)
                    .SendAsync("GameOver", "失败");
                    await _hubContext.Clients.Client(room.Player2)
                        .SendAsync("GameOver", "胜利");
                }

                // 发送排行榜信息给获胜者和失败者
                var playerRank = await GetPlayerRank(winnerUserId);
                var rankChangeMsg = playerRank > 0
                    ? $"恭喜！您当前排名第{playerRank}位"
                    : "您暂未进入排行榜";
                await _hubContext.Clients.Client(winnerConnectionId)
                    .SendAsync("RankInfo", rankChangeMsg, true);
            }
            else // 如果落子失败或者需要爆破
            {
                await _hubContext.Clients.Client(connectionId)
                    .SendAsync("PauseGame", msg);
            }
        }
        // 获取玩家当前排名
        private async Task<int> GetPlayerRank(string userId)
        {
            using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
            {
                // 找出玩家在排行榜中的位置
                var allPlayers = await dbContext.GameRecords
                    .OrderByDescending(g => g.WinTimes)
                    .ToListAsync();

                for (int i = 0; i < allPlayers.Count; i++)
                {
                    if (allPlayers[i].UserId == userId)
                    {
                        return i + 1; // 返回1-based索引作为排名
                    }
                }

                return 0; // 玩家不在排行榜中
            }
        }
        // 更新胜利记录
        private async Task UpdateWinRecord(string userId)
        {
            try
            {
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    // 查找用户的游戏记录
                    var gameRecord = await dbContext.GameRecords.FindAsync(userId);
                    if (gameRecord != null)
                    {
                        // 更新胜利次数
                        gameRecord.WinTimes += 1;
                        await dbContext.SaveChangesAsync();

                        // 通知所有客户端排行榜已更新
                        await NotifyLeaderboardUpdated();
                    }
                    else
                    {
                        // 如果记录不存在，创建新记录
                        var player = await dbContext.Players.FindAsync(userId);
                        if (player != null)
                        {
                            var newRecord = new GameRecord
                            {
                                UserId = player.UserId,
                                UserName = player.UserName,
                                WinTimes = 1
                            };
                            dbContext.GameRecords.Add(newRecord);
                            await dbContext.SaveChangesAsync();

                            // 通知所有客户端排行榜已更新
                            await NotifyLeaderboardUpdated();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不中断游戏流程
                Console.WriteLine($"更新胜利记录失败: {ex.Message}");
            }
        }

        // 通知所有客户端排行榜已更新
        private async Task NotifyLeaderboardUpdated()
        {
            try
            {
                // 获取最新的排行榜数据
                var leaderboard = await GetTopPlayersAsync(10);

                // 将排行榜数据广播给所有客户端
                await _hubContext.Clients.All.SendAsync("LeaderboardUpdated", leaderboard);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"通知排行榜更新失败: {ex.Message}");
            }
        }

        // 获取排行榜前N名玩家
        private async Task<List<LeaderboardEntry>> GetTopPlayersAsync(int count)
        {
            using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
            {
                // 获取按胜利次数排序的前N名玩家
                var topPlayers = await dbContext.GameRecords
                    .OrderByDescending(g => g.WinTimes)
                    .Take(count)
                    .ToListAsync();

                var result = new List<LeaderboardEntry>();
                int rank = 1;
                int lastScore = -1;
                int lastRank = 0;

                foreach (var player in topPlayers)
                {
                    // 处理相同分数的情况
                    if (player.WinTimes != lastScore)
                    {
                        lastRank = rank;
                        lastScore = player.WinTimes;
                    }

                    result.Add(new LeaderboardEntry
                    {
                        UserId = player.UserId,
                        UserName = player.UserName,
                        WinTimes = player.WinTimes,
                        Rank = lastRank
                    });

                    rank++;
                }

                return result;
            }
        }
    }
}
