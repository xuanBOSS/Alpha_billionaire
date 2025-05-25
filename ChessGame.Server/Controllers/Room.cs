using ChessGame.GameLogic;
using System.Windows.Navigation;
using ChessGame.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChessGame.AI;

namespace ChessGame.Server.Controllers
{
    public class Room//除了玩家和房间信息外，还需要维护一个棋盘
    {
        public string RoomID {  get;private set; }//房间ID
        public string Player1 { get; set; }//玩家1的ID（黑棋）
        public string Player2 {  get; set; }//玩家2的ID（白棋）

        public string Player1UserId { get; set; }//玩家1的用户ID
        public string Player2UserId { get; set; }//玩家2的用户ID
        public   AIHelper AIHelper { get; set; } = new AIHelper(15);//AI助手，用于计算落子位置和胜率
        public bool IsFull => !string.IsNullOrEmpty(Player1) && !string.IsNullOrEmpty(Player2);//房间是否已满
        public bool IfEnd => GameManager.IsGameOver;//对局是否结束
        public  GameManager GameManager { get; set; }//对局管理
        
        public Room(string roomid, string player1ConnectionId, string player1UserId)//在RoomManager中生成唯一的房间号传入，创建房间
        {
            RoomID = roomid;
            Player1 = player1ConnectionId;
            Player1UserId = player1UserId;

            // 创建游戏管理器，使用默认的玩家对战模式
            GameManager = new GameManager(GameMode.PlayerVsPlayer);

            // 订阅游戏结束事件
            GameManager.GameEnded += OnGameEnded;
        }

        public void AddPlayer(string player2ConnectionId, string player2UserId)
        {
            Player2 = player2ConnectionId;
            Player2UserId = player2UserId;
        }

        // 判断是否是当前玩家的回合
        public bool IsPlayerTurn(string connectionId)
        {
            // 黑棋先手，对应Player1；白棋后手，对应Player2
            bool isPlayer1Turn = GameManager.CurrentPlayer == PlayerColor.Black;
            return (isPlayer1Turn && connectionId == Player1) ||
                   (!isPlayer1Turn && connectionId == Player2);
        }

        // 处理落子的方法
        public bool DealPiece(int x, int y, string connectionId, out string msg)
        {
            // 验证是否是当前玩家的回合
            if (!IsPlayerTurn(connectionId))
            {
                msg = "不是你的回合";
                return false;
            }

            // 使用 GameManager 尝试落子
            bool moveResult = GameManager.TryMakeMove_1(x, y, out msg);

            // 如果游戏结束，设置消息
            if (moveResult && GameManager.IsGameOver)
            {
                msg = "获胜";
            }

            return moveResult;
        }

        // 获取当前胜利者的用户ID
        public string GetWinnerUserId()
        {
            if (!GameManager.IsGameOver)
            {
                return null;
            }

            // 根据获胜者颜色确定用户ID
            if (GameManager.Winner == PlayerColor.Black)
            {
                return Player1UserId;
            }
            else if (GameManager.Winner == PlayerColor.White)
            {
                return Player2UserId;
            }

            return null; // 平局或其他情况
        }

        // 获取玩家在游戏中使用的颜色
        public PlayerColor GetPlayerColor(string connectionId)
        {
            if (connectionId == Player1)
            {
                return PlayerColor.Black;
            }
            else if (connectionId == Player2)
            {
                return PlayerColor.White;
            }
            return PlayerColor.None;
        }

        // 获取当前玩家的连接ID
        public string GetCurrentPlayerConnectionId()
        {
            return GameManager.CurrentPlayer == PlayerColor.Black ? Player1 : Player2;
        }

        // 游戏结束事件处理
        private void OnGameEnded(PlayerColor winner)
        {
            // 游戏结束时的额外处理逻辑可以放在这里
            // 例如记录游戏结果等
        }

        // 清理资源
        public void Dispose()
        {
            // 取消订阅事件以避免内存泄漏
            if (GameManager != null)
            {
                GameManager.GameEnded -= OnGameEnded;
            }
        }
    }
}
