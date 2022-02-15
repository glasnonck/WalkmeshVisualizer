using KotOR_IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WalkmeshCompareWpf.Models
{
    public abstract class GameData : INotifyPropertyChanged
    {
        private ObservableCollection<ModuleWalkmesh> _modules;

        public ObservableCollection<ModuleWalkmesh> Modules
        {
            get => _modules;
            set => SetField(ref _modules, value);
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

        #endregion // END REGION INotifyPropertyChanged Implementation
    }

    public class K1GameData : GameData
    {

    }

    public class K2GameData : GameData
    {

    }
}
