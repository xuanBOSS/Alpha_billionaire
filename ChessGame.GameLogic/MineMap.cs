using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace ChessGame.GameLogic
{
    [Serializable]
    public class MineMap
    {
        public static readonly int Size = 14;//地图尺寸
        [JsonIgnore] // 忽略原始二维数组
        public bool[,] mines;

        [JsonIgnore] // 忽略原始二维数组
        public int[,] numbers;

        // 用于序列化的一维数组属性
        [JsonPropertyName("mines")]
        public bool[] SerializedMines
        {
            get => mines != null ? Flatten2DArrayBool(mines) : new bool[Size * Size];
            set => mines = value != null ? Unflatten2DArrayBool(value) : new bool[Size, Size];
        }

        [JsonPropertyName("numbers")]
        public int[] SerializedNumbers
        {
            get => numbers != null ? Flatten2DArrayInt(numbers) : new int[Size * Size];
            set => numbers = value != null ? Unflatten2DArrayInt(value) : new int[Size, Size];
        }

        // 辅助方法：将二维布尔数组转换为一维
        private bool[] Flatten2DArrayBool(bool[,] array)
        {
            var result = new bool[Size * Size];
            for (int i = 0; i < Size; i++)
                for (int j = 0; j < Size; j++)
                    result[i * Size + j] = array[i, j];
            return result;
        }

        // 辅助方法：将一维布尔数组转换为二维
        private bool[,] Unflatten2DArrayBool(bool[] array)
        {
            var result = new bool[Size, Size];
            for (int i = 0; i < Size; i++)
                for (int j = 0; j < Size; j++)
                    result[i, j] = array[i * Size + j];
            return result;
        }

        // 辅助方法：将二维整数数组转换为一维
        private int[] Flatten2DArrayInt(int[,] array)
        {
            var result = new int[Size * Size];
            for (int i = 0; i < Size; i++)
                for (int j = 0; j < Size; j++)
                    result[i * Size + j] = array[i, j];
            return result;
        }

        // 辅助方法：将一维整数数组转换为二维
        private int[,] Unflatten2DArrayInt(int[] array)
        {
            var result = new int[Size, Size];
            for (int i = 0; i < Size; i++)
                for (int j = 0; j < Size; j++)
                    result[i, j] = array[i * Size + j];
            return result;
        }

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

        

        // 递归检查并爆炸周围的地雷
        private void CheckAndExplodeAdjacentMines(int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (InBounds(nx, ny) && mines[nx, ny])
                    {
                        // 清除相邻地雷
                        mines[nx, ny] = false;

                        

                        // 递归检查这个地雷周围的其他地雷
                        CheckAndExplodeAdjacentMines(nx, ny);
                    }
                }
            }
        }

        // 更新所有受影响区域的数字提示
        private void UpdateAllAffectedNumbers()
        {
            for (int x = 0; x < mines.GetLength(0); x++)
            {
                for (int y = 0; y < mines.GetLength(1); y++)
                {
                    if (!mines[x, y])
                    {
                        numbers[x, y] = CalculateAdjacentMines(x, y);
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

        
        public void ClearAMine(int x,int y)
        {
            mines[x, y] = false;
            int BombRadius = 1;

            // 递归检查周围的地雷并清除
            CheckAndExplodeAdjacentMines(x, y);

            // 更新所有受影响区域的数字提示
            UpdateAllAffectedNumbers();

            // 更新3x3区域的所有数字
            /*for (int dx = -BombRadius; dx <= BombRadius; dx++)
            {
                for (int dy = -BombRadius; dy <= BombRadius; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if(InBounds(nx, ny) && numbers[nx, ny] != 0) numbers[nx, ny]--;
                }
            }*/
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size;
        }

        public bool IsMine(int x, int y)
        {
            return InBounds(x, y) && mines[x, y];
        }

        // 添加一个方法来获取用于传输的数据
        public MineMapDTO GetTransferData()
        {
            return new MineMapDTO
            {
                Mines = SerializedMines,
                Numbers = SerializedNumbers,
                Size = Size
            };
        }
    }
    // 添加一个专门用于传输的DTO类
    public class MineMapDTO
    {
        public bool[] Mines { get; set; }
        public int[] Numbers { get; set; }
        public int Size { get; set; }
    }
}
