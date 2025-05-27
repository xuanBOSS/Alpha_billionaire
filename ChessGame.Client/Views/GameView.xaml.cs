using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ChessGame.GameLogic;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Media.Media3D;
using Microsoft.AspNetCore.SignalR.Client;
using static ChessGame.Client.SignalRService;

namespace ChessGame.Client.Views
{
    public partial class GameView : Window
    {
        private SignalRService _signalRService;
        private string _roomId;   // 当前游戏房间ID
        // private GameManager _gameManager;

        private const int BoardSize = 15;//棋盘线条数
        private const int spacing = 35;//棋盘格边长

        private MineMap _mineMap;
        private BombManager _bombManager;
        private Board _board;

        private SignalRService.PlayerInfo _currentPlayer;
        private SignalRService.PlayerInfo _opponentPlayer;


        private readonly ImageSource _explosionImage;//存储图片资源

        private Dictionary<(int, int), Rectangle> _coverTiles = new Dictionary<(int, int), Rectangle>();
        
        public GameView()
        {
            InitializeComponent();



            //加载爆炸图片
            _explosionImage = new BitmapImage(new Uri("pack://application:,,,/source/bomb.png"));

            // 通过类型名访问静态成员 ServiceProvider
            _signalRService = App.ServiceProvider.GetRequiredService<SignalRService>();

            //向后端发送玩家身份请求（黑棋或者白棋）
            GetIdentify();

            // 初始化地雷图，确保不为null
            _mineMap = new MineMap();

            //注册接收玩家信息的事件(黑棋或者白棋)
            _signalRService.ReceiveIdentify((blackName,bWinTimes,whiteName,wWinTimes) =>
            {
                // 在接收到消息时更新 UI
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // 直接更新UI
                        /*PlayerANicknameText.Text = player1Info?.UserName ?? "玩家1";
                        PlayerAScoreText.Text = $"积分: {player1Info?.WinTimes ?? 0}";

                        PlayerBNicknameText.Text = player2Info?.UserName ?? "玩家2";
                        PlayerBScoreText.Text = $"积分: {player2Info?.WinTimes ?? 0}";*/

                        PlayerANicknameText.Text = blackName;
                        PlayerAScoreText.Text = $"{bWinTimes}";

                        PlayerBNicknameText.Text = whiteName;
                        PlayerBScoreText.Text = $"{wWinTimes}";

                        Console.WriteLine($"UI已更新: {PlayerANicknameText.Text} vs {PlayerBNicknameText.Text}");

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"UI更新失败: {ex.Message}");
                    }
                });
            });

            // 注册胜率更新事件
            _signalRService.OnWinRateUpdate(HandleWinRateUpdate);


            //注册接收地图信息的事件
            _signalRService.GetMap((mineMap) => 
            {
                
                // 在接收到消息时更新 UI
                Application.Current.Dispatcher.Invoke(() =>
                {
                   
                    _mineMap = mineMap;

                    // 地图加载后请求初始胜率
                    RequestWinRateUpdate();
                    UpdateMineNumbers();

                });
            });

            //注册接收落子信息的事件
            _signalRService.ReceivePiece((result,x,y,c) =>
            {
                // 在接收到消息时更新 UI
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if(result)
                    {
                        Brush color = null;
                        if (c == 1) color = Brushes.Black;
                        else if(c == 2) color = Brushes.LightGray;

                        PlacePiece(x, y, color);

                        // 在这里调用MineisBomb并传递坐标参数
                        MineisBomb(x, y);
                    }
                });
            });

            



            //注册使游戏暂停的事件
            _signalRService.PauseGame((msg) =>
            {
               
                Application.Current.Dispatcher.Invoke(() =>
                {
                    //MessageBox.Show($"收到PauseGame消息: {msg}");

                    if (msg == "超出棋盘范围。")
                    {
                        Is_OutofRange(); // 正确调用方法
                    }
                    else if (msg == "禁手：黑棋此处落子违反规则。")
                    {
                        Is_illegalMove(); // 正确调用方法
                    }
                    else if (msg == "该位置已有棋子。")
                    {
                        Is_AlreadyhavePiece(); // 正确调用方法
                    }
                });
            });

            
            //注册使游戏结束的事件
            _signalRService.GameEnd((result) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    //MessageBox.Show($"收到GameEnd消息: {result}");

                    if (result == "胜利")
                    {
                        //Is_Win(); // 正确调用方法
                        Is_Lose();
                    }
                    else
                    {
                        //Is_Lose(); // 正确调用方法
                        Is_Win();
                    }
                });

            });
            
            // 处理联机模式匹配成功，更新对手信息
            _signalRService.MatchResult((roomId, message, player1Info, player2Info) => {
                Console.WriteLine($"GameView收到匹配成功: {roomId}");

                // 在UI线程上更新界面
                Dispatcher.Invoke(() => {
                    try
                    {
                        _roomId = roomId;

                        // 直接更新UI
                        PlayerANicknameText.Text = player1Info?.UserName ?? "玩家1";
                        PlayerAScoreText.Text = $"积分: {player1Info?.WinTimes ?? 0}";

                        PlayerBNicknameText.Text = player2Info?.UserName ?? "玩家2";
                        PlayerBScoreText.Text = $"积分: {player2Info?.WinTimes ?? 0}";

                        Console.WriteLine($"UI已更新: {PlayerANicknameText.Text} vs {PlayerBNicknameText.Text}");

                        // 显示匹配成功消息
                        //MessageBox.Show($"匹配成功!\n房间: {roomId}\n您执: {message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"UI更新失败: {ex.Message}");
                    }
                });
            });


            DrawBoard();//绘制棋盘

            // 显式调用 Loaded 事件处理程序          
            this.Loaded += GameView_Loaded;
        }

        // 处理胜率更新事件
        private void HandleWinRateUpdate(double blackRate, double whiteRate)
        {
            // 在UI线程中更新胜率显示
            Dispatcher.Invoke(() =>
            {
                UpdateWinRateDisplay((blackRate, whiteRate));
            });
        }

        // 在每次落子后请求更新胜率
        private async Task RequestWinRateUpdate()
        {
            try
            {
                await _signalRService.RequestWinRateCalculation();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"请求胜率更新失败: {ex.Message}");
            }
        }

        private async void GetIdentify()
        {
            await _signalRService.GetIdentify();
        }

        private void GameView_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("GameView_Loaded 事件触发");
            LoadPlayerData();
        }

        
        private void LoadPlayerData()
        {
            Console.WriteLine("执行 LoadPlayerData()");

            // 加载当前玩家 (Player A) 信息
            if (_signalRService.CurrentUser != null)
            {
                Console.WriteLine($"从CurrentUser加载玩家A信息: {_signalRService.CurrentUser.UserName}");
                PlayerANicknameText.Text = _signalRService.CurrentUser.UserName;
                PlayerAScoreText.Text = $"积分: {_signalRService.CurrentUser.WinTimes}";
            }
            else
            {
                Console.WriteLine("CurrentUser为空，显示默认值");
                PlayerANicknameText.Text = "玩家A";
                PlayerAScoreText.Text = "积分: -";
            }

            // 默认显示等待对手
            PlayerBNicknameText.Text = "等待对手...";
            PlayerBScoreText.Text = "积分: -";
            //// 在游戏开始前显示默认信息
            //PlayerANicknameText.Text = "等待匹配...";
            //PlayerAScoreText.Text = "积分: -";
            //PlayerBNicknameText.Text = "等待匹配...";
            //PlayerBScoreText.Text = "积分: -";
        }

        // 添加调试方法
        private void DebugMineMap()
        {
            if (_mineMap == null)
            {
                Console.WriteLine("地雷地图为空！");
                return;
            }

            Console.WriteLine("地雷地图状态：");
            for (int y = 0; y < BoardSize - 1; y++)
            {
                string line = "";
                for (int x = 0; x < BoardSize - 1; x++)
                {
                    if (_mineMap.IsMine(x, y))
                        line += "M ";
                    else
                        line += _mineMap.numbers[x, y] + " ";
                }
                Console.WriteLine(line);
            }
        }

        //绘制棋盘
        private void DrawBoard()
        {
            // 清除现有覆盖层
            _coverTiles.Clear();
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
            // 添加覆盖层
            for (int x = 0; x < BoardSize - 1; x++)
            {
                for (int y = 0; y < BoardSize - 1; y++)
                {
                    var cover = new Rectangle
                    {
                        Width = spacing,
                        Height = spacing,
                        Fill = new SolidColorBrush(Color.FromRgb(210, 180, 140)), //不透明
                        Stroke = Brushes.Wheat,//背景色
                        StrokeThickness = 0.5
                    };

                    Canvas.SetLeft(cover, x * spacing);
                    Canvas.SetTop(cover, y * spacing);

                    Panel.SetZIndex(cover, 10);

                    BoardCanvas.Children.Add(cover);
                    _coverTiles[(x, y)] = cover;

                    // 设置覆盖层在棋盘上层
                    Panel.SetZIndex(cover, 10);
                }
            }

        }

        //绘制数字提示，即周围地雷数
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

        //根据数字值返回不同颜色
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
        private void ClearAffectedPieces(int crossX, int crossY)
        {
            var points = new HashSet<(int, int)>();
            //内圈4点 + 外圈12点，3*3的9宫格
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int gx = crossX + dx, gy = crossY + dy;
                    ClearPieceAt(gx, gy);
                    /*if (_mineMap.InBounds(gx, gy))
                    {
                        points.Add((gx, gy));       //棋子左上格子
                       /points.Add((gx + 1, gy));   //棋子右上格子
                        points.Add((gx, gy + 1));   //棋子左下格子
                        points.Add((gx + 1, gy + 1)); //棋子右下格子
                    }*/
                }
            }
           /*foreach (var (x, y) in points)
            {
                ClearPieceAt(x, y);
            }*/

            //ClearPieceAt(crossX, crossY);
        }

        //更新棋盘上的地雷提示数字
        private void UpdateMineNumbers()
        {
            //先清除所有已显示的数字
            for (int i = BoardCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (BoardCanvas.Children[i] is TextBlock)
                {
                    BoardCanvas.Children.RemoveAt(i);
                }
            }

            //重新绘制所有已揭开的数字
            for (int x = 0; x < BoardSize - 1; x++)
            {
                for (int y = 0; y < BoardSize - 1; y++)
                {
                    if (!_coverTiles.ContainsKey((x, y)) && _mineMap.numbers[x, y] > 0)
                    {
                        var numText = new TextBlock
                        {
                            Text = _mineMap.numbers[x, y].ToString(),
                            FontSize = 16,
                            FontWeight = FontWeights.Bold,
                            Foreground = GetNumberColor(_mineMap.numbers[x, y]),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Width = spacing,
                            Height = spacing,
                            TextAlignment = TextAlignment.Center
                        };

                        double centerX = x * spacing + spacing / 2;
                        double centerY = y * spacing + spacing / 2;

                        Canvas.SetLeft(numText, centerX - numText.Width / 2);
                        Canvas.SetTop(numText, centerY - numText.Height / 2 + 8);
                        Panel.SetZIndex(numText, 5);
                        BoardCanvas.Children.Add(numText);
                    }
                }
            }
        }

        //显示爆炸效果
        private async Task ShowExplosionEffect(int x, int y)
        {
            // 计算地雷格位置
            double centerX = x * spacing + spacing / 2;
            double centerY = y * spacing + spacing / 2;

            // 创建爆炸图片控件
            var explosionImage = new Image
            {
                Source = _explosionImage,
                Width = spacing,//开始时一个格子大小
                Height = spacing,
                Stretch = Stretch.Uniform,
                RenderTransformOrigin = new Point(0.5, 0.5), //中心点缩放
                Opacity = 1.0
            };

            Canvas.SetLeft(explosionImage, x * spacing - spacing / 2);
            Canvas.SetTop(explosionImage, y * spacing - spacing / 2);
            Panel.SetZIndex(explosionImage, 100); //设置高ZIndex，图片在最上层

            //添加到画布
            BoardCanvas.Children.Add(explosionImage);

            //创建缩放动画（扩大到3x3格子大小）
            var scaleTransform = new ScaleTransform(1, 1);
            explosionImage.RenderTransform = scaleTransform;

            var scaleAnimation = new DoubleAnimation
            {
                From = 1,
                To = 2, //从1*1到3*3的大小
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

        }

        //鼠标尝试放置棋子
        private async void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //获取鼠标点击位置
            Point clickPoint = e.GetPosition(BoardCanvas);

            //计算最近交叉点
            int crossX = (int)System.Math.Round(clickPoint.X / spacing);
            int crossY = (int)System.Math.Round(clickPoint.Y / spacing);

            //调用 TryPlacePiece 方法
            await _signalRService.TryPlacePiece(crossX, crossY);

            // 请求更新胜率
            await RequestWinRateUpdate();

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
                Stroke = baseColor == Brushes.LightGray ? Brushes.LightGray : Brushes.Black,
                StrokeThickness = 0.5
            };

            //阴影效果
            if (baseColor == Brushes.LightGray)
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
                Fill = baseColor == Brushes.LightGray
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
            if (baseColor == Brushes.LightGray)
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

        //递归展开覆盖层
        private void RevealAdjacentSafeArea(int x, int y)
        {
            //检查坐标是否有效
            if (x < 0 || x >= BoardSize - 1 || y < 0 || y >= BoardSize - 1)
                return;

            //如果这个格子已经被揭开，则停止
            if (!_coverTiles.ContainsKey((x, y)))
                return;

            //揭开当前格子
            RemoveCover(x, y);

            //如果当前格子数字为0(周围无地雷)，则递归揭开相邻4个方向的格子
            if (_mineMap.numbers[x, y] == 0)
            {
                //4方向递归(上、下、左、右)
                RevealAdjacentSafeArea(x, y - 1); // 上
                RevealAdjacentSafeArea(x, y + 1); // 下
                RevealAdjacentSafeArea(x - 1, y); // 左
                RevealAdjacentSafeArea(x + 1, y); // 右
            }
        }

        //检查交叉点周围的4个格子是否有雷
        private bool HasAdjacentMines(int crossX, int crossY)
        {
            // 检查每个格子是否在范围内且有雷
            bool hasMine = false;

            // 左上格子
            if (crossX > 0 && crossY > 0 && _mineMap.IsMine(crossX - 1, crossY - 1))
                hasMine = true;

            // 右上格子
            if (crossX < BoardSize - 1 && crossY > 0 && _mineMap.IsMine(crossX, crossY - 1))
                hasMine = true;

            // 左下格子
            if (crossX > 0 && crossY < BoardSize - 1 && _mineMap.IsMine(crossX - 1, crossY))
                hasMine = true;

            // 右下格子
            if (crossX < BoardSize - 1 && crossY < BoardSize - 1 && _mineMap.IsMine(crossX, crossY))
                hasMine = true;

            return hasMine;
        }

        //揭开交叉点周围的3x3区域
        private void Reveal4Area(int crossX, int crossY)
        {
            // 定义四个方向：上、下、左、右
            int[,] directions = new int[4, 2]
            {
                { -1, -1 },   { -1, 0 },  { 0, -1 }, { 0, 0 }
            };

            for (int i = 0; i < 4; i++)
            {
                int revealX = crossX + directions[i, 0];
                int revealY = crossY + directions[i, 1];

                // 检查是否在棋盘范围内
                if (revealX >= 0 && revealX < BoardSize &&
                    revealY >= 0 && revealY < BoardSize)
                {
                    RemoveCover(revealX, revealY); // 移除覆盖层
                }
            }
        }

        //揭开覆盖层
        private void RemoveCover(int x, int y)
        {
            if (_coverTiles.TryGetValue((x, y), out var cover))
            {
                //先移除该位置可能存在的旧数字
                ClearNumberAt(x, y);

                BoardCanvas.Children.Remove(cover);
                _coverTiles.Remove((x, y));

                //如果这个格子有数字，显示它
                if (_mineMap.numbers[x, y] > 0)
                {
                    var numText = new TextBlock
                    {
                        Text = _mineMap.numbers[x, y].ToString(),
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        Foreground = GetNumberColor(_mineMap.numbers[x, y]),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Width = spacing,
                        Height = spacing,
                        TextAlignment = TextAlignment.Center
                    };

                    double centerX = x * spacing + spacing / 2;
                    double centerY = y * spacing + spacing / 2;

                    Canvas.SetLeft(numText, centerX - numText.Width / 2);
                    Canvas.SetTop(numText, centerY - numText.Height / 2 + 8);

                    //确保数字在正确层级
                    Panel.SetZIndex(numText, 5);
                    BoardCanvas.Children.Add(numText);
                }
            }
        }

        //添加清除数字的方法
        private void ClearNumberAt(int x, int y)
        {
            for (int i = BoardCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (BoardCanvas.Children[i] is TextBlock textBlock)
                {
                    double left = Canvas.GetLeft(textBlock);
                    double top = Canvas.GetTop(textBlock);
                    int tx = (int)((left + spacing / 2) / spacing);
                    int ty = (int)((top + spacing / 2 - 8) / spacing);

                    if (tx == x && ty == y)
                    {
                        BoardCanvas.Children.RemoveAt(i);
                    }
                }
            }
        }

        //点击退出游戏按钮，弹出确认小窗口
        private void Return_Click(object sender, RoutedEventArgs e)
        {
            ShowReturnTest();
        }

        //确认是否确认退出游戏的小窗口，点击确认退出则回到主界面
        private void ShowReturnTest()
        {
            ReturnTest ReturnTest = new ReturnTest
            {
                Owner = this //设置所有者窗口以确保对话框显示在主窗口中央
            };

            ReturnTest.ShowDialog(); //使用ShowDialog以模态方式显示

            if (ReturnTest.DialogResult == true)
            {
                // 用户点击确定
               
                this.Close(); // 关闭当前窗口
            }
            // 用户点击取消或关闭，不做任何操作
        }

        //禁手点
        private void Is_illegalMove()
        {
            illegalMove illegalMovet = new illegalMove
            {
                Owner = this //设置所有者窗口主窗口中央
            };
            illegalMovet.ShowDialog();
        }

        //该位置已经有棋子
        private void Is_AlreadyhavePiece()
        {
            AlreadyhavePiece AlreadyhavePiece = new AlreadyhavePiece
            {
                Owner = this //设置所有者窗口主窗口中央
            };
            AlreadyhavePiece.ShowDialog();
        }

        //超出棋盘范围
        private void Is_OutofRange()
        {
            OutofRange OutofRange = new OutofRange
            {
                Owner = this //设置所有者窗口主窗口中央
            };
            OutofRange.ShowDialog();
        }

        //游戏已经结束
        private void Is_GameOver()
        {
            GameisOver GameisOver = new GameisOver
            {
                Owner = this //设置所有者窗口主窗口中央
            };
        }

        //地雷爆炸
        private async void MineisBomb(int crossX, int crossY)
        {
            
            //MessageBox.Show($"MineisBomb被调用：位置({crossX}, {crossY})");

            if (_mineMap == null)
            {
                //MessageBox.Show("错误：_mineMap为null");
                return;
            }

            try
            {
                //检查周围4个格子是否有雷
                bool hasAdjacentMines = HasAdjacentMines(crossX, crossY);
                //MessageBox.Show($"周围是否有雷: {hasAdjacentMines}");

                if (hasAdjacentMines)
                {
                    //有雷，揭开3x3区域
                    Reveal4Area(crossX, crossY);
                }
                else
                {
                    //无雷，递归展开
                    //从周围4个格子开始展开
                    if (crossX > 0 && crossY > 0) RevealAdjacentSafeArea(crossX - 1, crossY - 1);
                    if (crossX < BoardSize - 1 && crossY > 0) RevealAdjacentSafeArea(crossX, crossY - 1);
                    if (crossX > 0 && crossY < BoardSize - 1) RevealAdjacentSafeArea(crossX - 1, crossY);
                    if (crossX < BoardSize - 1 && crossY < BoardSize - 1) RevealAdjacentSafeArea(crossX, crossY);
                }

                //检查是否引爆地雷
                bool isExploded = _mineMap.CheckExplosion(crossX, crossY);
                //MessageBox.Show($"是否引爆地雷: {isExploded}");

                if (isExploded)
                {
                    var explodedMines = _mineMap.GetLastExplodedMines();
                    //MessageBox.Show($"引爆的地雷数量: {explodedMines.Count()}");
                    ClearAffectedPieces(crossX, crossY);
                    await ShowExplosionEffect(crossX, crossY);

                    foreach (var (x, y) in explodedMines)
                    {
                        //await ShowExplosionEffect(x, y);
                        /*ClearAffectedPieces(x, y);*/

                        /*更新3x3区域的数字显示
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                int revealX = x + dx;
                                int revealY = y + dy;
                                if (revealX >= 0 && revealX < BoardSize - 1 &&
                                    revealY >= 0 && revealY < BoardSize - 1)
                                {
                                    RemoveCover(revealX, revealY);
                                }
                            }
                        }*/
                    }

                    //更新所有数字显示
                    UpdateMineNumbers();
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"MineisBomb执行出错：{ex.Message}");
            }
        }

        //放置棋子
        private void PlacePiece(int crossX, int crossY, Brush PieceColor)
        {
            //创建棋子
            var piece = CreateRealisticPiece(PieceColor);

            //放置棋子
            Canvas.SetLeft(piece, crossX * spacing - (spacing - 10) / 2);
            Canvas.SetTop(piece, crossY * spacing - (spacing - 10) / 2);

            // 确保棋子在正确的层级显示
            Panel.SetZIndex(piece, 50);

            BoardCanvas.Children.Add(piece);
        }

        //如果游戏胜利
        private void Is_Win()
        {
            Win Win = new Win
            {
                Owner = this //设置所有者窗口主窗口中央
            };

            Win.Show(); 
        }

        //如果游戏失败
        private void Is_Lose()
        {
            Lose Lose = new Lose
            {
                Owner = this //设置所有者窗口主窗口中央
            };

            Lose.Show(); 
        }

        private void WhiteWinProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void BlackWinProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }
    }
}
