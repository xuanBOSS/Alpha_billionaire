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
using ChessGame.GameLogic;
using Microsoft.Extensions.DependencyInjection;

namespace ChessGame.Client.Views
{
    /// <summary>
    /// AIchoose.xaml 的交互逻辑
    /// </summary>
    public partial class AIchoose : Window
    {
        private SignalRService _signalRService;
        public AIchoose()
        {
            InitializeComponent();

            _signalRService = App.ServiceProvider.GetRequiredService<SignalRService>();
        }

        //选择黑棋
        private void Black_Click(object sender, RoutedEventArgs e)
        {
            Brush color = Brushes.Black;
            //这里上传通信把color传上去)
            _signalRService.SelectPlayerColor(PlayerColor.Black);

            GameViewAI GameViewAI = new GameViewAI();
            GameViewAI.Show();

            this.Close();//关闭选择颜色小窗口
        }

        //选择白棋
        private void White_Click(object sender, RoutedEventArgs e)
        {
            Brush color = Brushes.White;
            //这里上传通信把color传上去
            _signalRService.SelectPlayerColor(PlayerColor.White);

            GameViewAI GameViewAI = new GameViewAI();
            GameViewAI.Show();

            this.Close();//关闭选择颜色小窗口
        }

        
    }
}
