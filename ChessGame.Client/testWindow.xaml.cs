using ChessGame.Client.Views;
using Microsoft.Extensions.DependencyInjection;
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

namespace ChessGame.Client
{
    /// <summary>
    /// testWindow.xaml 的交互逻辑
    /// </summary>
    public partial class testWindow : Window
    {
        private readonly SignalRService _signalRService;
        public testWindow()
        {
            InitializeComponent();

            // 通过类型名访问静态成员 ServiceProvider
            _signalRService = App.ServiceProvider.GetRequiredService<SignalRService>();

            // 注册接收消息的事件
            _signalRService.RegisterReceiveMessage((user, message) =>
            {
                // 在接收到消息时更新 UI
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessagesListBox.Text = ($"{user}: {message}");
                });
            });

        }

        private async Task testServer(string user, string msg)
        {
            // 客户端调用 SendMessage 方法，发送消息到服务器
            await _signalRService.SendMessageToServer(user, msg);

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string user = name.Text;
            string msg = message.Text;

            testServer(user,msg);
        }
    }
}
