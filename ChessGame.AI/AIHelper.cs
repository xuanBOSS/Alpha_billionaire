//AI决策模块
//支持合法落子筛选 → 胜负检测 → 爆炸模拟 → 得分评估 → 最佳决策
using ChessGame.GameLogic;
using System;
using System.Collections.Generic;
using ChessGame.GameLogic.Interfaces;

namespace ChessGame.AI
{
    // 提供AI所需的通用辅助功能和局势分析工具
    public class AIHelper:IAIHelper
    {
        // 棋盘评分参数
        public const int FIVE_IN_A_ROW = 100000;  // 五子连珠
        public const int OPEN_FOUR = 50000;       // 活四
        public const int FOUR = 30000;             // 冲四
        public const int OPEN_THREE = 20000;       // 活三
        public const int THREE = 500;             // 眠三
        public const int OPEN_TWO = 100;          // 活二
        public const int TWO = 10;                // 眠二
        public const int MINE_RISK = -500;        // 地雷风险惩罚

        private readonly int boardSize;
        private double[,] mineProbabilities;

        // 新增：用于存储棋型统计
        private Dictionary<string, int> myShapesCache;
        private Dictionary<string, int> opponentShapesCache;
        private int lastCacheColor = 0;
        private Board lastCachedBoard = null;

        public AIHelper(int boardSize = 15)
        {
            this.boardSize = boardSize;
            this.mineProbabilities = new double[boardSize, boardSize];
            ResetMineProbabilities();
            this.myShapesCache = new Dictionary<string, int>();
            this.opponentShapesCache = new Dictionary<string, int>();
        }


        // 新增：检查是否有必胜点
        public bool HasWinningMove(Board board, int playerColor, out (int x, int y) winMove)
        {
            winMove = (-1, -1);

            // 检查自己是否有必胜点
            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (board.GetCell(i, j) == PlayerColor.None)
                    {
                        // 检查是否是禁手位置
                        if (playerColor == 1 && IsForbiddenMove(board, i, j, playerColor))
                            continue;

                        // 模拟自己在此落子
                        Board tempBoard = board.Clone();
                        tempBoard.SetCell(i, j, (PlayerColor)playerColor);

                        // 检查是否形成五连
                        if (HasFiveInARow(tempBoard, i, j, playerColor))
                        {
                            winMove = (i, j);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // 新增：检查是否有紧急威胁需要防守
        public bool HasImmediateThreat(Board board, int playerColor, out (int x, int y) defenseMove)
        {
            defenseMove = (-1, -1);
            int opponentColor = playerColor == 1 ? 2 : 1;

            List<(int x, int y, int priority)> threats = new List<(int x, int y, int priority)>();

            // 检查对手是否有威胁点
            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (board.GetCell(i, j) == PlayerColor.None)
                    {
                        // 检查是否是禁手位置（对我方而言）
                        if (playerColor == 1 && IsForbiddenMove(board, i, j, playerColor))
                            continue;

                        // 模拟对手在此落子
                        Board tempBoard = board.Clone();
                        tempBoard.SetCell(i, j, (PlayerColor)opponentColor);

                        // 检查威胁等级
                        int threatLevel = EvaluateThreatLevel(tempBoard, i, j, opponentColor);

                        if (threatLevel > 0)
                        {
                            threats.Add((i, j, threatLevel));
                        }
                    }
                }
            }

            // 按威胁等级排序，优先防守最高威胁
            if (threats.Count > 0)
            {
                var highestThreat = threats.OrderByDescending(t => t.priority).First();
                defenseMove = (highestThreat.x, highestThreat.y);
                return highestThreat.priority >= 5; // 只有高威胁才需要立即防守
            }

            return false;
        }

        // 新增：评估威胁等级
        private int EvaluateThreatLevel(Board board, int x, int y, int playerColor)
        {
            // 检查是否形成五连（最高威胁）
            if (HasFiveInARow(board, x, y, playerColor))
                return 10;

            // 检查是否形成有效的活四（次高威胁）
            if (IsValidOpenFourAtPosition(board, x, y, playerColor))
                return 9;

            // 检查是否形成有效的冲四
            if (IsValidFourAtPosition(board, x, y, playerColor))
                return 7;

            // 检查是否形成有效的活三
            if (IsValidOpenThreeAtPosition(board, x, y, playerColor))
                return 5;

            // 检查是否形成有效的眠三
            if (IsValidThreeAtPosition(board, x, y, playerColor))
                return 3;

            return 0;
        }

        // 新增：检查是否形成活四
        public bool IsValidOpenFourAtPosition(Board board, int x, int y, int playerColor)
        {
            int[][] directions = new int[][]
            {
                new int[] { 1, 0 }, new int[] { 0, 1 },
                new int[] { 1, 1 }, new int[] { 1, -1 }
            };

            foreach (var dir in directions)
            {
                if (CountConsecutive(board, x, y, dir[0], dir[1], playerColor) == 4)
                {
                    if (IsValidOpenFourPattern(board, x, y, dir[0], dir[1], playerColor))
                        return true;
                }
            }
            return false;
        }

        // 新增：检查是否形成冲四
        public bool IsValidFourAtPosition(Board board, int x, int y, int playerColor)
        {
            int[][] directions = new int[][]
            {
                new int[] { 1, 0 }, new int[] { 0, 1 },
                new int[] { 1, 1 }, new int[] { 1, -1 }
            };

            foreach (var dir in directions)
            {
                if (CountConsecutive(board, x, y, dir[0], dir[1], playerColor) == 4)
                {
                    if (IsValidFourPattern(board, x, y, dir[0], dir[1], playerColor))
                        return true;
                }
            }
            return false;
        }

        // 新增：检查是否形成活三
        public bool IsValidOpenThreeAtPosition(Board board, int x, int y, int playerColor)
        {
            int[][] directions = new int[][]
            {
                new int[] { 1, 0 }, new int[] { 0, 1 },
                new int[] { 1, 1 }, new int[] { 1, -1 }
            };

            foreach (var dir in directions)
            {
                if (CountConsecutive(board, x, y, dir[0], dir[1], playerColor) == 3)
                {
                    if (IsValidOpenThreePattern(board, x, y, dir[0], dir[1], playerColor))
                        return true;
                }
            }
            return false;
        }

        // 新增：检查是否形成眠三
        private bool IsValidThreeAtPosition(Board board, int x, int y, int playerColor)
        {
            int[][] directions = new int[][]
            {
                new int[] { 1, 0 }, new int[] { 0, 1 },
                new int[] { 1, 1 }, new int[] { 1, -1 }
            };

            foreach (var dir in directions)
            {
                if (CountConsecutive(board, x, y, dir[0], dir[1], playerColor) == 3)
                {
                    if (IsValidThreePattern(board, x, y, dir[0], dir[1], playerColor))
                        return true;
                }
            }
            return false;
        }

        // 新增：检查是否形成活四
        public bool HasOpenFour(Board board, int x, int y, int playerColor)
        {
            int[][] directions = new int[][]
            {
                new int[] { 1, 0 }, new int[] { 0, 1 },
                new int[] { 1, 1 }, new int[] { 1, -1 }
            };

            foreach (var dir in directions)
            {
                int count = CountConsecutive(board, x, y, dir[0], dir[1], playerColor);
                bool leftOpen = IsOpenEnd(board, x, y, -dir[0], -dir[1], playerColor);
                bool rightOpen = IsOpenEnd(board, x, y, dir[0], dir[1], playerColor);

                if (count == 4 && leftOpen && rightOpen)
                    return true;
            }
            return false;
        }

        // 新增：检查是否形成冲四
        public bool HasFour(Board board, int x, int y, int playerColor)
        {
            int[][] directions = new int[][]
            {
                new int[] { 1, 0 }, new int[] { 0, 1 },
                new int[] { 1, 1 }, new int[] { 1, -1 }
            };

            foreach (var dir in directions)
            {
                int count = CountConsecutive(board, x, y, dir[0], dir[1], playerColor);
                bool leftOpen = IsOpenEnd(board, x, y, -dir[0], -dir[1], playerColor);
                bool rightOpen = IsOpenEnd(board, x, y, dir[0], dir[1], playerColor);

                if (count == 4 && (leftOpen || rightOpen))
                    return true;
            }
            return false;
        }

        // 新增：检查是否形成活三
        public bool HasOpenThree(Board board, int x, int y, int playerColor)
        {
            int[][] directions = new int[][]
            {
                new int[] { 1, 0 }, new int[] { 0, 1 },
                new int[] { 1, 1 }, new int[] { 1, -1 }
            };

            foreach (var dir in directions)
            {
                int count = CountConsecutive(board, x, y, dir[0], dir[1], playerColor);
                bool leftOpen = IsOpenEnd(board, x, y, -dir[0], -dir[1], playerColor);
                bool rightOpen = IsOpenEnd(board, x, y, dir[0], dir[1], playerColor);

                if (count == 3 && leftOpen && rightOpen)
                    return true;
            }
            return false;
        }

        // 新增：检查是否形成眠三
        private bool HasThree(Board board, int x, int y, int playerColor)
        {
            int[][] directions = new int[][]
            {
                new int[] { 1, 0 }, new int[] { 0, 1 },
                new int[] { 1, 1 }, new int[] { 1, -1 }
            };

            foreach (var dir in directions)
            {
                int count = CountConsecutive(board, x, y, dir[0], dir[1], playerColor);
                bool leftOpen = IsOpenEnd(board, x, y, -dir[0], -dir[1], playerColor);
                bool rightOpen = IsOpenEnd(board, x, y, dir[0], dir[1], playerColor);

                if (count == 3 && (leftOpen || rightOpen))
                    return true;
            }
            return false;
        }

        // 重置地雷概率数组
        private void ResetMineProbabilities()
        {
            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    mineProbabilities[i, j] = 0.5; // 初始默认概率为0.5
                }
            }
        }

        // 基于数字提示更新地雷概率推理
        public void UpdateMineProbabilities(int[,] numbers)
        {
            // 重置概率
            ResetMineProbabilities();
            // 获取实际数组尺寸
            int numRows = numbers.GetLength(0);
            int numCols = numbers.GetLength(1);

            // 使用约束满足问题计算地雷概率
            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (i < numRows && j < numCols && numbers[i, j] > 0)
                    {
                        UpdateProbabilitiesAroundCell(i, j, numbers);
                    }
                }
            }

            // 调整最终概率值
            NormalizeProbabilities();
        }

        // 更新单个数字周围格子的地雷概率
        private void UpdateProbabilitiesAroundCell(int x, int y, int[,] numbers)
        {
            int mineCount = numbers[x, y];
            // 防止越界的检查
            int numRows = numbers.GetLength(0);
            int numCols = numbers.GetLength(1);
            List<(int x, int y)> unknownCells = new List<(int x, int y)>();
            // 确保坐标在有效范围内
            if (x < 0 || x >= numRows || y < 0 || y >= numCols)
                return;
            // 找出周围未知的格子
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < boardSize && ny >= 0 && ny < boardSize)
                    {
                        // 如果是未确认的格子，加入列表
                        unknownCells.Add((nx, ny));
                    }
                }
            }

            if (unknownCells.Count > 0)
            {
                // 分配概率 - 周围的地雷数除以未知格子数
                double probabilityPerCell = (double)mineCount / unknownCells.Count;

                foreach (var cell in unknownCells)
                {
                    // 更新概率值 - 使用加权平均
                    mineProbabilities[cell.x, cell.y] =
                        (mineProbabilities[cell.x, cell.y] + probabilityPerCell) / 2;
                }
            }
        }

        // 确保概率值在有效范围内
        private void NormalizeProbabilities()
        {
            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    // 确保概率在0到1之间
                    mineProbabilities[i, j] = Math.Max(0, Math.Min(1, mineProbabilities[i, j]));
                }
            }
        }

        // 获取特定位置的地雷概率
        public double GetMineProbability(int x, int y)
        {
            if (x >= 0 && x < boardSize && y >= 0 && y < boardSize)
                return mineProbabilities[x, y];
            return 0;
        }

        // 检查落子是否会引爆地雷
        public bool WillExplodeMine(MineMap mineMap, int x, int y)
        {
            // 检查四个相邻格子是否有地雷
            int[] dx = { -1, 0 };
            int[] dy = { -1, 0 };

            foreach (int ix in dx)
            {
                foreach (int iy in dy)
                {
                    int gx = x + ix;
                    int gy = y + iy;

                    if (gx >= 0 && gx < boardSize && gy >= 0 && gy < boardSize && mineMap.mines[gx, gy])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

       // 模拟爆炸后的局势
        public (Board, bool) SimulateExplosion(Board board, int x, int y, int playerColor)
        {
            Board afterExplosion = board.Clone();

            // 清除爆炸范围内的棋子（3x3范围）
            int myStoneRemoved = 0;
            int opponentStoneRemoved = 0;
            PlayerColor aiPlayerColor = playerColor == 1 ? PlayerColor.Black : PlayerColor.White;
            PlayerColor opponentPlayerColor = playerColor == 1 ? PlayerColor.White : PlayerColor.Black;

            for (int i = x - 1; i <= x + 1; i++)
            {
                for (int j = y - 1; j <= y + 1; j++)
                {
                    if (i >= 0 && i < boardSize && j >= 0 && j < boardSize)
                    {
                        if (board.GetCell(i, j) == aiPlayerColor)
                            myStoneRemoved++;
                        else if (board.GetCell(i, j) == opponentPlayerColor)
                            opponentStoneRemoved++;

                        // 清除棋子
                        afterExplosion.RemovePiece(i, j);
                    }
                }
            }

            // 判断爆炸是否有利（移除对手更多棋子则有利）
            bool isPositive = opponentStoneRemoved > myStoneRemoved;

            return (afterExplosion, isPositive);
        }

        // 检查是否是禁手位置
        public bool IsForbiddenMove(Board board, int x, int y, int playerColor)
        {
            // 只检查黑棋禁手
            if (playerColor != 1) return false;

            // 检查位置是否为空
            if (board.GetCell(x, y) != PlayerColor.None)
                return true; // 已经有棋子的位置是"禁手"

            // 模拟落子
            Board tempBoard = board.Clone();
            PlayerColor color = playerColor == 1 ? PlayerColor.Black : PlayerColor.White;
            tempBoard.SetCell(x, y, color);

            // 检查五连胜利情况 - 如果可以五连，不是禁手
            if (HasFiveInARow(tempBoard, x, y, playerColor))
                return false;

            // 检查三三禁手
            if (HasDoubleThree(tempBoard, x, y, playerColor))
                return true;

            // 检查四四禁手
            if (HasDoubleFour(tempBoard, x, y, playerColor))
                return true;

            // 检查长连禁手
            if (HasOverline(tempBoard, x, y, playerColor))
                return true;

            return false;
        }

        // 检查三三禁手
        private bool HasDoubleThree(Board board, int x, int y, int playerColor)
        {
            int openThreeCount = 0;

            // 水平方向
            if (IsOpenThree(board, x, y, 1, 0, playerColor))
                openThreeCount++;

            // 垂直方向
            if (IsOpenThree(board, x, y, 0, 1, playerColor))
                openThreeCount++;

            // 右斜方向
            if (IsOpenThree(board, x, y, 1, 1, playerColor))
                openThreeCount++;

            // 左斜方向
            if (IsOpenThree(board, x, y, 1, -1, playerColor))
                openThreeCount++;

            return openThreeCount >= 2;
        }

       // 检查是否形成活三
        private bool IsOpenThree(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            // 向两侧计数连续棋子
            int count = 1; // 包括当前位置
            bool leftOpen = false;
            bool rightOpen = false;
            PlayerColor color = playerColor == 1 ? PlayerColor.Black : PlayerColor.White;

            // 向一个方向计数
            for (int i = 1; i <= 3; i++)
            {
                int nx = x + i * dx;
                int ny = y + i * dy;

                if (nx < 0 || nx >= boardSize || ny < 0 || ny >= boardSize)
                    break;

                if (board.GetCell(nx, ny) == color)
                    count++;
                else if (board.GetCell(nx, ny) == PlayerColor.None)
                {
                    rightOpen = true;
                    break;
                }
                else
                    break;
            }

            // 向相反方向计数
            for (int i = 1; i <= 3; i++)
            {
                int nx = x - i * dx;
                int ny = y - i * dy;

                if (nx < 0 || nx >= boardSize || ny < 0 || ny >= boardSize)
                    break;

                if (board.GetCell(nx, ny) == color)
                    count++;
                else if (board.GetCell(nx, ny) == PlayerColor.None)
                {
                    leftOpen = true;
                    break;
                }
                else
                    break;
            }

            // 判断是否是活三：恰好3个连续棋子，两端都是空位
            return count == 3 && leftOpen && rightOpen;
        }

        // 检查四四禁手
        private bool HasDoubleFour(Board board, int x, int y, int playerColor)
        {
            int fourCount = 0;

            // 水平方向
            if (IsFour(board, x, y, 1, 0, playerColor))
                fourCount++;

            // 垂直方向
            if (IsFour(board, x, y, 0, 1, playerColor))
                fourCount++;

            // 右斜方向
            if (IsFour(board, x, y, 1, 1, playerColor))
                fourCount++;

            // 左斜方向
            if (IsFour(board, x, y, 1, -1, playerColor))
                fourCount++;

            return fourCount >= 2;
        }

        // 检查是否形成四（活四或冲四）
        private bool IsFour(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            // 实现四（活四或冲四）的检测
            int count = CountConsecutive(board, x, y, dx, dy, playerColor);
            return count == 4;
        }

        // 检查长连禁手
        public bool HasOverline(Board board, int x, int y, int playerColor)
        {
            // 水平方向
            if (CountConsecutive(board, x, y, 1, 0, playerColor) > 5)
                return true;

            // 垂直方向
            if (CountConsecutive(board, x, y, 0, 1, playerColor) > 5)
                return true;

            // 右斜方向
            if (CountConsecutive(board, x, y, 1, 1, playerColor) > 5)
                return true;

            // 左斜方向
            if (CountConsecutive(board, x, y, 1, -1, playerColor) > 5)
                return true;

            return false;
        }

        // 计算连续棋子数量
        public int CountConsecutive(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            int count = 1; // 包括当前位置
            PlayerColor color = playerColor == 1 ? PlayerColor.Black : PlayerColor.White;

            // 向一个方向计数
            for (int i = 1; i < 5; i++)
            {
                int nx = x + i * dx;
                int ny = y + i * dy;

                if (nx < 0 || nx >= boardSize || ny < 0 || ny >= boardSize ||
                    board.GetCell(nx, ny) != color)
                    break;

                count++;
            }

            // 向相反方向计数
            for (int i = 1; i < 5; i++)
            {
                int nx = x - i * dx;
                int ny = y - i * dy;

                if (nx < 0 || nx >= boardSize || ny < 0 || ny >= boardSize ||
                    board.GetCell(nx, ny) != color)
                    break;

                count++;
            }

            return count;
        }

        // 检查一端是否为开放状态
        public bool IsOpenEnd(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            // 检查连续棋子末端的下一个位置是否为空
            int count = CountConsecutive(board, x, y, dx, dy, playerColor);
            int nx = x + dx * count;
            int ny = y + dy * count;

            return nx >= 0 && nx < boardSize && ny >= 0 && ny < boardSize &&
                   board.GetCell(nx, ny) == PlayerColor.None;
        }


        // 新增：判断是否是序列的起始点
        private bool IsStartOfSequence(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            int nx = x + dx;
            int ny = y + dy;

            if (nx < 0 || nx >= boardSize || ny < 0 || ny >= boardSize)
                return true;

            PlayerColor color = playerColor == 1 ? PlayerColor.Black : PlayerColor.White;
            return board.GetCell(nx, ny) != color;
        }

        // 评估落子的启发式得分，排除不合法的落子
        public double EvaluateMoveHeuristic(Board board, MineMap mineMap, int x, int y, int playerColor)
        {
            // 先检查是否是禁手位置
            if (playerColor == 1 && IsForbiddenMove(board, x, y, playerColor))
                return double.MinValue; // 如果是禁手，则评分为最低

            double score = 0;

            // 1. 检查是否会触发爆炸及其影响
            bool willExplode = WillExplodeMine(mineMap, x, y);

            // 2. 模拟落子
            Board tempBoard = board.Clone();
            PlayerColor color = playerColor == 1 ? PlayerColor.Black : PlayerColor.White;
            tempBoard.SetCell(x, y, color);

            // 3. 如果会爆炸，评估爆炸后的局势
            if (willExplode)
            {
                // 模拟爆炸
                (Board afterExplosionBoard, bool isPositive) = SimulateExplosion(tempBoard, x, y, playerColor);

                // 根据爆炸是否有利来调整分数
                if (isPositive)
                    score += 500; // 对自己有利的爆炸
                else
                    score -= 300; // 对自己不利的爆炸

                // 使用爆炸后的棋盘来评估
                tempBoard = afterExplosionBoard;
            }
            else
            {
                // 如果是高风险地雷区域，增加惩罚分数
                if (GetMineProbability(Math.Min(x, boardSize - 1), Math.Min(y, boardSize - 1)) > 0.6)
                {
                    score += MINE_RISK;
                }
            }

            // 4. 评估落子后的棋型分数
            score += EvaluateShapeScore(tempBoard, x, y, playerColor);

            return score;
        }

        // 新增：综合评估落子（使用有效棋型）
        public (double score, double winRate, double confidence) EvaluateMoveComprehensive(Board board, MineMap mineMap, int x, int y, int playerColor)
        {
            // 检查是否是禁手位置
            if (playerColor == 1 && IsForbiddenMove(board, x, y, playerColor))
            {
                return (double.MinValue, 0.0, 1.0);
            }

            // 模拟落子
            Board tempBoard = board.Clone();
            tempBoard.SetCell(x, y, (PlayerColor)playerColor);

            // 检查是否会爆炸
            bool willExplode = WillExplodeMine(mineMap, x, y);
            if (willExplode)
            {
                (tempBoard, _) = SimulateExplosion(tempBoard, x, y, playerColor);
            }

            // 计算基础评分（使用有效棋型评估）
            double baseScore = EvaluateValidPatternScore(tempBoard, x, y, playerColor);

            // 计算胜率（使用修正后的胜率计算）
            double winRate = CalculateWinProbability(tempBoard, mineMap, playerColor);

            // 计算置信度
            double confidence = CalculateConfidence(tempBoard, mineMap, x, y);

            // 地雷风险调整
            if (willExplode)
            {
                baseScore *= 0.7; // 降低爆炸位置的评分
                confidence *= 0.8; // 降低置信度
            }

            return (baseScore, winRate, confidence);
        }


        // 新增：计算决策置信度
        private double CalculateConfidence(Board board, MineMap mineMap, int x, int y)
        {
            int totalPieces = 0;
            int criticalShapes = 0; // 活四、冲四等关键棋型数量

            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (board.GetCell(i, j) != PlayerColor.None)
                    {
                        totalPieces++;
                        // 检查是否有关键棋型
                        if (HasCriticalShape(board, i, j))
                            criticalShapes++;
                    }
                }
            }

            // 棋局越复杂，置信度越低
            double complexity = (double)totalPieces / (boardSize * boardSize);
            double criticalRatio = (double)criticalShapes / Math.Max(1, totalPieces);

            return Math.Max(0.1, 1.0 - complexity * 0.5 - criticalRatio * 0.3);
        }

        // 新增：检查是否有关键棋型
        private bool HasCriticalShape(Board board, int x, int y)
        {
            PlayerColor color = board.GetCell(x, y);
            if (color == PlayerColor.None) return false;

            int playerColor = color == PlayerColor.Black ? 1 : 2;

            // 四个方向
            int[][] directions = new int[][]
            {
                new int[] { 1, 0 },
                new int[] { 0, 1 },
                new int[] { 1, 1 },
                new int[] { 1, -1 }
            };

            foreach (var dir in directions)
            {
                int count = CountConsecutive(board, x, y, dir[0], dir[1], playerColor);

                // 只检查有效的关键棋型
                if (count >= 4)
                {
                    if (IsValidFourPattern(board, x, y, dir[0], dir[1], playerColor))
                        return true;
                }
                else if (count == 3)
                {
                    if (IsValidOpenThreePattern(board, x, y, dir[0], dir[1], playerColor))
                        return true;
                }
            }

            return false;
        }


        // 评估特定位置落子的棋型分数
        private double EvaluateValidShapeScore(Board board, int x, int y, int playerColor)
        {
            double score = 0;

            // 四个方向：水平、垂直、右斜、左斜
            int[][] directions = new int[][]
            {
                new int[] { 1, 0 },  // 水平
                new int[] { 0, 1 },  // 垂直
                new int[] { 1, 1 },  // 右斜
                new int[] { 1, -1 }  // 左斜
            };

            foreach (var dir in directions)
            {
                // 分析该方向的有效棋型
                double shapeScore = EvaluateValidDirectionShape(board, x, y, dir[0], dir[1], playerColor);
                score += shapeScore;
            }

            return score;
        }

        // 评估一个方向的棋型
        private double EvaluateValidDirectionShape(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            // 计算连续棋子数
            int count = CountConsecutive(board, x, y, dx, dy, playerColor);

            // 根据连子数评分，但只给有效棋型打分
            if (count >= 5) return FIVE_IN_A_ROW;

            if (count == 4)
            {
                if (IsValidOpenFourPattern(board, x, y, dx, dy, playerColor))
                    return OPEN_FOUR;
                else if (IsValidFourPattern(board, x, y, dx, dy, playerColor))
                    return FOUR;
                else
                    return 50; // 被完全阻挡的四子给低分
            }

            if (count == 3)
            {
                if (IsValidOpenThreePattern(board, x, y, dx, dy, playerColor))
                    return OPEN_THREE;
                else if (IsValidThreePattern(board, x, y, dx, dy, playerColor))
                    return THREE;
                else
                    return 20; // 被完全阻挡的三子给低分
            }

            if (count == 2)
            {
                // 检查两端是否为开放状态
                bool leftOpen = IsOpenEnd(board, x, y, -dx, -dy, playerColor);
                bool rightOpen = IsOpenEnd(board, x, y, dx, dy, playerColor);

                if (leftOpen && rightOpen) return OPEN_TWO;
                if (leftOpen || rightOpen) return TWO;
            }

            return 0;
        }

        // 新增：评估有效棋型的分数
        private double EvaluateValidPatternScore(Board board, int x, int y, int playerColor)
        {
            double score = 0;

            // 四个方向
            int[][] directions = new int[][]
            {
                new int[] { 1, 0 }, new int[] { 0, 1 },
                new int[] { 1, 1 }, new int[] { 1, -1 }
            };

            foreach (var dir in directions)
            {
                int count = CountConsecutive(board, x, y, dir[0], dir[1], playerColor);

                if (count >= 2)
                {
                    // 检查这个连珠是否有效（不被完全阻挡）
                    if (IsValidPattern(board, x, y, dir[0], dir[1], playerColor, count))
                    {
                        switch (count)
                        {
                            case 2:
                                score += 10;
                                break;
                            case 3:
                                // 只有真正的活三才给高分
                                if (IsValidOpenThreePattern(board, x, y, dir[0], dir[1], playerColor))
                                    score += 100;
                                else
                                    score += 20; // 被阻挡的三子给低分
                                break;
                            case 4:
                                // 只有真正的活四或冲四才给高分
                                if (IsValidFourPattern(board, x, y, dir[0], dir[1], playerColor))
                                    score += 1000;
                                else
                                    score += 50; // 被完全阻挡的四子给低分
                                break;
                            case 5:
                                score += 10000; // 五子连珠
                                break;
                        }
                    }
                }
            }

            return score;
        }

        // 新增：检查棋型是否有效（不被完全阻挡）
        private bool IsValidPattern(Board board, int x, int y, int dx, int dy, int playerColor, int count)
        {
            if (count >= 5) return true; // 五子连珠总是有效的

            // 找到连珠的两端
            var (leftEnd, rightEnd) = FindPatternEnds(board, x, y, dx, dy, playerColor, count);

            // 检查两端是否至少有一端可以扩展
            bool leftCanExtend = CanExtendAtPosition(board, leftEnd.x - dx, leftEnd.y - dy);
            bool rightCanExtend = CanExtendAtPosition(board, rightEnd.x + dx, rightEnd.y + dy);

            return leftCanExtend || rightCanExtend;
        }

        // 新增：检查是否是有效的活三棋型
        private bool IsValidOpenThreePattern(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            var (leftEnd, rightEnd) = FindPatternEnds(board, x, y, dx, dy, playerColor, 3);

            // 活三需要两端都可以扩展
            bool leftCanExtend = CanExtendAtPosition(board, leftEnd.x - dx, leftEnd.y - dy);
            bool rightCanExtend = CanExtendAtPosition(board, rightEnd.x + dx, rightEnd.y + dy);

            if (!leftCanExtend || !rightCanExtend) return false;

            // 进一步检查扩展后是否能形成活四
            return CanFormOpenFourFromThree(board, x, y, dx, dy, playerColor);
        }

        // 新增：检查是否是有效的四子棋型
        private bool IsValidFourPattern(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            var (leftEnd, rightEnd) = FindPatternEnds(board, x, y, dx, dy, playerColor, 4);

            // 四子至少需要一端可以扩展成五
            bool leftCanExtend = CanExtendAtPosition(board, leftEnd.x - dx, leftEnd.y - dy);
            bool rightCanExtend = CanExtendAtPosition(board, rightEnd.x + dx, rightEnd.y + dy);

            return leftCanExtend || rightCanExtend;
        }

        // 新增：检查是否是有效的三子棋型
        private bool IsValidThreePattern(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            var (leftEnd, rightEnd) = FindPatternEnds(board, x, y, dx, dy, playerColor, 3);

            // 三子至少需要一端可以扩展
            bool leftCanExtend = CanExtendAtPosition(board, leftEnd.x - dx, leftEnd.y - dy);
            bool rightCanExtend = CanExtendAtPosition(board, rightEnd.x + dx, rightEnd.y + dy);

            return leftCanExtend || rightCanExtend;
        }

        // 新增：检查是否是有效的活四棋型
        private bool IsValidOpenFourPattern(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            var (leftEnd, rightEnd) = FindPatternEnds(board, x, y, dx, dy, playerColor, 4);

            // 活四需要两端都可以扩展成五
            bool leftCanExtend = CanExtendAtPosition(board, leftEnd.x - dx, leftEnd.y - dy);
            bool rightCanExtend = CanExtendAtPosition(board, rightEnd.x + dx, rightEnd.y + dy);

            return leftCanExtend && rightCanExtend;
        }

        // 新增：检查三子是否能形成活四
        private bool CanFormOpenFourFromThree(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            var (leftEnd, rightEnd) = FindPatternEnds(board, x, y, dx, dy, playerColor, 3);

            // 检查左端扩展
            int leftExtX = leftEnd.x - dx;
            int leftExtY = leftEnd.y - dy;
            if (CanExtendAtPosition(board, leftExtX, leftExtY))
            {
                // 模拟扩展，检查是否能形成活四
                Board tempBoard = board.Clone();
                tempBoard.SetCell(leftExtX, leftExtY, (PlayerColor)playerColor);
                if (IsValidFourPattern(tempBoard, leftExtX, leftExtY, dx, dy, playerColor))
                    return true;
            }

            // 检查右端扩展
            int rightExtX = rightEnd.x + dx;
            int rightExtY = rightEnd.y + dy;
            if (CanExtendAtPosition(board, rightExtX, rightExtY))
            {
                // 模拟扩展，检查是否能形成活四
                Board tempBoard = board.Clone();
                tempBoard.SetCell(rightExtX, rightExtY, (PlayerColor)playerColor);
                if (IsValidFourPattern(tempBoard, rightExtX, rightExtY, dx, dy, playerColor))
                    return true;
            }

            return false;
        }

        // 新增：找到棋型的两端
        private ((int x, int y) leftEnd, (int x, int y) rightEnd) FindPatternEnds(Board board, int x, int y, int dx, int dy, int playerColor, int expectedCount)
        {
            PlayerColor color = playerColor == 1 ? PlayerColor.Black : PlayerColor.White;

            int leftX = x, leftY = y;
            int rightX = x, rightY = y;

            // 向左找边界
            for (int i = 1; i < expectedCount; i++)
            {
                int nx = x - i * dx;
                int ny = y - i * dy;

                if (IsValidPosition(nx, ny) && board.GetCell(nx, ny) == color)
                {
                    leftX = nx;
                    leftY = ny;
                }
                else
                {
                    break;
                }
            }

            // 向右找边界
            for (int i = 1; i < expectedCount; i++)
            {
                int nx = x + i * dx;
                int ny = y + i * dy;

                if (IsValidPosition(nx, ny) && board.GetCell(nx, ny) == color)
                {
                    rightX = nx;
                    rightY = ny;
                }
                else
                {
                    break;
                }
            }

            return ((leftX, leftY), (rightX, rightY));
        }

        // 新增：检查位置是否可以扩展
        private bool CanExtendAtPosition(Board board, int x, int y)
        {
            return IsValidPosition(x, y) && board.GetCell(x, y) == PlayerColor.None;
        }

        // 新增：检查位置是否有效
        private bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < boardSize && y >= 0 && y < boardSize;
        }

        // 新增：计算有效的活四数量
        private int CountValidOpenFours(Board board, int playerColor)
        {
            int count = 0;
            PlayerColor color = playerColor == 1 ? PlayerColor.Black : PlayerColor.White;

            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (board.GetCell(i, j) == color)
                    {
                        // 检查四个方向
                        int[][] directions = new int[][]
                        {
                            new int[] { 1, 0 }, new int[] { 0, 1 },
                            new int[] { 1, 1 }, new int[] { 1, -1 }
                        };

                        foreach (var dir in directions)
                        {
                            if (CountConsecutive(board, i, j, dir[0], dir[1], playerColor) == 4)
                            {
                                if (IsValidOpenFourPattern(board, i, j, dir[0], dir[1], playerColor))
                                {
                                    count++;
                                    break; // 避免重复计算同一个四子
                                }
                            }
                        }
                    }
                }
            }

            return count / 4; // 每个活四会被计算4次，所以除以4
        }

        // 新增：计算有效的冲四数量
        private int CountValidFours(Board board, int playerColor)
        {
            int count = 0;
            PlayerColor color = playerColor == 1 ? PlayerColor.Black : PlayerColor.White;

            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (board.GetCell(i, j) == color)
                    {
                        // 检查四个方向
                        int[][] directions = new int[][]
                        {
                            new int[] { 1, 0 }, new int[] { 0, 1 },
                            new int[] { 1, 1 }, new int[] { 1, -1 }
                        };

                        foreach (var dir in directions)
                        {
                            if (CountConsecutive(board, i, j, dir[0], dir[1], playerColor) == 4)
                            {
                                if (IsValidFourPattern(board, i, j, dir[0], dir[1], playerColor) &&
                                    !IsValidOpenFourPattern(board, i, j, dir[0], dir[1], playerColor))
                                {
                                    count++;
                                    break; // 避免重复计算同一个四子
                                }
                            }
                        }
                    }
                }
            }

            return count / 4; // 每个冲四会被计算4次，所以除以4
        }

        // 新增：计算有效的活三数量
        private int CountValidOpenThrees(Board board, int playerColor)
        {
            int count = 0;
            PlayerColor color = playerColor == 1 ? PlayerColor.Black : PlayerColor.White;

            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (board.GetCell(i, j) == color)
                    {
                        // 检查四个方向
                        int[][] directions = new int[][]
                        {
                            new int[] { 1, 0 }, new int[] { 0, 1 },
                            new int[] { 1, 1 }, new int[] { 1, -1 }
                        };

                        foreach (var dir in directions)
                        {
                            if (CountConsecutive(board, i, j, dir[0], dir[1], playerColor) == 3)
                            {
                                if (IsValidOpenThreePattern(board, i, j, dir[0], dir[1], playerColor))
                                {
                                    count++;
                                    break; // 避免重复计算同一个三子
                                }
                            }
                        }
                    }
                }
            }

            return count / 3; // 每个活三会被计算3次，所以除以3
        }

        // 评估特定位置落子的棋型分数
        public int EvaluateShapeScore(Board board, int x, int y, int playerColor)
        {
            int score = 0;

            // 四个方向：水平、垂直、右斜、左斜
            int[][] directions = new int[][]
            {
                new int[] { 1, 0 },  // 水平
                new int[] { 0, 1 },  // 垂直
                new int[] { 1, 1 },  // 右斜
                new int[] { 1, -1 }  // 左斜
            };

            foreach (var dir in directions)
            {
                // 分析该方向的棋型
                int shapeScore = EvaluateDirectionShape(board, x, y, dir[0], dir[1], playerColor);
                score += shapeScore;
            }

            return score;
        }

        // 评估一个方向的棋型
        public int EvaluateDirectionShape(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            // 计算连续棋子数和空位情况
            int count = CountConsecutive(board, x, y, dx, dy, playerColor);

            // 检查两端是否为开放状态
            bool leftOpen = IsOpenEnd(board, x, y, -dx, -dy, playerColor);
            bool rightOpen = IsOpenEnd(board, x, y, dx, dy, playerColor);

            // 根据连子数和开放状态评分
            if (count >= 5) return FIVE_IN_A_ROW;

            if (count == 4)
            {
                if (leftOpen && rightOpen) return OPEN_FOUR;
                if (leftOpen || rightOpen) return FOUR;
            }

            if (count == 3)
            {
                if (leftOpen && rightOpen) return OPEN_THREE;
                if (leftOpen || rightOpen) return THREE;
            }

            if (count == 2)
            {
                if (leftOpen && rightOpen) return OPEN_TWO;
                if (leftOpen || rightOpen) return TWO;
            }

            return 0;
        }

        // 评估当前棋局
        public int EvaluateBoard(Board board, MineMap mineMap, int playerColor)
        {
            int opponentColor = playerColor == 1 ? 2 : 1;
            int score = 0;
            PlayerColor aiColor = playerColor == 1 ? PlayerColor.Black : PlayerColor.White;
            PlayerColor opColor = playerColor == 1 ? PlayerColor.White : PlayerColor.Black;

            // 检查游戏是否已经结束
            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (board.GetCell(i, j) == aiColor)
                    {
                        // 评估己方每个棋子的价值
                        score += EvaluateStoneValue(board, i, j, playerColor);
                    }
                    else if (board.GetCell(i, j) == opColor)
                    {
                        // 评估对手每个棋子的价值，并取反
                        score -= EvaluateStoneValue(board, i, j, opponentColor);
                    }
                }
            }

            return score;
        }

        // 评估棋子的价值
        public int EvaluateStoneValue(Board board, int x, int y, int playerColor)
        {
            int score = 0;

            // 四个方向：水平、垂直、右斜、左斜
            int[][] directions = new int[][]
            {
                new int[] { 1, 0 },  // 水平
                new int[] { 0, 1 },  // 垂直
                new int[] { 1, 1 },  // 右斜
                new int[] { 1, -1 }  // 左斜
            };

            foreach (var dir in directions)
            {
                score += EvaluateDirectionShape(board, x, y, dir[0], dir[1], playerColor);
            }

            return score;
        }

        // 检查游戏是否结束
        public bool IsGameOver(Board board)
        {
            // 检查是否有五子连珠
            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    PlayerColor stone = board.GetCell(i, j);
                    if (stone != PlayerColor.None)
                    {
                        int playerColor = stone == PlayerColor.Black ? 1 : 2;
                        if (HasFiveInARow(board, i, j, playerColor))
                            return true;
                    }
                }
            }

            // 检查棋盘是否已满
            return board.IsFull();
        }

        // 检查是否形成五子连珠
        public bool HasFiveInARow(Board board, int x, int y, int playerColor)
        {
            // 四个方向：水平、垂直、右斜、左斜
            int[][] directions = new int[][]
            {
                new int[] { 1, 0 },  // 水平
                new int[] { 0, 1 },  // 垂直
                new int[] { 1, 1 },  // 右斜
                new int[] { 1, -1 }  // 左斜
            };

            foreach (var dir in directions)
            {
                if (CountConsecutive(board, x, y, dir[0], dir[1], playerColor) >= 5)
                    return true;
            }

            return false;
        }
       
        // 添加一个获取随机合法落子的方法
        public (int x, int y) GetRandomLegalMove(Board board, int playerColor)
        {
            List<(int x, int y)> legalMoves = new List<(int x, int y)>();

            // 收集所有空位
            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (board.GetCell(i, j) == PlayerColor.None)
                    {
                        // 检查是否是黑棋的禁手位置
                        if (playerColor == 1 && IsForbiddenMove(board, i, j, playerColor))
                            continue;

                        legalMoves.Add((i, j));
                    }
                }
            }

            // 如果有合法位置，随机选择一个
            if (legalMoves.Count > 0)
            {
                Random random = new Random();
                int randomIndex = random.Next(legalMoves.Count);
                return legalMoves[randomIndex];
            }

            // 如果没有合法位置，返回一个特殊值表示无法移动
            return (-1, -1);
        }

        // 获取所有可能的落子位置
        public List<(int x, int y)> GetPossibleMoves(Board board, int playerColor)
        {
            List<(int x, int y)> moves = new List<(int x, int y)>();
            HashSet<(int, int)> consideredMoves = new HashSet<(int, int)>();

            // 先检查棋盘是否为空或几乎为空
            bool isEmpty = true;
            int piecesCount = 0;

            for (int i = 0; i < boardSize && isEmpty; i++)
            {
                for (int j = 0; j < boardSize && isEmpty; j++)
                {
                    if (board.GetCell(i, j) != PlayerColor.None)
                    {
                        isEmpty = false;
                        piecesCount++;
                        if (piecesCount > 5) break; // 如果棋盘有超过5个子，就不算空
                    }
                }
            }

            // 如果棋盘几乎为空，优先考虑中心区域
            if (isEmpty || piecesCount <= 5)
            {
                int center = boardSize / 2;
                int range = 3; // 中心3x3区域

                for (int i = center - range; i <= center + range; i++)
                {
                    for (int j = center - range; j <= center + range; j++)
                    {
                        if (i >= 0 && i < boardSize && j >= 0 && j < boardSize &&
                            board.GetCell(i, j) == PlayerColor.None)
                        {
                            // 检查是否是禁手
                            if (playerColor == 1 && IsForbiddenMove(board, i, j, playerColor))
                                continue;

                            moves.Add((i, j));
                            consideredMoves.Add((i, j));
                        }
                    }
                }

                // 如果中心区域找到了合法位置，就直接返回
                if (moves.Count > 0)
                    return moves;
            }

            // 否则，考虑离现有棋子近的位置
            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (board.GetCell(i, j) != PlayerColor.None) // 非空位
                    {
                        // 检查周围3x3区域的空位
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            for (int dy = -2; dy <= 2; dy++)
                            {
                                int nx = i + dx;
                                int ny = j + dy;

                                if (nx >= 0 && nx < boardSize && ny >= 0 && ny < boardSize &&
                                    board.GetCell(nx, ny) == PlayerColor.None && !consideredMoves.Contains((nx, ny)))
                                {
                                    // 检查是否是禁手
                                    if (playerColor == 1 && IsForbiddenMove(board, nx, ny, playerColor))
                                        continue;

                                    moves.Add((nx, ny));
                                    consideredMoves.Add((nx, ny));
                                }
                            }
                        }
                    }
                }
            }

            // 如果还是没找到合法位置，那就尝试整个棋盘的任意空位
            if (moves.Count == 0)
            {
                for (int i = 0; i < boardSize; i++)
                {
                    for (int j = 0; j < boardSize; j++)
                    {
                        if (board.GetCell(i, j) == PlayerColor.None && !consideredMoves.Contains((i, j)))
                        {
                            // 检查是否是禁手
                            if (playerColor == 1 && IsForbiddenMove(board, i, j, playerColor))
                                continue;

                            moves.Add((i, j));
                        }
                    }
                }
            }

            // 如果还是没有找到任何合法位置（可能黑棋被禁手规则限制住了）
            // 随机选择一个空位作为后备选项
            if (moves.Count == 0 && playerColor != 1)
            {
                for (int i = 0; i < boardSize; i++)
                {
                    for (int j = 0; j < boardSize; j++)
                    {
                        if (board.GetCell(i, j) == PlayerColor.None)
                        {
                            moves.Add((i, j));
                            break;
                        }
                    }
                    if (moves.Count > 0) break;
                }
            }

            return moves;
        }

        // 改进的胜率计算方法
        public double CalculateWinProbability(Board board, MineMap mineMap, int playerColor)
        {
            // 检查游戏是否已结束
            if (IsGameOver(board))
            {
                var winner = GetWinner(board);
                if (winner == playerColor) return 1.0;
                if (winner != 0) return 0.0;
                return 0.5; // 平局
            }

            // 计算双方的有效威胁数量
            double myScore = CalculateValidPositionalScore(board, playerColor);
            double opponentScore = CalculateValidPositionalScore(board, playerColor == 1 ? 2 : 1);

            // 检查是否有必胜点
            if (HasWinningMove(board, playerColor, out _))
                return 0.95; // 几乎必胜，但留一点余地

            if (HasWinningMove(board, playerColor == 1 ? 2 : 1, out _))
                return 0.05; // 几乎必败

            // 检查有效的活四威胁
            int myValidOpenFours = CountValidOpenFours(board, playerColor);
            int opponentValidOpenFours = CountValidOpenFours(board, playerColor == 1 ? 2 : 1);

            if (myValidOpenFours > 0 && opponentValidOpenFours == 0)
                return 0.9;
            if (opponentValidOpenFours > 0 && myValidOpenFours == 0)
                return 0.1;

            // 检查有效的冲四威胁
            int myValidFours = CountValidFours(board, playerColor);
            int opponentValidFours = CountValidFours(board, playerColor == 1 ? 2 : 1);

            if (myValidFours > 0 && opponentValidFours == 0)
                return 0.8;
            if (opponentValidFours > 0 && myValidFours == 0)
                return 0.2;

            // 检查有效的活三威胁
            int myValidOpenThrees = CountValidOpenThrees(board, playerColor);
            int opponentValidOpenThrees = CountValidOpenThrees(board, playerColor == 1 ? 2 : 1);

            // 基于有效威胁数量计算胜率
            double threatAdvantage = (myValidOpenFours * 4 + myValidFours * 2 + myValidOpenThrees * 1) -
                                   (opponentValidOpenFours * 4 + opponentValidFours * 2 + opponentValidOpenThrees * 1);

            // 基于位置评分计算胜率
            double scoreAdvantage = myScore - opponentScore;

            // 综合威胁优势和位置优势
            double totalAdvantage = threatAdvantage * 1000 + scoreAdvantage;

            // 使用sigmoid函数将优势转换为胜率
            double winRate = 1.0 / (1.0 + Math.Exp(-totalAdvantage / 5000.0));

            // 确保胜率在合理范围内
            return Math.Max(0.05, Math.Min(0.95, winRate));
        }

        // 新增：计算有效的位置评分（排除被阻挡的棋型）
        private double CalculateValidPositionalScore(Board board, int playerColor)
        {
            double score = 0;
            PlayerColor color = playerColor == 1 ? PlayerColor.Black : PlayerColor.White;

            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (board.GetCell(i, j) == color)
                    {
                        // 只计算有效棋型的分数
                        score += EvaluateValidPatternScore(board, i, j, playerColor);
                    }
                }
            }

            return score;
        }

        // 新增：获取获胜者
        private int GetWinner(Board board)
        {
            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    PlayerColor stone = board.GetCell(i, j);
                    if (stone != PlayerColor.None)
                    {
                        int playerColor = stone == PlayerColor.Black ? 1 : 2;
                        if (HasFiveInARow(board, i, j, playerColor))
                            return playerColor;
                    }
                }
            }
            return 0; // 没有获胜者
        }

        // 新增：计算单个玩家的综合评分
        private double CalculatePlayerTotalScore(Board board, MineMap mineMap, int playerColor)
        {
            // 1. 统计棋型
            var shapes = CountAllShapes(board, playerColor);

            // 2. 计算基础棋型评分
            double shapeScore = CalculateShapeScore(shapes);

            // 3. 计算位置优势
            double positionScore = CalculatePositionBonus(board, playerColor);

            // 4. 计算地雷风险（负分）
            double mineRisk = CalculateMineRisk(board, mineMap, playerColor);

            // 5. 计算威胁和机会评分
            double threatScore = CalculateIndividualThreatScore(shapes);

            // 6. 计算连接性评分（棋子之间的配合）
            double connectivityScore = CalculateConnectivityScore(board, playerColor);

            // 7. 综合评分，使用权重
            double totalScore = shapeScore * 1.0 +           // 棋型权重最高
                               threatScore * 1.2 +           // 威胁评分权重较高
                               positionScore * 0.3 +         // 位置评分权重较低
                               connectivityScore * 0.5 -     // 连接性评分
                               mineRisk * 0.8;               // 地雷风险

            // 确保评分为正数（用于相对比较）
            return Math.Max(1.0, totalScore + 1000); // 加基础分确保为正
        }

        // 新增：计算个体威胁评分（不依赖对手）
        private double CalculateIndividualThreatScore(Dictionary<string, int> shapes)
        {
            double score = 0;

            // 根据棋型的威胁程度给分
            score += shapes["five"] * 10000;        // 已经获胜
            score += shapes["open_four"] * 5000;    // 必胜威胁
            score += shapes["four"] * 1000;         // 强威胁
            score += shapes["open_three"] * 800;    // 中等威胁
            score += shapes["three"] * 200;         // 轻微威胁

            // 多重威胁的组合奖励
            if (shapes["open_three"] >= 2)
                score += 2000; // 双活三

            if (shapes["four"] >= 2)
                score += 3000; // 双冲四

            if (shapes["open_four"] >= 1)
                score += 8000; // 活四几乎必胜

            return score;
        }

        // 新增：计算棋子连接性评分
        private double CalculateConnectivityScore(Board board, int playerColor)
        {
            double score = 0;
            PlayerColor color = playerColor == 1 ? PlayerColor.Black : PlayerColor.White;

            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (board.GetCell(i, j) == color)
                    {
                        // 检查周围8个方向的友方棋子
                        int friendlyNeighbors = 0;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                if (dx == 0 && dy == 0) continue;

                                int nx = i + dx;
                                int ny = j + dy;

                                if (nx >= 0 && nx < boardSize && ny >= 0 && ny < boardSize &&
                                    board.GetCell(nx, ny) == color)
                                {
                                    friendlyNeighbors++;
                                }
                            }
                        }

                        // 连接的棋子越多，评分越高
                        score += friendlyNeighbors * 10;
                    }
                }
            }

            return score;
        }

        // 检查玩家是否获胜
        private bool HasPlayerWon(Board board, int playerColor)
        {
            PlayerColor color = playerColor == 1 ? PlayerColor.Black : PlayerColor.White;

            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (board.GetCell(i, j) == color)
                    {
                        if (HasFiveInARow(board, i, j, playerColor))
                            return true;
                    }
                }
            }
            return false;
        }

        // 统计所有棋型
        private Dictionary<string, int> CountAllShapes(Board board, int playerColor)
        {
            // 使用缓存
            var cache = playerColor == lastCacheColor ? myShapesCache : opponentShapesCache;
            if (cache.Count > 0)
                return cache;

            Dictionary<string, int> shapes = new Dictionary<string, int>
            {
                ["five"] = 0,
                ["open_four"] = 0,
                ["four"] = 0,
                ["open_three"] = 0,
                ["three"] = 0,
                ["open_two"] = 0,
                ["two"] = 0
            };

            PlayerColor color = playerColor == 1 ? PlayerColor.Black : PlayerColor.White;
            HashSet<(int, int, int, int)> countedShapes = new HashSet<(int, int, int, int)>();

            // 四个方向
            int[][] directions = new int[][]
            {
                new int[] { 1, 0 },
                new int[] { 0, 1 },
                new int[] { 1, 1 },
                new int[] { 1, -1 }
            };

            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (board.GetCell(i, j) == color)
                    {
                        foreach (var dir in directions)
                        {
                            // 只从序列的起始点开始计算，避免重复
                            if (IsStartOfSequence(board, i, j, -dir[0], -dir[1], playerColor))
                            {
                                var shapeKey = (i, j, dir[0], dir[1]);
                                if (!countedShapes.Contains(shapeKey))
                                {
                                    string shapeType = AnalyzeShape(board, i, j, dir[0], dir[1], playerColor);
                                    if (!string.IsNullOrEmpty(shapeType))
                                    {
                                        shapes[shapeType]++;
                                        countedShapes.Add(shapeKey);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 更新缓存
            if (playerColor == lastCacheColor)
                myShapesCache = shapes;
            else
                opponentShapesCache = shapes;

            return shapes;
        }

        // 分析特定方向的棋型
        private string AnalyzeShape(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            int count = CountConsecutive(board, x, y, dx, dy, playerColor);

            if (count < 2) return null;

            bool leftOpen = IsOpenEnd(board, x, y, -dx, -dy, playerColor);
            bool rightOpen = IsOpenEnd(board, x, y, dx, dy, playerColor);

            if (count >= 5) return "five";
            if (count == 4)
            {
                if (leftOpen && rightOpen) return "open_four";
                if (leftOpen || rightOpen) return "four";
            }
            if (count == 3)
            {
                if (leftOpen && rightOpen) return "open_three";
                if (leftOpen || rightOpen) return "three";
            }
            if (count == 2)
            {
                if (leftOpen && rightOpen) return "open_two";
                if (leftOpen || rightOpen) return "two";
            }

            return null;
        }

        // 计算棋型评分
        private double CalculateShapeScore(Dictionary<string, int> shapes)
        {
            double score = 0;

            score += shapes["five"] * 100000;
            score += shapes["open_four"] * 10000;
            score += shapes["four"] * 1000;
            score += shapes["open_three"] * 1500;
            score += shapes["three"] * 100;
            score += shapes["open_two"] * 100;
            score += shapes["two"] * 10;

            return score;
        }

        // 计算威胁奖励
        private double CalculateThreatBonus(Dictionary<string, int> myShapes, Dictionary<string, int> opponentShapes)
        {
            double bonus = 0;

            // 防守评分：对手威胁越大，我方压力越大
            if (opponentShapes["open_four"] > 0)
                bonus -= 8000; // 对手活四，极度危险

            if (opponentShapes["four"] > 1)
                bonus -= 3000; // 对手多个冲四

            if (opponentShapes["open_three"] > 1)
                bonus -= 1500; // 对手多个活三

            // 进攻评分：我方威胁越大，优势越大
            if (myShapes["open_four"] > 0)
                bonus += 10000; // 我方活四，几乎必胜

            if (myShapes["four"] > 1)
                bonus += 4000; // 我方多个冲四

            if (myShapes["open_three"] > 1)
                bonus += 2000; // 我方多个活三

            return bonus;
        }

        // 计算位置奖励
        private double CalculatePositionBonus(Board board, int playerColor)
        {
            double bonus = 0;
            int center = boardSize / 2;
            PlayerColor color = playerColor == 1 ? PlayerColor.Black : PlayerColor.White;

            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (board.GetCell(i, j) == color)
                    {
                        // 距离中心越近，奖励越高
                        double distance = Math.Sqrt((i - center) * (i - center) + (j - center) * (j - center));
                        bonus += Math.Max(0, 50 - distance * 5);
                    }
                }
            }

            return bonus;
        }

        // 计算地雷风险
        private double CalculateMineRisk(Board board, MineMap mineMap, int playerColor)
        {
            double risk = 0;
            PlayerColor color = playerColor == 1 ? PlayerColor.Black : PlayerColor.White;

            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (board.GetCell(i, j) == color)
                    {
                        // 检查周围的地雷风险
                        double mineProb = GetMineProbability(i, j);
                        risk += mineProb * 100; // 地雷概率越高，风险越大
                    }
                }
            }

            return risk;
        }



    }
}