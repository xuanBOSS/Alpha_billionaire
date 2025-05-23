﻿//AI决策模块
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
        public const int OPEN_FOUR = 10000;       // 活四
        public const int FOUR = 1000;             // 冲四
        public const int OPEN_THREE = 1000;       // 活三
        public const int THREE = 100;             // 眠三
        public const int OPEN_TWO = 100;          // 活二
        public const int TWO = 10;                // 眠二
        public const int MINE_RISK = -500;        // 地雷风险惩罚

        private readonly int boardSize;
        private double[,] mineProbabilities;

        public AIHelper(int boardSize = 15)
        {
            this.boardSize = boardSize;
            this.mineProbabilities = new double[boardSize, boardSize];
            ResetMineProbabilities();
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

        // 计算当前局面对指定玩家的胜率估计
        public double CalculateWinProbability(Board board, MineMap mineMap, int playerColor)
        {
            int totalScore = EvaluateBoard(board, mineMap, playerColor);

            // 使用sigmoid函数将评分转换为0-1之间的胜率
            double winProbability = 1.0 / (1.0 + Math.Exp(-totalScore * 0.0001));

            return winProbability;
        }

    }
}