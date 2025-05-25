using ChessGame.AI;
using ChessGame.Database;
using ChessGame.GameLogic;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace testLogic
{
    internal class Program 
    {

        // 添加数据库上下文工厂，用于创建数据库连接
        private static readonly ChessDbContextFactory _dbContextFactory = new ChessDbContextFactory();


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

            // 显示主菜单
            while (true)
            {
                Console.Clear();
                Console.WriteLine("欢迎来到五子棋游戏测试程序！");
                Console.WriteLine("请选择功能：");
                Console.WriteLine("1. 游戏测试");
                Console.WriteLine("2. 数据库测试");
                Console.WriteLine("0. 退出程序");

                int choice = GetValidInput(0, 2);

                if (choice == 0)
                {
                    break;
                }
                else if (choice == 1)
                {
                    // 游戏模式测试
                    GameModeTest();
                }
                else if (choice == 2)
                {
                    // 数据库测试
                    DatabaseTest().Wait(); // 异步任务需要等待完成
                }
            }
        }

        // 新增：数据库测试功能
        private static async Task DatabaseTest()
        {
            Console.Clear();
            Console.WriteLine("========== 数据库功能测试 ==========");
            Console.WriteLine("请选择测试功能：");
            Console.WriteLine("1. 测试数据库连接");
            Console.WriteLine("2. 注册新用户");
            Console.WriteLine("3. 用户登录");
            Console.WriteLine("4. 查看排行榜");
            Console.WriteLine("5. 增加玩家胜利次数");
            Console.WriteLine("0. 返回主菜单");

            int choice = GetValidInput(0, 5);

            switch (choice)
            {
                case 0:
                    return;
                case 1:
                    await TestDatabaseConnection();
                    break;
                case 2:
                    await RegisterUser();
                    break;
                case 3:
                    await UserLogin();
                    break;
                case 4:
                    await ViewLeaderboard();
                    break;
                case 5:
                    await IncrementWins();
                    break;
            }

            Console.WriteLine("\n按任意键返回...");
            Console.ReadKey();
        }

        // 测试数据库连接
        private static async Task TestDatabaseConnection()
        {
            Console.WriteLine("\n正在测试数据库连接...");

            try
            {
                using (var dbContext = _dbContextFactory.CreateDbContext(Array.Empty<string>()))
                {
                    // 尝试从数据库获取数据
                    bool canConnect = await dbContext.Database.CanConnectAsync();

                    if (canConnect)
                    {
                        Console.WriteLine("✓ 数据库连接成功！");

                        // 显示一些基本数据库信息
                        int playerCount = await dbContext.Players.CountAsync();
                        int recordCount = await dbContext.GameRecords.CountAsync();

                        Console.WriteLine($"数据库信息:");
                        Console.WriteLine($"- 用户数量: {playerCount}");
                        Console.WriteLine($"- 游戏记录数量: {recordCount}");
                        Console.WriteLine($"- 数据库提供商: {dbContext.Database.ProviderName}");
                    }
                    else
                    {
                        Console.WriteLine("✗ 数据库连接失败！请检查连接字符串和服务器状态。");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 数据库连接错误: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部错误: {ex.InnerException.Message}");
                }
            }
        }

        // 注册新用户
        private static async Task RegisterUser()
        {
            Console.WriteLine("\n===== 用户注册 =====");

            Console.Write("请输入用户ID: ");
            string userId = Console.ReadLine();

            Console.Write("请输入密码: ");
            string password = Console.ReadLine();

            Console.Write("请输入昵称: ");
            string userName = Console.ReadLine();

            try
            {
                using (var dbContext = _dbContextFactory.CreateDbContext(Array.Empty<string>()))
                {
                    // 检查用户是否已存在
                    var existingUser = await dbContext.Players.FindAsync(userId);
                    if (existingUser != null)
                    {
                        Console.WriteLine("✗ 注册失败: 该用户ID已存在！");
                        return;
                    }

                    // 创建新用户
                    var newPlayer = new Player
                    {
                        UserId = userId,
                        PassWord = password,
                        UserName = userName
                    };

                    dbContext.Players.Add(newPlayer);

                    // 创建新的游戏记录
                    var newRecord = new GameRecord
                    {
                        UserId = userId,
                        UserName = userName,
                        WinTimes = 0
                    };

                    dbContext.GameRecords.Add(newRecord);

                    await dbContext.SaveChangesAsync();
                    Console.WriteLine("✓ 用户注册成功！");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 注册失败: {ex.Message}");
            }
        }

        // 用户登录
        private static async Task UserLogin()
        {
            Console.WriteLine("\n===== 用户登录 =====");

            Console.Write("请输入用户ID: ");
            string userId = Console.ReadLine();

            Console.Write("请输入密码: ");
            string password = Console.ReadLine();

            try
            {
                using (var dbContext = _dbContextFactory.CreateDbContext(Array.Empty<string>()))
                {
                    // 查找用户
                    var player = await dbContext.Players.FirstOrDefaultAsync(p => p.UserId == userId);

                    if (player == null)
                    {
                        Console.WriteLine("✗ 登录失败: 用户不存在！");
                        return;
                    }

                    if (player.PassWord != password)
                    {
                        Console.WriteLine("✗ 登录失败: 密码错误！");
                        return;
                    }

                    // 获取用户的游戏记录
                    var gameRecord = await dbContext.GameRecords.FindAsync(userId);

                    Console.WriteLine("✓ 登录成功！");
                    Console.WriteLine("\n===== 用户信息 =====");
                    Console.WriteLine($"用户ID: {player.UserId}");
                    Console.WriteLine($"昵称: {player.UserName}");
                    Console.WriteLine($"胜利次数: {gameRecord?.WinTimes ?? 0}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 登录失败: {ex.Message}");
            }
        }

        // 查看排行榜
        private static async Task ViewLeaderboard()
        {
            Console.WriteLine("\n===== 游戏排行榜 =====");

            try
            {
                using (var dbContext = _dbContextFactory.CreateDbContext(Array.Empty<string>()))
                {
                    // 获取排序后的游戏记录
                    var leaderboard = await dbContext.GameRecords
                        .OrderByDescending(r => r.WinTimes)
                        .ToListAsync();

                    if (leaderboard.Count == 0)
                    {
                        Console.WriteLine("暂无排行榜数据。");
                        return;
                    }

                    // 打印排行榜表头
                    Console.WriteLine("\n排名\t用户ID\t\t昵称\t\t胜利次数");
                    Console.WriteLine("--------------------------------------------------");

                    // 打印排行榜数据
                    int rank = 1;
                    int lastWins = -1;
                    int lastRank = 0;

                    foreach (var record in leaderboard)
                    {
                        // 处理并列排名
                        if (record.WinTimes != lastWins)
                        {
                            lastRank = rank;
                            lastWins = record.WinTimes;
                        }

                        // 打印排行信息
                        Console.WriteLine($"{lastRank}\t{record.UserId}\t\t{record.UserName}\t\t{record.WinTimes}");
                        rank++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 获取排行榜失败: {ex.Message}");
            }
        }

        // 增加玩家胜利次数
        private static async Task IncrementWins()
        {
            Console.WriteLine("\n===== 增加胜利次数 =====");

            Console.Write("请输入用户ID: ");
            string userId = Console.ReadLine();

            try
            {
                using (var dbContext = _dbContextFactory.CreateDbContext(Array.Empty<string>()))
                {
                    // 查找用户游戏记录
                    var gameRecord = await dbContext.GameRecords.FindAsync(userId);

                    if (gameRecord == null)
                    {
                        Console.WriteLine("✗ 操作失败: 用户不存在！");
                        return;
                    }

                    // 增加胜利次数
                    gameRecord.WinTimes += 1;
                    await dbContext.SaveChangesAsync();

                    Console.WriteLine($"✓ 操作成功！{gameRecord.UserName} 的胜利次数已增加到 {gameRecord.WinTimes}。");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 操作失败: {ex.Message}");
            }
        }

        // 游戏模式测试（原有游戏测试功能）
        static void GameModeTest()
        {
            Console.Clear();
            // 显示游戏模式选择菜单
            Console.WriteLine("请选择游戏模式：");
            Console.WriteLine("1. 人人对战");
            Console.WriteLine("2. 人机对战");
            Console.WriteLine("3. 人人对战（带胜率分析）");
            Console.WriteLine("0. 返回主菜单");

            int mode = GetValidInput(0, 3);

            if (mode == 0)
            {
                return;
            }
            else if (mode == 1)
            {
                PlayHumanVsHuman(false);
            }
            else if (mode == 2)
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

            // 初始化AI - 使用AlphaBetaAI而不仅仅是AIHelper
            AlphaBetaAI ai = new AlphaBetaAI(boardSize: 15, searchDepth: 3);

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
                MakeAIMove(gameManager, ai, aiColor);
            }

            // 主游戏循环
            while (true)
            {
                Console.Clear();  // 清除上一轮的显示
                                  // 显示棋盘和胜率分析
                PrintBoardWithWinRate(gameManager, ai);

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
                }
                else
                {
                    // AI回合
                    Console.WriteLine("AI正在思考...");
                    System.Threading.Thread.Sleep(1000); // 给AI一点"思考时间"

                    MakeAIMove(gameManager, ai, aiColor);
                }
            }
        }

        // AI落子方法 - 修改为使用AlphaBetaAI
        static void MakeAIMove(GameManager gameManager, AlphaBetaAI ai, PlayerColor aiColor)
        {
            // 使用AlphaBetaAI获取最佳落子位置
            int aiColorValue = aiColor == PlayerColor.Black ? 1 : 2;
            (int bestX, int bestY) = ai.GetNextMove(gameManager.Board, gameManager.MineMap, aiColorValue);

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
                    // 如果AI的最佳落子无效，尝试获取随机合法落子
                    Console.WriteLine($"AI选择的位置无效：{message}，尝试其他位置...");

                    // 创建临时AIHelper以获取随机落子
                    AIHelper aiHelper = new AIHelper(15);
                    (int x, int y) = aiHelper.GetRandomLegalMove(gameManager.Board, aiColorValue);

                    if (x >= 0 && y >= 0 && gameManager.TryMakeMove_1(x, y, out message))
                    {
                        Console.WriteLine($"AI落子于：({x}, {y})");
                    }
                    else
                    {
                        Console.WriteLine("AI无法找到合法的落子位置！");
                    }
                }
            }
            else
            {
                Console.WriteLine("AI无法决定落子位置！");
            }
        }

        // 显示棋盘和胜率的方法 - 修改为使用AlphaBetaAI计算胜率
        // 显示棋盘及胜率信息
        public static void PrintBoardWithWinRate(GameManager gm, AlphaBetaAI ai)
        {
            // 先打印棋盘
            PrintBoard(gm);

            // 计算双方胜率 - 使用AlphaBetaAI计算更准确的胜率
            double blackWinRate = ai.CalculateWinProbability(gm.Board, gm.MineMap, 1) * 100;
            double whiteWinRate = ai.CalculateWinProbability(gm.Board, gm.MineMap, 2) * 100;

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
    }
}
