namespace ChessGame.GameLogic.Interfaces
{
    /// <summary>
    /// AI 接口定义
    /// </summary>
    public interface IAI
    {
        /// <summary>
        /// 计算最佳落子位置
        /// </summary>
        /// <param name="board">当前棋盘</param>
        /// <param name="mineMap">地雷图</param>
        /// <param name="playerColorValue">玩家颜色值（1=黑，2=白）</param>
        /// <param name="timeLimit">计算时间限制（毫秒）</param>
        /// <returns>最佳落子坐标</returns>
        (int x, int y) GetNextMoveWithTimeLimit(Board board, MineMap mineMap, int playerColorValue, int timeLimit);
    }

    /// <summary>
    /// AI 助手接口，用于计算胜率
    /// </summary>
    public interface IAIHelper
    {
        /// <summary>
        /// 计算指定玩家的胜率
        /// </summary>
        /// <param name="board">当前棋盘</param>
        /// <param name="mineMap">地雷图</param>
        /// <param name="playerColorValue">玩家颜色值（1=黑，2=白）</param>
        /// <returns>胜率估计值</returns>
        double CalculateWinProbability(Board board, MineMap mineMap, int playerColorValue);
    }
}