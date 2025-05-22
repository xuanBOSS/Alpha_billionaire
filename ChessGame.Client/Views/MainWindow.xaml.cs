using System;
using System.Collections.Generic;
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
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace ChessGame.Client.Views
{
    public partial class MainWindow : Window
    {
        private SignalRService _signalRService;

        MatchRequest matchRequest = new MatchRequest();
        public MainWindow()
        {
            InitializeComponent();

            _signalRService = App.ServiceProvider.GetRequiredService<SignalRService>();

            // 注册房间匹配结果事件
            _signalRService.MatchResult((roomId, gameMessage) =>
            {
                // 在接收到消息时更新 UI
                Application.Current.Dispatcher.Invoke(MatchSuccess);
            });
        }

        //选择人机模式
        private void ManMachineMode_Click(object sender, RoutedEventArgs e)
        {
            GameViewAI GameViewAI = new GameViewAI();
            GameViewAI.Show();

            this.Close();
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

        //通信测试
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            testWindow testwindow = new testWindow();
            testwindow.Show();

            this.Close();
        }

        //显示匹配成功进入房间，且为黑方
        private async void MatchSuccess()
        {
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
    }
}
