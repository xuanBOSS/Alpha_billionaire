using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using static ChessGame.Client.SignalRService;

namespace ChessGame.Client.Views
{
    public partial class MainWindow : Window
    {
        private SignalRService _signalRService;
        private string _matchedRoomId;
        private SignalRService.UserInfo _currentUser; // 添加当前用户信息字段
                                                      // 简化属性定义，避免复杂的getter/setter
        public string CurrentUserName => _currentUser?.UserName ?? "未登录用户";
        public string CurrentUserScore => _currentUser?.WinTimes.ToString() ?? "0";
        MatchRequest matchRequest = new MatchRequest();
        
        public MainWindow(SignalRService.UserInfo userInfo = null)
        {
            

            // 先设置数据，再初始化组件
            _signalRService = App.ServiceProvider.GetRequiredService<SignalRService>();
            
            _currentUser = userInfo ?? _signalRService.CurrentUser;

            if (_currentUser == null)
            {
                _currentUser = new SignalRService.UserInfo
                {
                    UserName = "未登录用户",
                    WinTimes = 0
                };
            }

            // 设置数据上下文必须在InitializeComponent之前
            this.DataContext = this;

            InitializeComponent();

            Debug.WriteLine($"MainWindow初始化 - 用户: {CurrentUserName}, 积分: {CurrentUserScore}");

            // 在构造函数结束前手动更新UI显示
            UserNameTextBlock.Text = CurrentUserName;
            ScoreTextBlock.Text = CurrentUserScore;

            // 注册房间匹配结果事件
            _signalRService.MatchResult((roomId, gameMessage, player1Info, player2Info) =>
            {
                // 在接收到消息时更新 UI
                if (gameMessage == "你是黑方")
                {
                    Application.Current.Dispatcher.Invoke(MatchSuccess);
                }
                else if (gameMessage == "你是白方")
                {
                    Application.Current.Dispatcher.Invoke(MatchSuccess_white);
                }
            });
        
        }

        //退出房间
        private async void ExitRoom()
        {
            await _signalRService.ExitAIGame();
        }

        private void OnUserInfoUpdated(SignalRService.UserInfo updatedUser)
        {
            Dispatcher.Invoke(() =>
            {
                _currentUser = updatedUser;
                UpdateUserInfoDisplay();
            });
        }

        private void OnMatchResult(string roomId, string gameMessage, SignalRService.PlayerInfo player1Info, SignalRService.PlayerInfo player2Info)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (gameMessage == "你是黑方") MatchSuccess();
                else if (gameMessage == "你是白方") MatchSuccess_white();
            });
        }

        private void UpdateUserInfoDisplay(bool forceUpdate = false)
        {
            // 三重保险刷新机制
            Dispatcher.InvokeAsync(() =>
            {
                // 1. 直接设置控件值
                UserNameTextBlock.Text = _currentUser?.UserName ?? "未登录";
                ScoreTextBlock.Text = _currentUser?.WinTimes.ToString() ?? "0";

                // 2. 强制重绘控件
                UserNameTextBlock.InvalidateVisual();
                ScoreTextBlock.InvalidateVisual();

                // 3. 更新布局树
                var parent = VisualTreeHelper.GetParent(UserNameTextBlock) as UIElement;
                while (parent != null)
                {
                    parent.InvalidateArrange();
                    parent.InvalidateMeasure();
                    parent.UpdateLayout();
                    parent = VisualTreeHelper.GetParent(parent) as UIElement;
                }

                // 调试输出实际值
                Debug.WriteLine($"暴力刷新后 - 昵称UI值: {UserNameTextBlock.Text}, 积分UI值: {ScoreTextBlock.Text}");
            }, DispatcherPriority.Render);
        }

        //选择人机模式
        private async void ManMachineMode_Click(object sender, RoutedEventArgs e)
        {
            ExitRoom();//如果房间管理系统中，有当前玩家房间的数据，先清除房间

            //小窗口设置在当前窗口中间
            matchRequest.Owner = this;

            //请求匹配的小窗口
            matchRequest.Show();

            //this.Close();

            AIChoose_show();

            await _signalRService.StartAIGame();


            /*GameViewAI GameViewAI = new GameViewAI();
            GameViewAI.Show();*/

            //this.Close();
        }

        //选择联机模式
        private async void OnlineMode_Click(object sender, RoutedEventArgs e)
        {
            //小窗口设置在当前窗口中间
            matchRequest.Owner = this;

            //请求匹配的小窗口
            matchRequest.Show();

            await _signalRService.TryMatchRoom();
        }

        private void RankingList_Click(object sender, RoutedEventArgs e)
        {
            RankingList RankingList = new RankingList();
            RankingList.Show();

            this.Close();
        }

        //显示匹配成功进入房间，且为黑方
        private async void MatchSuccess()
        {
            //关闭请求匹配的窗口
            matchRequest.Close();

            //显示匹配成功小窗口
            MatchSuccess matchSuccess_black = new MatchSuccess
            {
                Owner = this
            };
            matchSuccess_black.Show();

            //等待2秒
            await Task.Delay(2000);

            matchSuccess_black.Close();

            //显示联机游戏窗口
            GameView gameView = new GameView();
            gameView.Show();

            this.Close();
        }

        //显示匹配成功进入房间，且为白方
        private async void MatchSuccess_white()
        {
            //关闭请求匹配的窗口
            matchRequest.Close();

            //显示匹配成功小窗口
            WhiteMatch matchSuccess_white = new WhiteMatch
            {
                Owner = this
            };
            matchSuccess_white.Show();

            //等待2秒
            await Task.Delay(2000);

            matchSuccess_white.Close();

            //显示联机游戏窗口
            GameView gameView = new GameView();
            gameView.Show();

            this.Close();
        }

        public void AIChoose_show()
        {
            //关闭请求匹配的窗口
            matchRequest.Close();

            AIchoose aichoose = new AIchoose();

            //显示选择棋子颜色的窗口
            aichoose.Show();
            
            this.Close();
        }


        private void ForceUpdateBindings()
        {
            // 暴力刷新所有绑定
            UserNameTextBlock.GetBindingExpression(TextBlock.TextProperty)?.UpdateTarget();
            ScoreTextBlock.GetBindingExpression(TextBlock.TextProperty)?.UpdateTarget();

            // 备用方案：直接设置控件值
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UserNameTextBlock.Text = _currentUser?.UserName ?? "未登录";
                ScoreTextBlock.Text = _currentUser?.WinTimes.ToString() ?? "0";
            }), DispatcherPriority.Render);

            Debug.WriteLine($"控件实际值: 昵称={UserNameTextBlock.Text}, 积分={ScoreTextBlock.Text}");
        }
    }
}
