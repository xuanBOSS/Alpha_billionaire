/*namespace ChessGame.Server.Controllers
{
    public class AIRoonManager
    {
    }
}*/

using ChessGame.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using ChessGame.Database;
using ChessGame.Server.Services;
using ChessGame.GameLogic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ChessGame.AI;

namespace ChessGame.Server.Controllers
{
    public class AIRoomManager
    {
        private readonly List<AIRoom> _airooms = new();//维护房间信息的链表
        private readonly IHubContext<GameHub> _hubContext;//允许在Hub类外执行与客户端的交互
        private readonly IDbContextFactory<ChessDbContext> _dbContextFactory;
        private readonly PlayerSessionManager _sessionManager;

        public AIRoomManager(IHubContext<GameHub> hubContext, IDbContextFactory<ChessDbContext> dbContextFactory, PlayerSessionManager sessionManager)
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

        

        //增加新房间
        public async Task MatchAI(string connectionId, string userId)
        {
            try
            {
                // 创建新房间
                var roomName = Guid.NewGuid().ToString();
                var newRoom = new AIRoom(roomName, connectionId, userId);
                _airooms.Add(newRoom);

                await _hubContext.Groups.AddToGroupAsync(connectionId, roomName);
                /*Console.WriteLine($"创建新房间 {roomName}，等待其他玩家加入");*/
                Console.WriteLine($"创建新房间 {roomName}，AI加入对局");

                // 1. 获取玩家名称
                var playerName = await GetPlayerName(newRoom.PlayerUserId);

                // 2. 发送匹配成功消息
                var task = _hubContext.Clients.Client(newRoom.Player)
                 .SendAsync("MatchSuccess", newRoom.RoomID, "AI已加入对局");

                await task; // 等待发送完成

                // 3. 短暂延迟确保客户端准备就绪
                await Task.Delay(100);

                // 4. 打印调试信息
                newRoom.GameManager.MineMap.PrintDebugBoard();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建房间时出错: {ex.Message}");
                await _hubContext.Clients.Client(connectionId)
                    .SendAsync("MatchError", "创建房间失败，请重试");
            }

        }

        //玩家选择棋子颜色
        public async Task SelectColor(string connectionId, string userId, PlayerColor playerColor)
        {
            try
            {
                AIRoom airoom = FindRoomByPlayer(connectionId);

                airoom.setPlayerColor(playerColor);

                //发送设置棋子颜色成功消息
                var task = _hubContext.Clients.Client(airoom.Player)
                 .SendAsync("SetColorSuccess", airoom.RoomID, "设置棋子颜色成功");

                await task; // 等待发送完成

                //短暂延迟确保客户端准备就绪
                await Task.Delay(100);

                if(playerColor == PlayerColor.White)
                {
                    var task1 = _hubContext.Clients.Client(airoom.Player)
                        .SendAsync("AIColorInfo", "AI:黑色");

                }

                /*var task1 = _hubContext.Clients.Client(airoom.Player)
                 .SendAsync("AIColorInfo", "");

                await task; // 等待发送完成

                //短暂延迟确保客户端准备就绪
                await Task.Delay(100);*/
            }
            catch (Exception ex)
            {
                Console.WriteLine($"选择棋子颜色时出错: {ex.Message}");
                await _hubContext.Clients.Client(connectionId)
                    .SendAsync("MatchError", "选择棋子颜色失败，请重试");
            }

        }

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

        public async Task GetIdentify(string connectionId)
        {
            // 查找玩家所在的房间
            AIRoom airoom = FindRoomByPlayer(connectionId);
            if (airoom == null) return;

            PlayerInfoDTO playerInfo1 = await GetPlayerInfoDTOAsync(airoom.PlayerUserId);

            /*await _hubContext.Clients.Client(connectionId)
                    .SendAsync("IdentifyInfo", playerInfo1.UserName, playerInfo1.WinTimes, playerInfo2.UserName, playerInfo2.WinTimes);*/
            // 发送玩家身份信息
            if (airoom.PlayerColor == PlayerColor.Black)
            {
                await _hubContext.Clients.Client(connectionId)
                    .SendAsync("IdentifyInfo", playerInfo1.UserName, playerInfo1.WinTimes, "机器人", -1);
            }
            else if (airoom.PlayerColor == PlayerColor.White)
            {
                await _hubContext.Clients.Client(connectionId)
                    .SendAsync("IdentifyInfo", "机器人", -1, playerInfo1.UserName, playerInfo1.WinTimes);
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
            AIRoom airoom = FindRoomByPlayer(connectionId);
            if (airoom == null) return;

            // 通知对方玩家
            /*string otherPlayerConnectionId = room.Player1 == connectionId ? room.Player2 : room.Player1;
            if (!string.IsNullOrEmpty(otherPlayerConnectionId))
            {
                await _hubContext.Clients.Client(otherPlayerConnectionId)
                    .SendAsync("PauseGame", "对方已离开游戏");
            }*/

            // 删除房间
            _airooms.Remove(airoom);
        }

        // 根据 Player 查找房间
        public AIRoom FindRoomByPlayer(string connectionId)
        {
            // 使用 List.Find 方法根据连接ID查找房间
            /*return _rooms.Find(room =>
                (room.Player1 == connectionId || room.Player2 == connectionId));*/
            return _airooms.Find(airoom =>
                (airoom.Player == connectionId));
        }

        // 处理落子
        public async Task HandlePiece(string connectionId, int x, int y)
        {
            AIRoom airoom = FindRoomByPlayer(connectionId);
            if (airoom == null) return;

            var transferData = airoom.GameManager.MineMap.GetTransferData();
            if (transferData == null)
            {
                throw new InvalidOperationException("地图数据为空");
            }

            Console.WriteLine($"准备发送地图数据到房间 {airoom.RoomID}");
            Console.WriteLine($"地图数据大小: {transferData.Size}");
            Console.WriteLine($"Mines数组长度: {transferData.Mines?.Length ?? 0}");
            Console.WriteLine($"Numbers数组长度: {transferData.Numbers?.Length ?? 0}");


            await _hubContext.Clients.Group(airoom.RoomID).SendAsync("MapInfo", transferData);
            Console.WriteLine($"地图数据已发送到房间 {airoom.RoomID}");

            string msg;
            bool result = airoom.DealPiece(x, y, connectionId, out msg);

            int color = 0;
            //game manager已经switchturn,传相反的数据
            if (airoom.GameManager.CurrentPlayer == GameLogic.PlayerColor.Black) color = 2;
            if (airoom.GameManager.CurrentPlayer == GameLogic.PlayerColor.White) color = 1;

            await _hubContext.Clients.Group(airoom.RoomID)
                   .SendAsync("PlayerPieceInfoInAIMode", result, x, y, color);

            // 计算并发送胜率
            if (result)
            {
                // 使用AIHelper计算胜率
                var aiHelper = new AIHelper();

                // 确定玩家和AI的颜色
                int playerColorValue = airoom.PlayerColor == PlayerColor.Black ? 1 : 2;
                int aiColorValue = airoom.PlayerColor == PlayerColor.Black ? 2 : 1;

                double playerWinRate = aiHelper.CalculateWinProbability(airoom.GameManager.Board, airoom.GameManager.MineMap, playerColorValue);
                double aiWinRate = aiHelper.CalculateWinProbability(airoom.GameManager.Board, airoom.GameManager.MineMap, aiColorValue);

                // 发送胜率信息
                await _hubContext.Clients.Client(connectionId).SendAsync("AIWinRateUpdate", playerWinRate, aiWinRate);
            }

            if (result && msg != "获胜") // 如果落子成功，向玩家展示落子并切换轮次
            {
                if (connectionId == airoom.Player)
                {
                    await _hubContext.Clients.Client(airoom.Player)
                        .SendAsync("State", "Wait");
                }
                else
                {
                    await _hubContext.Clients.Client(airoom.Player)
                        .SendAsync("State", "Act");
                }
            }
            else if (result) // 落子成功且对局结束
            {
                // 获取胜利者和失败者的用户ID
                /*string winnerConnectionId = connectionId;
                string winnerUserId = connectionId == room.Player1 ? room.Player1UserId : room.Player2UserId;
                string loserUserId = connectionId == room.Player1 ? room.Player2UserId : room.Player1UserId;

                // 更新胜利次数
                await UpdateWinRecord(winnerUserId);*/

                //如果currentplayer与玩家颜色相同，则更新胜利次数
                if(airoom.GameManager.CurrentPlayer == airoom.PlayerColor) await UpdateWinRecord(connectionId);


                // -----------------------------------------------------发送游戏结束消息给所有玩家-------------------------------------------------------

                /*if (airoom.GameManager.CurrentPlayer == airoom.PlayerColor)
                {
                    await _hubContext.Clients.Client(airoom.Player)
                    .SendAsync("GameOver", "胜利");
                }
                else
                {
                    await _hubContext.Clients.Client(airoom.Player)
                    .SendAsync("GameOver", "失败");
                }*/

                await Task.Delay(1000); // 异步等待1秒

                await _hubContext.Clients.Group(airoom.RoomID)
                    .SendAsync("GameOver", "胜利");

                /* 没有发送排行榜信息 */

                // 发送排行榜信息给获胜者和失败者
                /*var playerRank = await GetPlayerRank(winnerUserId);
                var rankChangeMsg = playerRank > 0
                    ? $"恭喜！您当前排名第{playerRank}位"
                    : "您暂未进入排行榜";
                await _hubContext.Clients.Client(winnerConnectionId)
                    .SendAsync("RankInfo", rankChangeMsg, true);*/
            }
            else // 如果落子失败或者需要爆破
            {
                await _hubContext.Clients.Client(connectionId)
                    .SendAsync("PauseGame", msg);
            }

            //更新地图信息
            transferData = airoom.GameManager.MineMap.GetTransferData();
            if (transferData == null)
            {
                throw new InvalidOperationException("地图数据为空");
            }

            Console.WriteLine($"准备发送地图数据到房间 {airoom.RoomID}");
            Console.WriteLine($"地图数据大小: {transferData.Size}");
            Console.WriteLine($"Mines数组长度: {transferData.Mines?.Length ?? 0}");
            Console.WriteLine($"Numbers数组长度: {transferData.Numbers?.Length ?? 0}");


            //await Task.Delay(100); // 异步等待0.1秒
            await _hubContext.Clients.Group(airoom.RoomID).SendAsync("MapInfo", transferData);
            Console.WriteLine($"地图数据已发送到房间 {airoom.RoomID}");
        }

        //AI落子
        public async Task HandleAIPiece(string connectionId)
        {
            try
            {
                Console.WriteLine("AI开始落子");

                AIRoom airoom = FindRoomByPlayer(connectionId);//找到请求玩家所在的房间

                var transferData = airoom.GameManager.MineMap.GetTransferData();
                if (transferData == null)
                {
                    throw new InvalidOperationException("地图数据为空");
                }

                Console.WriteLine($"准备发送地图数据到房间 {airoom.RoomID}");
                Console.WriteLine($"地图数据大小: {transferData.Size}");
                Console.WriteLine($"Mines数组长度: {transferData.Mines?.Length ?? 0}");
                Console.WriteLine($"Numbers数组长度: {transferData.Numbers?.Length ?? 0}");


                await _hubContext.Clients.Group(airoom.RoomID).SendAsync("MapInfo", transferData);
                Console.WriteLine($"地图数据已发送到房间 {airoom.RoomID}");

                int AIx = 0;
                int AIy = 0;
                bool ifAIWin = airoom.GetAIMove(out AIx,out AIy);

                PlayerColor aiColor = (airoom.PlayerColor == PlayerColor.Black)?PlayerColor.White : PlayerColor.Black;

                await Task.Delay(1000); // 异步等待1秒

                await _hubContext.Clients.Group(airoom.RoomID)
                   .SendAsync("PieceInfo", true, AIx, AIy, aiColor);

                // 计算并发送胜率
                // 使用AIHelper计算胜率
                var aiHelper = new AIHelper();

                // 确定玩家和AI的颜色
                int playerColorValue = airoom.PlayerColor == PlayerColor.Black ? 1 : 2;
                int aiColorValue = airoom.PlayerColor == PlayerColor.Black ? 2 : 1;

                double playerWinRate = aiHelper.CalculateWinProbability(airoom.GameManager.Board, airoom.GameManager.MineMap, playerColorValue);
                double aiWinRate = aiHelper.CalculateWinProbability(airoom.GameManager.Board, airoom.GameManager.MineMap, aiColorValue);

                // 发送胜率信息
                await _hubContext.Clients.Client(connectionId).SendAsync("AIWinRateUpdate", playerWinRate, aiWinRate);


                if (ifAIWin)
                {
                    await Task.Delay(1000); // 异步等待1秒

                    await _hubContext.Clients.Client(airoom.Player)
                    .SendAsync("GameOver", "失败");
                }

                //更新地图信息
                transferData = airoom.GameManager.MineMap.GetTransferData();
                if (transferData == null)
                {
                    throw new InvalidOperationException("地图数据为空");
                }

                Console.WriteLine($"准备发送地图数据到房间 {airoom.RoomID}");
                Console.WriteLine($"地图数据大小: {transferData.Size}");
                Console.WriteLine($"Mines数组长度: {transferData.Mines?.Length ?? 0}");
                Console.WriteLine($"Numbers数组长度: {transferData.Numbers?.Length ?? 0}");


                //await Task.Delay(100); // 异步等待0.1秒
                await _hubContext.Clients.Group(airoom.RoomID).SendAsync("MapInfo", transferData);
                Console.WriteLine($"地图数据已发送到房间 {airoom.RoomID}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"AI落子时出错: {ex.Message}");
                await _hubContext.Clients.Client(connectionId)
                    .SendAsync("MatchError", "AI落子失败，请重试");
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
