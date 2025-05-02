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
                    /*Console.Out.Flush();  // 强制刷新输出*/
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
                    /*Console.Out.Flush();  // 强制刷新输出*/
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
                Console.Out.Flush();  // 强制刷新输出*/
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
