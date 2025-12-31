using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WalkmeshVisualizerWpf.Models
{
    public enum KotorGlobalType
    {
        Unknown = 0,
        Boolean,
        Number,
        Location,
        String,
    }

    [Serializable]
    public class KotorGlobal : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
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
            NotifyPropertyChanged(nameof(HasChanged));
            return true;
        }
        #endregion // INotifyPropertyChanged Implementation

        #region Properties
        public int ID
        {
            get => _id;
            set => SetField(ref _id, value);
        }
        private int _id;

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }
        private string _name;

        public KotorGlobalType Type
        {
            get => _type;
            set => SetField(ref _type, value);
        }
        private KotorGlobalType _type;

        public string Value
        {
            get => _value;
            set => SetField(ref _value, value);
        }
        private string _value;

        [JsonIgnore]
        public string LastValue
        {
            get => _lastValue;
            set => SetField(ref _lastValue, value);
        }
        private string _lastValue;

        public DateTime? LastReadAt
        {
            get => _lastReadAt;
            set => SetField(ref _lastReadAt, value);
        }
        private DateTime? _lastReadAt;

        [JsonIgnore]
        public DateTime? LastChangeAt
        {
            get => _lastChangeAt;
            set => SetField(ref _lastChangeAt, value);
        }
        private DateTime? _lastChangeAt;

        [JsonIgnore]
        public bool IsWatched
        {
            get => _isWatched;
            set => SetField(ref _isWatched, value);
        }
        private bool _isWatched = false;

        [JsonIgnore]
        public bool HasChanged => Value != LastValue;
        #endregion // Properties
    }
}
