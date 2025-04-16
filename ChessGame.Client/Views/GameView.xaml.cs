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
    /// GameView.xaml 的交互逻辑
    /// </summary>
    public partial class GameView : Window
    {
        public List<double> HorizontalLines { get; set; }
        public List<double> VerticalLines { get; set; }
        public GameView()
        {
            InitializeComponent();


            // 生成15条水平线和垂直线的位置
            HorizontalLines = new List<double>();
            VerticalLines = new List<double>();

            // 计算每条线的间隔（600 / (15-1) ≈ 42.857）
            double spacing = 490.0 / 14;

            for (int i = 0; i < 15; i++)
            {
                double position = 0 + i * spacing;
                HorizontalLines.Add(position);
                VerticalLines.Add(position);
            }

            // 确保 DataContext 设置正确
            this.DataContext = this;
        }
    }
}
