using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
using ChessGame.Client.Models;
using static ChessGame.Client.SignalRService;
namespace ChessGame.Client.Views
{
    /// <summary>
    /// RankingList.xaml 的交互逻辑
    /// </summary>
    public partial class RankingList : Window
    {
        private readonly SignalRService _signalRService;
        private LeaderboardViewModel _viewModel;

        public RankingList()
        {
            InitializeComponent();

            //// 模拟数据 - 以后可以替换为从数据库或Excel加载
            //var players = new List<Player>
            //{
            //    new Player { Name = "棋王", Score = 1250 },
            //    new Player { Name = "风清扬", Score = 1180 },
            //    new Player { Name = "棋圣", Score = 1120 },
            //    new Player { Name = "黑白子", Score = 1050 },
            //    new Player { Name = "围棋少年", Score = 980 },
            //    new Player { Name = "五子棋大师", Score = 920 },
            //    new Player { Name = "棋魂", Score = 870 },
            //    new Player { Name = "落子无悔", Score = 810 },
            //    new Player { Name = "棋乐无穷", Score = 760 },
            //    new Player { Name = "新手玩家", Score = 700 },
            //    new Player { Name = "棋逢对手", Score = 650 },
            //    new Player { Name = "观棋不语", Score = 600 },
            //    new Player { Name = "棋高一着", Score = 550 },
            //    new Player { Name = "弈秋", Score = 500 }
            //};

            //LeaderboardList.ItemsSource = players;
            _signalRService = SignalRService.Instance;
            _viewModel = new LeaderboardViewModel();
            DataContext = _viewModel;

            // 注册排行榜更新事件
            _signalRService.RegisterLeaderboardUpdatedEvent(OnLeaderboardUpdated);
            // 加载排行榜数据
            LoadLeaderboardData();
        }
        // 添加排行榜更新处理方法
        private void OnLeaderboardUpdated(List<LeaderboardEntry> leaderboard)
        {
            // 在UI线程上更新
            Dispatcher.Invoke(() =>
            {
                // 清空现有数据
                _viewModel.Players.Clear();

                // 填充新数据
                foreach (var entry in leaderboard)
                {
                    _viewModel.Players.Add(new PlayerRankData
                    {
                        Name = entry.UserName,
                        Score = entry.WinTimes,
                        Rank = entry.Rank
                    });
                }
            });
        }
        private async void LoadLeaderboardData()
        {
            try
            {
                // 显示加载提示或动画(可选)

                // 获取排行榜数据
                var leaderboard = await _signalRService.GetLeaderboardAsync();

                // 清空现有数据
                _viewModel.Players.Clear();

                // 填充数据
                foreach (var entry in leaderboard)
                {
                    _viewModel.Players.Add(new PlayerRankData
                    {
                        Name = entry.UserName,
                        Score = entry.WinTimes,
                        Rank = entry.Rank
                    });
                }

                // 如果没有数据，显示提示
                if (_viewModel.Players.Count == 0)
                {
                    MessageBox.Show("暂无排行榜数据", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    /*NoRankingList noRankingList = new NoRankingList
                    {
                        Owner = this
                    };
                    noRankingList.Show();*/
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载排行榜数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 隐藏加载提示或动画(可选)
            }
        }

        private void Return_Click(object sender, RoutedEventArgs e)
        {
            MainWindow MainWindow = new MainWindow();
            MainWindow.Show();

            this.Close();
        }
    }

    public class Player
    {
        public string Name { get; set; }
        public int Score { get; set; }
    }
}
