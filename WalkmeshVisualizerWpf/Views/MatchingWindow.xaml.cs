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

namespace WalkmeshVisualizerWpf.Views
{
    /// <summary>
    /// Interaction logic for MatchingWindow.xaml
    /// </summary>
    public partial class MatchingWindow : Window
    {
        public MatchingWindow(string message)
        {
            InitializeComponent();

            lblMessage.Text = message;
        }

        internal void UpdateMessage(string msg)
        {
            lblMessage.Text = msg;
        }
    }
}
