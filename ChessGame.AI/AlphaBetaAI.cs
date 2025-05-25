using ChessGame.GameLogic;
using ChessGame.GameLogic.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessGame.AI
{
    // 使用Alpha-Beta剪枝的高级AI实现
    public class AlphaBetaAI:IAI
    {
        private readonly int boardSize;
        private readonly int searchDepth;
        private readonly AIHelper helper;
        private readonly Random random = new Random();

        public AlphaBetaAI(int boardSize = 14, int searchDepth = 3)
        {
            this.boardSize = boardSize;
            this.searchDepth = searchDepth;
            this.helper = new AIHelper(boardSize);
        }

        // 获取AI的下一步最佳落子
        public (int x, int y) GetNextMove(Board board, MineMap mineMap, int aiColor)
        {
            // 更新地雷概率推理
            helper.UpdateMineProbabilities(mineMap.numbers);

            // 生成并评估所有可能的落子
            List<(int x, int y, double score)> candidateMoves = GeneratePossibleMoves(board, mineMap, aiColor);

            // 如果没有合法落子，返回(-1, -1)
            if (candidateMoves.Count == 0)
                return (-1, -1);

            // 从评分最高的几个落子点中选择最佳落子
            List<(int x, int y, int score)> bestMoves = new List<(int x, int y, int score)>();

            // 限制搜索的候选落子数，提高效率
            int candidateCount = Math.Min(7, candidateMoves.Count);

            for (int i = 0; i < candidateCount; i++)
            {
                var move = candidateMoves[i];

                // 模拟落子
                Board tempBoard = board.Clone();
                tempBoard.SetCell(move.x, move.y, (PlayerColor)aiColor);

                // 检查是否会爆炸
                if (helper.WillExplodeMine(mineMap, move.x, move.y))
                {
                    (tempBoard, _) = helper.SimulateExplosion(tempBoard, move.x, move.y, aiColor);
                }

                // 执行Alpha-Beta搜索
                int moveScore = AlphaBeta(tempBoard, mineMap, searchDepth - 1, int.MinValue, int.MaxValue, false, aiColor);

                bestMoves.Add((move.x, move.y, moveScore));
            }

            // 选择评分最高的落子
            var bestMove = bestMoves.OrderByDescending(m => m.score).First();

            // 如果有多个同分最高的落子，随机选择其中一个以增加多样性
            var topMoves = bestMoves.Where(m => m.score == bestMove.score).ToList();
            if (topMoves.Count > 1)
            {
                var randomMove = topMoves[random.Next(topMoves.Count)];
                return (randomMove.x, randomMove.y);
            }

            return (bestMove.x, bestMove.y);
        }

        // Alpha-Beta剪枝搜索算法
        private int AlphaBeta(Board board, MineMap mineMap, int depth, int alpha, int beta, bool isMaximizing, int playerColor)
        {
            // 终止条件
            if (depth == 0 || helper.IsGameOver(board))
            {
                return helper.EvaluateBoard(board, mineMap, playerColor);
            }

            int opponentColor = playerColor == 1 ? 2 : 1;
            int currentColor = isMaximizing ? playerColor : opponentColor;

            // 获取所有可能的落子
            var possibleMoves = helper.GetPossibleMoves(board, currentColor);

            if (possibleMoves.Count == 0)
            {
                // 没有合法落子，评估当前局面
                return helper.EvaluateBoard(board, mineMap, playerColor);
            }

            // 对落子进行初步评估和排序，提高剪枝效率
            List<(int x, int y, double score)> sortedMoves = new List<(int x, int y, double score)>();
            foreach (var move in possibleMoves)
            {
                double score = helper.EvaluateMoveHeuristic(board, mineMap, move.x, move.y, currentColor);
                sortedMoves.Add((move.x, move.y, score));
            }

            // 根据当前玩家正反排序
            if (isMaximizing)
                sortedMoves = sortedMoves.OrderByDescending(m => m.score).ToList();
            else
                sortedMoves = sortedMoves.OrderBy(m => m.score).ToList();

            if (isMaximizing)
            {
                int maxEval = int.MinValue;

                foreach (var move in sortedMoves)
                {
                    // 模拟落子
                    Board tempBoard = board.Clone();
                    tempBoard.SetCell(move.x, move.y, (PlayerColor)currentColor);

                    // 检查是否会爆炸
                    if (helper.WillExplodeMine(mineMap, move.x, move.y))
                    {
                        (tempBoard, _) = helper.SimulateExplosion(tempBoard, move.x, move.y, currentColor);
                    }

                    // 递归搜索
                    int eval = AlphaBeta(tempBoard, mineMap, depth - 1, alpha, beta, false, playerColor);
                    maxEval = Math.Max(maxEval, eval);
                    alpha = Math.Max(alpha, eval);

                    // Alpha-Beta剪枝
                    if (beta <= alpha)
                        break;
                }

                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;

                foreach (var move in sortedMoves)
                {
                    // 模拟落子
                    Board tempBoard = board.Clone();
                    tempBoard.SetCell(move.x, move.y, (PlayerColor)currentColor);

                    // 检查是否会爆炸
                    if (helper.WillExplodeMine(mineMap, move.x, move.y))
                    {
                        (tempBoard, _) = helper.SimulateExplosion(tempBoard, move.x, move.y, currentColor);
                    }

                    // 递归搜索
                    int eval = AlphaBeta(tempBoard, mineMap, depth - 1, alpha, beta, true, playerColor);
                    minEval = Math.Min(minEval, eval);
                    beta = Math.Min(beta, eval);

                    // Alpha-Beta剪枝
                    if (beta <= alpha)
                        break;
                }

                return minEval;
            }
        }

        // 生成所有可能的落子并初步评估
        private List<(int x, int y, double score)> GeneratePossibleMoves(Board board, MineMap mineMap, int playerColor)
        {
            List<(int x, int y, double score)> moves = new List<(int x, int y, double score)>();

            // 获取所有可能的合法落子
            var possibleMoves = helper.GetPossibleMoves(board, playerColor);

            foreach (var move in possibleMoves)
            {
                double score = helper.EvaluateMoveHeuristic(board, mineMap, move.x, move.y, playerColor);
                moves.Add((move.x, move.y, score));
            }

            // 按初步评分排序
            return moves.OrderByDescending(m => m.score).ToList();
        }

        // 计算当前局面的胜率
        public double CalculateWinProbability(Board board, MineMap mineMap, int playerColor)
        {
            // 使用AIHelper中的方法计算胜率
            return helper.CalculateWinProbability(board, mineMap, playerColor);
        }

        // 执行迭代加深搜索，在有限时间内找到最佳落子
        public (int x, int y) GetNextMoveWithTimeLimit(Board board, MineMap mineMap, int aiColor, int timeLimit)
        {
            // 更新地雷概率推理
            helper.UpdateMineProbabilities(mineMap.numbers);

            // 生成候选落子
            List<(int x, int y, double score)> candidateMoves = GeneratePossibleMoves(board, mineMap, aiColor);

            if (candidateMoves.Count == 0)
                return (-1, -1);

            // 使用浅层搜索作为备选结果
            var bestMove = candidateMoves.OrderByDescending(m => m.score).First();
            (int bestX, int bestY) = (bestMove.x, bestMove.y);

            // 迭代加深搜索
            DateTime startTime = DateTime.Now;
            for (int depth = 1; depth <= searchDepth; depth++)
            {
                // 时间检查
                if ((DateTime.Now - startTime).TotalMilliseconds > timeLimit * 0.7)
                {
                    break; // 如果已用时超过限制的70%，停止搜索
                }

                // 在当前深度下搜索最佳落子
                (int x, int y) = RunAlphaBetaSearch(board, mineMap, aiColor, depth, candidateMoves);

                // 更新最佳落子
                bestX = x;
                bestY = y;
            }

            return (bestX, bestY);
        }

        // 在指定深度运行Alpha-Beta搜索
        private (int x, int y) RunAlphaBetaSearch(Board board, MineMap mineMap, int aiColor, int depth, List<(int x, int y, double score)> candidateMoves)
        {
            List<(int x, int y, int score)> bestMoves = new List<(int x, int y, int score)>();

            // 限制搜索的候选落子数
            int candidateCount = Math.Min(7, candidateMoves.Count);

            for (int i = 0; i < candidateCount; i++)
            {
                var move = candidateMoves[i];

                // 模拟落子
                Board tempBoard = board.Clone();
                tempBoard.SetCell(move.x, move.y, (PlayerColor)aiColor);

                // 检查是否会爆炸
                if (helper.WillExplodeMine(mineMap, move.x, move.y))
                {
                    (tempBoard, _) = helper.SimulateExplosion(tempBoard, move.x, move.y, aiColor);
                }

                // 执行Alpha-Beta搜索
                int moveScore = AlphaBeta(tempBoard, mineMap, depth - 1, int.MinValue, int.MaxValue, false, aiColor);

                bestMoves.Add((move.x, move.y, moveScore));
            }

            // 选择评分最高的落子
            var bestMove = bestMoves.OrderByDescending(m => m.score).First();

            // 如果有多个同分最高的落子，随机选择其中一个
            var topMoves = bestMoves.Where(m => m.score == bestMove.score).ToList();
            if (topMoves.Count > 1)
            {
                return (topMoves[random.Next(topMoves.Count)].x, topMoves[random.Next(topMoves.Count)].y);
            }

            return (bestMove.x, bestMove.y);
        }
    }
}