using ChessGame.AI;
using ChessGame.GameLogic;

public class AIService
{
    private MinimaxAI _ai;
    private MineMap _mineMap;
    private PlayerColor _aiColor;

    public AIService(PlayerColor aiColor = PlayerColor.Black)
    {
        _ai = new MinimaxAI();
        _mineMap = new MineMap();
        _aiColor = aiColor;
    }

    public Move GetBestMove(ChessGame.GameLogic.Board board)
    {
        // 将PlayerColor枚举转换为MinimaxAI期望的整数值
        int aiColorInt = _aiColor == PlayerColor.Black ? 1 : 2;

        // 调用MinimaxAI的GetNextMove方法
        var (x, y) = _ai.GetNextMove(board, _mineMap, aiColorInt);

        // 检查返回的移动是否有效
        if (x == -1 || y == -1)
        {
            throw new InvalidOperationException("AI无法找到有效的移动");
        }

        // 使用Move的构造函数创建新的Move对象
        return new Move(x, y, _aiColor);
    }

    // 设置AI难度
    public void SetDifficulty(int depth)
    {
        _ai = new MinimaxAI(boardSize: MineMap.Size, searchDepth: depth);
    }

    // 更新地雷图
    public void UpdateMineMap(MineMap mineMap)
    {
        _mineMap = mineMap;
    }

    // 设置AI颜色
    public void SetAIColor(PlayerColor color)
    {
        _aiColor = color;
    }

    // 初始化地雷图
    public void InitializeMineMap(double density = 0.15)
    {
        _mineMap.PlaceMinesByDensity(density);
        _mineMap.CalculateNumbers();
    }
}