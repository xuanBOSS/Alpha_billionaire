﻿// ChessGame.Logic/Move.cs
namespace ChessGame.GameLogic
{
    /* public class Move
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
    */
    public enum PlayerColor
    {
        None,
        Black,
        White
    }

    public class Move
    {
        public int X { get; set; }  // 横坐标
        public int Y { get; set; }  // 纵坐标
        public PlayerColor Player { get; set; }  // 玩家颜色
        public bool IsSpecialMove { get; set; } = false; // 是否特殊操作，如移动、炸弹

        public Move(int x, int y, PlayerColor player)
        {
            X = x;
            Y = y;
            Player = player;
        }
    }
}