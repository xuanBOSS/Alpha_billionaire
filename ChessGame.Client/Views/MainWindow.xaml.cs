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
using Microsoft.Extensions.DependencyInjection;

namespace ChessGame.Client.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

        }

        //选择人机模式
        private void ManMachineMode_Click(object sender, RoutedEventArgs e)
        {
            GameViewAI GameViewAI = new GameViewAI();
            GameViewAI.Show();

            this.Close();
        }

        //选择联机模式
        private void OnlineMode_Click(object sender, RoutedEventArgs e)
        {
            GameView GameView = new GameView();
            GameView.Show();

            this.Close();
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
    }
}
