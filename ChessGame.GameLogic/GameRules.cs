/*namespace ChessGame.GameLogic
{
    public class GameRules
    {
        public static bool TestValid(int x,int y,string color,Board board)//判断落子合法性
        {
            return true;
        }

        //和棋或者胜利或未结束
        public static string TestEnd(int x,int y,string color,Board board)//判断落子后是否胜利
        {
            //return "CONTINUE";//对局继续
            //return "DRAW";//和棋
            return "WIN";//胜利
        }
    }
}*/

using System;
using System.Collections.Generic;

namespace ChessGame.GameLogic
{
    public static class GameRules
    {
        // 四个基本方向（横、竖、两斜）
        private static readonly (int dx, int dy)[] directions = new (int, int)[]
        {
            (1, 0), (0, 1), (1, 1), (1, -1)
        };

        // 检查是否五连胜利
        public static bool CheckWin(Board board, int x, int y, PlayerColor player)
        {
            foreach (var (dx, dy) in directions)
            {
                int count = 1;
                count += CountDirection(board, x, y, dx, dy, player);
                count += CountDirection(board, x, y, -dx, -dy, player);
                if (count >= 5) return true;
            }
            return false;
        }

        // 某个方向统计连续棋子
        private static int CountDirection(Board board, int x, int y, int dx, int dy, PlayerColor player)
        {
            int count = 0;
            int cx = x + dx;
            int cy = y + dy;

            while (board.InBounds(cx, cy) && board.GetCell(cx, cy) == player)
            {
                count++;
                cx += dx;
                cy += dy;
            }
            return count;
        }

        // 黑棋禁手判断（仅对黑棋启用）
        public static bool IsForbiddenMove(Board board, int x, int y)
        {
            var player = PlayerColor.Black;

            board.PlaceMove(new Move(x, y, player));

            bool isDoubleThree = DetectDoubleLiveThree(board, x, y, player);
            bool isDoubleFour = DetectDoubleFour(board, x, y, player);
            bool isOverline = DetectOverline(board, x, y, player);

            board.RemovePiece(x, y);
            return isDoubleThree || isDoubleFour || isOverline;
        }

        // 检测黑棋三三禁手
        private static bool DetectDoubleLiveThree(Board board, int x, int y, PlayerColor player)
        {
            if (player != PlayerColor.Black) return false;

            board.PlaceMove(new Move(x, y, player));
            try
            {
                int liveThreeCount = 0;
                foreach (var (dx, dy) in directions)
                {
                    if (IsLiveThree(board, x, y, dx, dy)) liveThreeCount++;
                    if (IsLiveThree(board, x, y, -dx, -dy)) liveThreeCount++;
                    if (liveThreeCount >= 2) return true;
                }
                return false;
            }
            finally
            {
                board.RemovePiece(x, y);
            }
        }

        // 判断是否构成活三
        private static bool IsLiveThree(Board board, int x, int y, int dx, int dy)
        {
            int consecutive = 1;
            int openEnds = 0;

            // 正向
            for (int i = 1; i <= 3; i++)
            {
                int nx = x + dx * i, ny = y + dy * i;
                if (!board.InBounds(nx, ny)) break;
                var cell = board.GetCell(nx, ny);
                if (cell == PlayerColor.Black) consecutive++;
                else
                {
                    if (cell == PlayerColor.None) openEnds++;
                    break;
                }
            }

            // 反向
            for (int i = 1; i <= 3; i++)
            {
                int nx = x - dx * i, ny = y - dy * i;
                if (!board.InBounds(nx, ny)) break;
                var cell = board.GetCell(nx, ny);
                if (cell == PlayerColor.Black) consecutive++;
                else
                {
                    if (cell == PlayerColor.None) openEnds++;
                    break;
                }
            }

            return consecutive == 3 && openEnds == 2;
        }

        // 检测黑棋四四禁手
        private static bool DetectDoubleFour(Board board, int x, int y, PlayerColor player)
        {
            if (player != PlayerColor.Black) return false;

            board.PlaceMove(new Move(x, y, player));
            try
            {
                int fourCount = 0;
                foreach (var (dx, dy) in directions)
                {
                    if (IsFourFormation(board, x, y, dx, dy)) fourCount++;
                    if (IsFourFormation(board, x, y, -dx, -dy)) fourCount++;
                    if (fourCount >= 2) return true;
                }
                return false;
            }
            finally
            {
                board.RemovePiece(x, y);
            }
        }

        // 判断是否构成四连（活四或冲四）
        private static bool IsFourFormation(Board board, int x, int y, int dx, int dy)
        {
            int consecutive = 1;
            bool hasOpenEnd = false;

            // 正向
            for (int i = 1; i <= 4; i++)
            {
                int nx = x + dx * i, ny = y + dy * i;
                if (!board.InBounds(nx, ny)) break;

                var cell = board.GetCell(nx, ny);
                if (cell == PlayerColor.Black) consecutive++;
                else
                {
                    if (cell == PlayerColor.None) hasOpenEnd = true;
                    break;
                }
            }

            // 反向
            for (int i = 1; i <= 4; i++)
            {
                int nx = x - dx * i, ny = y - dy * i;
                if (!board.InBounds(nx, ny)) break;

                var cell = board.GetCell(nx, ny);
                if (cell == PlayerColor.Black) consecutive++;
                else
                {
                    if (cell == PlayerColor.None) hasOpenEnd = true;
                    break;
                }
            }

            return consecutive == 4;
        }

        // 检测黑棋是否形成长连（≥6）
        private static bool DetectOverline(Board board, int x, int y, PlayerColor player)
        {
            foreach (var (dx, dy) in directions)
            {
                int count = 1;
                count += CountDirection(board, x, y, dx, dy, player);
                count += CountDirection(board, x, y, -dx, -dy, player);
                if (count >= 6) return true;
            }
            return false;
        }

        //另外做一个检测活四的函数
        public static bool IsLiveFour(Board board, int x, int y, int dx, int dy, PlayerColor player)
        {
            int consecutive = 1;
            int openEnds = 0;
            // 正向
            for (int i = 1; i <= 4; i++)
            {
                int nx = x + dx * i, ny = y + dy * i;
                if (!board.InBounds(nx, ny)) break;
                var cell = board.GetCell(nx, ny);
                if (cell == player) consecutive++;
                else
                {
                    if (cell == PlayerColor.None) openEnds++;
                    break;
                }
            }
            // 反向
            for (int i = 1; i <= 4; i++)
            {
                int nx = x - dx * i, ny = y - dy * i;
                if (!board.InBounds(nx, ny)) break;
                var cell = board.GetCell(nx, ny);
                if (cell == player) consecutive++;
                else
                {
                    if (cell == PlayerColor.None) openEnds++;
                    break;
                }
            }
            return consecutive == 4 && openEnds == 2;
        }
        public static bool IsLiveFour(Board board, PlayerColor player)
        {
            for (int x = 0; x < Board.Size; x++)
            {
                for (int y = 0; y < Board.Size; y++)
                {
                    if (board.GetCell(x, y) != player) continue;

                    foreach (var (dx, dy) in directions)
                    {
                        if (IsLiveFour(board, x, y, dx, dy, player))
                            return true;
                    }
                }
            }
            return false;
        }
    }
}
