using KotOR_IO;
using KotOR_IO.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WalkmeshVisualizerWpf.Models
{
    public class RimDataSet : INotifyPropertyChanged
    {
        private ObservableCollection<RimData> _rimData = new ObservableCollection<RimData>();
        public string Version = "v1.0";

        public readonly Regex regexAll = new Regex(@"(^g_t_\w+\d+$)|(^g_zonecata(\d\d\d)$)|(^k_flee_trigge(\d\d\d)?r?$)|(_flee$)$");
        public readonly Regex regexTrap = new Regex(@"^g_t_\w+\d+$");
        //public readonly Regex regexTrap = new Regex(@"(^g_t_\w+\d+$)|(^man27_steam\d\d$)|(^blowtrigd?\d$)");
        public readonly Regex regexZone = new Regex(@"(^g_zonecata(\d\d\d)$)|(^k_flee_trigge(\d\d\d)?r?$)|(_flee$)");

        /// <summary>
        /// Collection of ModuleDLZ objects.
        /// </summary>
        public ObservableCollection<RimData> RimData
        {
            get => _rimData;
            set => SetField(ref _rimData, value);
        }

        public void LoadDlzDataFile()
        {
            var dataFilePath = @"Resources\DlzData.txt";
            using (var r = new StreamReader(dataFilePath))
            {
                var json = r.ReadToEnd();
                RimData = JsonConvert.DeserializeObject<ObservableCollection<RimData>>(json);
            }
        }

        public void LoadGameData(string gamePath)
        {
            if (!RimData.Any()) LoadDlzDataFile();

            var kpath = new KPaths(gamePath);
            foreach (var rimFileInfo in kpath.FilesInModules.Where(fi => !fi.Name.EndsWith("_s.rim") && fi.Extension != ".erf"))
            {
                var moduleName = rimFileInfo.Name.Substring(0, rimFileInfo.Name.Length - 4);
                var rim = new RIM(rimFileInfo.FullName);
                var srim = new RIM(rimFileInfo.FullName.Substring(0, rimFileInfo.FullName.Length - 4) + "_s.rim");

                var rimDoors = ParseRimDoors(rim);
                var rimTriggers = ParseRimTriggers(rim);
                var rimTraps = ParseRimTraps(rim);
                var rimZones = ParseRimZones(rim);
                var rimEncounters = ParseRimEncounters(rim, srim);

                RimData module = RimData.FirstOrDefault(m => m.Module == moduleName.ToLower());
                if (module == null)
                {
                    module = new RimData { Module = moduleName.ToLower() };
                    RimData.Add(module);
                }

                foreach (var door in module.Doors)
                {
                    door.RimDataType = RimDataType.Door;
                    door.Module = module.Module;
                    for (int i = 0; i < door.CornersX.Count; i++)
                        door.Geometry.Add(new Tuple<float, float>(door.CornersX[i], door.CornersY[i]));
                }
                module.Triggers = rimTriggers.Select(t => new RimDataInfo(t, module.Module)).ToList();
                module.Traps = rimTraps.Select(t => new RimDataInfo(t, module.Module, RimDataType.Trap)).ToList();
                module.Zones = rimZones.Select(t => new RimDataInfo(t, module.Module, RimDataType.Zone)).ToList();
                module.Encounters = rimEncounters.Select(e => new RimDataInfo(e, module.Module)).ToList();
            }
        }

        public RimDataSet() { }

        #region Sub-Classes

        public struct Geometry
        {
            public List<Tuple<float, float>> Corners;
            public Geometry(GFF.LIST g, string prefix = "")
            {
                Corners = new List<Tuple<float, float>>();
                foreach (var s in g.Structs)
                {
                    Corners.Add(new Tuple<float, float>
                    (
                        item1: (s.Fields.First(f => f.Label == prefix + "X") as GFF.FLOAT).Value,
                        item2: (s.Fields.First(f => f.Label == prefix + "Y") as GFF.FLOAT).Value
                    ));
                }
            }
        }

        public class Door
        {
            public string TemplateResRef;
            public string LinkedToModule;
            public float X;
            public float Y;

            public Door(GFF.STRUCT s)
            {
                TemplateResRef = (s.Fields.First(f => f.Label == "TemplateResRef") as GFF.ResRef).Reference;
                LinkedToModule = (s.Fields.First(f => f.Label == "LinkedToModule") as GFF.ResRef).Reference;
                X = (s.Fields.First(f => f.Label == "X") as GFF.FLOAT).Value;
                Y = (s.Fields.First(f => f.Label == "Y") as GFF.FLOAT).Value;
            }
        }

        public class Trigger
        {
            public List<Tuple<float, float>> Corners = new List<Tuple<float, float>>();
            public string TemplateResRef;

            public override string ToString() =>
                $"ResRef: {TemplateResRef}; Corners: [{string.Join(", ", Corners.Select(t => $"({t.Item1},{t.Item2})"))}]";

            public Trigger(GFF.STRUCT s)
            {
                TemplateResRef = (s.Fields.First(f => f.Label == "TemplateResRef") as GFF.ResRef).Reference;

                var xPos = (s.Fields.First(f => f.Label == "XPosition") as GFF.FLOAT).Value;
                var yPos = (s.Fields.First(f => f.Label == "YPosition") as GFF.FLOAT).Value;
                var geometry = new Geometry(s.Fields.First(f => f.Label == "Geometry") as GFF.LIST, "Point");

                foreach (var corner in geometry.Corners)
                {
                    Corners.Add(new Tuple<float, float>
                    (
                        item1: xPos + corner.Item1,
                        item2: yPos + corner.Item2
                    ));
                }
            }
        }

        public class Encounter
        {
            public List<Tuple<float, float>> Corners = new List<Tuple<float, float>>();
            public List<Tuple<float, float>> SpawnPoints = new List<Tuple<float, float>>();
            public string TemplateResRef;
            public float X;
            public float Y;

            public override string ToString() =>
                base.ToString();

            public Encounter(GFF.STRUCT s)
            {
                TemplateResRef = (s.Fields.First(f => f.Label == "TemplateResRef") as GFF.ResRef).Reference;
                X = (s.Fields.First(f => f.Label == "XPosition") as GFF.FLOAT).Value;
                Y = (s.Fields.First(f => f.Label == "YPosition") as GFF.FLOAT).Value;

                var geometry = new Geometry(s.Fields.First(f => f.Label == "Geometry") as GFF.LIST);
                foreach (var g in geometry.Corners)
                    Corners.Add(new Tuple<float, float>(X + g.Item1, Y + g.Item2));

                var spawns = new Geometry(s.Fields.First(f => f.Label == "SpawnPointList") as GFF.LIST);
                foreach (var p in spawns.Corners)
                    SpawnPoints.Add(new Tuple<float, float>(p.Item1, p.Item2));
            }
        }

        #endregion

        private List<Door> ParseRimDoors(RIM rim)
        {
            var doors = new List<Door>();
            foreach (var d in rim.GitFile.Doors.Structs) doors.Add(new Door(d));
            return doors;
        }

        private List<Trigger> ParseRimTriggers(RIM rim)
        {
            var triggers = new List<Trigger>();
            foreach (var t in rim.GitFile.Triggers.Structs.Where(s => !regexAll.IsMatch((s.Fields.First(f => f.Label == "TemplateResRef") as GFF.ResRef).Reference)))
                triggers.Add(new Trigger(t));
            return triggers;
        }

        private List<Trigger> ParseRimTraps(RIM rim)
        {
            var traps = new List<Trigger>();
            foreach (var t in rim.GitFile.Triggers.Structs.Where(s => regexTrap.IsMatch((s.Fields.First(f => f.Label == "TemplateResRef") as GFF.ResRef).Reference)))
                traps.Add(new Trigger(t));
            return traps;
        }

        private List<Trigger> ParseRimZones(RIM rim)
        {
            var zones = new List<Trigger>();
            foreach (var t in rim.GitFile.Triggers.Structs.Where(s => regexZone.IsMatch((s.Fields.First(f => f.Label == "TemplateResRef") as GFF.ResRef).Reference)))
                zones.Add(new Trigger(t));
            return zones;
        }

        private List<Encounter> ParseRimEncounters(RIM rim, RIM srim)
        {
            var encounters = new List<Encounter>();
            foreach (var e in rim.GitFile.Encounters.Structs) encounters.Add(new Encounter(e));
            return encounters;
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
    public class RimData
    {
        public string Module { get; set; }
        public string Name { get; set; }
        public List<RimDataInfo> Doors { get; set; } = new List<RimDataInfo>();
        public List<RimDataInfo> Triggers { get; set; } = new List<RimDataInfo>();
        public List<RimDataInfo> Traps { get; set; } = new List<RimDataInfo>();
        public List<RimDataInfo> Zones { get; set; } = new List<RimDataInfo>();
        public List<RimDataInfo> Encounters { get; set; } = new List<RimDataInfo>();

        public override string ToString()
        {
            return $"{Module}, {Doors.Count} door(s), {Triggers.Count + Traps.Count + Zones.Count} trigger(s), {Encounters.Count} encounter(s)";
        }
    }

    public enum RimDataType
    {
        Unknown = 0,
        Door,
        Trigger,
        Encounter,
        Trap,
        Zone,
    }

    [Serializable]
    public class RimDataInfo : INotifyPropertyChanged, IComparable<RimDataInfo>
    {
        public RimDataType RimDataType { get; set; }
        public string Module { get; set; }
        public string ResRef { get; set; }
        public List<float> CornersX { get; set; } = new List<float>();
        public List<float> CornersY { get; set; } = new List<float>();
        public List<Tuple<float, float>> Geometry { get; set; } = new List<Tuple<float, float>>();
        public List<Tuple<float, float>> SpawnPoints { get; set; } = new List<Tuple<float, float>>();

        public Canvas LineCanvas { get; set; }
        public List<Line> Lines { get; set; } = new List<Line>();
        public Polygon Polygon { get; set; }
        public List<Ellipse> Ellipses { get; set; } = new List<Ellipse>();

        public bool MeshVisible => MeshColor != Brushes.Transparent;
        public bool LineVisible => LineColor != Brushes.Transparent;

        private Brush _meshColor = Brushes.Transparent;
        private Brush _lineColor = Brushes.Transparent;

        #region Constructors

        public RimDataInfo() { }

        public RimDataInfo(RimDataSet.Trigger trigger, string module = "", RimDataType type = RimDataType.Trigger)
        {
            RimDataType = type;
            Module = module;
            ResRef = trigger.TemplateResRef;
            Geometry = trigger.Corners;
        }

        public RimDataInfo(RimDataSet.Encounter encounter, string module = "")
        {
            RimDataType = RimDataType.Encounter;
            Module = module;
            ResRef = encounter.TemplateResRef;
            Geometry = encounter.Corners;
            SpawnPoints = encounter.SpawnPoints;
        }

        #endregion

        public Brush MeshColor
        {
            get => _meshColor;
            set
            {
                foreach (var ellipse in Ellipses)
                {
                    ellipse.Fill = value;
                    ellipse.Stroke = (value == Brushes.Transparent) ? value : Brushes.Black;
                }

                Polygon.Fill = value;
                Polygon.Stroke = (value == Brushes.Transparent) ? value : Brushes.Black;
                Polygon.StrokeLineJoin = PenLineJoin.Round;
                
                SetField(ref _meshColor, value);
                NotifyPropertyChanged(nameof(MeshVisible));
            }
        }

        public Brush LineColor
        {
            get => _lineColor;
            set
            {
                foreach (var line in Lines)
                    line.Stroke = value;
                SetField(ref _lineColor, value);
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

        public int CompareTo(RimDataInfo other)
        {
            var val = Module.CompareTo(other.Module);
            if (val == 0) val = ResRef.CompareTo(other.ResRef);
            if (val == 0) val = CornersX.Count.CompareTo(other.CornersX.Count);
            if (val == 0) val = CornersY.Count.CompareTo(other.CornersY.Count);
            return val;
        }

        #endregion

        public override string ToString()
        {
            return $"{ResRef}, {Geometry.Count} corner(s)";
        }

        public void Hide()
        {
            if (MeshVisible)
            {
                MeshColor = Brushes.Transparent;
                LineColor = Brushes.Transparent;
            }
        }

        public bool IsTouching(Point testPoint)
        {
            // True if near a spawn point.
            const double NEAR_POINT_DISTANCE = 3.0;
            foreach (var sp in SpawnPoints)
            {
                if ((new Point(sp.Item1, sp.Item2) - testPoint).Length <= NEAR_POINT_DISTANCE)
                    return true;
            }

            // Determine if point is inside geometry.
            var result = false;
            int j = Geometry.Count - 1;
            for (int i = 0; i < Geometry.Count; i++)
            {
                if (Geometry[i].Item2 < testPoint.Y && Geometry[j].Item2 >= testPoint.Y ||
                    Geometry[j].Item2 < testPoint.Y && Geometry[i].Item2 >= testPoint.Y)
                {
                    if (Geometry[i].Item1 + (testPoint.Y - Geometry[i].Item2) /
                        (Geometry[j].Item2 - Geometry[i].Item2) *
                        (Geometry[j].Item1 - Geometry[i].Item1) < testPoint.X)
                    {
                        result = !result;
                    }
                }
                j = i;
            }

            return result;
        }

    }
}
