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

namespace ChessGame.Client.Views
{
    /// <summary>
    /// Win.xaml 的交互逻辑
    /// </summary>
    public partial class Win : Window
    {
        public Win()
        {
            InitializeComponent();
        }

        //点击返回按钮
        private void WinReturn_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow == null || !(Application.Current.MainWindow is MainWindow))
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
                Application.Current.MainWindow = mainWindow;
            }
            this.Close(); 
        }
       
        //点击右上角叉关闭窗口
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Application.Current.MainWindow == null || !(Application.Current.MainWindow is MainWindow))
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
                Application.Current.MainWindow = mainWindow;
            }
        }
    }
}
