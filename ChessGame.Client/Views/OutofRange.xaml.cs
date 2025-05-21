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
    /// OutofRange.xaml 的交互逻辑
    /// </summary>
    public partial class OutofRange : Window
    {
        public OutofRange()
        {
            InitializeComponent();
        }

        //点击确认按钮
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
