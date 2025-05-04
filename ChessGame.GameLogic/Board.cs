
// ChessGame.Logic/Board.cs
using System;
using System.Collections.Generic;

namespace ChessGame.GameLogic
{
    public class Board
    {
        public const int Size = 15;
        /*private PlayerColor[,] grid;*/
        public PlayerColor[,] grid;

        public Board()
        {
            grid = new PlayerColor[Size, Size];
            ClearBoard();
        }

        // 重置棋盘
        public void ClearBoard()
        {
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    grid[x, y] = PlayerColor.None;
        }

        // 检查是否可以落子
        public bool IsCellEmpty(int x, int y)
        {
            return InBounds(x, y) && grid[x, y] == PlayerColor.None;
        }

        // 检查是否越界
        public bool InBounds(int x, int y)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size;
        }

        // 落子操作
        public bool PlaceMove(Move move)
        {
            if (!IsCellEmpty(move.X, move.Y)) return false;

            grid[move.X, move.Y] = move.Player;
            return true;
        }

        // 移除一个点上的棋子（用于炸弹或撤销）
        public void RemovePiece(int x, int y)
        {
            if (InBounds(x, y))
                grid[x, y] = PlayerColor.None;
        }

        // 获取当前某点的棋子归属
        public PlayerColor GetCell(int x, int y)
        {
            return InBounds(x, y) ? grid[x, y] : PlayerColor.None;
        }

        // 获取当前棋盘状态（可用于保存/回放）
        public PlayerColor[,] GetBoardSnapshot()
        {
            return (PlayerColor[,])grid.Clone();
        }

        //白棋移动
        public bool MovePiece(int fromX, int fromY, int toX, int toY)
        {
            if (InBounds(fromX, fromY) && InBounds(toX, toY))
            {
                PlayerColor piece = grid[fromX, fromY];
                if (piece == PlayerColor.White && IsCellEmpty(toX, toY))
                {
                    grid[toX, toY] = piece;
                    grid[fromX, fromY] = PlayerColor.None;
                    return true;
                }
            }
            return false;
        }
        public bool IsFull()
        {
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    if (GetCell(x, y) == PlayerColor.None)
                        return false;
            return true;
        }

        public int TotalMoveCount { get; set; }
        public int MaxMovesPerPlayer { get; set; } = 25; // 对应你的规则

        public Board Clone()
        {
            var clone = new Board();
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    clone.SetCell(x, y, GetCell(x, y));
                }
            }
            return clone;
        }

        public void SetCell(int x, int y, PlayerColor player)
        {
            if (InBounds(x, y))
                grid[x, y] = player;
        }
    }
}