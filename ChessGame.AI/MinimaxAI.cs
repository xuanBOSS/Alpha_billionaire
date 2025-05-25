using ChessGame.GameLogic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessGame.AI
{
    // 使用极小极大搜索算法的AI实现
    public class MinimaxAI
    {
        private readonly int boardSize;
        private readonly int searchDepth;
        private readonly AIHelper helper;

        public MinimaxAI(int boardSize = 14, int searchDepth = 2)
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
            List<(int x, int y, double score)> candidateMoves = new List<(int x, int y, double score)>();

            // 获取所有可能的合法落子
            var possibleMoves = helper.GetPossibleMoves(board, aiColor);

            foreach (var move in possibleMoves)
            {
                double score = helper.EvaluateMoveHeuristic(board, mineMap, move.x, move.y, aiColor);
                candidateMoves.Add((move.x, move.y, score));
            }

            // 按初步评分排序
            candidateMoves = candidateMoves.OrderByDescending(m => m.score).ToList();

            // 如果没有合法落子，返回(-1, -1)
            if (candidateMoves.Count == 0)
                return (-1, -1);

            // 从评分最高的几个落子点中选择最佳落子
            (int bestX, int bestY, int bestScore) = (-1, -1, int.MinValue);

            // 限制搜索的候选落子数，提高效率
            int candidateCount = Math.Min(5, candidateMoves.Count);

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

                // 执行极小极大搜索
                int moveScore = Minimax(tempBoard, mineMap, searchDepth - 1, false, aiColor);

                if (moveScore > bestScore)
                {
                    bestScore = moveScore;
                    bestX = move.x;
                    bestY = move.y;
                }
            }

            return (bestX, bestY);
        }

        // 极小极大搜索核心算法
        private int Minimax(Board board, MineMap mineMap, int depth, bool isMaximizing, int playerColor)
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

            if (isMaximizing)
            {
                int maxEval = int.MinValue;

                foreach (var move in possibleMoves)
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
                    int eval = Minimax(tempBoard, mineMap, depth - 1, false, playerColor);
                    maxEval = Math.Max(maxEval, eval);
                }

                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;

                foreach (var move in possibleMoves)
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
                    int eval = Minimax(tempBoard, mineMap, depth - 1, true, playerColor);
                    minEval = Math.Min(minEval, eval);
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
    }
}