using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using WalkmeshVisualizerWpf.Models;

namespace WalkmeshVisualizerWpf.UserControls
{
    /// <summary>
    /// Interaction logic for PaletteSelectUserControl.xaml
    /// </summary>
    public partial class PaletteSelectUserControl : UserControl, INotifyPropertyChanged
    {
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

        #endregion // END REGION INotifyPropertyChanged Implementation

        public PaletteSelectUserControl()
        {
            InitializeComponent();
        }
    }
}
