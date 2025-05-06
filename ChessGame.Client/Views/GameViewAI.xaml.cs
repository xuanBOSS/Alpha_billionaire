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

namespace ChessGame.Client.Views
{
    public partial class GameViewAI : Window
    {
        private SignalRService _signalRService;

        private const int BoardSize = 15;
        private const int spacing = 35;
        public GameViewAI()
        {
            InitializeComponent();
            DrawBoard();

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
