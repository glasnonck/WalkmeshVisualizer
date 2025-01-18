using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WalkmeshVisualizerWpf.Models
{
    public class DlzData : INotifyPropertyChanged
    {
        private ObservableCollection<ModuleDLZ> _moduleDLZs;

        /// <summary>
        /// Collection of ModuleDLZ objects.
        /// </summary>
        public ObservableCollection<ModuleDLZ> ModuleDLZs
        {
            get => _moduleDLZs;
            set => SetField(ref _moduleDLZs, value);
        }

        public DlzData()
        {
            var path = @"Resources\DlzData.txt";
            using (var r = new StreamReader(path))
            {
                var json = r.ReadToEnd();
                ModuleDLZs = JsonConvert.DeserializeObject<ObservableCollection<ModuleDLZ>>(json);
            }

            foreach (var module in ModuleDLZs)
            {
                foreach (var door in module.Doors)
                    door.Module = module.Module;
                foreach (var trigger in module.Triggers)
                    trigger.Module = module.Module;
            }
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

    [Serializable]
    public class ModuleDLZ
    {
        public string Module { get; set; }
        public string Name { get; set; }
        public List<DlzInfo> Doors { get; set; } = new List<DlzInfo>();
        public List<DlzInfo> Triggers { get; set; } = new List<DlzInfo>();

        public override string ToString()
        {
            return $"{Module}, {Doors.Count} door(s), {Triggers.Count} trigger(s)";
        }
    }

    [Serializable]
    public class DlzInfo : INotifyPropertyChanged, IComparable<DlzInfo>
    {
        public string Module { get; set; }
        public string ResRef { get; set; }
        public List<float> Corners { get; set; } = new List<float>();
        public List<Line> Lines { get; set; } = new List<Line>();
        public bool Visible => MeshColor != Brushes.Transparent;

        private Brush _meshColor = Brushes.Transparent;
        public Brush MeshColor
        {
            get => _meshColor;
            set
            {
                foreach (var line in Lines)
                    line.Stroke = value;
                SetField(ref _meshColor, value);
            }
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

        #region IComparable<DlzInfo> Implementation

        public int CompareTo(DlzInfo other)
        {
            var val = Module.CompareTo(other.Module);
            if (val == 0) val = ResRef.CompareTo(other.ResRef);
            if (val == 0) val = Corners.Count.CompareTo(other.Corners.Count);
            return val;
        }

        #endregion

        public override string ToString()
        {
            return $"{ResRef}, {Corners.Count} corner(s)";
        }
    }
}
