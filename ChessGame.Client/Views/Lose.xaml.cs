﻿using System;
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
    /// Lose.xaml 的交互逻辑
    /// </summary>
    public partial class Lose : Window
    {
        public Lose()
        {
            InitializeComponent();
        }

        //点击返回按钮
        private void LoseReturn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
