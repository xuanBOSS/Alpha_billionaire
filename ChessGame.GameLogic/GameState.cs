//用于统一表示当前游戏状态，包括棋盘布局、玩家资源（剩余棋子、跳过回合等）与当前回合信息
using System;
using System.Collections.Generic;

namespace ChessGame.GameLogic
{
    public class PlayerState
    {
        public int RemainingPieces { get; set; } = 25;
        public int SkippedTurns { get; set; } = 0;
        public int PlunderedPieces { get; set; } = 0;
        public int RecoveredPieces { get; set; } = 0;
    }

    public class GameState
    {
        public Board Board { get; set; } = new Board();
        public PlayerState[] Players { get; set; } //= new PlayerState[3] { null, new PlayerState(), new PlayerState() }; // 1: Black, 2: White
        public PlayerColor CurrentPlayer { get; set; }
        public int TurnNumber { get; set; } = 0;
        public BombManager BombManager { get; set; } = new BombManager();


        public GameState()
        {
            Players = new PlayerState[3];
            Players[1] = new PlayerState();
            Players[2] = new PlayerState();

        }

        // 计算当前对手是谁
        public int OpponentPlayer => 3 - (int)CurrentPlayer;

        // 判断是否在棋盘范围内
        public bool IsInsideBoard(int x, int y)
        {
            return Board.InBounds(x, y);
        }

        // 获取某格子的棋子信息（1：黑，2：白，0：空，-1：越界）
        public int GetCell(int x, int y)
        {
            if (!Board.InBounds(x, y)) return -1;

            var color = Board.GetCell(x, y);
            return color == PlayerColor.None ? 0 :
                   color == PlayerColor.Black ? 1 :
                   color == PlayerColor.White ? 2 : -1;
        }

        // 设置某格子上的棋子
        public void SetCell(int x, int y, int player)
        {
            if (!Board.InBounds(x, y)) return;

            PlayerColor color = player == 1 ? PlayerColor.Black :
                                player == 2 ? PlayerColor.White :
                                PlayerColor.None;

            Board.SetCell(x, y, color);
        }

        // 判断当前玩家是否还能落子
        public bool CanCurrentPlayerMove()
        {
            return Players[(int)CurrentPlayer].RemainingPieces > 0 && Players[(int)CurrentPlayer].SkippedTurns == 0;
        }

        // 克隆方法，用于模拟对局
        public GameState Clone()
        {

            if (this.Players == null || this.Players.Length != 3)
                throw new InvalidOperationException("Invalid Players array: must contain exactly 3 elements (null, Black, White).");

            var newState = new GameState();
            newState.Board = this.Board.Clone();
            newState.CurrentPlayer = this.CurrentPlayer;
            newState.TurnNumber = this.TurnNumber;
            newState.BombManager = new BombManager
            {
                BombEnabled = this.BombManager.BombEnabled
            };

            // 如果有候选炸弹位置，也复制
            if (this.BombManager.CandidateBombPosition.HasValue)
            {
                newState.BombManager.GenerateCandidate(null); // 临时绕过构造逻辑
                newState.BombManager.ClearCandidate();
                newState.BombManager.GetType().GetProperty("CandidateBombPosition")!
                    .SetValue(newState.BombManager, this.BombManager.CandidateBombPosition);
            }

            for (int i = 0; i < 3; i++)
            {
                if (this.Players[i] != null)
                {
                    newState.Players[i] = new PlayerState
                    {
                        RemainingPieces = this.Players[i].RemainingPieces,
                        SkippedTurns = this.Players[i].SkippedTurns,
                        PlunderedPieces = this.Players[i].PlunderedPieces,
                        RecoveredPieces = this.Players[i].RecoveredPieces
                    };
                }
            }
            return newState;
        }

        // 获取棋盘大小
        public int GetBoardSize()
        {
            return Board != null ? Board.grid.GetLength(0) : 15;
        }

        // 遍历棋盘所有落子点（用于 AI 模拟）
        public IEnumerable<(int x, int y)> GetAllMoves()
        {
            int size = GetBoardSize();
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    if (Board.IsCellEmpty(x, y))
                        yield return (x, y);
                }
            }
        }
    }
}
