using System.Configuration;
using System.Data;
using System.Windows;
using ChessGame.Client.Views;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace ChessGame.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // 定义一个服务提供者来管理应用的依赖
        public static IServiceProvider ServiceProvider { get; private set; }

        public App()
        {
            var serviceCollection = new ServiceCollection();

            // 注册 SignalRService 为单例
            serviceCollection.AddSingleton<SignalRService>();

            // 配置 DI 容器
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 获取 SignalRService 实例
            var signalRService = ServiceProvider.GetRequiredService<SignalRService>();

            // 启动 SignalR 连接
            signalRService.StartConnectionAsync();

            // 获取并显示主窗口
            /*var mainWindow = new MainWindow(); 
            mainWindow.Show();*/
        }

    }

}
