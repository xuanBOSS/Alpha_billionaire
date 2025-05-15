using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ChessGame.GameLogic;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;

namespace ChessGame.Client.Views
{
    public partial class GameView : Window
    {
        private SignalRService _signalRService;

        private GameManager _gameManager;

        private const int BoardSize = 15;//棋盘线条数
        private const int spacing = 35;//棋盘格边长

        private MineMap _mineMap;

        private readonly ImageSource _explosionImage;//存储图片资源
        public GameView()
        {
            InitializeComponent();

            //加载爆炸图片
            _explosionImage = new BitmapImage(new Uri("pack://application:,,,/source/bomb.png"));

            //初始化游戏管理器
            _gameManager = new GameManager();

            //初始化地雷地图
            _mineMap = new MineMap();
            _mineMap.PlaceMinesByDensity(0.08); //8%的地雷密度
            _mineMap.CalculateNumbers();//计算地雷数字
            _mineMap.PrintDebugBoard(); //调试输出

            DrawBoard();//绘制棋盘
            DrawMineNumbers(); //绘制数字提示

            _signalRService = new SignalRService();
        }

        //绘制棋盘
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
                    Stroke = Brushes.BurlyWood,
                    StrokeThickness = 1
                };
                BoardCanvas.Children.Add(horizontalLine);//添加到棋盘中

                // 绘制竖线
                Line verticalLine = new Line
                {
                    X1 = i * spacing,
                    Y1 = 0,
                    X2 = i * spacing,
                    Y2 = (BoardSize - 1) * spacing,
                    Stroke = Brushes.BurlyWood,
                    StrokeThickness = 1
                };
                BoardCanvas.Children.Add(verticalLine);
            }
        }

        // 绘制数字提示，即周围地雷数
        private void DrawMineNumbers()
        {
            for (int x = 0; x < BoardSize - 1; x++)
            {
                for (int y = 0; y < BoardSize - 1; y++)
                {
                    int number = _mineMap.numbers[x, y];
                    if (number > 0) //只显示有数字的格子
                    {
                        //创建数字文本
                        var numText = new TextBlock
                        {
                            Text = number.ToString(),
                            FontSize = 16,
                            FontWeight = FontWeights.Bold,
                            Foreground = GetNumberColor(number),//根据数字大小获取相应颜色
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Width = spacing,  //设置宽度与格子相同
                            Height = spacing, //设置高度与格子相同
                            TextAlignment = TextAlignment.Center
                        };

                        //计算格子中心位置
                        double centerX = x * spacing + spacing / 2;
                        double centerY = y * spacing + spacing / 2;

                        //将数字放置在格子中心
                        Canvas.SetLeft(numText, centerX - numText.Width / 2);
                        Canvas.SetTop(numText, centerY - numText.Height / 2 + 8);

                        BoardCanvas.Children.Add(numText);//添加到棋盘中
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

        //移除棋盘上的棋子
        private void ClearPieceAt(int x, int y)
        {
            for (int i = BoardCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (BoardCanvas.Children[i] is Grid grid)
                {
                    double left = Canvas.GetLeft(grid);
                    double top = Canvas.GetTop(grid);
                    int px = (int)(left + spacing / 2) / spacing;
                    int py = (int)(top + spacing / 2) / spacing;

                    if (px == x && py == y)
                    {
                        BoardCanvas.Children.RemoveAt(i);//移除指定位置的棋子
                    }
                }
            }
        }

        //地雷爆炸后清除棋子
        private void ClearAffectedPieces(int mineX, int mineY)
        {
            var points = new HashSet<(int, int)>();
            //内圈4点 + 外圈12点，3*3的9宫格
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int gx = mineX + dx, gy = mineY + dy;
                    if (_mineMap.InBounds(gx, gy))
                    {
                        points.Add((gx, gy));       //棋子左上格子
                        points.Add((gx + 1, gy));   //棋子右上格子
                        points.Add((gx, gy + 1));   //棋子左下格子
                        points.Add((gx + 1, gy + 1)); //棋子右下格子
                    }
                }
            }
            foreach (var (x, y) in points)
            {
                ClearPieceAt(x, y);
            }
        }

        //更新棋盘上的地雷提示数字
        private void UpdateMineNumbers()
        {
            // 移除旧数字
            for (int i = BoardCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (BoardCanvas.Children[i] is TextBlock)
                    BoardCanvas.Children.RemoveAt(i);
            }
            // 重新绘制数字
            DrawMineNumbers();
        }

        //显示爆炸效果
        private async Task ShowExplosionEffect(int x, int y)
        {
            // 创建爆炸小图标
            var explosionIcon = new TextBlock
            {
                Text = "💥",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.DarkGoldenrod,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = spacing,
                Height = spacing,
                TextAlignment = TextAlignment.Center
            };

            // 计算地雷格位置
            double centerX = x * spacing + spacing / 2;
            double centerY = y * spacing + spacing / 2;

            Canvas.SetLeft(explosionIcon, centerX - explosionIcon.Width / 2);
            Canvas.SetTop(explosionIcon, centerY - explosionIcon.Height / 2);

            // 添加到画布
            BoardCanvas.Children.Add(explosionIcon);

            // 创建爆炸图片控件
            var explosionImage = new Image
            {
                Source = _explosionImage,
                Width = spacing,//开始时一个格子大小
                Height = spacing,
                Stretch = Stretch.Uniform,
                RenderTransformOrigin = new Point(0.5, 0.5), // 中心点缩放
                Opacity = 1.0
            };

            Canvas.SetLeft(explosionImage, centerX - explosionImage.Width / 2);
            Canvas.SetTop(explosionImage, centerY - explosionImage.Height / 2);

            //添加到画布
            BoardCanvas.Children.Add(explosionImage);

            //创建缩放动画（扩大到3x3格子大小）
            var scaleTransform = new ScaleTransform(1, 1);
            explosionImage.RenderTransform = scaleTransform;

            var scaleAnimation = new DoubleAnimation
            {
                From = 1,
                To = 3, //从1*1到3*3的大小
                Duration = TimeSpan.FromMilliseconds(800),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            //创建透明度动画（2秒淡出消失）
            var opacityAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                BeginTime = TimeSpan.FromMilliseconds(800), //缩放完成后开始
                Duration = TimeSpan.FromSeconds(2),
                FillBehavior = FillBehavior.Stop
            };

            //动画完成后移除图片
            opacityAnimation.Completed += (s, e) =>
            {
                BoardCanvas.Children.Remove(explosionImage);
            };

            //启动动画
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            explosionImage.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);

            //等待2秒
            await Task.Delay(2000);

            //移除地雷小图标
            BoardCanvas.Children.Remove(explosionIcon);
        }

        //放置棋子
        private async void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //获取鼠标点击位置
            Point clickPoint = e.GetPosition(BoardCanvas);

            //计算最近交叉点
            int crossX = (int)System.Math.Round(clickPoint.X / spacing);
            int crossy = (int)System.Math.Round(clickPoint.Y / spacing);

            if (crossX >= 0 && crossX < BoardSize && crossy >= 0 && crossy < BoardSize)
            {
                /*Ellipse blackPiece = new Ellipse
                {
                    Width = spacing - 10,
                    Height = spacing - 10,
                    Fill = Brushes.Black
                };
                */

                //创建棋子
                var piece = CreateRealisticPiece(Brushes.LightGray); //黑棋

                //调用 TryPlacePiece 方法
                await _signalRService.TryPlacePiece(crossX, crossy);

                //放置棋子
                Canvas.SetLeft(piece, crossX * spacing - (spacing - 10) / 2);
                Canvas.SetTop(piece, crossy * spacing - (spacing - 10) / 2);
                BoardCanvas.Children.Add(piece);

                //检查是否引爆地雷
                bool isExploded = _mineMap.CheckExplosion(crossX, crossy);
                if (isExploded)
                {
                    //获取被引爆的地雷位置
                    var explodedMines = _mineMap.GetLastExplodedMines();
                    foreach (var (x, y) in explodedMines)
                    {
                        //显示地雷图标
                        await ShowExplosionEffect(x, y);

                        //清除受影响的棋子
                        ClearAffectedPieces(x, y);

                        //更新数字显示
                        UpdateMineNumbers();
                    }
                }
            }
        }
        //创建棋子
        private FrameworkElement CreateRealisticPiece(Brush baseColor)
        {
            //主容器
            var container = new Grid
            {
                Width = spacing - 10,
                Height = spacing - 10
            };

            //棋子主体
            var pieceBody = new Ellipse
            {
                Width = spacing - 10,
                Height = spacing - 10,
                Fill = baseColor,
                Stroke = baseColor == Brushes.White ? Brushes.Gray : Brushes.Transparent,
                StrokeThickness = 0.5
            };

            //阴影效果
            if (baseColor == Brushes.White)
            {
                //白棋
                pieceBody.Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 320,
                    ShadowDepth = 3,
                    Opacity = 0.4,
                    BlurRadius = 8,
                    RenderingBias = RenderingBias.Quality
                };

                //添加额外的内阴影效果
                var innerShadow = new Ellipse
                {
                    Width = spacing - 12,
                    Height = spacing - 12,
                    Stroke = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                    StrokeThickness = 4,
                    Fill = Brushes.Transparent
                };
                Canvas.SetLeft(innerShadow, 1);
                Canvas.SetTop(innerShadow, 1);
                container.Children.Add(innerShadow);
            }
            else
            {
                //黑棋
                pieceBody.Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 320,
                    ShadowDepth = 4,
                    Opacity = 0.6,
                    BlurRadius = 8
                };
            }

            //光泽效果
            var highlight = new Ellipse
            {
                Width = (spacing - 10) * 0.6,
                Height = (spacing - 10) * 0.3,
                Fill = baseColor == Brushes.White
                    ? new LinearGradientBrush(
                        new GradientStopCollection
                        {
                    new GradientStop(Color.FromArgb(90, 255, 255, 255), 0),
                    new GradientStop(Color.FromArgb(20, 255, 255, 255), 0.8),
                    new GradientStop(Color.FromArgb(0, 255, 255, 255), 1)
                        },
                        new Point(0.5, 0),
                        new Point(0.5, 1))
                    : new LinearGradientBrush(
                        new GradientStopCollection
                        {
                    new GradientStop(Color.FromArgb(120, 255, 255, 255), 0),
                    new GradientStop(Color.FromArgb(0, 255, 255, 255), 1)
                        },
                        new Point(0.5, 0),
                        new Point(0.5, 1))
            };

            //边缘高光（仅白棋）
            if (baseColor == Brushes.White)
            {
                var edgeGlow = new Ellipse
                {
                    Width = spacing - 11,
                    Height = spacing - 11,
                    Stroke = new LinearGradientBrush(
                        new GradientStopCollection
                        {
                    new GradientStop(Color.FromArgb(60, 255, 255, 255), 0.2),
                    new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.8)
                        },
                        new Point(0, 0),
                        new Point(1, 1)),
                    StrokeThickness = 1.2,
                    Fill = Brushes.Transparent
                };

                Canvas.SetLeft(edgeGlow, 0.5);
                Canvas.SetTop(edgeGlow, 0.5);
                container.Children.Add(edgeGlow);
            }

            //设置光泽位置
            Canvas.SetLeft(highlight, (spacing - 10) * 0.2);
            Canvas.SetTop(highlight, (spacing - 10) * 0.1);

            //添加到容器
            container.Children.Add(pieceBody);
            container.Children.Add(highlight);

            return container;
        }

        //胜率进度条动画
        private void UpdateWinRateDisplay((double black, double white) probabilities)
        {
            //清除现有动画
            BlackWinProgress.BeginAnimation(ProgressBar.ValueProperty, null);
            WhiteWinProgress.BeginAnimation(ProgressBar.ValueProperty, null);

            double blackWinProb = Math.Max(0, Math.Min(1, probabilities.black));
            double whiteWinProb = Math.Max(0, Math.Min(1, probabilities.white));

            //创建动画，添加缓动函数使动画更平滑
            var blackAnimation = new DoubleAnimation(
                blackWinProb,
                new Duration(TimeSpan.FromMilliseconds(500)))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            var whiteAnimation = new DoubleAnimation(
                whiteWinProb,
                new Duration(TimeSpan.FromMilliseconds(500)))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            //应用动画
            BlackWinProgress.BeginAnimation(ProgressBar.ValueProperty, blackAnimation);
            WhiteWinProgress.BeginAnimation(ProgressBar.ValueProperty, whiteAnimation);

            //更新文本
            BlackWinText.Text = $"{blackWinProb:P0}";
            WhiteWinText.Text = $"{whiteWinProb:P0}";
        }

        //无参数版本
        private void UpdateWinRateDisplay()
        {
            if (_gameManager == null) return;
            UpdateWinRateDisplay(_gameManager.GetWinProbabilities());
        }

        //测试进度条是否能正常显示
        private void TestWinRate_Click(object sender, RoutedEventArgs e)
        {
            //随机生成测试数据
            Random rand = new Random();
            double blackProb = rand.NextDouble();
            double whiteProb = 1 - blackProb;

            var testProbabilities = (black: blackProb, white: whiteProb);
            UpdateWinRateDisplay(testProbabilities);
        }
    }

}
