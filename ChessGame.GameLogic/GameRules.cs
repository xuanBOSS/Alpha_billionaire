
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
        // 判断是否有五连
        public static bool HasFiveInRow(Board board, PlayerColor player)
        {
            int size = board != null ? board.grid.GetLength(0) : 15;

            int[][] directions = new int[][]
            {
                new[] { 1, 0 }, // 横
                new[] { 0, 1 }, // 竖
                new[] { 1, 1 }, // 斜 \
                new[] { 1, -1 } // 斜 /
            };

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    if (board.GetCell(x, y) != player) continue;

                    foreach (var dir in directions)
                    {
                        int count = 1;
                        for (int i = 1; i < 5; i++)
                        {
                            int nx = x + dir[0] * i;
                            int ny = y + dir[1] * i;

                            if (nx < 0 || ny < 0 || nx >= size || ny >= size) break;
                            if (board.GetCell(nx, ny) == player) count++;
                            else break;
                        }

                        if (count >= 5) return true;
                    }
                }
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

            // 不需要重复放置棋子，因为在调用此方法前IsForbiddenMove已经放置了棋子
            // board.PlaceMove(new Move(x, y, player));

            int liveThreeCount = 0;

            // 检查四个方向上是否形成活三
            for (int i = 0; i < 4; i++)
            {
                int dx = directions[i].Item1;
                int dy = directions[i].Item2;

                if (IsLiveThree(board, x, y, dx, dy))
                {
                    liveThreeCount++;
                }
            }

            // 禁手规则：两个或以上的活三才构成禁手
            return liveThreeCount >= 2;
        }

        // 判断是否构成活三 - 修改逻辑，确保准确检测
        private static bool IsLiveThree(Board board, int x, int y, int dx, int dy)
        {
            // 计算这个方向上的连续黑子
            int count = 1; // 当前位置的棋子
            int emptyBefore = 0; // 序列前的空位
            int emptyAfter = 0;  // 序列后的空位

            // 正向计数
            for (int i = 1; i <= 5; i++) // 考虑更长的序列以检查特殊情况
            {
                int nx = x + dx * i;
                int ny = y + dy * i;

                if (!board.InBounds(nx, ny)) break;

                if (board.GetCell(nx, ny) == PlayerColor.Black)
                {
                    if (emptyAfter > 0) break; // 如果已经遇到空位后又遇到棋子，不是简单的活三
                    count++;
                }
                else if (board.GetCell(nx, ny) == PlayerColor.None)
                {
                    emptyAfter++;
                    if (emptyAfter > 2) break; // 最多考虑两个空位
                }
                else
                {
                    break; // 遇到对手棋子
                }
            }

            // 反向计数
            for (int i = 1; i <= 5; i++)
            {
                int nx = x - dx * i;
                int ny = y - dy * i;

                if (!board.InBounds(nx, ny)) break;

                if (board.GetCell(nx, ny) == PlayerColor.Black)
                {
                    if (emptyBefore > 0) break; // 如果已经遇到空位后又遇到棋子，不是简单的活三
                    count++;
                }
                else if (board.GetCell(nx, ny) == PlayerColor.None)
                {
                    emptyBefore++;
                    if (emptyBefore > 2) break; // 最多考虑两个空位
                }
                else
                {
                    break; // 遇到对手棋子
                }
            }

            // 判断是否为活三：
            // 1. X X X _ _  (连续三子带两空)
            // 2. X X _ X _  (间隔三子带两空)
            // 3. X _ X X _  (间隔三子带两空)

            // 典型活三模式: 连续三颗子，两边都是空格
            if (count == 3 && emptyBefore >= 1 && emptyAfter >= 1)
                return true;

            // 还需检查特殊的间隔活三模式，如 "X_XX_" 或 "XX_X_"
            // 这需要更复杂的模式匹配，此处简化处理

            return false;
        }

        // 检测黑棋四四禁手
        private static bool DetectDoubleFour(Board board, int x, int y, PlayerColor player)
        {
            if (player != PlayerColor.Black) return false;

            // 不需要重复放置棋子
            // board.PlaceMove(new Move(x, y, player));

            int fourCount = 0;

            // 检查四个方向
            for (int i = 0; i < 4; i++)
            {
                int dx = directions[i].Item1;
                int dy = directions[i].Item2;

                if (IsFourFormation(board, x, y, dx, dy))
                {
                    fourCount++;
                    if (fourCount >= 2) return true;  // 两个或以上的四连形成禁手
                }
            }

            return false;
        }

        // 判断是否构成四连（活四或冲四）- 修改逻辑
        private static bool IsFourFormation(Board board, int x, int y, int dx, int dy)
        {
            // 计算在这个方向上连续的黑子数量
            int count = 1; // 当前位置已有一个黑子
            bool hasOpenEnd = false;

            // 正向计数
            for (int i = 1; i <= 4; i++)
            {
                int nx = x + dx * i;
                int ny = y + dy * i;

                if (!board.InBounds(nx, ny)) break;

                if (board.GetCell(nx, ny) == PlayerColor.Black)
                {
                    count++;
                }
                else if (board.GetCell(nx, ny) == PlayerColor.None)
                {
                    hasOpenEnd = true;
                    break;  // 遇到空位停止计数
                }
                else
                {
                    break;  // 遇到对手棋子停止计数
                }
            }

            // 反向计数
            bool hasReverseOpenEnd = false;
            for (int i = 1; i <= 4; i++)
            {
                int nx = x - dx * i;
                int ny = y - dy * i;

                if (!board.InBounds(nx, ny)) break;

                if (board.GetCell(nx, ny) == PlayerColor.Black)
                {
                    count++;
                }
                else if (board.GetCell(nx, ny) == PlayerColor.None)
                {
                    hasReverseOpenEnd = true;
                    break;  // 遇到空位停止计数
                }
                else
                {
                    break;  // 遇到对手棋子停止计数
                }
            }

            // 判断是否形成四连
            // 活四：连续四子，至少一端是空格
            // 冲四：连续四子，一端是空格，另一端是边界或对手棋子
            return count == 4 && (hasOpenEnd || hasReverseOpenEnd);
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
