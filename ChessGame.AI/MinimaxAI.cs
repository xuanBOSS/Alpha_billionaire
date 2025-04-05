using ChessGame.GameLogic;
namespace ChessGame.AI  // 命名空间必须一致
{
    public class MinimaxAI 
    {
        public ChessGame.GameLogic.Move CalculateBestMove(ChessGame.GameLogic.Board board)
        {
            // 1. 获取所有合法落子位置（需实现此方法）
            var validMoves = GetAllValidMoves(board);  // 替换原GetValidMoves

            // 2. 随机选择一个落子位置
            if (validMoves.Count == 0)
                throw new InvalidOperationException("No valid moves");

            int randomIndex = new Random().Next(validMoves.Count);  // 传入集合长度
            return validMoves[randomIndex];
        }

        // 新增方法：获取所有合法落子位置
        private List<ChessGame.GameLogic.Move> GetAllValidMoves(ChessGame.GameLogic.Board board)
        {
            var moves = new List<ChessGame.GameLogic.Move>();
            // 实现具体逻辑（示例：遍历棋盘空位）
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    // 创建Move时必须传入player参数
                    var move = new Move(x, y, 1); // 1表示当前玩家
                    if (board.IsEmpty(x, y))
                    moves.Add(move);
                }
            }
            return moves;
        }
    }
}