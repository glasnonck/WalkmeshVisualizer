using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// Interaction logic for CoordinateInputDialog.xaml
    /// </summary>
    public partial class CoordinateInputDialog : Window, INotifyPropertyChanged
    {
        public CoordinateInputDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        public CoordinateInputDialog(double x, double y)
            : this()
        {
            txtValueX.Text = x.ToString("N2");
            txtValueY.Text = y.ToString("N2");
        }

        #region Properties

        public Brush PointFill
        {
            get => _pointFill;
            set => SetField(ref _pointFill, value);
        }
        private Brush _pointFill = Brushes.Transparent;

        public Brush PointStroke
        {
            get => _pointStroke;
            set => SetField(ref _pointStroke, value);
        }
        private Brush _pointStroke = Brushes.Transparent;

        public string PointName
        {
            get => _pointName;
            set => SetField(ref _pointName, value);
        }
        private string _pointName = "Unknown";

        public string X => txtValueX.Text;

        public string Y => txtValueY.Text;

        #endregion

        #region Events

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(txtValueX.Text, out var _))
            {
                _ = MessageBox.Show("X value is not a valid number.");
                return;
            }

            if (!double.TryParse(txtValueY.Text, out var _))
            {
                _ = MessageBox.Show("Y value is not a valid number.");
                return;
            }

            DialogResult = true;
        }

        private void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.KeyboardDevice.IsKeyDown(Key.Tab))
                ((TextBox)sender).SelectAll();
        }

        private void TextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ((TextBox)sender).Select(0, 0);
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            txtValueX.SelectAll();
            _ = txtValueX.Focus();
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}
