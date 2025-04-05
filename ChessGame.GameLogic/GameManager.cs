namespace ChessGame.GameLogic  // 命名空间必须一致
{
    public class GameManager  // 必须为public
    {
        public bool IsValidMove(int x, int y)
        {  // 实现逻辑（示例：检查棋盘边界）
            return x >= 0 && x < 15 && y >= 0 && y < 15;
        }
    }
}