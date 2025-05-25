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
using Microsoft.Extensions.DependencyInjection;


namespace ChessGame.Client.Views
{
    
    public partial class login : Window
    {
        private readonly SignalRService _signalRService;

        public login()
        {
            InitializeComponent();

            // 获取GameConnection单例
            //_signalRService = SignalRService.Instance;
            _signalRService = App.ServiceProvider.GetRequiredService<SignalRService>();
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

                
                // 在登录按钮点击事件中
                // 确保先连接到服务器
                var hubConnection = _signalRService.GetHubConnection();
                if (hubConnection == null)
                {
                    MessageBox.Show("SignalR连接未初始化", "连接错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (hubConnection.State != Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                {
                    try
                    {
                        await _signalRService.StartConnectionAsync();

                        // 再次检查连接状态
                        if (hubConnection.State != Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                        {
                            MessageBox.Show("无法连接到服务器，请检查网络连接。", "连接错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    catch (Exception connEx)
                    {
                        MessageBox.Show($"连接错误: {connEx.Message}", "连接错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                // 执行登录
                var result = await _signalRService.LoginAsync(userId, password);

                if (result.Success)
                {
                    // 登录成功，打开主界面并传递用户信息
                    var mainWindow = new MainWindow(new SignalRService.UserInfo
                    {
                        UserId = result.UserId,
                        UserName = result.UserName,
                        WinTimes = result.WinTimes
                    });
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    // 登录失败，显示错误消息
                    MessageBox.Show(result.Message, "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show(result.Message, "注册失败", MessageBoxButton.OK, MessageBoxImage.Error);
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
