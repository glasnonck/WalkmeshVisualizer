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
using WalkmeshVisualizerWpf.Views;

namespace WalkmeshVisualizerWpf.Models
{
    public class RimDataSet : INotifyPropertyChanged
    {
        private readonly Dictionary<string, string> moduleToLayoutLookup = new Dictionary<string, string>();
        private readonly Dictionary<string, BIF> bifLayoutsLookup = new Dictionary<string, BIF>();
        private readonly Dictionary<string, KEY> keyLookup = new Dictionary<string, KEY>();
        private readonly Dictionary<string, LYT> lytLookup = new Dictionary<string, LYT>();

        private ObservableCollection<RimData> _rimData = new ObservableCollection<RimData>();
        public string Version = "v1.0";

        /** Trigger Sorting Notes
            * Mines / Traps
                * No Damage
                    * k_kor_rockfall2
                    * k_kor_rockfalltr
                    * k_kor_deadrocktr
                * Verify these triggers
                    * k39_trg_trap
            * Zone Controller
                * k_zonetrigger##(#)? (manm27aa)
                * tar09_area09
                * tar10_wtflee
                * unk41_fleetrig
                * unk44_area08
                * unk44_area11
                * unk44_area14
                * dan15_compon002 / 003 / 1
                * area## (unk_44ac, summit)
                * dan14_rapid03
                * man_enter#? (Manaan Docking Bay)
            * Conversation Triggers
                * g_i_partyinit###
                * g_partyinit###
                * k_bant_trig(###)?
                * plottalk (How broad do we want conversations triggers to be?)
            * Other Encounters ?
                * kas22_kinrathen#
                * kas24_katarnenc#
                * k34_trg_shywyrm#
                * k34_trg_tukdire1
                * tat18_tuskenc_##
                * encounttrig
            * Unused triggers
                * [modulename]_swooptrig#
                * k_kor_swoop_03
         */
        public readonly Regex regexAll  = new Regex(@"(^g_t_\w+\d+$)|(^man27_steam\d\d$)|(^blowtrigd?\d$)|(^poisongas$)|(^g_zoncata\d+$)|(^zonecatalogu?er?\d\d$)|(^k_flee_trigge(\d\d\d)?r?$)|(_flee$)$");
        public readonly Regex regexTrap = new Regex(@"(^g_t_\w+\d+$)|(^man27_steam\d\d$)|(^blowtrigd?\d$)|(^poisongas$)");
        public readonly Regex regexZone = new Regex(@"(^g_zoncata\d+$)|(^zonecatalogu?er?\d\d$)|(^k_flee_trigge(\d\d\d)?r?$)|(_flee$)");

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
                var moduleName = rimFileInfo.Name.Substring(0, rimFileInfo.Name.Length - 4).ToLower();
                var rim = new RIM(rimFileInfo.FullName);
                var srim = new RIM(rimFileInfo.FullName.Substring(0, rimFileInfo.FullName.Length - 4) + "_s.rim");
                
                var rimDoors = ParseRimDoors(rim);
                var rimTriggers = ParseRimTriggers(rim);
                var rimTraps = ParseRimTraps(rim);
                var rimZones = ParseRimZones(rim);
                var rimEncounters = ParseRimEncounters(rim, srim);

                RimData module = RimData.FirstOrDefault(m => m.Module == moduleName);
                if (module == null)
                {
                    module = new RimData { Module = moduleName };
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

                // Handle Swoop Modules
                if (!moduleToLayoutLookup.ContainsKey(moduleName) && IsSwoopModule(moduleName))
                {
                    // Parse LYT files.
                    var ifo = new GFF(rim.File_Table.First(rf => rf.TypeID == (int)ResourceType.IFO).File_Data);
                    var lytFile = (ifo.Top_Level.Fields.First(f => f.Label == "Mod_Entry_Area") as GFF.ResRef).Reference;
                    var lyt = GetLayout(kpath, lytFile);
                    moduleToLayoutLookup.Add(moduleName, lytFile);

                    var are = new GFF(rim.File_Table.First(rf => rf.TypeID == (int)ResourceType.ARE).File_Data);
                    var mg = are.Top_Level.Fields.First(f => f.Label == "MiniGame") as GFF.STRUCT;
                    var mgE = mg.Fields.First(f => f.Label == "Enemies") as GFF.LIST;
                    //var mgO = mg.Fields.First(f => f.Label == "Obstacles") as GFF.LIST;
                    var mgP = mg.Fields.First(f => f.Label == "Player") as GFF.STRUCT;
                    var mgT = mgP.Fields.First(f => f.Label == "Track") as GFF.ResRef;
                    var playerRadius = (mgP.Fields.First(f => f.Label == "Sphere_Radius") as GFF.FLOAT).Value;

                    module.Triggers.AddRange(ParseLayoutPlaceable(moduleName, lyt.Tracks, playerRadius, mgE, mgT.Reference));
                    module.Traps.AddRange(ParseLayoutPlaceable(moduleName, lyt.Obstacles, playerRadius));
                }
            }
        }

        private List<RimDataInfo> ParseLayoutPlaceable(string moduleName, List<LYT.ArtPlaceable> items, float playerRadius, GFF.LIST boosters = null, string ignoreTrack = "")
        {
            var triggers = new List<RimDataInfo>();
            foreach (var item in items)
            {
                // Ignore the track LYT entry.
                if (item.Model.ToLower() == ignoreTrack.ToLower()) continue;

                // Calculate booster / obstacle radius.
                var radius = playerRadius;
                if (boosters != null)
                {
                    var obj = boosters.Structs.Find(s =>
                        (s.Fields.First(f => f.Label == "Track") as GFF.ResRef)
                        .Reference.ToLower() == item.Model.ToLower());
                    var flo = obj.Fields.First(f => f.Label == "Sphere_Radius") as GFF.FLOAT;
                    var val = flo.Value;

                    radius += (boosters.Structs.First(s => 
                        (s.Fields.First(f => f.Label == "Track") as GFF.ResRef)
                        .Reference.ToLower() == item.Model.ToLower())
                        .Fields.First(f => f.Label == "Sphere_Radius") as GFF.FLOAT)
                        .Value;
                }

                // Create RDI object.
                triggers.Add(new RimDataInfo(item, moduleName)
                {
                    EllipseRadius = radius,
                });
            }
            return triggers;
        }

        private static bool IsSwoopModule(string moduleName)
        {
            return moduleName == "tar_m03mg"
                || moduleName == "tat_m17mg"
                || moduleName == "manm26mg"

                //|| moduleName == "m12ab"
                //|| moduleName == "107per"
                //|| moduleName == "421dxn"
                //|| moduleName == "505ond"

                || moduleName == "211tel"
                || moduleName == "371nar"
                || moduleName == "510ond";
        }

        private static void AddMoreToSwoops(Random rand, string moduleName, RIM rim, string lytFile, LYT lyt, string outputDirectory)
        {
            // Swoop Tracks
            // K1: tar_m03mg, tat_m17mg, manm26mg
            // K2:    211tel,    371nar,   510ond
            // Other LYT with tracks / obstacles
            // K1: m12ab (fighters)
            // K2: 107per (turrets), 421dxn (blockade), 505ond (turrets)

            // ** Manaan Swoops (55:75)
            // Start:  (0.042248, 100)
            // Finish: y = ~3800
            // X [ 84.7235,  123.842] <  39.1185> (104.28275)
            // Y [170.042 , 3589.13 ] <3419.088 >
            // [0] t01 - all zeros
            // [8] t09 - out of range (1703.79, 83.7564)

            // ** Taris Swoops (54:08)
            // Start:  (0, 0) slight left push, slides to -1.359033 if unchanged
            // Finish: y = ~3800
            // X [ 81.4833,  122.339] <  40.8557> (101.91115)
            // Y [190.006 , 3742.73 ] <3552.724 >
            // [0] tr01 - (95.0, 0.0)  Not sure what this is.

            // ** Tatooine Swoops (55:75)
            // Start:  (0.031483, 100) appear to start at x=0, but slight right shift at start
            // Finish: y = ~3800
            // X [ 80.796,  119.461] <  38.6649> (100.1285)
            // Y [209.04 , 3778.61 ] <3569.57  >
            // [0] t01 - all zeroes
            // [8] t09 - out of range (1933.7, 677.246, 4993.0)

            var are = new GFF(rim.File_Table.First(rf => rf.TypeID == (int)ResourceType.ARE).File_Data);
            var mg = are.Top_Level.Fields.First(f => f.Label == "MiniGame") as GFF.STRUCT;
            var mgE = mg.Fields.First(f => f.Label == "Enemies") as GFF.LIST;
            var mgO = mg.Fields.First(f => f.Label == "Obstacles") as GFF.LIST;
            var mgP = mg.Fields.First(f => f.Label == "Player") as GFF.STRUCT;
            var mgT = mgP.Fields.First(f => f.Label == "Track") as GFF.ResRef;

            var xCoords = lyt.Tracks.Concat(lyt.Obstacles).Select(o => o.X).ToList();
            xCoords.RemoveAt(8);
            xCoords.RemoveAt(0);
            var xMin = xCoords.Min();
            var xMax = xCoords.Max();
            var xDif = xMax - xMin;
            var xMid = xMin + xDif/2;

            var yCoords = lyt.Tracks.Concat(lyt.Obstacles).Select(o => o.Y).ToList();
            yCoords.RemoveAt(8);
            yCoords.RemoveAt(0);
            var yMin = yCoords.Min();
            var yMax = yCoords.Max();
            var yDif = yMax - yMin;

            var trackPrefix = string.Empty;
            if (moduleName == "manm26mg") trackPrefix = "m26mg_mgt";
            if (moduleName == "tat_m17mg") trackPrefix = "m17mg_mgt";
            if (moduleName == "tar_m03mg") trackPrefix = "m03mg_mgt";

            for (int i = 50; i < 70; i++)
            {
                var newpad = new GFF.STRUCT("", 0, mgE.Structs[4].Fields.ToList());
                var resref = newpad.Fields.First(f => f.Label == "Track") as GFF.ResRef;
                newpad.Fields.Remove(resref);
                resref = new GFF.ResRef("Track", $"{trackPrefix}{i}");
                newpad.Fields.Add(resref);
                mgE.Structs.Add(newpad);
                lyt.Tracks.Add(new LYT.ArtPlaceable()
                {
                    Model = $"{trackPrefix}{i}",
                    X = (float)((rand.NextDouble() * xDif) + xMin),
                    Y = (float)((rand.NextDouble() * yDif) + yMin),
                    Z = 0.0f
                });
            }

            lyt.WriteToFile($"{outputDirectory}\\{moduleName}\\{lytFile}_mod.lyt");
            are.WriteToFile($"{outputDirectory}\\{moduleName}\\{lytFile}_mod.are");
            rim.File_Table.RemoveAll(rf => rf.TypeID == (int)ResourceType.ARE);
            rim.File_Table.Add(new RIM.rFile() { Label = lytFile, TypeID = (int)ResourceType.ARE, File_Data = are.ToRawData() });
            rim.WriteToFile($"{outputDirectory}\\{moduleName}\\{moduleName}_mod.rim");
        }

        private LYT GetLayout(KPaths kpath, string fileName)
        {
            if (lytLookup.ContainsKey(fileName))
                return lytLookup[fileName];

            if (!bifLayoutsLookup.ContainsKey(kpath.swkotor))
            {
                bifLayoutsLookup.Add(kpath.swkotor, new BIF(System.IO.Path.Combine(kpath.data, "layouts.bif")));
                keyLookup.Add(kpath.swkotor, new KEY(kpath.chitin));
                bifLayoutsLookup[kpath.swkotor].AttachKey(keyLookup[kpath.swkotor], $"data\\layouts.bif");
            }

            var vre = bifLayoutsLookup[kpath.swkotor].VariableResourceTable.First(x => x.ResRef == fileName);
            var lyt = new LYT(vre.EntryData);
            lytLookup.Add(fileName, lyt);
            return lyt;
        }

        public LYT GetLayoutFromModuleName(string moduleName)
            => moduleToLayoutLookup.ContainsKey(moduleName) &&
                lytLookup.ContainsKey(moduleToLayoutLookup[moduleName])
                ? lytLookup[moduleToLayoutLookup[moduleName]]
                : null;

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
        Swoop,
    }

    [Serializable]
    public class RimDataInfo : INotifyPropertyChanged, IComparable<RimDataInfo>
    {
        public bool AreVisualsBuilt { get; set; } = false;
        public ColorTheme ColorThemeUsed { get; set; } = ColorTheme.Unknown;

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
        public double EllipseRadius { get; set; } = 1.0;

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

        public RimDataInfo(LYT.ArtPlaceable art, string module = "")
        {        
            RimDataType = RimDataType.Swoop;
            Module = module;
            ResRef = art.Model;
            SpawnPoints = new List<Tuple<float, float>> { new Tuple<float, float>(art.X, art.Y) };
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
            var point_distance = Math.Max(NEAR_POINT_DISTANCE, EllipseRadius);
            foreach (var sp in SpawnPoints)
            {
                if ((new Point(sp.Item1, sp.Item2) - testPoint).Length <= point_distance)
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
