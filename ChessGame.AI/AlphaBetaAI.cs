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

        public AlphaBetaAI(int boardSize = 15, int searchDepth = 4)
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

            // 1. 优先检查是否有必胜点
            if (helper.HasWinningMove(board, aiColor, out var winMove))
            {
                return winMove;
            }

            // 2. 检查对手是否有必胜点需要阻止（最高优先级防守）
            int opponentColor = aiColor == 1 ? 2 : 1;
            if (helper.HasWinningMove(board, opponentColor, out var blockMove))
            {
                // 检查这个阻止位置是否是我方的禁手
                if (!(aiColor == 1 && helper.IsForbiddenMove(board, blockMove.x, blockMove.y, aiColor)))
                {
                    return blockMove;
                }
            }

            // 3. 检查对手是否有真正的活四需要阻止
            var opponentOpenFours = FindAllValidOpenFours(board, opponentColor);
            if (opponentOpenFours.Count > 0)
            {
                var bestDefense = FindBestDefenseAgainstOpenFour(board, opponentOpenFours, aiColor);
                if (bestDefense.x != -1)
                {
                    return bestDefense;
                }
            }

            // 4. 检查对手是否有真正的冲四需要阻止
            var opponentFours = FindAllValidFours(board, opponentColor);
            if (opponentFours.Count > 0)
            {
                var bestDefense = FindBestDefenseAgainstFour(board, opponentFours, aiColor);
                if (bestDefense.x != -1)
                {
                    return bestDefense;
                }
            }

            // 5. 检查对手是否有真正的活三需要阻止（强制防守）
            var opponentOpenThrees = FindAllValidOpenThrees(board, opponentColor);
            if (opponentOpenThrees.Count > 0)
            {
                var bestDefense = FindBestDefenseAgainstOpenThree(board, opponentOpenThrees, aiColor);
                if (bestDefense.x != -1)
                {
                    return bestDefense;
                }
            }

            // 6. 检查是否需要紧急防守其他威胁
            if (helper.HasImmediateThreat(board, aiColor, out var defenseMove))
            {
                return defenseMove;
            }

            // 7. 进行常规的AI搜索
            return PerformRegularSearch(board, mineMap, aiColor);
        }

        // 修改：找到所有有效的活四位置（排除被阻挡的）
        private List<(int x, int y)> FindAllValidOpenFours(Board board, int playerColor)
        {
            List<(int x, int y)> validOpenFours = new List<(int x, int y)>();

            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (board.GetCell(i, j) == PlayerColor.None)
                    {
                        // 模拟在此位置落子
                        Board tempBoard = board.Clone();
                        tempBoard.SetCell(i, j, (PlayerColor)playerColor);

                        // 检查是否形成真正的活四（不被阻挡）
                        if (IsValidOpenFour(tempBoard, i, j, playerColor))
                        {
                            validOpenFours.Add((i, j));
                        }
                    }
                }
            }

            return validOpenFours;
        }

        // 修改：找到所有有效的冲四位置（排除被阻挡的）
        private List<(int x, int y)> FindAllValidFours(Board board, int playerColor)
        {
            List<(int x, int y)> validFours = new List<(int x, int y)>();

            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (board.GetCell(i, j) == PlayerColor.None)
                    {
                        // 模拟在此位置落子
                        Board tempBoard = board.Clone();
                        tempBoard.SetCell(i, j, (PlayerColor)playerColor);

                        // 检查是否形成真正的冲四（不被完全阻挡）
                        if (IsValidFour(tempBoard, i, j, playerColor))
                        {
                            validFours.Add((i, j));
                        }
                    }
                }
            }

            return validFours;
        }

        // 修改：找到所有有效的活三位置（排除被阻挡的）
        private List<(int x, int y)> FindAllValidOpenThrees(Board board, int playerColor)
        {
            List<(int x, int y)> validOpenThrees = new List<(int x, int y)>();

            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    if (board.GetCell(i, j) == PlayerColor.None)
                    {
                        // 模拟在此位置落子
                        Board tempBoard = board.Clone();
                        tempBoard.SetCell(i, j, (PlayerColor)playerColor);

                        // 检查是否形成真正的活三（不被阻挡）
                        if (IsValidOpenThree(tempBoard, i, j, playerColor))
                        {
                            validOpenThrees.Add((i, j));
                        }
                    }
                }
            }

            return validOpenThrees;
        }

        // 新增：检查是否是真正的活四（两端都有空位可以成五）
        private bool IsValidOpenFour(Board board, int x, int y, int playerColor)
        {
            if (!helper.HasOpenFour(board, x, y, playerColor))
                return false;

            // 四个方向
            int[][] directions = new int[][]
            {
                new int[] { 1, 0 }, new int[] { 0, 1 },
                new int[] { 1, 1 }, new int[] { 1, -1 }
            };

            foreach (var dir in directions)
            {
                if (IsFourInDirection(board, x, y, dir[0], dir[1], playerColor))
                {
                    // 检查这个方向的四子是否真的可以成五
                    if (CanFormFiveInDirection(board, x, y, dir[0], dir[1], playerColor))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // 新增：检查是否是真正的冲四（至少有一端可以成五）
        private bool IsValidFour(Board board, int x, int y, int playerColor)
        {
            if (!helper.HasFour(board, x, y, playerColor))
                return false;

            // 如果是活四，肯定是有效的
            if (helper.HasOpenFour(board, x, y, playerColor))
                return true;

            // 四个方向
            int[][] directions = new int[][]
            {
                new int[] { 1, 0 }, new int[] { 0, 1 },
                new int[] { 1, 1 }, new int[] { 1, -1 }
            };

            foreach (var dir in directions)
            {
                if (IsFourInDirection(board, x, y, dir[0], dir[1], playerColor))
                {
                    // 检查这个方向的四子是否至少有一端可以成五
                    if (HasAtLeastOneOpenEnd(board, x, y, dir[0], dir[1], playerColor))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // 新增：检查是否是真正的活三（可以形成活四）
        private bool IsValidOpenThree(Board board, int x, int y, int playerColor)
        {
            if (!helper.HasOpenThree(board, x, y, playerColor))
                return false;

            // 四个方向
            int[][] directions = new int[][]
            {
                new int[] { 1, 0 }, new int[] { 0, 1 },
                new int[] { 1, 1 }, new int[] { 1, -1 }
            };

            foreach (var dir in directions)
            {
                if (IsThreeInDirection(board, x, y, dir[0], dir[1], playerColor))
                {
                    // 检查这个方向的三子是否可以形成活四
                    if (CanFormOpenFourInDirection(board, x, y, dir[0], dir[1], playerColor))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // 新增：检查特定方向是否有四子连珠
        private bool IsFourInDirection(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            int count = helper.CountConsecutive(board, x, y, dx, dy, playerColor);
            return count == 4;
        }

        // 新增：检查特定方向是否有三子连珠
        private bool IsThreeInDirection(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            int count = helper.CountConsecutive(board, x, y, dx, dy, playerColor);
            return count == 3;
        }

        // 新增：检查四子是否可以在这个方向成五
        private bool CanFormFiveInDirection(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            PlayerColor color = playerColor == 1 ? PlayerColor.Black : PlayerColor.White;

            // 找到四子连珠的两端
            var (leftEnd, rightEnd) = FindFourEnds(board, x, y, dx, dy, playerColor);

            // 检查左端是否可以扩展
            int leftExtX = leftEnd.x - dx;
            int leftExtY = leftEnd.y - dy;
            bool leftCanExtend = IsValidPosition(leftExtX, leftExtY) &&
                                board.GetCell(leftExtX, leftExtY) == PlayerColor.None;

            // 检查右端是否可以扩展
            int rightExtX = rightEnd.x + dx;
            int rightExtY = rightEnd.y + dy;
            bool rightCanExtend = IsValidPosition(rightExtX, rightExtY) &&
                                 board.GetCell(rightExtX, rightExtY) == PlayerColor.None;

            // 活四需要两端都可以扩展，冲四需要至少一端可以扩展
            return leftCanExtend && rightCanExtend; // 这里检查活四的条件
        }

        // 新增：检查四子是否至少有一端可以成五
        private bool HasAtLeastOneOpenEnd(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            var (leftEnd, rightEnd) = FindFourEnds(board, x, y, dx, dy, playerColor);

            // 检查左端是否可以扩展
            int leftExtX = leftEnd.x - dx;
            int leftExtY = leftEnd.y - dy;
            bool leftCanExtend = IsValidPosition(leftExtX, leftExtY) &&
                                board.GetCell(leftExtX, leftExtY) == PlayerColor.None;

            // 检查右端是否可以扩展
            int rightExtX = rightEnd.x + dx;
            int rightExtY = rightEnd.y + dy;
            bool rightCanExtend = IsValidPosition(rightExtX, rightExtY) &&
                                 board.GetCell(rightExtX, rightExtY) == PlayerColor.None;

            return leftCanExtend || rightCanExtend;
        }

        // 新增：检查三子是否可以形成活四
        private bool CanFormOpenFourInDirection(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            var (leftEnd, rightEnd) = FindThreeEnds(board, x, y, dx, dy, playerColor);

            // 检查左端扩展后是否能形成活四
            int leftExtX = leftEnd.x - dx;
            int leftExtY = leftEnd.y - dy;
            bool leftCanFormOpenFour = false;
            if (IsValidPosition(leftExtX, leftExtY) && board.GetCell(leftExtX, leftExtY) == PlayerColor.None)
            {
                // 模拟在左端落子，检查是否能形成活四
                Board tempBoard = board.Clone();
                tempBoard.SetCell(leftExtX, leftExtY, (PlayerColor)playerColor);
                leftCanFormOpenFour = IsValidOpenFour(tempBoard, leftExtX, leftExtY, playerColor);
            }

            // 检查右端扩展后是否能形成活四
            int rightExtX = rightEnd.x + dx;
            int rightExtY = rightEnd.y + dy;
            bool rightCanFormOpenFour = false;
            if (IsValidPosition(rightExtX, rightExtY) && board.GetCell(rightExtX, rightExtY) == PlayerColor.None)
            {
                // 模拟在右端落子，检查是否能形成活四
                Board tempBoard = board.Clone();
                tempBoard.SetCell(rightExtX, rightExtY, (PlayerColor)playerColor);
                rightCanFormOpenFour = IsValidOpenFour(tempBoard, rightExtX, rightExtY, playerColor);
            }

            return leftCanFormOpenFour || rightCanFormOpenFour;
        }

        // 新增：找到四子连珠的两端
        private ((int x, int y) leftEnd, (int x, int y) rightEnd) FindFourEnds(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            PlayerColor color = playerColor == 1 ? PlayerColor.Black : PlayerColor.White;

            int leftX = x, leftY = y;
            int rightX = x, rightY = y;

            // 向左找边界
            for (int i = 1; i < 4; i++)
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
            for (int i = 1; i < 4; i++)
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

        // 新增：找到三子连珠的两端
        private ((int x, int y) leftEnd, (int x, int y) rightEnd) FindThreeEnds(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            PlayerColor color = playerColor == 1 ? PlayerColor.Black : PlayerColor.White;

            int leftX = x, leftY = y;
            int rightX = x, rightY = y;

            // 向左找边界
            for (int i = 1; i < 3; i++)
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
            for (int i = 1; i < 3; i++)
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

        // 新增：检查位置是否有效
        private bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < boardSize && y >= 0 && y < boardSize;
        }

        // 新增：找到对抗活四的最佳防守位置
        private (int x, int y) FindBestDefenseAgainstOpenFour(Board board, List<(int x, int y)> opponentOpenFours, int aiColor)
        {
            // 活四只有一个防守点，必须阻止
            foreach (var threat in opponentOpenFours)
            {
                // 检查是否是禁手
                if (!(aiColor == 1 && helper.IsForbiddenMove(board, threat.x, threat.y, aiColor)))
                {
                    return threat;
                }
            }
            return (-1, -1);
        }

        // 新增：找到对抗冲四的最佳防守位置
        private (int x, int y) FindBestDefenseAgainstFour(Board board, List<(int x, int y)> opponentFours, int aiColor)
        {
            // 冲四也只有一个防守点
            foreach (var threat in opponentFours)
            {
                // 检查是否是禁手
                if (!(aiColor == 1 && helper.IsForbiddenMove(board, threat.x, threat.y, aiColor)))
                {
                    return threat;
                }
            }
            return (-1, -1);
        }

        // 新增：找到对抗活三的最佳防守位置
        private (int x, int y) FindBestDefenseAgainstOpenThree(Board board, List<(int x, int y)> opponentOpenThrees, int aiColor)
        {
            List<(int x, int y, double score)> defenseOptions = new List<(int x, int y, double score)>();

            // 对于每个活三威胁，找到所有可能的防守位置
            foreach (var threat in opponentOpenThrees)
            {
                var defensePositions = FindDefensePositionsForOpenThree(board, threat.x, threat.y, aiColor == 1 ? 2 : 1, aiColor);

                foreach (var defense in defensePositions)
                {
                    // 检查是否是禁手
                    if (aiColor == 1 && helper.IsForbiddenMove(board, defense.x, defense.y, aiColor))
                        continue;

                    // 评估这个防守位置的价值
                    double score = EvaluateDefensePosition(board, defense.x, defense.y, aiColor);
                    defenseOptions.Add((defense.x, defense.y, score));
                }
            }

            // 如果有防守选项，选择最佳的
            if (defenseOptions.Count > 0)
            {
                // 去重并选择评分最高的
                var uniqueDefenses = defenseOptions
                    .GroupBy(d => new { d.x, d.y })
                    .Select(g => g.OrderByDescending(d => d.score).First())
                    .OrderByDescending(d => d.score)
                    .ToList();

                return (uniqueDefenses.First().x, uniqueDefenses.First().y);
            }

            return (-1, -1);
        }

        // 新增：找到针对特定活三的防守位置
        private List<(int x, int y)> FindDefensePositionsForOpenThree(Board board, int threatX, int threatY, int opponentColor, int aiColor)
        {
            List<(int x, int y)> defensePositions = new List<(int x, int y)>();

            // 模拟对手在威胁位置落子
            Board tempBoard = board.Clone();
            tempBoard.SetCell(threatX, threatY, (PlayerColor)opponentColor);

            // 四个方向检查
            int[][] directions = new int[][]
            {
                new int[] { 1, 0 }, new int[] { 0, 1 },
                new int[] { 1, 1 }, new int[] { 1, -1 }
            };

            foreach (var dir in directions)
            {
                // 检查这个方向是否有活三
                if (IsThreeInDirection(tempBoard, threatX, threatY, dir[0], dir[1], opponentColor))
                {
                    // 找到这个方向活三的两个延伸点
                    var extensionPoints = FindThreeExtensionPoints(tempBoard, threatX, threatY, dir[0], dir[1], opponentColor);
                    defensePositions.AddRange(extensionPoints);
                }
            }

            return defensePositions.Distinct().ToList();
        }

        // 新增：找到活三的延伸点
        private List<(int x, int y)> FindThreeExtensionPoints(Board board, int x, int y, int dx, int dy, int playerColor)
        {
            List<(int x, int y)> points = new List<(int x, int y)>();

            var (leftEnd, rightEnd) = FindThreeEnds(board, x, y, dx, dy, playerColor);

            // 添加两端的延伸点
            int leftExtX = leftEnd.x - dx;
            int leftExtY = leftEnd.y - dy;
            if (IsValidPosition(leftExtX, leftExtY) && board.GetCell(leftExtX, leftExtY) == PlayerColor.None)
            {
                points.Add((leftExtX, leftExtY));
            }

            int rightExtX = rightEnd.x + dx;
            int rightExtY = rightEnd.y + dy;
            if (IsValidPosition(rightExtX, rightExtY) && board.GetCell(rightExtX, rightExtY) == PlayerColor.None)
            {
                points.Add((rightExtX, rightExtY));
            }

            return points;
        }

        // 新增：评估防守位置的价值
        private double EvaluateDefensePosition(Board board, int x, int y, int aiColor)
        {
            double score = 0;

            // 基础防守价值
            score += 1000;

            // 检查这个位置对我方是否也有价值
            double myValue = helper.EvaluateMoveHeuristic(board, new MineMap(), x, y, aiColor);
            if (myValue != double.MinValue)
            {
                score += myValue * 0.1; // 给我方价值一个小权重
            }

            // 位置价值（中心位置更好）
            int centerDistance = Math.Abs(x - boardSize / 2) + Math.Abs(y - boardSize / 2);
            score += Math.Max(0, 10 - centerDistance) * 10;

            return score;
        }

        // 执行常规的AI搜索
        private (int x, int y) PerformRegularSearch(Board board, MineMap mineMap, int aiColor)
        {
            // 生成并评估所有可能的落子
            var possibleMoves = helper.GetPossibleMoves(board, aiColor);
            if (possibleMoves.Count == 0)
                return helper.GetRandomLegalMove(board, aiColor);

            List<(int x, int y, double finalScore)> evaluatedMoves = new List<(int x, int y, double finalScore)>();

            // 对每个可能的落子进行综合评估
            foreach (var move in possibleMoves)
            {
                // 获取综合评估
                var (score, winRate, confidence) = helper.EvaluateMoveComprehensive(board, mineMap, move.x, move.y, aiColor);

                // 如果是禁手位置，跳过
                if (score == double.MinValue)
                    continue;

                // 检查这个位置的战术价值
                double tacticalBonus = EvaluateValidTacticalValue(board, move.x, move.y, aiColor);



                // 根据置信度混合评分和胜率
                double finalScore;
                if (confidence > 0.7)
                {
                    // 高置信度：主要依靠评分（更精确）
                    finalScore = score * 0.7 + winRate * 10000 * 0.2 + tacticalBonus * 0.1;
                }
                else if (confidence > 0.4)
                {
                    // 中等置信度：平衡考虑
                    finalScore = score * 0.4 + winRate * 10000 * 0.4 + tacticalBonus * 0.2;
                }
                else
                {
                    // 低置信度：主要依靠胜率（更稳健）
                    finalScore = score * 0.2 + winRate * 10000 * 0.6 + tacticalBonus * 0.2;
                }

                evaluatedMoves.Add((move.x, move.y, finalScore));
            }

            // 如果没有合法落子，返回随机位置
            if (evaluatedMoves.Count == 0)
            {
                return helper.GetRandomLegalMove(board, aiColor);
            }

            // 选择最佳候选进行Alpha-Beta搜索
            var bestCandidates = evaluatedMoves
                .OrderByDescending(m => m.finalScore)
                .Take(Math.Min(8, evaluatedMoves.Count))
                .ToList();

            List<(int x, int y, double score)> searchResults = new List<(int x, int y, double score)>();

            foreach (var move in bestCandidates)
            {
                // 模拟落子
                Board tempBoard = board.Clone();
                tempBoard.SetCell(move.x, move.y, (PlayerColor)aiColor);

                // 检查是否会爆炸
                if (helper.WillExplodeMine(mineMap, move.x, move.y))
                {
                    (tempBoard, _) = helper.SimulateExplosion(tempBoard, move.x, move.y, aiColor);
                }

                // 使用Alpha-Beta搜索评估这个位置的价值
                double searchScore = AlphaBeta(tempBoard, mineMap, searchDepth - 1, double.MinValue, double.MaxValue, false, aiColor);
                searchResults.Add((move.x, move.y, searchScore));
            }

            // 选择搜索结果最好的落子
            var finalBest = searchResults.OrderByDescending(m => m.score).First();

            // 如果有多个同等评分的落子，随机选择其中一个
            var topMoves = searchResults.Where(m => Math.Abs(m.score - finalBest.score) < 1000).ToList();
            if (topMoves.Count > 1)
            {
                var randomMove = topMoves[random.Next(topMoves.Count)];
                return (randomMove.x, randomMove.y);
            }

            return (finalBest.x, finalBest.y);
        }

        // 新增：评估位置的战术价值
        // 修改：评估位置的有效战术价值（排除被阻挡的棋型）
        private double EvaluateValidTacticalValue(Board board, int x, int y, int playerColor)
        {
            double tacticalValue = 0;

            // 模拟落子
            Board tempBoard = board.Clone();
            tempBoard.SetCell(x, y, (PlayerColor)playerColor);

            int opponentColor = playerColor == 1 ? 2 : 1;

            // 检查是否能形成有效的威胁（只计算真正有威胁的棋型）
            int myValidThreats = CountValidThreats(tempBoard, x, y, playerColor);
            tacticalValue += myValidThreats * 500;

            // 检查是否能阻止对手的有效威胁
            int blockedValidThreats = CountBlockedValidThreats(board, tempBoard, x, y, opponentColor);
            tacticalValue += blockedValidThreats * 300;

            // 检查位置的控制价值（中心位置更有价值）
            int centerDistance = Math.Abs(x - boardSize / 2) + Math.Abs(y - boardSize / 2);
            tacticalValue += Math.Max(0, 10 - centerDistance) * 20;

            return tacticalValue;
        }

        // 修改：计算有效威胁数量（只计算真正有威胁的棋型）
        private int CountValidThreats(Board board, int x, int y, int playerColor)
        {
            int threats = 0;

            // 检查是否形成有效的活四
            if (IsValidOpenFour(board, x, y, playerColor))
                threats += 3;

            // 检查是否形成有效的冲四
            else if (IsValidFour(board, x, y, playerColor))
                threats += 2;

            // 检查是否形成有效的活三
            else if (IsValidOpenThree(board, x, y, playerColor))
                threats += 1;

            return threats;
        }

        // 修改：计算阻止的有效威胁数量
        private int CountBlockedValidThreats(Board originalBoard, Board newBoard, int x, int y, int opponentColor)
        {
            int blockedThreats = 0;

            // 检查在这个位置，对手原本能形成什么有效威胁
            Board tempBoard = originalBoard.Clone();
            tempBoard.SetCell(x, y, (PlayerColor)opponentColor);

            if (IsValidOpenFour(tempBoard, x, y, opponentColor))
                blockedThreats += 3;
            else if (IsValidFour(tempBoard, x, y, opponentColor))
                blockedThreats += 2;
            else if (IsValidOpenThree(tempBoard, x, y, opponentColor))
                blockedThreats += 1;

            return blockedThreats;
        }


        // Alpha-Beta剪枝搜索算法
        private double AlphaBeta(Board board, MineMap mineMap, int depth, double alpha, double beta, bool isMaximizing, int playerColor)
        {
            // 终止条件
            if (depth == 0 || helper.IsGameOver(board))
            {
                return helper.EvaluateBoard(board, mineMap, playerColor);
            }

            int opponentColor = playerColor == 1 ? 2 : 1;
            int currentColor = isMaximizing ? playerColor : opponentColor;

            // 在搜索过程中也要检查紧急威胁
            if (depth >= 2) // 只在较深层检查，避免影响性能
            {
                // 检查当前玩家是否有必胜点
                if (helper.HasWinningMove(board, currentColor, out var winMove))
                {
                    // 检查是否是禁手
                    if (!(currentColor == 1 && helper.IsForbiddenMove(board, winMove.x, winMove.y, currentColor)))
                    {
                        // 模拟这个必胜落子
                        Board tempBoard = board.Clone();
                        tempBoard.SetCell(winMove.x, winMove.y, (PlayerColor)currentColor);

                        if (helper.WillExplodeMine(mineMap, winMove.x, winMove.y))
                        {
                            (tempBoard, _) = helper.SimulateExplosion(tempBoard, winMove.x, winMove.y, currentColor);
                        }

                        return AlphaBeta(tempBoard, mineMap, depth - 1, alpha, beta, !isMaximizing, playerColor);
                    }
                }

                // 检查是否有紧急威胁需要防守
                int defendingColor = currentColor;
                int attackingColor = defendingColor == 1 ? 2 : 1;

                // 检查对手是否有有效的活四
                var opponentValidOpenFours = FindAllValidOpenFours(board, attackingColor);
                if (opponentValidOpenFours.Count > 0)
                {
                    var bestDefense = FindBestDefenseAgainstOpenFour(board, opponentValidOpenFours, defendingColor);
                    if (bestDefense.x != -1)
                    {
                        Board tempBoard = board.Clone();
                        tempBoard.SetCell(bestDefense.x, bestDefense.y, (PlayerColor)currentColor);

                        if (helper.WillExplodeMine(mineMap, bestDefense.x, bestDefense.y))
                        {
                            (tempBoard, _) = helper.SimulateExplosion(tempBoard, bestDefense.x, bestDefense.y, currentColor);
                        }

                        return AlphaBeta(tempBoard, mineMap, depth - 1, alpha, beta, !isMaximizing, playerColor);
                    }
                }

                // 检查对手是否有有效的活三
                var opponentValidOpenThrees = FindAllValidOpenThrees(board, attackingColor);
                if (opponentValidOpenThrees.Count > 0)
                {
                    var bestDefense = FindBestDefenseAgainstOpenThree(board, opponentValidOpenThrees, defendingColor);
                    if (bestDefense.x != -1)
                    {
                        Board tempBoard = board.Clone();
                        tempBoard.SetCell(bestDefense.x, bestDefense.y, (PlayerColor)currentColor);

                        if (helper.WillExplodeMine(mineMap, bestDefense.x, bestDefense.y))
                        {
                            (tempBoard, _) = helper.SimulateExplosion(tempBoard, bestDefense.x, bestDefense.y, currentColor);
                        }

                        return AlphaBeta(tempBoard, mineMap, depth - 1, alpha, beta, !isMaximizing, playerColor);
                    }
                }
            }


            // 获取所有可能的落子
            var possibleMoves = helper.GetPossibleMoves(board, currentColor);

            if (possibleMoves.Count == 0)
            {
                // 没有合法落子，评估当前局面
                return helper.EvaluateBoard(board, mineMap, playerColor);
            }

            // 对落子进行启发式排序，提高剪枝效率
            var sortedMoves = possibleMoves
                .Select(move => new
                {
                    Move = move,
                    Score = helper.EvaluateMoveHeuristic(board, mineMap, move.x, move.y, currentColor)
                })
                .Where(m => m.Score != double.MinValue) // 排除禁手位置
                .OrderByDescending(m => isMaximizing ? m.Score : -m.Score)
                .Take(Math.Min(12, possibleMoves.Count)) // 适当增加搜索宽度
                .Select(m => m.Move)
                .ToList();

            if (isMaximizing)
            {
                double maxEval = double.MinValue;

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
                    double eval = AlphaBeta(tempBoard, mineMap, depth - 1, alpha, beta, false, playerColor);
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
                double minEval = double.MaxValue;

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
                    double eval = AlphaBeta(tempBoard, mineMap, depth - 1, alpha, beta, true, playerColor);
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
                //double score = helper.EvaluateMoveHeuristic(board, mineMap, move.x, move.y, playerColor);
                //moves.Add((move.x, move.y, score));
                // 使用启发式函数评估每个落子
                double score = helper.EvaluateMoveHeuristic(board, mineMap, move.x, move.y, playerColor);

                // 排除禁手位置
                if (score != double.MinValue)
                {
                    moves.Add((move.x, move.y, score));
                }
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

            // 优先检查紧急情况
            if (helper.HasWinningMove(board, aiColor, out var winMove))
                return winMove;

            if (helper.HasImmediateThreat(board, aiColor, out var defenseMove))
                return defenseMove;

            int opponentColor = aiColor == 1 ? 2 : 1;
            if (helper.HasWinningMove(board, opponentColor, out var blockMove))
            {
                if (!(aiColor == 1 && helper.IsForbiddenMove(board, blockMove.x, blockMove.y, aiColor)))
                    return blockMove;
            }

            // 检查有效的活三威胁
            var opponentValidOpenThrees = FindAllValidOpenThrees(board, opponentColor);
            if (opponentValidOpenThrees.Count > 0)
            {
                var bestDefense = FindBestDefenseAgainstOpenThree(board, opponentValidOpenThrees, aiColor);
                if (bestDefense.x != -1)
                    return bestDefense;
            }

            if (helper.HasImmediateThreat(board, aiColor, out defenseMove))
                return defenseMove;


            // 生成候选落子
            List<(int x, int y, double score)> candidateMoves = GeneratePossibleMoves(board, mineMap, aiColor);

            if (candidateMoves.Count == 0)
                return helper.GetRandomLegalMove(board, aiColor);

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
            List<(int x, int y, double score)> bestMoves = new List<(int x, int y, double score)>();

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
                double moveScore = AlphaBeta(tempBoard, mineMap, depth - 1, int.MinValue, int.MaxValue, false, aiColor);

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