// ChessGame.Logic/Move.cs
namespace ChessGame.GameLogic
{
    public class Move
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Player { get; set; }

        // 确保所有构造函数参数都被使用
        public Move(int x, int y, int player)
        {
            X = x;
            Y = y;
            Player = player;
        }
    }
}