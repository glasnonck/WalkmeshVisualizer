using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace WalkmeshVisualizerWpf.Models
{
    public class RimModel : INotifyPropertyChanged, IComparable<RimModel>
    {
        public string FileName { get; set; }
        public string Planet { get; set; }
        public string CommonName { get; set; }
        public Point EntryPoint { get; set; }

        private Brush _meshColor;
        public Brush MeshColor
        {
            get => _meshColor;
            set => SetField(ref _meshColor, value);
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

        #region IComparable<RimModel> Implementation

        public int CompareTo(RimModel other)
        {
            return FileName.CompareTo(other.FileName);
        }

        #endregion
    }
}
