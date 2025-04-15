// ChessGame.Logic/Board.cs
namespace ChessGame.GameLogic
{
    public class Board
    {
        public int[,] Grid { get; set; } = new int[15, 15];

        // 添加尺寸属性
        public int Width => Grid.GetLength(1);  // 列数
        public int Height => Grid.GetLength(0); // 行数

        // 添加判空方法
        public bool IsEmpty(int x, int y)
        {
            return x >= 0 && x < Width &&
                   y >= 0 && y < Height &&
                   Grid[x, y] == 0;
        }

        public Board()
        {
            Grid = new int[15, 15];
        }

        public void AddPiece(int x, int y)//往棋盘上添加棋子
        {

        }
    }
}