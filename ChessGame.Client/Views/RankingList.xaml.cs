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

namespace ChessGame.Client.Views
{
    /// <summary>
    /// RankingList.xaml 的交互逻辑
    /// </summary>
    public partial class RankingList : Window
    {
        public RankingList()
        {
            InitializeComponent();

            // 模拟数据 - 以后可以替换为从数据库或Excel加载
            var players = new List<Player>
            {
                new Player { Name = "棋王", Score = 1250 },
                new Player { Name = "风清扬", Score = 1180 },
                new Player { Name = "棋圣", Score = 1120 },
                new Player { Name = "黑白子", Score = 1050 },
                new Player { Name = "围棋少年", Score = 980 },
                new Player { Name = "五子棋大师", Score = 920 },
                new Player { Name = "棋魂", Score = 870 },
                new Player { Name = "落子无悔", Score = 810 },
                new Player { Name = "棋乐无穷", Score = 760 },
                new Player { Name = "新手玩家", Score = 700 },
                new Player { Name = "棋逢对手", Score = 650 },
                new Player { Name = "观棋不语", Score = 600 },
                new Player { Name = "棋高一着", Score = 550 },
                new Player { Name = "弈秋", Score = 500 }
            };

            LeaderboardList.ItemsSource = players;
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
