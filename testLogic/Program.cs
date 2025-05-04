/*
using ChessGame.GameLogic;
using System.Globalization;

namespace testLogic
{
    internal class Program
    {
        public static void PrintBoard(GameManager gm)
        {
            int BoardSize = 15;
            int MapSize = 14;   

            for(int i = 0; i < BoardSize; i++)
            {
                Console.WriteLine();
                for (int j = 0; j < BoardSize; j++)
                {
                    if(gm.Board.grid[i,j] == PlayerColor.None) Console.Write("+   ");
                    else if (gm.Board.grid[i, j] == PlayerColor.Black) Console.Write("●   ");
                    else
                    {
                         Console.Write("○   ");
                    }
                }

                Console.WriteLine();
                if (i == MapSize) continue;
                Console.Write("  ");
                for(int j = 0;j < MapSize;j++)
                {
                    if (gm.MineMap.mines[i, j] == true) Console.Write("@   ");
                    else
                    {
                        if (gm.MineMap.numbers[i, j] == 0) Console.Write("    ");
                        else Console.Write($"{gm.MineMap.numbers[i, j]}   ");
                    }

                }

            }
        }
        static void Main(string[] args)
        {
            GameManager gameManager = new GameManager();
            Console.SetWindowSize(200, 200);  // 设置控制台窗口大小为200列，200行
            Console.SetBufferSize(200, 300);  // 设置缓冲区大小为200列，300行


            while (true)
            {
                Console.Clear();  // 清除上一轮的显示
                Console.Out.Flush();  // 强制刷新输出
                /*System.Diagnostics.Process.Start("cmd.exe", "/C cls");
                Console.Out.Flush();  // 强制刷新输出
                PrintBoard(gameManager);

                Console.WriteLine($"轮到玩家{gameManager.CurrentPlayer}，请输入坐标：");
                string input = Console.ReadLine();

                int x = 0;
                int y = 0;
                int num = 0;
                for(int i = 0;i<input.Length;i++)
                {
                    if (input[i] >= '0' && input[i] <= '9') num = num*10 +(input[i]-'0');
                    else
                    {
                        x = num;
                        num = 0;
                    }
                }
                y = num;

                string message;
                gameManager.TryMakeMove_1(x, y,out message);
            }
        }
    }
}
/*
using System;
using System.Threading;
using ChessGame.GameLogic;
using ChessGame.GameLogic.Interfaces;
using ChessGame.AI;

namespace ChessGame.TestLogic
{
    class AITester
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== 五子棋 AI 测试程序 ===");

            // 测试自对弈
            Console.WriteLine("\n[测试1] AI 自对弈测试");
            TestAIvsAI();

            // 测试AI移动生成
            Console.WriteLine("\n[测试2] AI 落子生成测试");
            TestAIMoveGeneration();

            // 测试胜率估计
            Console.WriteLine("\n[测试3] 胜率估计测试");
            TestWinRateCalculation();

            Console.WriteLine("\n所有测试完成。按任意键退出...");
            Console.ReadKey();
        }

        static void TestAIvsAI()
        {
            // 创建AI工厂
            Func<int, int, IAI> aiFactory = (boardSize, depth) => new AlphaBetaAI(boardSize, depth);
            Func<int, IAIHelper> aiHelperFactory = (boardSize) => new AIHelper(boardSize);

            // 创建游戏管理器，设置为AI对AI模式
            var gameManager = new GameManager(
                GameMode.AIVsAI,
                AIDifficulty.Easy,  // 使用Easy难度以加快测试速度
                aiFactory,
                aiHelperFactory
            );

            // 添加事件监听器
            int moveCount = 0;
            gameManager.MoveExecuted += (x, y, color) =>
            {
                moveCount++;
                Console.WriteLine($"移动 {moveCount}: {color} 在 ({x}, {y})");
                PrintBoard(gameManager);
            };

            gameManager.WinProbabilityChanged += (blackWinRate, whiteWinRate) =>
            {
                Console.WriteLine($"胜率: 黑方 {blackWinRate:P1} | 白方 {whiteWinRate:P1}");
            };

            gameManager.GameEnded += (winner) =>
            {
                Console.WriteLine($"游戏结束! 获胜方: {winner}");
            };

            // 模拟游戏直到结束或达到最大回合数
            int maxTurns = 50;
            int currentTurn = 0;

            while (!gameManager.IsGameOver && currentTurn < maxTurns)
            {
                // 模拟AI移动
                if (gameManager.CurrentPlayer == PlayerColor.Black)
                {
                    // 模拟黑方AI移动
                    SimulateAIMove(gameManager, PlayerColor.Black);
                }
                else
                {
                    // 模拟白方AI移动
                    SimulateAIMove(gameManager, PlayerColor.White);
                }

                currentTurn++;
                Thread.Sleep(100); // 添加短暂延迟使输出更易读
            }

            // 输出结果
            if (gameManager.IsGameOver)
            {
                Console.WriteLine($"游戏在 {currentTurn} 回合后结束。获胜方: {gameManager.Winner}");
            }
            else
            {
                Console.WriteLine($"游戏达到最大回合数 ({maxTurns})。未决出胜负。");
            }
        }

        static void TestAIMoveGeneration()
        {
            // 创建AI实例
            var ai = new AlphaBetaAI(15, 3);  // 15x15棋盘，搜索深度3

            // 创建一个棋盘和地雷图
            var board = new Board();
            var mineMap = new MineMap();
            mineMap.PlaceMinesByDensity(0.1);
            mineMap.CalculateNumbers();

            // 放置一些棋子形成特定局面
            board.PlaceMove(new Move(7, 7, PlayerColor.Black));
            board.PlaceMove(new Move(7, 8, PlayerColor.White));
            board.PlaceMove(new Move(8, 7, PlayerColor.Black));
            board.PlaceMove(new Move(8, 8, PlayerColor.White));

            // 打印初始棋盘
            Console.WriteLine("初始棋盘状态:");
            PrintBoard(board);

            // 测试黑方AI移动
            Console.WriteLine("\n黑方AI思考中...");
            (int blackX, int blackY) = ai.GetNextMoveWithTimeLimit(board, mineMap, 1, 2000);
            Console.WriteLine($"黑方AI选择落子位置: ({blackX}, {blackY})");

            // 执行黑方移动
            board.PlaceMove(new Move(blackX, blackY, PlayerColor.Black));
            PrintBoard(board);

            // 测试白方AI移动
            Console.WriteLine("\n白方AI思考中...");
            (int whiteX, int whiteY) = ai.GetNextMoveWithTimeLimit(board, mineMap, 2, 2000);
            Console.WriteLine($"白方AI选择落子位置: ({whiteX}, {whiteY})");

            // 执行白方移动
            board.PlaceMove(new Move(whiteX, whiteY, PlayerColor.White));
            PrintBoard(board);
        }

        static void TestWinRateCalculation()
        {
            // 创建AI助手
            var aiHelper = new AIHelper(15);

            // 创建测试局面
            var board = new Board();
            var mineMap = new MineMap();
            int boardSize = 15;
            int[,] numbers = new int[boardSize, boardSize];
            Console.WriteLine("1. 空棋盘胜率测试");
            double blackEmptyRate = aiHelper.CalculateWinProbability(board, mineMap, 1);
            double whiteEmptyRate = aiHelper.CalculateWinProbability(board, mineMap, 2);
            PrintWinRates(blackEmptyRate, whiteEmptyRate);

            Console.WriteLine("\n2. 黑方优势局面胜率测试");
            // 创建黑方占优局面 - 黑方接近连五
            board.PlaceMove(new Move(7, 7, PlayerColor.Black));
            board.PlaceMove(new Move(7, 8, PlayerColor.Black));
            board.PlaceMove(new Move(7, 9, PlayerColor.Black));
            board.PlaceMove(new Move(7, 10, PlayerColor.Black));
            // 白方在其他位置
            board.PlaceMove(new Move(5, 5, PlayerColor.White));
            board.PlaceMove(new Move(5, 6, PlayerColor.White));

            PrintBoard(board);
            double blackAdvRate = aiHelper.CalculateWinProbability(board, mineMap, 1);
            double whiteAdvRate = aiHelper.CalculateWinProbability(board, mineMap, 2);
            PrintWinRates(blackAdvRate, whiteAdvRate);

            Console.WriteLine("\n3. 白方优势局面胜率测试");
            // 重置棋盘
            board = new Board();
            // 创建白方占优局面
            board.PlaceMove(new Move(8, 8, PlayerColor.White));
            board.PlaceMove(new Move(9, 8, PlayerColor.White));
            board.PlaceMove(new Move(10, 8, PlayerColor.White));
            board.PlaceMove(new Move(11, 8, PlayerColor.White));
            // 黑方在其他位置
            board.PlaceMove(new Move(3, 3, PlayerColor.Black));
            board.PlaceMove(new Move(3, 4, PlayerColor.Black));

            PrintBoard(board);
            double blackAdvRate2 = aiHelper.CalculateWinProbability(board, mineMap, 1);
            double whiteAdvRate2 = aiHelper.CalculateWinProbability(board, mineMap, 2);
            PrintWinRates(blackAdvRate2, whiteAdvRate2);
        }

        // 辅助方法：打印棋盘
        static void PrintBoard(Board board)
        {
            Console.WriteLine("  | 0 1 2 3 4 5 6 7 8 9 10111213141");
            Console.WriteLine("--+--------------------------------");

            for (int y = 0; y < Board.Size; y++)
            {
                Console.Write($"{y,2}| ");
                for (int x = 0; x < Board.Size; x++)
                {
                    PlayerColor cell = board.GetCell(x, y);
                    char symbol = cell == PlayerColor.None ? '.' :
                                 cell == PlayerColor.Black ? 'X' : 'O';
                    Console.Write($"{symbol} ");
                }
                Console.WriteLine();
            }
        }

        // 辅助方法：打印棋盘（使用GameManager）
        static void PrintBoard(GameManager gameManager)
        {
            var snapshot = gameManager.GetBoardSnapshot();
            Console.WriteLine("  | 0 1 2 3 4 5 6 7 8 9 10111213141");
            Console.WriteLine("--+--------------------------------");

            for (int y = 0; y < snapshot.GetLength(1); y++)
            {
                Console.Write($"{y,2}| ");
                for (int x = 0; x < snapshot.GetLength(0); x++)
                {
                    PlayerColor cell = snapshot[x, y];
                    char symbol = cell == PlayerColor.None ? '.' :
                                 cell == PlayerColor.Black ? 'X' : 'O';
                    Console.Write($"{symbol} ");
                }
                Console.WriteLine();
            }
        }

        // 辅助方法：打印胜率
        static void PrintWinRates(double blackRate, double whiteRate)
        {
            // 归一化
            double sum = blackRate + whiteRate;
            if (sum > 0)
            {
                blackRate /= sum;
                whiteRate /= sum;
            }
            else
            {
                blackRate = 0.5;
                whiteRate = 0.5;
            }

            Console.WriteLine($"胜率评估: 黑方 {blackRate:P1} | 白方 {whiteRate:P1}");
        }

        // 辅助方法：模拟AI移动
        static void SimulateAIMove(GameManager gameManager, PlayerColor player)
        {
            // 创建一个AlphaBetaAI实例
            var ai = new AlphaBetaAI(Board.Size, 2);

            // 获取当前棋盘状态
            var board = gameManager.Board;
            var mineMap = gameManager.MineMap;

            // 计算最佳移动
            int playerValue = player == PlayerColor.Black ? 1 : 2;
            (int x, int y) = ai.GetNextMoveWithTimeLimit(board, mineMap, playerValue, 1000);

            // 执行移动
            string message;
            if (!gameManager.TryMakeMove_1(x, y, out message))
            {
                Console.WriteLine($"AI移动失败: {message}");
            }
        }
    }
}
*/

using ChessGame.AI;
using ChessGame.GameLogic;
using System;

namespace testLogic
{
    internal class Program
    {
        public static void PrintBoard(GameManager gm)
        {
            int BoardSize = 15;
            int MapSize = 14;

            for (int i = 0; i < BoardSize; i++)
            {
                Console.WriteLine();
                for (int j = 0; j < BoardSize; j++)
                {
                    if (gm.Board.grid[i, j] == PlayerColor.None) Console.Write("+   ");
                    else if (gm.Board.grid[i, j] == PlayerColor.Black) Console.Write("●   ");
                    else
                    {
                        Console.Write("○   ");
                    }
                }

                Console.WriteLine();
                if (i == MapSize) continue;
                Console.Write("  ");
                for (int j = 0; j < MapSize; j++)
                {
                    if (gm.MineMap.mines[i, j] == true) Console.Write("@   ");
                    else
                    {
                        if (gm.MineMap.numbers[i, j] == 0) Console.Write("    ");
                        else Console.Write($"{gm.MineMap.numbers[i, j]}   ");
                    }
                }
            }
        }
        // 新增：显示棋盘及胜率信息
        public static void PrintBoardWithWinRate(GameManager gm, AIHelper aiHelper)
        {
            // 先打印棋盘
            PrintBoard(gm);

            // 计算双方胜率
            double blackWinRate = aiHelper.CalculateWinProbability(gm.Board, gm.MineMap, 1) * 100;
            double whiteWinRate = aiHelper.CalculateWinProbability(gm.Board, gm.MineMap, 2) * 100;

            // 打印胜率信息
            Console.WriteLine();
            Console.WriteLine("--------- 当前局势分析 ---------");
            Console.WriteLine($"黑棋胜率: {blackWinRate:F2}%");
            Console.WriteLine($"白棋胜率: {whiteWinRate:F2}%");

            // 打印局势评估
            if (blackWinRate > whiteWinRate + 5)
            {
                Console.WriteLine("局势评估: 黑棋优势");
            }
            else if (whiteWinRate > blackWinRate + 5)
            {
                Console.WriteLine("局势评估: 白棋优势");
            }
            else
            {
                Console.WriteLine("局势评估: 势均力敌");
            }
            Console.WriteLine("--------------------------------");
        }
        static void Main(string[] args)
        {
            try
            {
                Console.SetWindowSize(200, 200);  // 设置控制台窗口大小为200列，200行
                Console.SetBufferSize(200, 300);  // 设置缓冲区大小为200列，300行
            }
            catch (Exception)
            {
                // 忽略可能的窗口大小调整错误
                Console.WriteLine("无法调整窗口大小，继续使用默认大小。");
            }

            // 显示游戏模式选择菜单
            Console.WriteLine("欢迎来到五子棋游戏！");
            Console.WriteLine("请选择游戏模式：");
            Console.WriteLine("1. 人人对战");
            Console.WriteLine("2. 人机对战");
            Console.WriteLine("3. 人人对战（带胜率分析）");

            int mode = GetValidInput(1, 3);

            if (mode == 1)
            {
                PlayHumanVsHuman(false);
            }
            else if(mode == 2)
            {
                PlayHumanVsAI();
            }
            else
            {
                PlayHumanVsHuman(true);
            }
        }

        // 获取有效的用户输入（数字在min和max之间）
        static int GetValidInput(int min, int max)
        {
            while (true)
            {
                try
                {
                    string input = Console.ReadLine();
                    int value = int.Parse(input);

                    if (value >= min && value <= max)
                    {
                        return value;
                    }
                    else
                    {
                        Console.WriteLine($"请输入{min}到{max}之间的数字！");
                    }
                }
                catch
                {
                    Console.WriteLine("请输入一个有效的数字！");
                }
            }
        }

        // 解析玩家输入的坐标
        static (int x, int y) ParseCoordinates(string input)
        {
            int x = 0;
            int y = 0;
            int num = 0;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] >= '0' && input[i] <= '9')
                {
                    num = num * 10 + (input[i] - '0');
                }
                else
                {
                    x = num;
                    num = 0;
                }
            }
            y = num;

            return (x, y);
        }

        // 人人对战模式
        static void PlayHumanVsHuman(bool showWinRate)
        {
            GameManager gameManager = new GameManager();
            AIHelper aiHelper = null;
            // 如果需要显示胜率，初始化AIHelper
            if (showWinRate)
            {
                aiHelper = new AIHelper(15);
            }
            while (true)
            {
                Console.Clear();  // 清除上一轮的显示
                // 根据是否显示胜率选择不同的显示方法
                if (showWinRate && aiHelper != null)
                {
                    PrintBoardWithWinRate(gameManager, aiHelper);
                }
                else
                {
                    PrintBoard(gameManager);
                }

                if (gameManager.IsGameOver)
                {
                    Console.WriteLine($"游戏结束！玩家{gameManager.Winner}获胜！");
                    Console.WriteLine("按任意键退出...");
                    Console.ReadKey();
                    break;
                }

                Console.WriteLine($"轮到玩家{gameManager.CurrentPlayer}，请输入坐标（格式：x,y）：");
                string input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                {
                    Console.WriteLine("输入无效，请重新输入！");
                    continue;
                }

                (int x, int y) = ParseCoordinates(input);

                string message;
                bool moveSuccess = gameManager.TryMakeMove_1(x, y, out message);

                if (!moveSuccess)
                {
                    Console.WriteLine($"落子失败：{message}");
                    Console.WriteLine("按任意键继续...");
                    Console.ReadKey();
                }
                // 如果需要显示胜率，更新AI的地雷概率
                if (showWinRate && aiHelper != null)
                {
                    aiHelper.UpdateMineProbabilities(gameManager.MineMap.numbers);
                }
            }
        }

        // 人机对战模式
        static void PlayHumanVsAI()
        {
            GameManager gameManager = new GameManager();

            // 初始化AI
            AIHelper aiHelper = new AIHelper(15);

            // 让玩家选择先手或后手
            Console.WriteLine("请选择：");
            Console.WriteLine("1. 玩家先手（黑棋）");
            Console.WriteLine("2. AI先手（黑棋）");

            int playerChoice = GetValidInput(1, 2);
            PlayerColor humanColor = playerChoice == 1 ? PlayerColor.Black : PlayerColor.White;
            PlayerColor aiColor = playerChoice == 1 ? PlayerColor.White : PlayerColor.Black;

            // 如果AI先手，先让AI下一步棋
            if (playerChoice == 2)
            {
                Console.WriteLine("AI正在思考...");
                MakeAIMove(gameManager, aiHelper, aiColor);
            }

            // 主游戏循环
            while (true)
            {
                Console.Clear();  // 清除上一轮的显示
                // 显示棋盘和胜率分析
                PrintBoardWithWinRate(gameManager, aiHelper);

                if (gameManager.IsGameOver)
                {
                    string winner = gameManager.Winner == humanColor ? "玩家" : "AI";
                    Console.WriteLine($"游戏结束！{winner}获胜！");
                    Console.WriteLine("按任意键退出...");
                    Console.ReadKey();
                    break;
                }

                // 判断当前是玩家回合还是AI回合
                if (gameManager.CurrentPlayer == humanColor)
                {
                    // 玩家回合
                    Console.WriteLine("轮到玩家，请输入坐标（格式：x,y）：");
                    string input = Console.ReadLine();

                    if (string.IsNullOrEmpty(input))
                    {
                        Console.WriteLine("输入无效，请重新输入！");
                        continue;
                    }

                    (int x, int y) = ParseCoordinates(input);

                    string message;
                    bool moveSuccess = gameManager.TryMakeMove_1(x, y, out message);

                    if (!moveSuccess)
                    {
                        Console.WriteLine($"落子失败：{message}");
                        Console.WriteLine("按任意键继续...");
                        Console.ReadKey();
                    }
                    // 更新AI的地雷概率
                    aiHelper.UpdateMineProbabilities(gameManager.MineMap.numbers);
                }
                else
                {
                    // AI回合
                    Console.WriteLine("AI正在思考...");
                    System.Threading.Thread.Sleep(1000); // 给AI一点"思考时间"

                    MakeAIMove(gameManager, aiHelper, aiColor);
                }
            }
        }

        // AI落子方法
        static void MakeAIMove(GameManager gameManager, AIHelper aiHelper, PlayerColor aiColor)
        {
            // 更新AI的地雷概率
            aiHelper.UpdateMineProbabilities(gameManager.MineMap.numbers);

            // 获取所有可能的落子位置
            List<(int x, int y)> possibleMoves = aiHelper.GetPossibleMoves(gameManager.Board, aiColor == PlayerColor.Black ? 1 : 2);

            // 如果没有可能的落子位置，尝试随机找一个合法位置
            if (possibleMoves.Count == 0)
            {
                (int x, int y) = aiHelper.GetRandomLegalMove(gameManager.Board, aiColor == PlayerColor.Black ? 1 : 2);

                if (x < 0 || y < 0)
                {
                    Console.WriteLine("AI无法找到合法的落子位置！");
                    return;
                }

                string message;
                gameManager.TryMakeMove_1(x, y, out message);
                Console.WriteLine($"AI落子于：({x}, {y})");
                return;
            }

            // 找出最佳落子位置
            (int bestX, int bestY) = (-1, -1);
            double bestScore = double.MinValue;

            foreach (var move in possibleMoves)
            {
                // 评估每个可能的落子位置
                double score = aiHelper.EvaluateMoveHeuristic(
                    gameManager.Board,
                    gameManager.MineMap,
                    move.x, move.y,
                    aiColor == PlayerColor.Black ? 1 : 2
                );

                if (score > bestScore)
                {
                    bestScore = score;
                    bestX = move.x;
                    bestY = move.y;
                }
            }

            // 执行最佳落子
            if (bestX >= 0 && bestY >= 0)
            {
                string message;
                bool success = gameManager.TryMakeMove_1(bestX, bestY, out message);

                if (success)
                {
                    Console.WriteLine($"AI落子于：({bestX}, {bestY})");
                }
                else
                {
                    // 如果AI的最佳落子无效，尝试随机选择一个合法位置
                    Console.WriteLine($"AI选择的位置无效：{message}，尝试其他位置...");

                    foreach (var move in possibleMoves)
                    {
                        if (gameManager.TryMakeMove_1(move.x, move.y, out message))
                        {
                            Console.WriteLine($"AI落子于：({move.x}, {move.y})");
                            break;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("AI无法决定落子位置！");
            }
        }
    }
}