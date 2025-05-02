namespace ChessGame.GameLogic
{
    public class BombManager
    {
        private Random random = new Random();

        //当前的候选爆破区域中心点（在爆破前一手生成，用于提示）
        //public (int x, int y)? CandidateBombPosition { get; private set; }
        public (int x, int y)? CandidateBombPosition { get; set; }

        //爆破范围半径（半径为1表示 3×3 区域）
        private const int BombRadius = 1;

        //是否启用爆破（后期可根据剩余棋步等判断是否禁用）
        public bool BombEnabled { get; set; } = true;

        // 在棋盘中生成新的爆破候选点
        public void GenerateCandidate(Board board)
        {
            if (!BombEnabled) return;

            // 随机选择一个 3x3 范围中心，必须保证范围合法
            List<(int x, int y)> candidates = new();

            for (int x = 1; x < Board.Size - 1; x++)
            {
                for (int y = 1; y < Board.Size - 1; y++)
                {
                    // 可设置更复杂的权重策略，这里只考虑范围可行
                    candidates.Add((x, y));
                }
            }

            if (candidates.Count > 0)
            {
                CandidateBombPosition = candidates[random.Next(candidates.Count)];
            }
        }

        // 执行当前爆破操作，清除候选区域的棋子
        public void TriggerBomb(Board board)
        {

            if (!BombEnabled || CandidateBombPosition == null) return;

            var (cx, cy) = CandidateBombPosition.Value;

            // 清除3x3区域的所有棋子
            for (int dx = -BombRadius; dx <= BombRadius; dx++)
            {
                for (int dy = -BombRadius; dy <= BombRadius; dy++)
                {
                    int nx = cx + dx;
                    int ny = cy + dy;

                    if (board.InBounds(nx, ny))
                    {
                        board.RemovePiece(nx, ny);
                    }
                }
            }


            CandidateBombPosition = null; // 本次炸弹完成，清除候选
        }

        //清除候选位置（可用于终局或特殊操作）
        public void ClearCandidate()
        {
            CandidateBombPosition = null;
        }
    }
}
