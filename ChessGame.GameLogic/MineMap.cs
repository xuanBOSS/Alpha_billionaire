namespace ChessGame.GameLogic
{
    public class MineMap
    {
        public const int Size = 14;//地图尺寸
        public bool[,] mines;//指示该位置是否埋雷
        public int[,] numbers;//根据地雷摆放情况生成的数字提示

        public MineMap()
        {
            mines = new bool[Size,Size];
            numbers = new int[Size,Size];
            for(int i = 0;i < Size; i++)
            {
                for(int j = 0;j < Size;j++)
                {
                    mines[i, j] = false;
                    numbers[i, j] = 0;
                }
            }
        }

        //地雷生成函数（包含密度调整）
        public void PlaceMinesByDensity(double density)//传入参数代表有多少比例的格子需要放置地雷
        {
            int totalCells = Size * Size;
            int mineCount = (int)(totalCells * density);//计算需要放置地雷的个数

            Random rnd = new Random();//生成随机种子
            int placed = 0;

            while (placed < mineCount)//尝试摆放地雷
            {
                int x = rnd.Next(0, Size);
                int y = rnd.Next(0, Size);

                if (!mines[x, y])
                {
                    mines[x, y] = true;
                    placed++;
                }
            }

            Console.WriteLine($"成功放置了 {mineCount} 颗地雷，占比 {(density * 100):0.#}%");
        }

        //生成数字提示函数
        public void CalculateNumbers()
        {
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    if (mines[x, y]) continue;//如果该位置已经放置地雷，则跳过

                    int count = 0;
                    //扫描以当前格子为中心的9个格子
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;

                            if (nx >= 0 && nx < Size && ny >= 0 && ny < Size)//防止越界
                            {
                                if (mines[nx, ny]) count++;
                            }
                        }
                    }
                    numbers[x, y] = count;//填入数字
                }
            }
        }

        // 复用原逻辑的局部计算
        private int CalculateAdjacentMines(int x, int y)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (InBounds(nx, ny) && mines[nx, ny]) count++;
                }
            }
            return count;
        }

        //打印扫雷地图
        public void PrintDebugBoard()
        {
            Console.WriteLine("=== 调试棋盘（地雷 + 数字） ===");
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (mines[x, y])
                        Console.Write(" * ");
                    else
                        Console.Write($" {numbers[x, y]} ");
                }
                Console.WriteLine();
            }
        }

        //地雷爆炸处理
        public void ExplodeMine(int mineX, int mineY)
        {
            mines[mineX, mineY] = false; //清除地雷
            UpdateAdjacentNumbers(mineX, mineY); //更新周围数字
        }

        //更新周围3x3格子的数字提示
        public void UpdateAdjacentNumbers(int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (InBounds(nx, ny) && !mines[nx, ny])
                    {
                        numbers[nx, ny] = CalculateAdjacentMines(nx, ny);
                    }
                }
            }
        }

        private List<(int x, int y)> _lastExplodedMines = new List<(int, int)>();

        //放置棋子后检查周围是否有地雷
        public bool CheckExplosion(int px, int py)
        {
            _lastExplodedMines.Clear();
            bool exploded = false;

            // 检查四个相邻格子
            int[] dx = { -1, 0 };
            int[] dy = { -1, 0 };

            foreach (int ix in dx)
            {
                foreach (int iy in dy)
                {
                    int gx = px + ix;
                    int gy = py + iy;
                    if (InBounds(gx, gy) && mines[gx, gy])
                    {
                        _lastExplodedMines.Add((gx, gy));
                        ClearAMine(gx, gy);
                        exploded = true;
                    }
                }
            }
            return exploded;
        }

        public List<(int x, int y)> GetLastExplodedMines() => _lastExplodedMines;

        /*//判断是否爆破地雷(不包括引爆)
        public bool CheckExplosion(int px, int py)
        {
            // 遍历四个相邻格子
            int[] dx = { -1, 0 };
            int[] dy = { -1, 0 };

            foreach (int ix in dx)
            {
                foreach (int iy in dy)
                {
                    int gx = px + ix;
                    int gy = py + iy;
                    if (gx >= 0 && gx < 15 && gy >= 0 && gy < 15 && mines[gx, gy])
                    {
                        Console.WriteLine($"💥 玩家在({px},{py})引爆了({gx},{gy})的地雷！");
                        ClearAMine(gx,gy);
                        //ExplodeArea(px, py);
                        return true;
                    }
                }
            }
            return false;
        }
*/
        public void ClearAMine(int x,int y)
        {
            mines[x, y] = false;
            int BombRadius = 1;

            // 更新3x3区域的所有数字
            for (int dx = -BombRadius; dx <= BombRadius; dx++)
            {
                for (int dy = -BombRadius; dy <= BombRadius; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if(InBounds(nx, ny) && numbers[nx, ny] != 0) numbers[nx, ny]--;
                }
            }
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size;
        }

        public bool IsMine(int x, int y)
        {
            return InBounds(x, y) && mines[x, y];
        }
    }
}
