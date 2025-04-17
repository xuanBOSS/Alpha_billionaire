/*namespace ChessGame.GameLogic  // 命名空间必须一致
{
    public class GameManager  // 必须为public
    {
        public bool IsValidMove(int x, int y)
        {  // 实现逻辑（示例：检查棋盘边界）
            return x >= 0 && x < 15 && y >= 0 && y < 15;
        }
    }
}*/
/*
管理棋盘状态 Board
管理当前回合 CurrentPlayer
落子验证 & 执行
禁手检测（黑棋）
胜负判断
重置棋局 
 */
using System;
using System.Collections.Generic;

namespace ChessGame.GameLogic  // 命名空间必须一致
{
    public class GameManager
    {
        //基础游戏状态
        public Board Board { get; private set; }
        public PlayerColor CurrentPlayer { get; private set; }
        public bool IsGameOver { get; private set; }
        public PlayerColor Winner { get; private set; }

        //资源限制
        private const int MaxPiecesPerPlayer = 25;
        private readonly Dictionary<PlayerColor, int> pieceCount = new()
        {
            [PlayerColor.Black] = 0,
            [PlayerColor.White] = 0
        };

        //游戏记录
        private List<Move> moveHistory;
        private BombManager bombManager;
        private int turnCount;

        //掠夺统计字段
        private int totalStolenByBlack = 0;
        private int totalStolenByWhite = 0;
        private bool shouldSkipNextTurn = false;

        //白棋第一个炸弹生成的特权
        private bool isFirstBomb = true;
        private bool isWhiteFirstBombChoicePending = false;


        public GameManager()
        {
            Board = new Board();
            CurrentPlayer = PlayerColor.Black; // 黑棋先手
            IsGameOver = false;
            Winner = PlayerColor.None;
            moveHistory = new List<Move>();
            bombManager = new BombManager();
            pieceCount[PlayerColor.Black] = 0;
            pieceCount[PlayerColor.White] = 0;
            turnCount = 0;
        }

        public bool TryMakeMove(int x, int y, out string message)
        {
            message = "";

            if (IsGameOver)
            {
                message = "游戏已结束。";
                return false;
            }

            if (!Board.InBounds(x, y))
            {
                message = "超出棋盘范围。";
                return false;
            }

            if (Board.GetCell(x, y) != PlayerColor.None)
            {
                message = "该位置已有棋子。";
                return false;
            }

            if (!CanPlaceMorePieces(CurrentPlayer))
            {
                message = $"{CurrentPlayer}方棋子已用完";
                return false;
            }

            // 黑棋禁手检测
            if (CurrentPlayer == PlayerColor.Black && GameRules.IsForbiddenMove(Board, x, y))
            {
                message = "禁手：黑棋此处落子违反规则。";
                return false;
            }

            var move = new Move(x, y, CurrentPlayer);
            Board.PlaceMove(move);
            moveHistory.Add(move);
            pieceCount[CurrentPlayer]++;
            turnCount++;

            // --- 爆破逻辑 ---
            if (turnCount % 6 == 0 && GetEmptyCells() >= 30)
            {
                // 检查是否有活四（任一方）
                bool blackHasLiveFour = GameRules.IsLiveFour(Board, PlayerColor.Black);
                bool whiteHasLiveFour = GameRules.IsLiveFour(Board, PlayerColor.White);

                if (!blackHasLiveFour && !whiteHasLiveFour)
                {
                    if (isFirstBomb && CurrentPlayer == PlayerColor.White)
                    {
                        //暂停游戏，等待白棋做出选择
                        isWhiteFirstBombChoicePending = true;
                        message = "白棋回合：请选择“移动棋子”或“延迟爆破";
                        return false;
                    }
                    bombManager.TriggerBomb(Board);  // 执行爆破
                    bombManager.GenerateCandidate(Board);  // 生成下一轮爆破候选
                    isFirstBomb = false;  //之后就不是第一次爆炸了
                }
                else
                {
                    // 跳过爆破生成
                    // 可选：你也可以记录log或设置 flag
                }
            }
            else if (turnCount % 6 == 5)
            {
                bombManager.GenerateCandidate(Board); // 爆破前一手生成提示
            }
            // 胜负判断
            if (GameRules.CheckWin(Board, x, y, CurrentPlayer))
            {
                IsGameOver = true;
                Winner = CurrentPlayer;
                message = $"{CurrentPlayer} 获胜！";
                return true;
            }

            // 切换回合
            SwitchTurn();
            return true;
        }
        //终局检查
        private int GetEmptyCells()
        {
            int count = 0;
            for (int x = 0; x < Board.Size; x++)
                for (int y = 0; y < Board.Size; y++)
                    if (Board.GetCell(x, y) == PlayerColor.None)
                        count++;
            return count;
        }

        public void ResetGame()
        {
            Board = new Board();
            CurrentPlayer = PlayerColor.Black;
            IsGameOver = false;
            Winner = PlayerColor.None;
            moveHistory.Clear();
        }

        //回合切换
        private void SwitchTurn()
        {
            if (shouldSkipNextTurn)
            {
                shouldSkipNextTurn = false;
                return; // 跳过回合切换
            }
            CurrentPlayer = CurrentPlayer == PlayerColor.Black ? PlayerColor.White : PlayerColor.Black;
        }

        public IReadOnlyList<Move> GetMoveHistory()
        {
            return moveHistory.AsReadOnly();
        }

        private bool CanPlaceMorePieces(PlayerColor player)
        {
            return pieceCount[player] < MaxPiecesPerPlayer;
        }

        //棋子回收和掠夺的方法
        private void RecoverPieces(Dictionary<PlayerColor, int> destroyedCounts)
        {
            // 重置停手状态
            shouldSkipNextTurn = false;

            foreach (var (player, count) in destroyedCounts)
            {
                if (count == 0) continue;

                // 己方棋子回收逻辑
                if (player == CurrentPlayer)
                {
                    HandleSelfRecovery(player, count);
                }
                // 敌方棋子掠夺逻辑
                else
                {
                    HandleEnemySteal(player, count);
                }
            }
        }
        //处理己方棋子回收
        private void HandleSelfRecovery(PlayerColor player, int destroyedCount)
        {
            int recovered;

            // 分级回收规则
            if (destroyedCount >= 5)
            {
                recovered = 4;
                shouldSkipNextTurn = true;
            }
            else if (destroyedCount >= 4)
            {
                recovered = 3;
                shouldSkipNextTurn = true;
            }
            else
            {
                recovered = Math.Min(destroyedCount, 3);
            }

            pieceCount[player] += recovered;
        }
        //处理敌方棋子掠夺
        private void HandleEnemySteal(PlayerColor victim, int destroyedCount)
        {
            var thief = CurrentPlayer;
            int stolen;

            // 计算当前掠夺效率
            if (GetTotalStolen(thief) >= 5)
            {
                // 效率降为3:1
                stolen = destroyedCount / 3;
            }
            else
            {
                // 基础效率2:1
                stolen = destroyedCount / 2;
            }

            // 单次上限2枚
            stolen = Math.Min(stolen, 2);

            // 更新掠夺统计
            if (thief == PlayerColor.Black)
                totalStolenByBlack += stolen;
            else
                totalStolenByWhite += stolen;

            pieceCount[thief] += stolen;
        }
        //获取玩家累计掠夺数
        private int GetTotalStolen(PlayerColor player) =>
            player == PlayerColor.Black ? totalStolenByBlack : totalStolenByWhite;

        //白棋的选择
        public bool HandleWhiteFirstBombChoice(bool isMove, out string message)
        {
            message = "";
            if (!isWhiteFirstBombChoicePending)
            {
                message = "当前不是白棋的选择回合。";
                return false;
            }
            isWhiteFirstBombChoicePending = false;
            isFirstBomb = false; // 重置状态
            if (isMove)
            {
                //前端引导白棋进入“移动模式”
                message = "请移动一个已有的棋子。";
                return true;
            }
            else
            {
                // 延迟爆破一回合（等下轮爆破）
                turnCount--; // 实际等于延后一次爆破检查
                message = "爆破延迟一回合。";
                return true;
            }
        }
        //尝试移动白棋
        public bool TryMovePiece(int fromX, int fromY, int toX, int toY, out string message)
        {
            message = "";

            if (!isWhiteFirstBombChoicePending)
            {
                message = "当前不是白棋移动棋子的阶段。";
                return false;
            }

            if (CurrentPlayer != PlayerColor.White)
            {
                message = "只有白棋可以使用此特权。";
                return false;
            }

            if (Board.GetCell(fromX, fromY) != PlayerColor.White)
            {
                message = "起始位置不是白棋棋子。";
                return false;
            }

            if (!Board.InBounds(toX, toY) || Board.GetCell(toX, toY) != PlayerColor.None)
            {
                message = "目标位置不合法。";
                return false;
            }

            bool moved = Board.MovePiece(fromX, fromY, toX, toY);
            if (!moved)
            {
                message = "棋子移动失败。";
                return false;
            }

            isWhiteFirstBombChoicePending = false;
            isFirstBomb = false;

            message = "白棋成功移动棋子，游戏继续。";
            return true;
        }
    }
}
