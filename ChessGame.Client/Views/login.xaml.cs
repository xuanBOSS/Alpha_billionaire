using System;
using System.Collections.Generic;
using System.Globalization;
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
using ChessGame.Client;
using System;


namespace ChessGame.Client.Views
{
    /// <summary>
    /// login.xaml 的交互逻辑
    /// </summary>
    //public partial class login : Window
    //{
    //    public login()
    //    {
    //        InitializeComponent();
    //    }

    //    //点击登录按钮跳转到主界面
    //    private void Login_Click(object sender, RoutedEventArgs e)
    //    {
    //        MainWindow MainWindow = new MainWindow();
    //        MainWindow.Show();

    //        this.Close();
    //    }
    //}
    public partial class login : Window
    {
        private readonly SignalRService _signalRService;

        public login()
        {
            InitializeComponent();

            // 获取GameConnection单例
            _signalRService = SignalRService.Instance;

            // 先连接服务器
            ConnectToServer();
        }

        private async void ConnectToServer()
        {
            try
            {
                await _signalRService.StartConnectionAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"连接服务器失败: {ex.Message}", "连接错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            string userId = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password?.Trim() ?? "";

            // 简单验证
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("用户名和密码不能为空", "登录错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 显示加载状态或禁用按钮
                LogInButton.IsEnabled = false;

                // 确保先连接到服务器
                if (_signalRService.GetHubConnection().State != Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                {
                    await _signalRService.StartConnectionAsync();

                    // 修改后 - 添加空引用检查
                    if (_signalRService == null)
                    {
                        MessageBox.Show("SignalR服务未初始化", "系统错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // 安全获取连接
                    var hubConnection = _signalRService.GetHubConnection();
                    if (hubConnection == null)
                    {
                        MessageBox.Show("SignalR连接未初始化", "连接错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // 检查连接状态
                    if (hubConnection.State != Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                    {
                        MessageBox.Show("无法连接到服务器，请检查网络连接。", "连接错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                // 执行登录
                var result = await _signalRService.LoginAsync(userId, password);

                if (result.Success)
                {
                    // 登录成功，打开主界面
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    // 登录失败，显示错误消息
                    MessageBox.Show(result.ErrorMessage, "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"登录过程中出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 恢复按钮状态
                LogInButton.IsEnabled = true;
            }
        }
        // 添加注册功能
        private async void Register_Click(object sender, RoutedEventArgs e)
        {
            string userId = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // 简单验证
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("用户名和密码不能为空", "注册错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 显示加载状态或禁用按钮
                RegisterButton.IsEnabled = false;

                // 执行注册，使用用户ID作为用户名（可以根据需要修改）
                var result = await _signalRService.RegisterAsync(userId, password, userId);

                if (result.Success)
                {
                    // 注册成功
                    MessageBox.Show("注册成功，请登录", "注册成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // 注册失败，显示错误消息
                    MessageBox.Show(result.ErrorMessage, "注册失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"注册过程中出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 恢复按钮状态
                RegisterButton.IsEnabled = true;
            }
        }
    }
}
