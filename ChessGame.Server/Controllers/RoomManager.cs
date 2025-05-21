using ChessGame.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using ChessGame.Database;
using ChessGame.Server.Services;
using ChessGame.GameLogic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ChessGame.Server.Controllers
{
    //public class RoomManager
    //{
    //    private readonly List<Room> _rooms = new();//维护房间信息的链表
    //    private readonly IHubContext<GameHub> _hubContext;//允许在Hub类外执行与客户端的交互

    //    public RoomManager(IHubContext<GameHub> hubContext)
    //    {
    //        _hubContext = hubContext;
    //    }

    //    //房间匹配
    //    public async Task MatchPlayer(string connectionId)
    //    {
    //        // 查找是否有等待中的房间
    //        var waitingRoom = _rooms.FirstOrDefault(r => !r.IsFull);

    //        //判断当前请求玩家是否需要等待
    //        bool WaitPartner = (waitingRoom == null);//如果没有等待中的房间，则玩家需要等待
    //        await _hubContext.Clients.Client(connectionId)
    //               .SendAsync("WaitingForOpponent", WaitPartner);

    //        if (waitingRoom != null)//加入玩家2
    //        {
    //            // 加入已有房间
    //            waitingRoom.AddPlayer(connectionId);

    //            await _hubContext.Groups.AddToGroupAsync(connectionId, waitingRoom.RoomID);

    //            // 通知双方配对成功
    //            await _hubContext.Clients.Client(waitingRoom.Player1)
    //                .SendAsync("MatchSuccess", waitingRoom.RoomID, "你是黑方");
    //            await _hubContext.Clients.Client(waitingRoom.Player2)
    //                .SendAsync("MatchSuccess", waitingRoom.RoomID, "你是白方");

    //        }
    //        else//加入玩家1
    //        {
    //            // 创建新房间
    //            var roomName = Guid.NewGuid().ToString();//为房间生成唯一的ID
    //            var newRoom = new Room(roomName, connectionId);
    //            _rooms.Add(newRoom);

    //            await _hubContext.Groups.AddToGroupAsync(connectionId, roomName);//将客户端加入指定的组，每个房间中的玩家在同一组
    //            /*await _hubContext.Clients.Client(connectionId)//向客户端发送等待匹配的消息
    //                .SendAsync("WaitingForOpponent");*/
    //        }
    //    }


    //    //离开房间
    //    public async Task ExitRoom(string connectionId)
    //    {
    //        Room room = FindRoomByPlayer(connectionId);
    //        //删除房间
    //        _rooms.Remove(room);
    //    }

    //    // 根据 Player 查找房间
    //    public Room FindRoomByPlayer(string connectionId)
    //    {
    //        // 使用 List.Find 方法根据 RoomID 查找房间
    //        return _rooms.Find(room => 
    //        (room.Player1 == connectionId|| room.Player2 == connectionId));
    //    }

    //    //处理落子
    //    public async Task HandlePiece(string connectionId,int x,int y)
    //    {
    //        Room room = FindRoomByPlayer(connectionId);


    //        string msg;
    //        bool result = room.DealPiece(x, y, connectionId, out msg);

    //        await _hubContext.Clients.Group(room.RoomID)
    //               .SendAsync("PieceInfo", result);

    //        if(result && msg != "获胜")//如果落子成功，向双方展示落子并切换轮次
    //        {
    //            if (connectionId == room.Player1)
    //            {
    //                await _hubContext.Clients.Client(room.Player1)
    //               .SendAsync("State", "Wait");

    //                await _hubContext.Clients.Client(room.Player2)
    //               .SendAsync("State", "Act");
    //            }
    //            else
    //            {
    //                await _hubContext.Clients.Client(room.Player1)
    //               .SendAsync("State", "Act");

    //                await _hubContext.Clients.Client(room.Player2)
    //               .SendAsync("State", "Wait");
    //            }
    //        }
    //        else if(result)//落子成功且对局结束
    //        {
    //            await _hubContext.Clients.Group(room.RoomID)
    //               .SendAsync("GameOver", msg);
    //        }
    //        else //如果落子失败或者需要爆破
    //        {
    //            await _hubContext.Clients.Client(connectionId)
    //               .SendAsync("PauseGame", msg);
    //        }



    //    }
    //}
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

        // 房间匹配，增加用户ID参数
        public async Task MatchPlayer(string connectionId, string userId)
        {
            // 查找是否有等待中的房间
            var waitingRoom = _rooms.FirstOrDefault(r => !r.IsFull);

            //判断当前请求玩家是否需要等待
            bool WaitPartner = (waitingRoom == null);//如果没有等待中的房间，则玩家需要等待
            await _hubContext.Clients.Client(connectionId)
                   .SendAsync("WaitingForOpponent", WaitPartner);

            if (waitingRoom != null)//加入玩家2
            {
                // 加入已有房间
                waitingRoom.AddPlayer(connectionId, userId);  // 传递用户ID

                await _hubContext.Groups.AddToGroupAsync(connectionId, waitingRoom.RoomID);

                // 获取玩家名称
                var player1Name = await GetPlayerName(waitingRoom.Player1UserId);
                var player2Name = await GetPlayerName(userId);

                // 通知双方配对成功，并传递对方的用户名
                await _hubContext.Clients.Client(waitingRoom.Player1)
                    .SendAsync("MatchSuccess", waitingRoom.RoomID, $"你是黑方，对手是 {player2Name}");
                await _hubContext.Clients.Client(waitingRoom.Player2)
                    .SendAsync("MatchSuccess", waitingRoom.RoomID, $"你是白方，对手是 {player1Name}");

                //将地图传送给双方玩家(只需要在最开始的时候将客户端与服务端的地图同步)
                await _hubContext.Clients.Group(waitingRoom.RoomID)
                   .SendAsync("MapInfo", waitingRoom.GameManager.MineMap);
            }
            else//加入玩家1
            {
                // 创建新房间
                var roomName = Guid.NewGuid().ToString();//为房间生成唯一的ID
                var newRoom = new Room(roomName, connectionId, userId);  // 传递用户ID
                _rooms.Add(newRoom);

                await _hubContext.Groups.AddToGroupAsync(connectionId, roomName);//将客户端加入指定的组，每个房间中的玩家在同一组
            }
        }

        // 获取玩家名称
        private async Task<string> GetPlayerName(string userId)
        {
            using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
            {
                var player = await dbContext.Players.FindAsync(userId);
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

        // 处理落子
        public async Task HandlePiece(string connectionId, int x, int y)
        {
            Room room = FindRoomByPlayer(connectionId);
            if (room == null) return;

            string msg;
            bool result = room.DealPiece(x, y, connectionId, out msg);

            int color = 0;
            if (room.GameManager.CurrentPlayer == GameLogic.PlayerColor.Black) color = 1;
            if (room.GameManager.CurrentPlayer == GameLogic.PlayerColor.White) color = 2;

            await _hubContext.Clients.Group(room.RoomID)
                   .SendAsync("PieceInfo", result,x,y,color);


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
