using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ChessGame.GameLogic;

namespace ChessGame.Client.Views
{
    public partial class GameViewAI : Window
    {
        private SignalRService _signalRService;

        private const int BoardSize = 15;
        private const int spacing = 35;

        private MineMap _mineMap;
        public GameViewAI()
        {
            InitializeComponent();

            // 初始化地雷地图
            _mineMap = new MineMap();
            _mineMap.PlaceMinesByDensity(0.08); // 8%的地雷密度
            _mineMap.CalculateNumbers();
            _mineMap.PrintDebugBoard(); // 调试输出

            DrawBoard();
            DrawMineNumbers(); // 绘制数字提示

            _signalRService = new SignalRService();
        }

        private void DrawBoard()
        {
            for (int i = 0; i < BoardSize; i++)
            {
                // 绘制横线
                Line horizontalLine = new Line
                {
                    X1 = 0,
                    Y1 = i * spacing,
                    X2 = (BoardSize - 1) * spacing,
                    Y2 = i * spacing,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                BoardCanvas.Children.Add(horizontalLine);

                // 绘制竖线
                Line verticalLine = new Line
                {
                    X1 = i * spacing,
                    Y1 = 0,
                    X2 = i * spacing,
                    Y2 = (BoardSize - 1) * spacing,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                BoardCanvas.Children.Add(verticalLine);
            }
        }


        // 绘制数字提示
        private void DrawMineNumbers()
        {
            for (int x = 0; x < BoardSize - 1; x++)
            {
                for (int y = 0; y < BoardSize - 1; y++)
                {
                    int number = _mineMap.numbers[x, y];
                    if (number > 0) // 只显示有数字的格子
                    {
                        // 创建数字文本
                        var numText = new TextBlock
                        {
                            Text = number.ToString(),
                            FontSize = 16,
                            FontWeight = FontWeights.Bold,
                            Foreground = GetNumberColor(number),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Width = spacing,  // 设置宽度与格子相同
                            Height = spacing, // 设置高度与格子相同
                            TextAlignment = TextAlignment.Center
                        };

                        // 计算格子中心位置
                        double centerX = x * spacing + spacing / 2;
                        double centerY = y * spacing + spacing / 2;

                        // 将数字放置在格子中心
                        Canvas.SetLeft(numText, centerX - numText.Width / 2);
                        Canvas.SetTop(numText, centerY - numText.Height / 2 + 8);

                        BoardCanvas.Children.Add(numText);
                    }
                }
            }
        }

        // 根据数字值返回不同颜色
        private Brush GetNumberColor(int number)
        {
            switch (number)
            {
                case 1: return Brushes.Blue;
                case 2: return Brushes.Green;
                case 3: return Brushes.Red;
                case 4: return Brushes.DarkBlue;
                case 5: return Brushes.DarkRed;
                case 6: return Brushes.Teal;
                case 7: return Brushes.Black;
                case 8: return Brushes.Gray;
                default: return Brushes.Black;
            }
        }

        //放置棋子，获取鼠标点击位置，计算最近交叉点，放置棋子
        private async void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point clickPoint = e.GetPosition(BoardCanvas);
            int x = (int)System.Math.Round(clickPoint.X / spacing);
            int y = (int)System.Math.Round(clickPoint.Y / spacing);

            if (x >= 0 && x < BoardSize && y >= 0 && y < BoardSize)
            {
                Ellipse blackPiece = new Ellipse
                {
                    Width = spacing - 2,
                    Height = spacing - 2,
                    Fill = Brushes.Black
                };

                x = x * spacing - (spacing - 2) / 2;
                y = y * spacing - (spacing - 2) / 2;

                // 调用 TryPlacePiece 方法
                await _signalRService.TryPlacePiece(x, y);

                Canvas.SetLeft(blackPiece, x);
                Canvas.SetTop(blackPiece, y);
                BoardCanvas.Children.Add(blackPiece);
            }
        }
    }
}
