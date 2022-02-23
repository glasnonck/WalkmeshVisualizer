using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WalkmeshVisualizerWpf.Models
{
    public class WalkabilityModel : INotifyPropertyChanged
    {
        private string _walkability;
        public string Walkability
        {
            get => _walkability;
            set => SetField(ref _walkability, value);
        }

        private RimModel _rim;
        public RimModel Rim
        {
            get => _rim;
            set => SetField(ref _rim, value);
        }

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
