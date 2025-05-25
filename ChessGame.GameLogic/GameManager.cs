
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChessGame.GameLogic.Interfaces;  // 使用接口命名空间

namespace ChessGame.GameLogic  // 命名空间必须一致
{
    public enum GameMode
    {
        PlayerVsPlayer,
        PlayerVsAI,
        AIVsAI  // 用于演示或测试
    }

    public enum AIDifficulty
    {
        Easy,   // 浅层搜索
        Normal, // 中等搜索深度
        Hard    // 深层搜索
    }
    public class GameManager
    {
        //基础游戏状态
        public Board Board { get; private set; }
        public MineMap MineMap { get; private set; }
        public PlayerColor CurrentPlayer { get; private set; }
        public bool IsGameOver { get; private set; }
        public PlayerColor Winner { get; private set; }
        
        //资源限制
        private const int MaxPiecesPerPlayer = 25;
        private readonly Dictionary<PlayerColor, int> pieceCount = new()
        {
            [PlayerColor.Black] = 0,
            [PlayerColor.White] = 0
        };
        
        //游戏记录
        private List<Move> moveHistory;
        private BombManager bombManager;
        private int turnCount;
        private bool shouldSkipNextTurn = false;

        // AI相关字段 - 使用接口类型
        private IAI ai;
        private IAIHelper aiHelper;
        private GameMode gameMode;
        private AIDifficulty aiDifficulty;
        private bool aiPlaysBlack = false;
        private bool isAIThinking = false;

        // 事件定义
        public event Action<int, int, PlayerColor> MoveExecuted;
        public event Action<PlayerColor> GameEnded;
        public event Action<double, double> WinProbabilityChanged;
        public event Action AIStartedThinking;
        public event Action AIFinishedThinking;

        // 添加工厂委托以创建AI实例
        private Func<int, int, IAI> aiFactory;
        private Func<int, IAIHelper> aiHelperFactory;

        public GameManager(
            GameMode mode = GameMode.PlayerVsPlayer,
            AIDifficulty difficulty = AIDifficulty.Normal,
            Func<int, int, IAI> aiFactory = null,
            Func<int, IAIHelper> aiHelperFactory = null)
        {
            Board = new Board();
            MineMap = new MineMap();
            MineMap.PlaceMinesByDensity(0.1);//生成地雷
            MineMap.CalculateNumbers();//生成数字提示
            CurrentPlayer = PlayerColor.Black; // 黑棋先手
            IsGameOver = false;
            Winner = PlayerColor.None;
            moveHistory = new List<Move>();
            bombManager = new BombManager();
            pieceCount[PlayerColor.Black] = 0;
            pieceCount[PlayerColor.White] = 0;
            turnCount = 0;
            // 设置AI工厂
            this.aiFactory = aiFactory;
            this.aiHelperFactory = aiHelperFactory;

            // 初始化游戏模式和AI
            gameMode = mode;
            aiDifficulty = difficulty;

            if (mode != GameMode.PlayerVsPlayer && aiFactory != null && aiHelperFactory != null)
            {
                InitializeAI(difficulty);
                aiHelper = aiHelperFactory(Board.Size);

                // 如果AI执黑且是AI对战模式，立即执行AI落子
                if (gameMode == GameMode.PlayerVsAI && aiPlaysBlack)
                {
                    ExecuteAIMove();
                }
            }
        }

        // 初始化AI并设置难度
        private void InitializeAI(AIDifficulty difficulty)
        {
            if (aiFactory == null) return;

            int searchDepth = 2; // 默认搜索深度

            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    searchDepth = 2;
                    break;
                case AIDifficulty.Normal:
                    searchDepth = 3;
                    break;
                case AIDifficulty.Hard:
                    searchDepth = 4;
                    break;
            }

            ai = aiFactory(Board.Size, searchDepth);
        }

        public bool TryMakeMove_1(int x, int y, out string message)
        {

            message = "";

            if (IsGameOver)
            {
                message = "游戏已结束。";
                return false;
            }

            if (!Board.InBounds(x, y))
            {
                message = "超出棋盘范围。";
                return false;
            }

            if (Board.GetCell(x, y) != PlayerColor.None)
            {
                message = "该位置已有棋子。";
                return false;
            }

            /*if (!CanPlaceMorePieces(CurrentPlayer))
            {
                message = $"{CurrentPlayer}方棋子已用完";
                return false;
            }*/

            // 黑棋禁手检测
            if (CurrentPlayer == PlayerColor.Black && GameRules.IsForbiddenMove(Board, x, y))
            {
                message = "禁手：黑棋此处落子违反规则。";
                return false;
            }

            var move = new Move(x, y, CurrentPlayer);
            Board.PlaceMove(move);
            moveHistory.Add(move);
            pieceCount[CurrentPlayer]++;
            turnCount++;

            //爆破逻辑
            bool explode = MineMap.CheckExplosion(x, y);//检查周围四个格子是否存在地雷
            if(explode)
            {
                bombManager.BombEnabled = true;  //爆破使能信号置位
                bombManager.CandidateBombPosition = (x, y);
                bombManager.TriggerBomb(Board);  // 执行爆破
                bombManager.BombEnabled = false; //爆破使能信号复原
                
            }

            // 胜负判断
            if (GameRules.CheckWin(Board, x, y, CurrentPlayer))
            {
                IsGameOver = true;
                Winner = CurrentPlayer;
                /*message = $"{CurrentPlayer} 获胜！";*/
                message = "获胜！";
                // 切换回合
                SwitchTurn();
                return true;
            }

            // 切换回合
            SwitchTurn();
            return true;
        }

        //终局检查
        private int GetEmptyCells()
        {
            int count = 0;
            for (int x = 0; x < Board.Size; x++)
                for (int y = 0; y < Board.Size; y++)
                    if (Board.GetCell(x, y) == PlayerColor.None)
                        count++;
            return count;
        }

        public void ResetGame()
        {
            Board = new Board();
            CurrentPlayer = PlayerColor.Black;
            IsGameOver = false;
            Winner = PlayerColor.None;
            moveHistory.Clear();
        }

        //回合切换
        private void SwitchTurn()
        {
            if (shouldSkipNextTurn)
            {
                shouldSkipNextTurn = false;
                return; // 跳过回合切换
            }
            CurrentPlayer = CurrentPlayer == PlayerColor.Black ? PlayerColor.White : PlayerColor.Black;
        }

        public IReadOnlyList<Move> GetMoveHistory()
        {
            return moveHistory.AsReadOnly();
        }

        private bool CanPlaceMorePieces(PlayerColor player)
        {
            return pieceCount[player] < MaxPiecesPerPlayer;
        }
        
        // 执行AI落子
        private async void ExecuteAIMove()
        {
            if (IsGameOver || ai == null) return;

            isAIThinking = true;
            AIStartedThinking?.Invoke();

            // 使用Task在后台线程计算AI落子，避免UI卡顿
            await Task.Run(() => {
                // 确定AI的颜色
                PlayerColor aiColor = CurrentPlayer;
                int aiColorValue = aiColor == PlayerColor.Black ? 1 : 2;

                // 让AI计算最佳落子
                int timeLimit = 2000; // 时间限制（毫秒）
                (int x, int y) = ai.GetNextMoveWithTimeLimit(Board, MineMap, aiColorValue, timeLimit);

                // 在主线程执行落子
                Task.Run(() => {
                    if (x != -1 && y != -1) // 确保AI找到了有效落子
                    {
                        string message;
                        TryMakeMove_1(x, y, out message);
                    }

                    isAIThinking = false;
                    AIFinishedThinking?.Invoke();
                }).Wait();
            });
        }

        // 计算并更新胜率
        private void UpdateWinProbabilities()
        {
            if (aiHelper == null) return;

            // 计算黑白两方的胜率
            double blackWinProb = aiHelper.CalculateWinProbability(Board, MineMap, 1);
            double whiteWinProb = aiHelper.CalculateWinProbability(Board, MineMap, 2);

            // 归一化胜率，确保总和为1
            double sum = blackWinProb + whiteWinProb;
            if (sum > 0)
            {
                blackWinProb /= sum;
                whiteWinProb /= sum;
            }
            else
            {
                blackWinProb = 0.5;
                whiteWinProb = 0.5;
            }

            // 触发胜率更新事件
            WinProbabilityChanged?.Invoke(blackWinProb, whiteWinProb);
        }

        // 添加一个方法来获取棋盘快照，便于测试程序进行展示
        public PlayerColor[,] GetBoardSnapshot()
        {
            PlayerColor[,] snapshot = new PlayerColor[Board.Size, Board.Size];

            for (int x = 0; x < Board.Size; x++)
            {
                for (int y = 0; y < Board.Size; y++)
                {
                    snapshot[x, y] = Board.GetCell(x, y);
                }
            }

            return snapshot;
        }

        //获取胜率
        public (double black, double white) GetWinProbabilities()
        {
            if (aiHelper == null) return (0.5, 0.5);

            double blackWinProb = aiHelper.CalculateWinProbability(Board, MineMap, 1);
            double whiteWinProb = aiHelper.CalculateWinProbability(Board, MineMap, 2);

            // 归一化处理
            double sum = blackWinProb + whiteWinProb;
            if (sum > 0)
            {
                blackWinProb /= sum;
                whiteWinProb /= sum;
            }
            else
            {
                blackWinProb = 0.5;
                whiteWinProb = 0.5;
            }

            return (blackWinProb, whiteWinProb);
        }
    }
}
