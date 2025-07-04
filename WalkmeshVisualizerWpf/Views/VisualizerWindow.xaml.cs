﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Xps.Packaging;
using KotOR_IO;
using KotOR_IO.GffFile;
using KotOR_IO.Helpers;
using Microsoft.Win32;
using WalkmeshVisualizerWpf.Helpers;
using WalkmeshVisualizerWpf.Models;
using kmih = KotorMessageInjector.KotorHelpers;
using kmia = KotorMessageInjector.Adapter;
using ZoomAndPan;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows.Documents;

namespace WalkmeshVisualizerWpf.Views
{
    /// <summary>
    /// Interaction logic for VisualizerWindow.xaml
    /// </summary>
    public partial class VisualizerWindow : Window, INotifyPropertyChanged
    {
        #region Constructors

        public VisualizerWindow()
        {
            InitializeComponent();
            XmlGameData.Initialize();

            // Hide selected game label.
            pnlSelectedGame.Visibility = Visibility.Collapsed;

            // Set up GameDataWorker
            GameDataWorker.WorkerReportsProgress = true;
            GameDataWorker.ProgressChanged += Bw_ProgressChanged;
            GameDataWorker.RunWorkerCompleted += Bw_RunWorkerCompleted;
            GameDataWorker.DoWork += GameDataWorker_DoWork;

            // Set up AddPolyWorker
            AddPolyWorker.WorkerReportsProgress = true;
            AddPolyWorker.ProgressChanged += Bw_ProgressChanged;
            AddPolyWorker.RunWorkerCompleted += Bw_RunWorkerCompleted;
            AddPolyWorker.DoWork += AddPolyWorker_DoWork;

            // Set up UpdateLayerVisibilityWorker
            UpdateLayerVisibilityWorker.WorkerReportsProgress = true;
            UpdateLayerVisibilityWorker.ProgressChanged += Bw_ProgressChanged;
            UpdateLayerVisibilityWorker.RunWorkerCompleted += Bw_RunWorkerCompleted;
            UpdateLayerVisibilityWorker.DoWork += UpdateLayerVisibilityWorker_DoWork;

            // Set up RemovePolyWorker
            RemovePolyWorker.WorkerReportsProgress = true;
            RemovePolyWorker.ProgressChanged += Bw_ProgressChanged;
            RemovePolyWorker.RunWorkerCompleted += Bw_RunWorkerCompleted;
            RemovePolyWorker.DoWork += RemovePolyWorker_DoWork;

            // Set up LivePositionWorker
            LivePositionWorker.WorkerReportsProgress = false;
            LivePositionWorker.WorkerSupportsCancellation = true;
            LivePositionWorker.RunWorkerCompleted += LivePositionWorker_RunWorkerCompleted;
            LivePositionWorker.DoWork += LivePositionWorker_DoWork;

            // Set up MouseHoverWorker
            MouseHoverWorker.WorkerReportsProgress = false;
            MouseHoverWorker.WorkerSupportsCancellation = true;
            MouseHoverWorker.RunWorkerCompleted += MouseHoverWorker_RunWorkerCompleted;
            MouseHoverWorker.DoWork += MouseHoverWorker_DoWork;

            KotorClassLookup = KotorClassDescriptionLookup.ToDictionary((c) => c.Value, (c) => c.Key);
            KotorPartyMembers = Kotor2PartyMembers;
            DataContext = this;
        }

        /// <summary>
        /// Event raised when the Window has loaded.
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var settings = Properties.Settings.Default;
            ShowWalkableFaces = settings.ShowWalkableFaces;
            ShowNonWalkableFaces = settings.ShowNonWalkableFaces;
            ShowTransAbortPoints = settings.ShowTransAbortPoints;
            ShowTransAbortRegions = settings.ShowTransAbortRegions;
            ShowDefaultSpawnPoints = settings.ShowDefaultSpawnPoints;
            ShowLeftClickPointCoordinates = settings.ShowLeftClickPointCoordinates;
            ShowRightClickPointCoordinates = settings.ShowRightClickPointCoordinates;
            ShowDlzLines = settings.ShowDlzLines;
            ShowDoorsOnAddRim = settings.ShowDoorsOnAddRim;
            ShowTriggersOnAddRim = settings.ShowTriggersOnAddRim;
            ShowTrapsOnAddRim = settings.ShowTrapsOnAddRim;
            ShowZonesOnAddRim = settings.ShowZonesOnAddRim;
            ShowEncountersOnAddRim = settings.ShowEncountersOnAddRim;
            ShowLivePositionCoordinates = settings.ShowLivePositionCoordinates;
            HidePreviousLiveModule = settings.HidePreviousLiveModule;
            ShowCurrentLiveModule = settings.ShowCurrentLiveModule;
            HotswapToLiveGame = settings.HotswapToLiveGame;
            ViewFollowsLivePosition = settings.ViewFollowsLivePosition;
            LivePositionUpdateDelay = settings.LivePositionUpdateDelay;
            ShowRimDataUnderMouse = settings.ShowRimDataUnderMouse;
            ShowGatherPartyRange = settings.ShowGatherPartyRange;
            ShowLeftClickGatherPartyRange = settings.ShowLeftClickGatherPartyRange;
            ShowCoordinatePanel = settings.ShowCoordinatePanel;
            ShowRimDataPanel = settings.ShowRimDataPanel;
            ShowDistancePanel = settings.ShowDistancePanel;
            ShowWalkmeshPanel = settings.ShowWalkmeshPanel;
            ShowToolsPanel = settings.ShowToolsPanel;
            ShowWireTargetPanel = settings.ShowWireTargetPanel;
            prevLeftPanelSize = settings.PrevLeftPanelSize;
            prevRightPanelSize = settings.PrevRightPanelSize;

            SelectedBackgroundColor = (BackgroundColor)settings.SelectedBackgroundColor;
            SelectedPalette = PaletteManager.Instance.Palettes.FirstOrDefault(p => p.FileName == settings.SelectedPaletteName);
            if (SelectedPalette == null) SelectedPalette = PaletteManager.Instance.Palettes.FirstOrDefault(p => p.Name == PaletteManager.DEFAULT_PALETTE_NAME);
            if (SelectedPalette == null) SelectedPalette = PaletteManager.Instance.Palettes.First();
            SelectedPalette.IsSelected = true;

            ShowRimDataDoors = settings.ShowRimDataDoors;
            ShowRimDataTriggers = settings.ShowRimDataTriggers;
            ShowRimDataTraps = settings.ShowRimDataTraps;
            ShowRimDataZones = settings.ShowRimDataZones;
            ShowRimDataEncounters = settings.ShowRimDataEncounters;

            ShowTypeUnderMouse = settings.ShowTypeUnderMouse;
            ShowResRefUnderMouse = settings.ShowResRefUnderMouse;
            ShowTagUnderMouse = settings.ShowTagUnderMouse;
            ShowLocalizedNameUnderMouse = settings.ShowLocalizedNameUnderMouse;
            ShowOnEnterUnderMouse = settings.ShowOnEnterUnderMouse;

            // Palette
            BrushToName = PaletteManager.GetSelectedPalette().ToDictionary();
            foreach (var kvp in BrushToName) PolyBrushCount.Add(kvp.Key, 0);

            BrushCycle = new List<Brush>(PolyBrushCount.Keys);
            CurrentRimDataInfoBrush = BrushCycle.First();

            BrushToName.Add(Brushes.Black, "Black");
            BrushToName.Add(Brushes.White, "White");

            // Canvas
            content.Background = ShowTransAbortRegions ? Brushes.Black : BackgroundColors[SelectedBackgroundColor];
            CoordinateTextBrush = ShowTransAbortRegions ? Brushes.White : ForegroundColors[SelectedBackgroundColor];
            if (ShowRimDataUnderMouse) RunMouseHoverWorker_Executed(this, null);

            // Left Panel
            if (ShowCoordinatePanel || ShowRimDataPanel || ShowDistancePanel || ShowToolsPanel)
                columnLeftPanel.Width = new GridLength(prevLeftPanelSize, GridUnitType.Pixel);

            // - Rim Data Panel
            SetRimDataTypePanelVisibility(ShowRimDataDoors, "Door");
            SetRimDataTypePanelVisibility(ShowRimDataTriggers, "Trigger");
            SetRimDataTypePanelVisibility(ShowRimDataTraps, "Trap");
            SetRimDataTypePanelVisibility(ShowRimDataZones, "Zone");
            SetRimDataTypePanelVisibility(ShowRimDataEncounters, "Encounter");

            // Right Panel
            if (ShowWalkmeshPanel) columnRightPanel.Width = new GridLength(prevRightPanelSize, GridUnitType.Pixel);

            // Set up RIM data panel filters
            ((CollectionView)CollectionViewSource.GetDefaultView(lvRimDoor.ItemsSource)).Filter = RimDataFilter;
            ((CollectionView)CollectionViewSource.GetDefaultView(lvRimTrigger.ItemsSource)).Filter = RimDataFilter;
            ((CollectionView)CollectionViewSource.GetDefaultView(lvRimTrap.ItemsSource)).Filter = RimDataFilter;
            ((CollectionView)CollectionViewSource.GetDefaultView(lvRimZone.ItemsSource)).Filter = RimDataFilter;
            ((CollectionView)CollectionViewSource.GetDefaultView(lvRimEncounter.ItemsSource)).Filter = RimDataFilter;

            // Set up RIM filter
            var view = (CollectionView)CollectionViewSource.GetDefaultView(lvOff.ItemsSource);
            if (view != null) view.Filter = HandleListFilter;

            // Set up Area ID filter
            view = (CollectionView)CollectionViewSource.GetDefaultView(lvAreaIds.ItemsSource);
            if (view != null) view.Filter = HandleAreaIdsFilter;
        }

        #endregion // END REGION Constructors

        #region ZoomAndPanControl Members

        /// <summary>
        /// Specifies the current state of the mouse handling logic.
        /// </summary>
        private MouseHandlingMode mouseHandlingMode = MouseHandlingMode.None;

        /// <summary>
        /// The point that was clicked relative to the ZoomAndPanControl.
        /// </summary>
        private Point origZoomAndPanControlMouseDownPoint;

        /// <summary>
        /// The point that was clicked relative to the content that is contained within the ZoomAndPanControl.
        /// </summary>
        private Point origContentMouseDownPoint;

        /// <summary>
        /// Records which mouse button clicked during mouse dragging.
        /// </summary>
        private MouseButton mouseButtonDown;

        /// <summary>
        /// Saves the previous zoom rectangle, pressing the backspace key jumps back to this zoom rectangle.
        /// </summary>
        private Rect prevZoomRect;

        /// <summary>
        /// Save the previous content scale, pressing the backspace key jumps back to this scale.
        /// </summary>
        private double prevZoomScale;

        /// <summary>
        /// Set to 'true' when the previous zoom rect is saved.
        /// </summary>
        private bool prevZoomRectSet = false;

        public bool TempSegmentVisible
        {
            get => _tempSegmentVisible;
            set => SetField(ref _tempSegmentVisible, value);
        }
        private bool _tempSegmentVisible = false;

        public Point TempSegmentStart
        {
            get => _tempSegmentStart;
            set => SetField(ref _tempSegmentStart, value);
        }
        private Point _tempSegmentStart = new Point();

        public Point TempSegmentEnd
        {
            get => _tempSegmentEnd;
            set => SetField(ref _tempSegmentEnd, value);
        }
        private Point _tempSegmentEnd = new Point();

        public bool SegmentIsSticky
        {
            get => _segmentIsSticky;
            set => SetField(ref _segmentIsSticky, value);
        }
        private bool _segmentIsSticky = false;

        public double SegmentStickyDistance
        {
            get => _segmentStickyDistance;
            set => SetField(ref _segmentStickyDistance, value);
        }
        private double _segmentStickyDistance = 1.0;

        public bool BluePathVisible
        {
            get => _bluePathVisible;
            set => SetField(ref _bluePathVisible, value);
        }
        private bool _bluePathVisible = false;

        public bool RedPathVisible
        {
            get => _redPathVisible;
            set => SetField(ref _redPathVisible, value);
        }
        private bool _redPathVisible = false;

        public bool GreenPathVisible
        {
            get => _greenPathVisible;
            set => SetField(ref _greenPathVisible, value);
        }
        private bool _greenPathVisible = false;

        public List<Tuple<Point,Point>> BluePathPointPairs = new List<Tuple<Point,Point>>();
        public List<Tuple<Point,Point>> RedPathPointPairs = new List<Tuple<Point,Point>>();
        public List<Tuple<Point,Point>> GreenPathPointPairs = new List<Tuple<Point,Point>>();

        #endregion // END REGION ZoomAndPanControl Members

        #region KIO Members

        /// <summary> Lookup from RIM filename to the canvas containing its walkmesh. </summary>
        private Dictionary<string, Canvas> RimFullCanvasLookup { get; set; } = new Dictionary<string, Canvas>();
        private Dictionary<string, Canvas> RimWalkableCanvasLookup { get; set; } = new Dictionary<string, Canvas>();
        private Dictionary<string, Canvas> RimNonwalkableCanvasLookup { get; set; } = new Dictionary<string, Canvas>();
        private Dictionary<string, Canvas> RimTransAbortPointCanvasLookup { get; set; } = new Dictionary<string, Canvas>();
        private Dictionary<string, Canvas> RimTransAbortRegionCanvasLookup { get; set; } = new Dictionary<string, Canvas>();
        private Dictionary<string, Canvas> RimDefaultSpawnPointCanvasLookup { get; set; } = new Dictionary<string, Canvas>();
        private Dictionary<string, Canvas> RimDataCanvasLookup { get; set; } = new Dictionary<string, Canvas>();

        /// <summary> Lookup from a RIM filename to the WOKs it contains. </summary>
        private Dictionary<string, IEnumerable<WOK>> RimWoksLookup { get; set; } = new Dictionary<string, IEnumerable<WOK>>();

        /// <summary> Lookup from a Room name to the room's walkmesh. </summary>
        private Dictionary<string, WOK> RoomWoksLookup { get; set; } = new Dictionary<string, WOK>();

        /// <summary> Lookup from RIM filename to a more readable Common Name. </summary>
        private Dictionary<string, string> RimNamesLookup { get; set; } = new Dictionary<string, string>();

        /// <summary> Lookup from RIM filename to a collection of walkmesh face polygons. </summary>
        private Dictionary<string, IEnumerable<Polygon>> RimPolyLookup { get; set; } = new Dictionary<string, IEnumerable<Polygon>>();

        /// <summary> Lookup from RIM filename to a collection of unwalkable face polygons. </summary>
        private Dictionary<string, IEnumerable<Polygon>> RimOutlinePolyLookup { get; set; } = new Dictionary<string, IEnumerable<Polygon>>();

        /// <summary> Lookup from RIM filename to the associated GIT file. </summary>
        private Dictionary<string, GIT> RimGitLookup { get; set; } = new Dictionary<string, GIT>();

        /// <summary> Lookup from RIM filename to a collection of trans_abort points. </summary>
        private Dictionary<string, IEnumerable<Ellipse>> RimTransAborts { get; set; } = new Dictionary<string, IEnumerable<Ellipse>>();

        private Dictionary<string, IEnumerable<Point>> RimTransAbortPoints { get; set; } = new Dictionary<string, IEnumerable<Point>>();

        /// <summary> Lookup from RIM filename to a collection of trans_abort region polygons. </summary>
        private Dictionary<string, IEnumerable<Polygon>> RimTransRegions { get; set; } = new Dictionary<string, IEnumerable<Polygon>>();

        /// <summary> </summary>
        private Dictionary<string, Polygon> RimDefaultSpawnPoint { get; set; } = new Dictionary<string, Polygon>();

        /// <summary>
        /// Lookup of the brushes used to draw and how many meshes are currently using them.
        /// </summary>
        private Dictionary<Brush, int> PolyBrushCount { get; set; } = new Dictionary<Brush, int>();

        private BackgroundColor SelectedBackgroundColor { get; set; }

        private Dictionary<BackgroundColor, Brush> BackgroundColors { get; set; } = new Dictionary<BackgroundColor, Brush>
        {
            { BackgroundColor.White,     Brushes.White },
            { BackgroundColor.LightGray, Brushes.LightGray },
            { BackgroundColor.DarkGray,  Brushes.DimGray },
            { BackgroundColor.Black,     Brushes.Black },
        };

        private Dictionary<BackgroundColor, Brush> ForegroundColors { get; set; } = new Dictionary<BackgroundColor, Brush>
        {
            { BackgroundColor.White,     Brushes.Black },
            { BackgroundColor.LightGray, Brushes.Black },
            { BackgroundColor.DarkGray,  Brushes.White },
            { BackgroundColor.Black,     Brushes.White },
        };

        private Palette SelectedPalette { get; set; } = null;

        private Dictionary<Brush, string> BrushToName { get; set; } = new Dictionary<Brush, string>();

        private Brush CurrentRimDataInfoBrush { get; set; }

        private List<Brush> BrushCycle { get; set; }

        private int BrushIndex { get; set; } = 0;

        private Brush GrayScaleBrush { get; set; } = Brushes.White;

        private Brush TransAbortBorderBrush { get; set; } = Brushes.White;

        private Dictionary<string, Brush> RimToBrushUsed { get; set; } = new Dictionary<string, Brush>();

        private List<string> RimPolysCreated { get; set; } = new List<string>();

        public BackgroundWorker GameDataWorker { get; set; } = new BackgroundWorker();
        public BackgroundWorker AddPolyWorker { get; set; } = new BackgroundWorker();
        public BackgroundWorker UpdateLayerVisibilityWorker { get; set; } = new BackgroundWorker();
        public BackgroundWorker RemovePolyWorker { get; set; } = new BackgroundWorker();
        public BackgroundWorker LivePositionWorker { get; set; } = new BackgroundWorker();
        public BackgroundWorker MouseHoverWorker { get; set; } = new BackgroundWorker();

        public bool K1Loaded { get; set; }
        public bool K2Loaded { get; set; }
        public XmlGame CurrentGame { get; set; }

        private readonly Point BaseOffset = new Point(20, 20);
        public const string DEFAULT = "N/A";
        public const string LOADING = "Loading";
        public const string K1_NAME = "KotOR 1";
        public const string K2_NAME = "KotOR 2";
        private const string K1_STEAM_DEFAULT_PATH = @"C:\Program Files (x86)\Steam\steamapps\common\swkotor";
        private const string K2_STEAM_DEFAULT_PATH = @"C:\Program Files (x86)\Steam\steamapps\common\Knights of the Old Republic II";
        private const string K1_GOG_DEFAULT_PATH = @"C:\GOG Games\Star Wars - KotOR";
        private const string K2_GOG_DEFAULT_PATH = @"C:\GOG Games\Star Wars - KotOR2";
        private KPaths Paths;

        #endregion // END REGION KIO Members

        #region DataBinding Members

        private double prevLeftPanelSize = 304.0;
        private double prevRightPanelSize = 315.0;

        public string Game { get; private set; }

        public string WindowTitle
        {
            get
            {
                var v = System.Reflection.Assembly.GetAssembly(typeof(MainWindow)).GetName().Version;
                return $"KotOR Walkmesh Visualizer (v{v.Major}.{v.Minor}.{v.Build})";
            }
        }

        public bool ShowCoordinatePanel
        {
            get => _showCoordinatePanel;
            set => SetField(ref _showCoordinatePanel, value);
        }
        private bool _showCoordinatePanel = false;

        public bool ShowRimDataPanel
        {
            get => _showRimDataPanel;
            set => SetField(ref _showRimDataPanel, value);
        }
        private bool _showRimDataPanel = true;

        public bool ShowDistancePanel
        {
            get => _showDistancePanel;
            set => SetField(ref _showDistancePanel, value);
        }
        private bool _showDistancePanel = false;

        public bool ShowToolsPanel
        {
            get => _showToolsPanel;
            set => SetField(ref _showToolsPanel, value);
        }
        private bool _showToolsPanel = false;

        public bool ShowWireTargetPanel
        {
            get => _showWireTargetPanel;
            set => SetField(ref _showWireTargetPanel, value);
        }
        private bool _showWireTargetPanel = false;

        public bool ShowRimDataDoors
        {
            get => _showRimDataDoors;
            set => SetField(ref _showRimDataDoors, value);
        }
        private bool _showRimDataDoors = true;

        public bool ShowRimDataTriggers
        {
            get => _showRimDataTriggers;
            set => SetField(ref _showRimDataTriggers, value);
        }
        private bool _showRimDataTriggers = true;

        public bool ShowRimDataTraps
        {
            get => _showRimDataTraps;
            set => SetField(ref _showRimDataTraps, value);
        }
        private bool _showRimDataTraps = false;

        public bool ShowRimDataZones
        {
            get => _showRimDataZones;
            set => SetField(ref _showRimDataZones, value);
        }
        private bool _showRimDataZones = false;

        public bool ShowRimDataEncounters
        {
            get => _showRimDataEncounters;
            set => SetField(ref _showRimDataEncounters, value);
        }
        private bool _showRimDataEncounters = false;

        public bool ShowWalkmeshPanel
        {
            get => _showWalkmeshPanel;
            set => SetField(ref _showWalkmeshPanel, value);
        }
        private bool _showWalkmeshPanel = true;

        private List<string> _allRimNames = new List<string>();
        public List<string> AllRimNames
        {
            get => _allRimNames;
            set => SetField(ref _allRimNames, value);
        }

        private ObservableCollection<RimModel> _onRims = new ObservableCollection<RimModel>();
        public ObservableCollection<RimModel> OnRims
        {
            get => _onRims;
            set => SetField(ref _onRims, value);
        }

        private ObservableCollection<RimModel> _offRims = new ObservableCollection<RimModel>();
        public ObservableCollection<RimModel> OffRims
        {
            get => _offRims;
            set => SetField(ref _offRims, value);
        }

        public RimDataSet RimDataSet { get; set; } = new RimDataSet();

        public RimModel RimDataFilter_SelectedItem
        {
            get => _rimDataFilter_SelectedItem;
            set => SetField(ref _rimDataFilter_SelectedItem, value);
        }
        private RimModel _rimDataFilter_SelectedItem = null;

        private ObservableCollection<RimDataInfo> _rimDoors = new ObservableCollection<RimDataInfo>();
        public ObservableCollection<RimDataInfo> RimDoors
        {
            get => _rimDoors;
            set => SetField(ref _rimDoors, value);
        }

        private int _visibleRimDoors = 0;
        public int VisibleRimDoors
        {
            get => _visibleRimDoors;
            set => SetField(ref _visibleRimDoors, value);
        }

        private int _hiddenRimDoors = 0;
        public int HiddenRimDoors
        {
            get => _hiddenRimDoors;
            set => SetField(ref _hiddenRimDoors, value);
        }

        private ObservableCollection<RimDataInfo> _rimTriggers = new ObservableCollection<RimDataInfo>();
        public ObservableCollection<RimDataInfo> RimTriggers
        {
            get => _rimTriggers;
            set => SetField(ref _rimTriggers, value);
        }

        private int _visibleRimTriggers = 0;
        public int VisibleRimTriggers
        {
            get => _visibleRimTriggers;
            set => SetField(ref _visibleRimTriggers, value);
        }

        private int _hiddenRimTriggers = 0;
        public int HiddenRimTriggers
        {
            get => _hiddenRimTriggers;
            set => SetField(ref _hiddenRimTriggers, value);
        }

        private ObservableCollection<RimDataInfo> _rimTraps = new ObservableCollection<RimDataInfo>();
        public ObservableCollection<RimDataInfo> RimTraps
        {
            get => _rimTraps;
            set => SetField(ref _rimTraps, value);
        }

        private int _visibleRimTraps = 0;
        public int VisibleRimTraps
        {
            get => _visibleRimTraps;
            set => SetField(ref _visibleRimTraps, value);
        }

        private int _hiddenRimTraps = 0;
        public int HiddenRimTraps
        {
            get => _hiddenRimTraps;
            set => SetField(ref _hiddenRimTraps, value);
        }

        private ObservableCollection<RimDataInfo> _rimZones = new ObservableCollection<RimDataInfo>();
        public ObservableCollection<RimDataInfo> RimZones
        {
            get => _rimZones;
            set => SetField(ref _rimZones, value);
        }

        private int _visibleRimZones = 0;
        public int VisibleRimZones
        {
            get => _visibleRimZones;
            set => SetField(ref _visibleRimZones, value);
        }

        private int _hiddenRimZones = 0;
        public int HiddenRimZones
        {
            get => _hiddenRimZones;
            set => SetField(ref _hiddenRimZones, value);
        }

        private ObservableCollection<RimDataInfo> _rimEncounters = new ObservableCollection<RimDataInfo>();
        public ObservableCollection<RimDataInfo> RimEncounters
        {
            get => _rimEncounters;
            set => SetField(ref _rimEncounters, value);
        }

        private int _visibleRimEncounters = 0;
        public int VisibleRimEncounters
        {
            get => _visibleRimEncounters;
            set => SetField(ref _visibleRimEncounters, value);
        }

        private int _hiddenRimEncounters = 0;
        public int HiddenRimEncounters
        {
            get => _hiddenRimEncounters;
            set => SetField(ref _hiddenRimEncounters, value);
        }

        private ObservableCollection<WalkabilityModel> _leftPointMatches = new ObservableCollection<WalkabilityModel>();
        public ObservableCollection<WalkabilityModel> LeftPointMatches
        {
            get => _leftPointMatches;
            set => SetField(ref _leftPointMatches, value);
        }

        private ObservableCollection<WalkabilityModel> _rightPointMatches = new ObservableCollection<WalkabilityModel>();
        public ObservableCollection<WalkabilityModel> RightPointMatches
        {
            get => _rightPointMatches;
            set => SetField(ref _rightPointMatches, value);
        }

        private ObservableCollection<WalkabilityModel> _bothPointMatches = new ObservableCollection<WalkabilityModel>();
        public ObservableCollection<WalkabilityModel> BothPointMatches
        {
            get => _bothPointMatches;
            set => SetField(ref _bothPointMatches, value);
        }

        private double _currentProgress;
        public double CurrentProgress
        {
            get => _currentProgress;
            set => SetField(ref _currentProgress, value);
        }

        private string _currentProgressStatus;
        public string CurrentProgressStatus
        {
            get => _currentProgressStatus;
            set => SetField(ref _currentProgressStatus, value);
        }

        public bool LeftOrRightClickPointVisible
        {
            get => _leftClickPointVisible || _rightClickPointVisible;
        }

        private bool _leftClickPointVisible = false;
        public bool LeftClickPointVisible
        {
            get => _leftClickPointVisible;
            set
            {
                SetField(ref _leftClickPointVisible, value);
                NotifyPropertyChanged(nameof(LeftOrRightClickPointVisible));
            }
        }

        private Point _leftClickPoint;
        public Point LeftClickPoint
        {
            get => _leftClickPoint;
            set => SetField(ref _leftClickPoint, value);
        }

        private Point _leftClickModuleCoords;
        public Point LeftClickModuleCoords
        {
            get => _leftClickModuleCoords;
            set => SetField(ref _leftClickModuleCoords, value);
        }

        private Point _lastLeftClickModuleCoords;
        public Point LastLeftClickModuleCoords
        {
            get => _lastLeftClickModuleCoords;
            set => SetField(ref _lastLeftClickModuleCoords, value);
        }

        private bool _showLeftClickPointCoordinates = true;
        public bool ShowLeftClickPointCoordinates
        {
            get => _showLeftClickPointCoordinates;
            set => SetField(ref _showLeftClickPointCoordinates, value);
        }

        private bool _rightClickPointVisible = false;
        public bool RightClickPointVisible
        {
            get => _rightClickPointVisible;
            set
            {
                SetField(ref _rightClickPointVisible, value);
                NotifyPropertyChanged(nameof(LeftOrRightClickPointVisible));
            }
        }

        private Point _rightClickPoint;
        public Point RightClickPoint
        {
            get => _rightClickPoint;
            set => SetField(ref _rightClickPoint, value);
        }

        private Point _rightClickModuleCoords;
        public Point RightClickModuleCoords
        {
            get => _rightClickModuleCoords;
            set => SetField(ref _rightClickModuleCoords, value);
        }

        private Point _lastRightClickModuleCoords;
        public Point LastRightClickModuleCoords
        {
            get => _lastRightClickModuleCoords;
            set => SetField(ref _lastRightClickModuleCoords, value);
        }

        private bool _showRightClickPointCoordinates = true;
        public bool ShowRightClickPointCoordinates
        {
            get => _showRightClickPointCoordinates;
            set => SetField(ref _showRightClickPointCoordinates, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetField(ref _isBusy, value);
        }

        public string SelectedGame
        {
            get { return (string)GetValue(SelectedGameProperty); }
            set { SetValue(SelectedGameProperty, value); }
        }

        public static readonly DependencyProperty SelectedGameProperty =
            DependencyProperty.Register(nameof(SelectedGame), typeof(string), typeof(VisualizerWindow), new PropertyMetadata(DEFAULT));

        public double _leftOffset;
        public double LeftOffset
        {
            get => _leftOffset;
            set => SetField(ref _leftOffset, value);
        }

        public double _bottomOffset;
        public double BottomOffset
        {
            get => _bottomOffset;
            set => SetField(ref _bottomOffset, value);
        }

        public bool ShowWalkableFaces
        {
            get => _showWalkableFaces;
            set => SetField(ref _showWalkableFaces, value);
        }
        private bool _showWalkableFaces = true;

        public bool ShowNonWalkableFaces
        {
            get => _showNonWalkableFaces;
            set => SetField(ref _showNonWalkableFaces, value);
        }
        private bool _showNonWalkableFaces = false;

        public bool ShowTransAbortPoints
        {
            get => _showTransAbortPoints;
            set => SetField(ref _showTransAbortPoints, value);
        }
        private bool _showTransAbortPoints = false;

        public bool ShowDefaultSpawnPoints
        {
            get => _showDefaultSpawnPoints;
            set => SetField(ref _showDefaultSpawnPoints, value);
        }
        private bool _showDefaultSpawnPoints = false;

        public bool ShowTransAbortRegions
        {
            get => _showTransAbortRegions;
            set => SetField(ref _showTransAbortRegions, value);
        }
        private bool _showTransAbortRegions = false;

        public RimModel SelectedGatherPartyRim
        {
            get => _selectedGatherPartyRim;
            set => SetField(ref _selectedGatherPartyRim, value);
        }
        private RimModel _selectedGatherPartyRim = null;

        public bool ShowDlzLines
        {
            get => _showDlzLines;
            set => SetField(ref _showDlzLines, value);
        }
        private bool _showDlzLines = false;

        public bool ShowLivePosition
        {
            get => _showLivePosition;
            set => SetField(ref _showLivePosition, value);
        }
        private bool _showLivePosition = false;

        public bool ShowLivePositionCoordinates
        {
            get => _showLivePositionCoordinates;
            set => SetField(ref _showLivePositionCoordinates, value);
        }
        private bool _showLivePositionCoordinates = false;

        public Point LivePositionPoint
        {
            get => _livePositionPoint;
            set => SetField(ref _livePositionPoint, value);
        }
        private Point _livePositionPoint = new Point();

        public Point LivePositionEllipsePoint
        {
            get => _livePositionEllipsePoint;
            set => SetField(ref _livePositionEllipsePoint, value);
        }
        private Point _livePositionEllipsePoint = new Point();

        public Point LivePositionEllipsePointPC1
        {
            get => _livePositionEllipsePointPC1;
            set => SetField(ref _livePositionEllipsePointPC1, value);
        }
        private Point _livePositionEllipsePointPC1 = new Point();

        public Point LivePositionEllipsePointPC2
        {
            get => _livePositionEllipsePointPC2;
            set => SetField(ref _livePositionEllipsePointPC2, value);
        }
        private Point _livePositionEllipsePointPC2 = new Point();

        public float LiveLeaderBearing
        {
            get => _liveLeaderBearing;
            set => SetField(ref _liveLeaderBearing, value);
        }
        private float _liveLeaderBearing = 0f;

        public uint LivePartyCount
        {
            get => _livePartyCount;
            set => SetField(ref _livePartyCount, value);
        }
        private uint _livePartyCount = 0;

        public float LiveBearingPC1
        {
            get => _liveBearingPC1;
            set => SetField(ref _liveBearingPC1, value);
        }
        private float _liveBearingPC1 = 0f;

        public float LiveBearingPC2
        {
            get => _liveBearingPC2;
            set => SetField(ref _liveBearingPC2, value);
        }
        private float _liveBearingPC2 = 0f;

        public bool HidePreviousLiveModule
        {
            get => _hidePreviousLiveModule;
            set => SetField(ref _hidePreviousLiveModule, value);
        }
        private bool _hidePreviousLiveModule = false;

        public bool ShowCurrentLiveModule
        {
            get => _showCurrentLiveModule;
            set => SetField(ref _showCurrentLiveModule, value);
        }
        private bool _showCurrentLiveModule = false;

        public bool HotswapToLiveGame
        {
            get => _hotswapToLiveGame;
            set => SetField(ref _hotswapToLiveGame, value);
        }
        private bool _hotswapToLiveGame = false;

        public bool ViewFollowsLivePosition
        {
            get => _viewFollowsLivePosition;
            set => SetField(ref _viewFollowsLivePosition, value);
        }
        private bool _viewFollowsLivePosition = true;

        public int LivePositionUpdateDelay
        {
            get => _livePositionUpdateDelay;
            set => SetField(ref _livePositionUpdateDelay, value);
        }
        private int _livePositionUpdateDelay = 50;

        public Point LiveGatherPartyRangePoint
        {
            get => _liveGatherPartyRangePoint;
            set => SetField(ref _liveGatherPartyRangePoint, value);
        }
        private Point _liveGatherPartyRangePoint = new Point();

        //public Point LastGatherPartyRangePosition
        //{
        //    get => _lastGatherPartyRangePosition;
        //    set => SetField(ref _lastGatherPartyRangePosition, value);
        //}
        //private Point _lastGatherPartyRangePosition = new Point();

        public Point3D LastGatherPartyRangePosition
        {
            get => _lastGatherPartyRangePosition;
            set => SetField(ref _lastGatherPartyRangePosition, value);
        }
        private Point3D _lastGatherPartyRangePosition = new Point3D();

        public Point LastGatherPartyRangePoint
        {
            get => _lastGatherPartyRangePoint;
            set => SetField(ref _lastGatherPartyRangePoint, value);
        }
        private Point _lastGatherPartyRangePoint = new Point();

        public bool ShowGatherPartyRange
        {
            get => _showGatherPartyRange;
            set => SetField(ref _showGatherPartyRange, value);
        }
        private bool _showGatherPartyRange = false;

        public bool ShowLeftClickGatherPartyRange
        {
            get => _showLeftClickGatherPartyRange;
            set => SetField(ref _showLeftClickGatherPartyRange, value);
        }
        private bool _showLeftClickGatherPartyRange = false;

        public bool LockGatherPartyRange
        {
            get => _lockGatherPartyRange;
            set => SetField(ref _lockGatherPartyRange, value);
        }
        private bool _lockGatherPartyRange = false;

        public Brush LiveGatherPartyRangeFillBrush
        {
            get => _liveGatherPartyRangeFillBrush;
            set => SetField(ref _liveGatherPartyRangeFillBrush, value);
        }
        private Brush _liveGatherPartyRangeFillBrush = Brushes.Green;

        public Brush LiveGatherPartyRangeStrokeBrush
        {
            get => _liveGatherPartyRangeStrokeBrush;
            set => SetField(ref _liveGatherPartyRangeStrokeBrush, value);
        }
        private Brush _liveGatherPartyRangeStrokeBrush = Brushes.Green;

        public Brush LeftClickGatherPartyRangeFillBrush
        {
            get => _leftClickGatherPartyRangeFillBrush;
            set => SetField(ref _leftClickGatherPartyRangeFillBrush, value);
        }
        private Brush _leftClickGatherPartyRangeFillBrush = Brushes.Green;

        public Brush LeftClickGatherPartyRangeStrokeBrush
        {
            get => _leftClickGatherPartyRangeStrokeBrush;
            set => SetField(ref _leftClickGatherPartyRangeStrokeBrush, value);
        }
        private Brush _leftClickGatherPartyRangeStrokeBrush = Brushes.Green;

        private SolidColorBrush gprStrokeGreen = new SolidColorBrush(new Color { R = 0x00, G = 0x80, B = 0x00, A = 0xFF });
        private SolidColorBrush gprStrokeRed   = new SolidColorBrush(new Color { R = 0x80, G = 0x00, B = 0x00, A = 0xFF });
        private SolidColorBrush gprFillGreen   = new SolidColorBrush(new Color { R = 0x00, G = 0x80, B = 0x00, A = 0x22 });
        private SolidColorBrush gprFillRed     = new SolidColorBrush(new Color { R = 0x80, G = 0x00, B = 0x00, A = 0x22 });

        public bool DoRimDataFilter
        {
            get => _doRimDataFilter;
            set => SetField(ref _doRimDataFilter, value);
        }
        private bool _doRimDataFilter = false;

        public bool ShowDoorsOnAddRim
        {
            get => _showDoorsOnAddRim;
            set => SetField(ref _showDoorsOnAddRim, value);
        }
        private bool _showDoorsOnAddRim = true;

        public bool ShowTriggersOnAddRim
        {
            get => _showTriggersOnAddRim;
            set => SetField(ref _showTriggersOnAddRim, value);
        }
        private bool _showTriggersOnAddRim = true;

        public bool ShowTrapsOnAddRim
        {
            get => _showTrapsOnAddRim;
            set => SetField(ref _showTrapsOnAddRim, value);
        }
        private bool _showTrapsOnAddRim = false;

        public bool ShowZonesOnAddRim
        {
            get => _showZonesOnAddRim;
            set => SetField(ref _showZonesOnAddRim, value);
        }
        private bool _showZonesOnAddRim = false;

        public bool ShowEncountersOnAddRim
        {
            get => _showEncountersOnAddRim;
            set => SetField(ref _showEncountersOnAddRim, value);
        }
        private bool _showEncountersOnAddRim = true;

        public bool ShowRimDataDoorsDlzLines
        {
            get => _showRimDataDoorsDlzLines;
            set => SetField(ref _showRimDataDoorsDlzLines, value);
        }
        private bool _showRimDataDoorsDlzLines = true;

        public bool ShowRimDataTriggersDlzLines
        {
            get => _showRimDataTriggersDlzLines;
            set => SetField(ref _showRimDataTriggersDlzLines, value);
        }
        private bool _showRimDataTriggersDlzLines = true;

        public bool ShowRimDataTrapsDlzLines
        {
            get => _showRimDataTrapsDlzLines;
            set => SetField(ref _showRimDataTrapsDlzLines, value);
        }
        private bool _showRimDataTrapsDlzLines = true;

        public bool ShowRimDataZonesDlzLines
        {
            get => _showRimDataZonesDlzLines;
            set => SetField(ref _showRimDataZonesDlzLines, value);
        }
        private bool _showRimDataZonesDlzLines = true;

        public bool ShowRimDataEncountersDlzLines
        {
            get => _showRimDataEncountersDlzLines;
            set => SetField(ref _showRimDataEncountersDlzLines, value);
        }
        private bool _showRimDataEncountersDlzLines = true;

        public Brush CoordinateTextBrush
        {
            get => _coordinateTextBrush;
            set => SetField(ref _coordinateTextBrush, value);
        }
        private Brush _coordinateTextBrush = Brushes.Black;

        public bool ShowRimDataUnderMouse
        {
            get => _showRimDataUnderMouse;
            set => SetField(ref _showRimDataUnderMouse, value);
        }
        private bool _showRimDataUnderMouse = false;

        public bool ShowTypeUnderMouse
        {
            get => _showTypeUnderMouse;
            set => SetField(ref _showTypeUnderMouse, value);
        }
        private bool _showTypeUnderMouse = true;

        public bool ShowResRefUnderMouse
        {
            get => _showResRefUnderMouse;
            set => SetField(ref _showResRefUnderMouse, value);
        }
        private bool _showResRefUnderMouse = true;

        public bool ShowTagUnderMouse
        {
            get => _showTagUnderMouse;
            set => SetField(ref _showTagUnderMouse, value);
        }
        private bool _showTagUnderMouse = true;

        public bool ShowLocalizedNameUnderMouse
        {
            get => _showLocalizedNameUnderMouse;
            set => SetField(ref _showLocalizedNameUnderMouse, value);
        }
        private bool _showLocalizedNameUnderMouse = true;

        public bool ShowOnEnterUnderMouse
        {
            get => _showOnEnterUnderMouse;
            set => SetField(ref _showOnEnterUnderMouse, value);
        }
        private bool _showOnEnterUnderMouse = true;

        public Point CurrentMousePosition
        {
            get => _currentMousePosition;
            set => SetField(ref _currentMousePosition, value);
        }
        private Point _currentMousePosition = new Point();

        public List<RimDataInfo> RimDataUnderMouse
        {
            get => _rimDataUnderMouse;
            set => SetField(ref _rimDataUnderMouse, value);
        }
        private List<RimDataInfo> _rimDataUnderMouse = new List<RimDataInfo>();

        public int MouseHoverUpdateDelay
        {
            get => _mouseHoverUpdateDelay;
            set => SetField(ref _mouseHoverUpdateDelay, value);
        }
        private int _mouseHoverUpdateDelay = 50;

        public Point TeleportToCoordinates
        {
            get => _teleportToCoordinates;
            set => SetField(ref _teleportToCoordinates, value);
        }
        private Point _teleportToCoordinates = new Point(0.0, 0.0);

        public float FreeCamSpeed
        {
            get => _freeCamSpeed;
            set => SetField(ref _freeCamSpeed, value);
        }
        private float _freeCamSpeed = 10f;

        public const float DEFAULT_MOVEMENT_SPEED = 5.4f;
        public string MoveSpeedMultiplier
        {
            get => _moveSpeedMultiplier;
            set => SetField(ref _moveSpeedMultiplier, value);
        }
        private string _moveSpeedMultiplier = "1.0";

        public bool SetMoveSpeedOnLoad
        {
            get => _setMoveSpeedOnLoad;
            set => SetField(ref _setMoveSpeedOnLoad, value);
        }
        private bool _setMoveSpeedOnLoad = false;

        #region Distance

        const double SPEED_UNITS_PER_SECOND = 5.4;

        /// <summary>
        /// Multiplier used to calulate for different movement speeds.
        /// </summary>
        public double DistanceToTimeMultiplier
        {
            get => _distanceToTimeMultiplier;
            set => SetField(ref _distanceToTimeMultiplier, value);
        }
        private double _distanceToTimeMultiplier = 1.0;

        /// <summary>
        /// Distance between left click point and right click point.
        /// </summary>
        public double DistanceLeftRight
        {
            get => _distanceLeftRight;
            set => SetField(ref _distanceLeftRight, value);
        }
        private double _distanceLeftRight = 0.0;

        public double DurationLeftRight
        {
            get => _durationLeftRight;
            set => SetField(ref _durationLeftRight, value);
        }
        private double _durationLeftRight = 0.0;

        /// <summary>
        /// Distance between live leader position and left click point.
        /// </summary>
        public double DistanceLiveLeft
        {
            get => _distanceLiveLeft;
            set => SetField(ref _distanceLiveLeft, value);
        }
        private double _distanceLiveLeft = 0.0;

        public double DurationLiveLeft
        {
            get => _durationLiveLeft;
            set => SetField(ref _durationLiveLeft, value);
        }
        private double _durationLiveLeft = 0.0;

        /// <summary>
        /// Distance between live leader position and right click point.
        /// </summary>
        public double DistanceLiveRight
        {
            get => _distanceLiveRight;
            set => SetField(ref _distanceLiveRight, value);
        }
        private double _distanceLiveRight = 0.0;

        public double DurationLiveRight
        {
            get => _durationLiveRight;
            set => SetField(ref _durationLiveRight, value);
        }
        private double _durationLiveRight = 0.0;

        /// <summary>
        /// Length of the temp segment.
        /// </summary>
        public double DistanceTempSegment
        {
            get => _distanceTempSegment;
            set => SetField(ref _distanceTempSegment, value);
        }
        private double _distanceTempSegment = 0.0;

        public double DurationTempSegment
        {
            get => _durationTempSegment;
            set => SetField(ref _durationTempSegment, value);
        }
        private double _durationTempSegment = 0.0;

        /// <summary>
        /// Length of the temp segment.
        /// </summary>
        public double DistanceBluePath
        {
            get => _distanceBluePath;
            set => SetField(ref _distanceBluePath, value);
        }
        private double _distanceBluePath = 0.0;

        public double DurationBluePath
        {
            get => _durationBluePath;
            set => SetField(ref _durationBluePath, value);
        }
        private double _durationBluePath = 0.0;

        /// <summary>
        /// Length of the temp segment.
        /// </summary>
        public double DistanceRedPath
        {
            get => _distanceRedPath;
            set => SetField(ref _distanceRedPath, value);
        }
        private double _distanceRedPath = 0.0;

        public double DurationRedPath
        {
            get => _durationRedPath;
            set => SetField(ref _durationRedPath, value);
        }
        private double _durationRedPath = 0.0;

        /// <summary>
        /// Length of the temp segment.
        /// </summary>
        public double DistanceGreenPath
        {
            get => _distanceGreenPath;
            set => SetField(ref _distanceGreenPath, value);
        }
        private double _distanceGreenPath = 0.0;

        public double DurationGreenPath
        {
            get => _durationGreenPath;
            set => SetField(ref _durationGreenPath, value);
        }
        private double _durationGreenPath = 0.0;

        #endregion

        #region Live Tools: Abilities

        private const string ALL_ABILITIES = "ALL";
        //private const string ALL_BAD_SPELLS = "Bad Spells";                 // Starts or Ends with XXX
        //private const string ALL_SPECIAL_ABILITIES = "Special Abilities";   // Starts with SPECIAL_ABILITY_
        //private const string ALL_ITEM_ABILITY = "Item Abilities";           // Starts with ITEM_ABILITY_
        //private const string ALL_MONSTER_ABILITY = "Monster Abilities";     // Starts with MONSTER_ABILITY_
        //private const string ALL_PLOT = "Plot";                             // Starts with PLOT_
        //private const string ALL_DROID_ITEM = "Droid Item";                 // Starts with DROID_ITEM_
        //private const string ALL_FORCE_POWERS = "Force Powers";             // Starts with FORCE_POWER_
        //private const string ALL_SABER_FORMS = "Saber Forms";               // Starts with FORM_SABER_
        //private const string ALL_FORCE_FORMS = "Force Forms";               // Starts with FORM_FORCE_
        //private const string ALL_WOOKIEE = "Wookiee Abilities";             // Starts with WOOKIEE_

        public int ValueInAttributeBox { get; set; } = 10;
        public int ValueInSkillBox { get; set; } = 0;

        // Kotor 1 MAX =   250,000
        // Kotor 2 MAX = 1,250,000
        public int ValueInExperienceBox { get; set; } = 1000;
        public int ValueInSetCreditsBox { get; set; } = 10000;

        public int ValueInAlignmentBox { get; set; } = 50;
        public int ValueInInfluenceBox { get; set; } = 50;

        // Classes
        private Dictionary<kmih.CLASSES, string> KotorClassDescriptionLookup = new Dictionary<kmih.CLASSES, string>()
        {
            { kmih.CLASSES.SOLDIER,           "Soldier" },
            { kmih.CLASSES.SCOUT,             "Scout" },
            { kmih.CLASSES.SCOUNDREL,         "Scoundrel" },
            { kmih.CLASSES.JEDI_GUARDIAN,     "Jedi Guardian" },
            { kmih.CLASSES.JEDI_CONSULAR,     "Jedi Consular" },
            { kmih.CLASSES.JEDI_SENTINEL,     "Jedi Sentinel" },
            { kmih.CLASSES.COMBAT_DROID,      "Combat Droid" },
            { kmih.CLASSES.EXPERT_DROID,      "Expert Droid" },
            { kmih.CLASSES.MINION,            "Minion" },
            { kmih.CLASSES.TECH_SPECIALIST,   "Tech Specialist" },
            { kmih.CLASSES.BOUNTY_HUNTER,     "Bounty Hunter" },
            { kmih.CLASSES.JEDI_WEAPONMASTER, "Jedi Weaponmaster" },
            { kmih.CLASSES.JEDI_MASTER,       "Jedi Master" },
            { kmih.CLASSES.JEDI_WATCHMAN,     "Jedi Watchman" },
            { kmih.CLASSES.SITH_MARAUDER,     "Sith Marauder" },
            { kmih.CLASSES.SITH_LORD,         "Sith Lord" },
            { kmih.CLASSES.SITH_ASSASSIN,     "Sith Assassin" },
        };

        private Dictionary<string, kmih.CLASSES> KotorClassLookup;

        public List<string> Kotor1Classes => Enum.GetValues(typeof(kmih.CLASSES)).Cast<kmih.CLASSES>()
            .Where(c => (byte)c < kmih.FIRST_KOTOR_2_CLASS).Select(c => KotorClassDescriptionLookup[c]).ToList();

        public List<string> Kotor2Classes => Enum.GetValues(typeof(kmih.CLASSES)).Cast<kmih.CLASSES>().Select(c => KotorClassDescriptionLookup[c]).ToList();

        public List<string> KotorClasses
        {
            get => _kotorClasses;
            set => SetField(ref _kotorClasses, value);
        }
        private List<string> _kotorClasses = new List<string>();

        // Attributes
        public List<string> KotorAttributes => new List<string> { ALL_ABILITIES }.Concat(Enum.GetNames(typeof(kmih.ATTRIBUTES))).ToList();

        // Skills
        public List<string> KotorSkills => new List<string> { ALL_ABILITIES }.Concat(Enum.GetNames(typeof(kmih.SKILLS))).ToList();

        // Feats
        public List<string> Kotor1Feats => new List<string> { ALL_ABILITIES }.Concat(Enum.GetValues(typeof(kmih.FEATS)).Cast<ushort>().ToList()
            .Where(f => f < kmih.FIRST_KOTOR_2_FEAT).Cast<kmih.FEATS>().Select(f => f.ToString())
            .Where(f => !f.StartsWith("XXX") && !f.EndsWith("XXX")).OrderBy(f => f)).ToList();

        public List<string> Kotor2Feats => new List<string> { ALL_ABILITIES }.Concat(Enum.GetNames(typeof(kmih.FEATS))
            .Where(f => !f.StartsWith("XXX") && !f.EndsWith("XXX")).OrderBy(f => f)).ToList();

        public List<string> KotorFeats
        {
            get => _kotorFeats;
            set => SetField(ref _kotorFeats, value);
        }
        private List<string> _kotorFeats = [];

        // Powers
        public List<string> Kotor1Powers => new List<string> { ALL_ABILITIES }.Concat(Enum.GetValues(typeof(kmih.SPELLS)).Cast<int>().ToList()
            .Where(p => p < kmih.FIRST_KOTOR_2_SPELL).Cast<kmih.SPELLS>().Select(p => p.ToString())
            .Where(p => !p.StartsWith("XXX") && !p.EndsWith("XXX")).OrderBy(p => p)).ToList();

        public List<string> Kotor2Powers => new List<string> { ALL_ABILITIES }.Concat(Enum.GetNames(typeof(kmih.SPELLS))
            .Where(p => !p.StartsWith("XXX") && !p.EndsWith("XXX")).OrderBy(p => p)).ToList();

        public List<string> KotorPowers
        {
            get => _kotorPowers;
            set => SetField(ref _kotorPowers, value);
        }
        private List<string> _kotorPowers = [];

        // K2 Party Members
        public static List<string> Kotor2PartyMembers => [ALL_ABILITIES, .. Enum.GetNames(typeof(kmih.PARTY_NPCS_K2)).OrderBy(p => p)];

        public List<string> KotorPartyMembers
        {
            get => _kotorPartyMembers;
            set => SetField(ref _kotorPartyMembers, value);
        }
        private List<string> _kotorPartyMembers = [];

        #endregion // ENDREGION Live Tools: Abilities

        #region Live Tools: Rendering

        public int ValueInGuiAlphaBox { get; set; } = 100;
        public int ValueInTriggerRedBox { get; set; } = 100;
        public int ValueInTriggerGreenBox { get; set; } = 100;
        public int ValueInTriggerBlueBox { get; set; } = 0;
        public int ValueInTriggerAlphaBox { get; set; } = 35;

        #endregion

        #region Wire Targeting

        public bool UpdateLookingAtId
        {
            get => _updateLookingAtId;
            set => SetField(ref _updateLookingAtId, value);
        }
        private bool _updateLookingAtId = false;

        public ObservableCollection<KotorGameObject> AreaGameObjects
        {
            get => _areaGameObjects;
            set => SetField(ref _areaGameObjects, value);
        }
        private ObservableCollection<KotorGameObject> _areaGameObjects = [];

        #endregion // Wire Targeting

        #endregion // ENDREGION DataBinding Members

        #region ZoomAndPanControl

        /// <summary>
        /// Event raised on mouse down in the ZoomAndPanControl.
        /// </summary>
        private void zoomAndPanControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            content.Focus();
            Keyboard.Focus(content);

            mouseButtonDown = e.ChangedButton;
            origZoomAndPanControlMouseDownPoint = e.GetPosition(zoomAndPanControl);
            origContentMouseDownPoint = e.GetPosition(content);

            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0 &&
                (e.ChangedButton == MouseButton.Left ||
                 e.ChangedButton == MouseButton.Right))
            {
                // Shift + left- or right-down initiates zooming mode.
                mouseHandlingMode = MouseHandlingMode.Zooming;
            }
            else if (mouseButtonDown == MouseButton.Left)
            {
                // Just a plain old left-down initiates panning mode.
                // If following live position, ignore mouse panning.
                if (!ShowLivePosition || !ViewFollowsLivePosition)
                    mouseHandlingMode = MouseHandlingMode.Panning;
            }
            else if (mouseButtonDown == MouseButton.Right)
            {
                // Bring segment to front.
                Application.Current.Dispatcher.Invoke(() =>
                {
                    content.Children.Remove(tempSegment);
                    _ = content.Children.Add(tempSegment);
                });

                mouseHandlingMode = MouseHandlingMode.DrawingLine;

                if (SegmentIsSticky)
                {
                    var pairs = BluePathPointPairs
                        .Concat(RedPathPointPairs)
                        .Concat(GreenPathPointPairs);
                    var match = pairs.FirstOrDefault(t => (t.Item1 - origContentMouseDownPoint).Length <= SegmentStickyDistance)?.Item1
                    ?? pairs.FirstOrDefault(t => (t.Item2 - origContentMouseDownPoint).Length <= SegmentStickyDistance)?.Item2;
                    if (match != null) origContentMouseDownPoint = match.Value;
                }

                TempSegmentStart = origContentMouseDownPoint;
                TempSegmentEnd = origContentMouseDownPoint;
                TempSegmentVisible = true;
                DistanceTempSegment = 0.0;
                DurationTempSegment = 0.0;
            }

            if (mouseHandlingMode != MouseHandlingMode.None)
            {
                // Capture the mouse so that we eventually receive the mouse up event.
                zoomAndPanControl.CaptureMouse();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Event raised on mouse up in the ZoomAndPanControl.
        /// </summary>
        private void zoomAndPanControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (mouseHandlingMode != MouseHandlingMode.None)
            {
                if (mouseHandlingMode == MouseHandlingMode.DragZooming)
                {
                    //
                    // When drag-zooming has finished we zoom in on the rectangle that was highlighted
                    // by the user.
                    //
                    ApplyDragZoomRect();
                }
                if (mouseHandlingMode == MouseHandlingMode.DrawingLine)
                {
                    if (TempSegmentStart == TempSegmentEnd)
                    {
                        TempSegmentVisible = false;
                    }
                    else if (SegmentIsSticky)
                    {
                        var pairs = BluePathPointPairs
                            .Concat(RedPathPointPairs)
                            .Concat(GreenPathPointPairs);
                        var match = pairs.FirstOrDefault(t => t.Item1 != TempSegmentStart && (t.Item1 - TempSegmentEnd).Length <= SegmentStickyDistance)?.Item1
                            ?? pairs.FirstOrDefault(t => t.Item2 != TempSegmentStart && (t.Item2 - TempSegmentEnd).Length <= SegmentStickyDistance)?.Item2;
                        if (match != null)
                        {
                            mouseHandlingMode = MouseHandlingMode.None;
                            TempSegmentEnd = match.Value;
                            DistanceTempSegment = (TempSegmentEnd - TempSegmentStart).Length;
                            DurationTempSegment = DistanceTempSegment / SPEED_UNITS_PER_SECOND / DistanceToTimeMultiplier;
                        }
                    }
                }

                zoomAndPanControl.ReleaseMouseCapture();
                mouseHandlingMode = MouseHandlingMode.None;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Event raised on mouse move in the ZoomAndPanControl.
        /// </summary>
        private void zoomAndPanControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseHandlingMode == MouseHandlingMode.DrawingLine)
            {
                TempSegmentEnd = e.GetPosition(content);
                DistanceTempSegment = (TempSegmentEnd - TempSegmentStart).Length;
                DurationTempSegment = DistanceTempSegment / SPEED_UNITS_PER_SECOND / DistanceToTimeMultiplier;
                e.Handled = true;
            }
            if (mouseHandlingMode == MouseHandlingMode.Panning)
            {
                //
                // The user is left-dragging the mouse.
                // Pan the viewport by the appropriate amount.
                //
                Point curContentMousePoint = e.GetPosition(content);
                Vector dragOffset = curContentMousePoint - origContentMouseDownPoint;

                zoomAndPanControl.ContentOffsetX -= dragOffset.X;
                zoomAndPanControl.ContentOffsetY -= dragOffset.Y;

                e.Handled = true;
            }
            else if (mouseHandlingMode == MouseHandlingMode.Zooming)
            {
                Point curZoomAndPanControlMousePoint = e.GetPosition(zoomAndPanControl);
                Vector dragOffset = curZoomAndPanControlMousePoint - origZoomAndPanControlMouseDownPoint;
                double dragThreshold = 10;
                if (mouseButtonDown == MouseButton.Left &&
                    (Math.Abs(dragOffset.X) > dragThreshold ||
                     Math.Abs(dragOffset.Y) > dragThreshold))
                {
                    //
                    // When Shift + left-down zooming mode and the user drags beyond the drag threshold,
                    // initiate drag zooming mode where the user can drag out a rectangle to select the
                    // area to zoom in on.
                    //
                    mouseHandlingMode = MouseHandlingMode.DragZooming;
                    Point curContentMousePoint = e.GetPosition(content);
                    InitDragZoomRect(origContentMouseDownPoint, curContentMousePoint);
                }

                e.Handled = true;
            }
            else if (mouseHandlingMode == MouseHandlingMode.DragZooming)
            {
                //
                // When in drag zooming mode continously update the position of the rectangle
                // that the user is dragging out.
                //
                Point curContentMousePoint = e.GetPosition(content);
                SetDragZoomRect(origContentMouseDownPoint, curContentMousePoint);

                e.Handled = true;
            }
        }

        /// <summary>
        /// Event raised by rotating the mouse wheel
        /// </summary>
        private void zoomAndPanControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            if (e.Delta > 0)
            {
                Point curContentMousePoint = e.GetPosition(content);
                ZoomIn(curContentMousePoint);
            }
            else if (e.Delta < 0)
            {
                Point curContentMousePoint = e.GetPosition(content);
                ZoomOut(curContentMousePoint);
            }
        }

        /// <summary>
        /// Event raised when the user has double clicked in the zoom and pan control.
        /// </summary>
        private void zoomAndPanControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    HideLeftClickPoint();
                    ClearLeftPointMatches();
                }
                else if (e.ChangedButton == MouseButton.Right)
                {
                    HideRightClickPoint();
                    ClearRightPointMatches();
                }
            }
            else
            {
                // Set left or right point.
                if (e.ChangedButton == MouseButton.Left)
                {
                    HandleLeftDoubleClick(e.GetPosition(content));
                    // Last coords not updated so coordinate matches are still accurate.
                    //LastLeftClickModuleCoords = LeftClickModuleCoords;
                }
                else if (e.ChangedButton == MouseButton.Right)
                {
                    HandleRightDoubleClick(e.GetPosition(content));
                    // Last coords not updated so coordinate matches are still accurate.
                    //LastRightClickModuleCoords = RightClickModuleCoords;
                }
            }

            CalculatePointDistance();
            UpdatePointMatchRows();
        }

        private void HideLeftClickPoint()
        {
            LeftClickPointVisible = false;
        }

        private void HideRightClickPoint()
        {
            RightClickPointVisible = false;
        }

        private void HideBothPoints()
        {
            HideLeftClickPoint();
            HideRightClickPoint();
        }

        private void ClearLeftPointMatches()
        {
            LeftPointMatches.Clear();
            BothPointMatches.Clear();
            LastLeftClickModuleCoords = new Point();
        }

        private void ClearRightPointMatches()
        {
            RightPointMatches.Clear();
            BothPointMatches.Clear();
            LastRightClickModuleCoords = new Point();
        }

        private void ClearBothPointMatches()
        {
            LeftPointMatches.Clear();
            RightPointMatches.Clear();
            BothPointMatches.Clear();
            LastLeftClickModuleCoords = new Point();
            LastRightClickModuleCoords = new Point();
        }

        private void UpdatePointMatchRows()
        {
            if (LeftClickPointVisible && RightClickPointVisible)
            {
                rowLeftMatches.Height = new GridLength(1, GridUnitType.Star);
                rowRightMatches.Height = new GridLength(1, GridUnitType.Star);
                rowBothMatches.Height = new GridLength(1, GridUnitType.Star);
            }
            else if (LeftClickPointVisible)
            {
                rowLeftMatches.Height = new GridLength(1, GridUnitType.Star);
                rowRightMatches.Height = new GridLength(1, GridUnitType.Auto);
                rowBothMatches.Height = new GridLength(1, GridUnitType.Auto);
            }
            else if (RightClickPointVisible)
            {
                rowLeftMatches.Height = new GridLength(1, GridUnitType.Auto);
                rowRightMatches.Height = new GridLength(1, GridUnitType.Star);
                rowBothMatches.Height = new GridLength(1, GridUnitType.Auto);
            }
        }

        /// <summary>
        /// Handle left double click event.
        /// </summary>
        private void HandleLeftDoubleClick(Point p)
        {
            // Adjust position to match center of ellipse.
            p.Y = theGrid.Height - p.Y - .5;
            p.X -= .5;
            LeftClickPoint = p;

            // Adjust to module coordinate space: PointPosition - (Offset - EllipseRadius)
            p.X -= LeftOffset - .5;
            p.Y -= BottomOffset - .5;
            LeftClickModuleCoords = p;

            LeftClickPointVisible = true;
            BringLeftPointToTop();
        }

        /// <summary>
        /// Handle right double click event.
        /// </summary>
        private void HandleRightDoubleClick(Point p)
        {
            // Adjust position to match center of ellipse.
            p.Y = theGrid.Height - p.Y - .5;
            p.X -= .5;
            RightClickPoint = p;

            // Adjust to module coordinate space: PointPosition - (Offset - EllipseRadius)
            p.X -= LeftOffset - .5;
            p.Y -= BottomOffset - .5;
            RightClickModuleCoords = p;

            RightClickPointVisible = true;
            BringRightPointToTop();
        }

        private void CalculatePointDistance()
        {
            if (!LeftClickPointVisible) return;
            var distance = (LeftClickModuleCoords - RightClickModuleCoords).Length;
            if (RightClickPointVisible)
            {
                DistanceLeftRight = distance;
                DurationLeftRight = DistanceLeftRight / SPEED_UNITS_PER_SECOND / DistanceToTimeMultiplier;
            }

            // If right click point is not visible OR if in range...
            if (!RightClickPointVisible || distance <= 30.0)
            {
                LeftClickGatherPartyRangeFillBrush = gprFillGreen;
                LeftClickGatherPartyRangeStrokeBrush = gprStrokeGreen;
            }
            else
            {
                LeftClickGatherPartyRangeFillBrush = gprFillRed;
                LeftClickGatherPartyRangeStrokeBrush = gprStrokeRed;
            }
        }

        private void BringLeftPointToTop()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                content.Children.Remove(leftClickGatherPartyRange);
                _ = content.Children.Add(leftClickGatherPartyRange);

                content.Children.Remove(leftClickEllipse);
                _ = content.Children.Add(leftClickEllipse);

                content.Children.Remove(leftClickCoords);
                _ = content.Children.Add(leftClickCoords);
            });
        }

        private void BringRightPointToTop()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                content.Children.Remove(rightClickEllipse);
                _ = content.Children.Add(rightClickEllipse);

                content.Children.Remove(rightClickCoords);
                _ = content.Children.Add(rightClickCoords);
            });
        }

        private void BringLivePositionPointToTop()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                content.Children.Remove(liveGatherPartyRange);
                _ = content.Children.Add(liveGatherPartyRange);

                content.Children.Remove(lastGatherPartyRangePosition);
                _ = content.Children.Add(lastGatherPartyRangePosition);

                content.Children.Remove(livePositionArrowPC2);
                _ = content.Children.Add(livePositionArrowPC2);

                content.Children.Remove(livePositionArrowPC1);
                _ = content.Children.Add(livePositionArrowPC1);

                content.Children.Remove(livePositionArrow);
                _ = content.Children.Add(livePositionArrow);

                content.Children.Remove(livePositionCoords);
                _ = content.Children.Add(livePositionCoords);
            });
        }

        /// <summary>
        /// The 'ZoomIn' command (bound to the plus key) was executed.
        /// </summary>
        private void ZoomIn_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ZoomIn(new Point(zoomAndPanControl.ContentZoomFocusX, zoomAndPanControl.ContentZoomFocusY));
        }

        /// <summary>
        /// The 'ZoomOut' command (bound to the minus key) was executed.
        /// </summary>
        private void ZoomOut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ZoomOut(new Point(zoomAndPanControl.ContentZoomFocusX, zoomAndPanControl.ContentZoomFocusY));
        }

        /// <summary>
        /// The 'JumpBackToPrevZoom' command was executed.
        /// </summary>
        private void JumpBackToPrevZoom_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            JumpBackToPrevZoom();
        }

        /// <summary>
        /// Determines whether the 'JumpBackToPrevZoom' command can be executed.
        /// </summary>
        private void JumpBackToPrevZoom_CanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = prevZoomRectSet;
        }

        /// <summary>
        /// The 'Fill' command was executed.
        /// </summary>
        private void Fill_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SavePrevZoomRect();

            zoomAndPanControl.AnimatedScaleToFit();
        }

        /// <summary>
        /// The 'OneHundredPercent' command was executed.
        /// </summary>
        private void OneHundredPercent_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SavePrevZoomRect();

            zoomAndPanControl.AnimatedZoomTo(3.0);
        }

        /// <summary>
        /// The 'FifteenHundredPercent' command was executed.
        /// </summary>
        private void FifteenHundredPercent_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SavePrevZoomRect();

            zoomAndPanControl.AnimatedZoomTo(15.0);
        }

        /// <summary>
        /// Jump back to the previous zoom level.
        /// </summary>
        private void JumpBackToPrevZoom()
        {
            zoomAndPanControl.AnimatedZoomTo(prevZoomScale, prevZoomRect);

            ClearPrevZoomRect();
        }

        /// <summary>
        /// Zoom the viewport out, centering on the specified point (in content coordinates).
        /// </summary>
        private void ZoomOut(Point contentZoomCenter)
        {
            var delta = zoomAndPanControl.ContentScale > 5 ? 0.2 : 0.1;
            zoomAndPanControl.ZoomAboutPoint(zoomAndPanControl.ContentScale - delta, contentZoomCenter);
        }

        /// <summary>
        /// Zoom the viewport in, centering on the specified point (in content coordinates).
        /// </summary>
        private void ZoomIn(Point contentZoomCenter)
        {
            var delta = zoomAndPanControl.ContentScale > 5 ? 0.2 : 0.1;
            zoomAndPanControl.ZoomAboutPoint(zoomAndPanControl.ContentScale + delta, contentZoomCenter);
        }

        /// <summary>
        /// Initialise the rectangle that the use is dragging out.
        /// </summary>
        private void InitDragZoomRect(Point pt1, Point pt2)
        {
            SetDragZoomRect(pt1, pt2);

            dragZoomCanvas.Visibility = Visibility.Visible;
            dragZoomBorder.Opacity = 0.5;
        }

        /// <summary>
        /// Update the position and size of the rectangle that user is dragging out.
        /// </summary>
        private void SetDragZoomRect(Point pt1, Point pt2)
        {
            double x, y, width, height;

            //
            // Deterine x, y, width, and height of the rect inverting the points if necessary.
            // 
            if (pt2.X < pt1.X)
            {
                // pt2 is left of pt1
                x = pt2.X;
                width = pt1.X - pt2.X;
            }
            else
            {
                // pt1 is left of pt2
                x = pt1.X;
                width = pt2.X - pt1.X;
            }

            if (pt2.Y < pt1.Y)
            {
                // pt2 is above pt1
                y = pt2.Y;
                height = pt1.Y - pt2.Y;
            }
            else
            {
                // pt1 is above pt2
                y = pt1.Y;
                height = pt2.Y - pt1.Y;
            }

            //
            // Update the coordinates of the rectangle that is being dragged out by the user.
            // The we offset and rescale to convert from content coordinates.
            //
            Canvas.SetLeft(dragZoomBorder, x);
            Canvas.SetTop(dragZoomBorder, y);
            dragZoomBorder.Width = width;
            dragZoomBorder.Height = height;
        }

        /// <summary>
        /// When the user has finished dragging out the rectangle the zoom operation is applied.
        /// </summary>
        private void ApplyDragZoomRect()
        {
            //
            // Record the previous zoom level, so that we can jump back to it when the backspace
            // key is pressed.
            //
            SavePrevZoomRect();

            //
            // Retreive the rectangle that the user draggged out and zoom in on it.
            //
            double contentX = Canvas.GetLeft(dragZoomBorder);
            double contentY = Canvas.GetTop(dragZoomBorder);
            double contentWidth = dragZoomBorder.Width;
            double contentHeight = dragZoomBorder.Height;
            zoomAndPanControl.AnimatedZoomTo(new Rect(contentX, contentY, contentWidth, contentHeight));

            FadeOutDragZoomRect();
        }

        /// <summary>
        /// Fade out the drag zoom rectangle.
        /// </summary>
        private void FadeOutDragZoomRect()
        {
            AnimationHelper.StartAnimation(dragZoomBorder, Border.OpacityProperty, 0.0, 0.1,
                delegate (object sender, EventArgs e)
                {
                    dragZoomCanvas.Visibility = Visibility.Collapsed;
                });
        }

        /// <summary>
        /// Record the previous zoom level, so that we can jump back to it when the backspace key is pressed.
        /// </summary>
        private void SavePrevZoomRect()
        {
            prevZoomRect = new Rect(
                zoomAndPanControl.ContentOffsetX,
                zoomAndPanControl.ContentOffsetY,
                zoomAndPanControl.ContentViewportWidth,
                zoomAndPanControl.ContentViewportHeight);
            prevZoomScale = zoomAndPanControl.ContentScale;
            prevZoomRectSet = true;
        }

        /// <summary>
        /// Clear the memory of the previous zoom level.
        /// </summary>
        private void ClearPrevZoomRect()
        {
            prevZoomRectSet = false;
        }

        #endregion // END REGION ZoomAndPanControl

        #region Game Selection Methods

        /// <summary>
        /// Load KotOR 1 game files.
        /// </summary>
        private void LoadK1_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (Directory.Exists(K1_STEAM_DEFAULT_PATH))
            {
                CurrentGame = XmlGameData.Kotor1Data;
                RimDataSet.LoadGameData(K1_STEAM_DEFAULT_PATH);
                LoadGameFiles(K1_STEAM_DEFAULT_PATH, K1_NAME);
            }
            else if (Directory.Exists(K1_GOG_DEFAULT_PATH))
            {
                CurrentGame = XmlGameData.Kotor1Data;
                RimDataSet.LoadGameData(K1_GOG_DEFAULT_PATH);
                LoadGameFiles(K1_GOG_DEFAULT_PATH, K1_NAME);
            }
            else
            {
                _ = MessageBox.Show("Default KotOR 1 path not found. Please use the 'Custom' option instead.");
            }
        }

        /// <summary>
        /// Load K1 can execute if KotOR 2 is selected or if no game has been selected.
        /// </summary>
        private void LoadK1_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsBusy && (SelectedGame == K2_NAME || SelectedGame == DEFAULT);
        }

        /// <summary>
        /// Load KotOR 2 game files.
        /// </summary>
        private void LoadK2_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (Directory.Exists(K2_STEAM_DEFAULT_PATH))
            {
                CurrentGame = XmlGameData.Kotor2Data;
                RimDataSet.LoadGameData(K2_STEAM_DEFAULT_PATH);
                LoadGameFiles(K2_STEAM_DEFAULT_PATH, K2_NAME);
            }
            else if (Directory.Exists(K2_GOG_DEFAULT_PATH))
            {
                CurrentGame = XmlGameData.Kotor2Data;
                RimDataSet.LoadGameData(K2_GOG_DEFAULT_PATH);
                LoadGameFiles(K2_GOG_DEFAULT_PATH, K2_NAME);
            }
            else
            {
                _ = MessageBox.Show("Default KotOR 2 path not found. Please use the 'Custom' option instead.");
            }
        }

        /// <summary>
        /// Load K2 can execute if KotOR 1 is selected or of no game has been selected.
        /// </summary>
        private void LoadK2_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsBusy && (SelectedGame == K1_NAME || SelectedGame == DEFAULT);
        }

        /// <summary>
        /// Load KotOR 1 or 2 from a custom directory.
        /// </summary>
        private void LoadCustom_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Select KotOR 1 or 2 Game File",
                Filter = "Game File (swkotor(2).exe)|swkotor.exe;swkotor2.exe"
            };

            if (ofd.ShowDialog() == true)
            {
                var dir = new FileInfo(ofd.FileName).Directory;
                var exe = dir.EnumerateFiles()
                    .FirstOrDefault(fi =>
                        fi.Name.ToLower() == "swkotor.exe" ||
                        fi.Name.ToLower() == "swkotor2.exe");
                if (exe == null) return;
                if (exe.Name.ToLower() == "swkotor.exe")
                {
                    CurrentGame = XmlGameData.Kotor1Data;
                    RimDataSet.LoadGameData(dir.FullName);
                    LoadGameFiles(dir.FullName, K1_NAME);
                }
                if (exe.Name.ToLower() == "swkotor2.exe")
                {
                    CurrentGame = XmlGameData.Kotor2Data;
                    RimDataSet.LoadGameData(dir.FullName);
                    LoadGameFiles(dir.FullName, K2_NAME);
                }
            }
        }

        private void LoadCustom_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsBusy && SelectedGame == DEFAULT;
        }

        /// <summary>
        /// Load game files based on the given game directory.
        /// </summary>
        private void LoadGameFiles(string path, string name)
        {
            HideGameButtons();

            // Initialize game path and set game as Loading.
            Paths = new KPaths(path);
            SelectedGame = LOADING;
            Game = name;

            // If any walkmeshes are active, remove them.
            if (OnRims.Any()) RemoveAll_Executed(this, null);

            // Clear the cache before loading game files.
            IsBusy = true;
            GameDataWorker.RunWorkerAsync(name);
        }

        /// <summary>
        /// Show the selected game panel and hide the game selection button panel.
        /// </summary>
        private void HideGameButtons()
        {
            pnlSelectGame.Visibility = Visibility.Collapsed;
            pnlSelectedGame.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Performs steps to load required game data.
        /// </summary>
        private void GameDataWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Create the KEY file.
                GameDataWorker.ReportProgress(25);
                var key = new KEY(Paths.chitin);
                var path = System.IO.Path.Combine(Environment.CurrentDirectory, $"{Game} Data");

                var thisGameLoaded = (Game == K1_NAME && K1Loaded) || (Game == K2_NAME && K2Loaded);
                if (!thisGameLoaded)
                {
                    if (Directory.Exists(path))
                    {
                        ReadRimFileCache(path);
                    }
                    else
                    {
                        FetchWokFiles(key);
                        GetRimData(key);
                    }
                }

                // Set up game data.
                var rimModels = new List<RimModel>();
                foreach (var xmlrim in CurrentGame.Rims)
                {
                    Brush brush = null;
                    if (RimToBrushUsed.ContainsKey(xmlrim.FileName))
                        brush = RimToBrushUsed[xmlrim.FileName];

                    rimModels.Add(new RimModel
                    {
                        FileName = xmlrim.FileName,
                        Planet = xmlrim.Planet,
                        CommonName = xmlrim.CommonName,
                        MeshColor = brush,
                        EntryPoint = new Point(xmlrim.EntryX, xmlrim.EntryY)
                    });
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    OffRims.Clear();
                    foreach (var r in rimModels) OffRims.Add(r);
                });

                AllRimNames = OffRims.Select(rm => rm.FileName).ToList();
                KotorClasses = Game == K1_NAME ? Kotor1Classes : Kotor2Classes;
                KotorFeats  = Game == K1_NAME ? Kotor1Feats  : Kotor2Feats;
                KotorPowers = Game == K1_NAME ? Kotor1Powers : Kotor2Powers;

                Application.Current.Dispatcher.Invoke(() => SelectedGame = e.Argument?.ToString() ?? DEFAULT);

                if (!thisGameLoaded && (Game == K1_NAME || Game == K2_NAME) && !Directory.Exists(path))
                {
                    SaveRimFileCache(path);
                }

                if (Game == K1_NAME) K1Loaded = true;
                if (Game == K2_NAME) K2Loaded = true;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                _ = sb.AppendLine("An unexpected error occurred while loading game data.")
                      .AppendLine($"-- {ex.Message}");
                if (ex.InnerException != null)
                    _ = sb.AppendLine($"-- {ex.InnerException.Message}");

                _ = MessageBox.Show(
                    this,
                    sb.ToString(),
                    "Loading Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Read walkmesh files we saved previously.
        /// </summary>
        private void ReadRimFileCache(string path)
        {
            var gameDir = Directory.CreateDirectory(path);
            var count = 0;
            var totalWoks = gameDir.EnumerateFiles("*.wok", SearchOption.AllDirectories).Count();
            foreach (var rimDir in gameDir.EnumerateDirectories())
            {
                var woks = new List<WOK>();
                foreach (var wokFile in rimDir.EnumerateFiles("*.wok"))
                {
                    GameDataWorker.ReportProgress(100 * count++ / totalWoks);
                    woks.Add(new WOK(wokFile.OpenRead())
                    {
                        RoomName = wokFile.Name.Replace(".wok", ""),
                    });
                }
                RimWoksLookup.Add(rimDir.Name, woks);

                var nameFile = rimDir.EnumerateFiles("*.txt").First();
                RimNamesLookup.Add(rimDir.Name, nameFile.Name.Replace(".txt", ""));

                var gitFile = rimDir.EnumerateFiles("*.git").First();
                RimGitLookup.Add(rimDir.Name, GIT.NewGIT(new GFF(gitFile.FullName)));
            }
        }

        /// <summary>
        /// Persist walkmesh files for future application use.
        /// </summary>
        private void SaveRimFileCache(string path)
        {
            var gameDir = Directory.CreateDirectory(path);
            var count = 0;
            var rimwoks = RimWoksLookup.Where(kvp => OffRims.Any(rm => rm.FileName == kvp.Key));
            var totalWoks = rimwoks.Sum(kvp => kvp.Value.Count());
            foreach (var rim in rimwoks)
            {
                var rimDir = gameDir.CreateSubdirectory(rim.Key);
                foreach (var wok in rim.Value)
                {
                    GameDataWorker.ReportProgress(100 * count++ / totalWoks);
                    var wokPath = System.IO.Path.Combine(rimDir.FullName, $"{wok.RoomName}.wok");
                    if (File.Exists(wokPath))
                        throw new Exception($"Save files error: File already exists at '{wokPath}'");
                    else
                        wok.WriteToFile(wokPath);
                }

                File.Create(System.IO.Path.Combine(rimDir.FullName, $"{RimNamesLookup[rim.Key]}.txt")).Close();
                RimGitLookup[rim.Key].WriteToFile(System.IO.Path.Combine(rimDir.FullName, $"{rim.Key}.git"));
            }
        }

        /// <summary>
        /// Clear game data related collections.
        /// </summary>
        private void ClearGameData()
        {
            HideBothPoints();
            RimNamesLookup.Clear();
            RimWoksLookup.Clear();
            RoomWoksLookup.Clear();
            RimPolyLookup.Clear();
            RimOutlinePolyLookup.Clear();
            RimToBrushUsed.Clear();
            RimPolysCreated.Clear();
            foreach (var key in PolyBrushCount.Keys.ToList())
            {
                PolyBrushCount[key] = 0;
            }
        }

        /// <summary>
        /// Retrieves all walkmesh files from game data.
        /// </summary>
        private void FetchWokFiles(KEY key)
        {
            // Create BIF for models.bif, which contains walkmeshes.
            GameDataWorker.ReportProgress(10);
            var mdlBif = new BIF(System.IO.Path.Combine(Paths.data, "models.bif"));
            mdlBif.AttachKey(key, "data\\models.bif");
            var wokVREs = mdlBif.VariableResourceTable.Where(vre => vre.ResourceType == ResourceType.WOK).ToList();

            // Create WOK objects.
            for (var i = 0; i < wokVREs.Count; i++)
            {
                GameDataWorker.ReportProgress(100 * i / wokVREs.Count);
                var wok = new WOK(wokVREs[i].EntryData)
                {
                    RoomName = wokVREs[i].ResRef
                };

                // Only save the WOK if it has verts.
                if (wok.Verts.Any())
                    RoomWoksLookup.Add(wokVREs[i].ResRef.ToLower(), wok);
            }
        }

        /// <summary>
        /// Retrieves game data from RIM files.
        /// </summary>
        private void GetRimData(KEY key)
        {
            // Get LYTs and create TLK.
            var lytFiles = GetLayoutFiles(key);
            var tlk = new TLK(Paths.dialog);

            // Parse RIMs for common name and LYTs in use.
            var rimFiles = Paths.FilesInModules.Where(fi => !fi.Name.EndsWith("_s.rim") && fi.Extension == ".rim").ToList();
            for (var i = 0; i < rimFiles.Count; i++)
            {
                GameDataWorker.ReportProgress(100 * i / rimFiles.Count);

                // Create RIM object
                var rim = new RIM(rimFiles[i].FullName);
                var rimName = rimFiles[i].Name.Replace(".rim", "").ToLower();  // something akin to warp codes ("danm13")

                // Store GIT file.
                RimGitLookup.Add(rimName, rim.GitFile);

                // Fetch ARE file
                var rfile = rim.File_Table.First(rf => rf.TypeID == (int)ResourceType.ARE);
                var are = new GFF(rfile.File_Data);
                var lytResRef = rfile.Label;  // label used for LYT resref and (usually) room prefix

                // Find the module (common) name
                var moduleName = string.Empty;   // something like "Dantooine - Jedi Enclave"
                if (are.Top_Level.Fields.First(f => f.Label == "Name") is GFF.CExoLocString field)
                {
                    moduleName = tlk[field.StringRef];
                }
                RimNamesLookup.Add(rimName, moduleName);

                // Check LYT file to collect this RIM's rooms.
                if (lytFiles.ContainsKey(lytResRef))
                {
                    // LYT that matches this RIM
                    var lyt = lytFiles[lytResRef];

                    var roomNames = lyt.Rooms
                        .Select(r => r.Name.ToLower())
                        .Where(r => !r.Contains("****") ||  // remove invalid
                                    !r.Contains("stunt"));  // remove cutscene
                    if (!roomNames.Any()) continue;         // Only store RIM if it has rooms.

                    var rimWoks =
                        RoomWoksLookup.Where(kvp => roomNames.Contains(kvp.Key))
                                      .Select(kvp => kvp.Value);
                    if (!rimWoks.Any()) continue;   // Only store RIM if it has WOKs.

                    RimWoksLookup.Add(rimName, rimWoks);
                }
                else
                {
                    Console.WriteLine($"ERROR: no layout file corresponds to the name '{lytResRef}'.");
                }
            }
        }

        /// <summary>
        /// Returns a collection of all module layout files found in the game files.
        /// </summary>
        private Dictionary<string, LYT> GetLayoutFiles(KEY key)
        {
            // Create BIF for layouts.bif
            var lytBif = new BIF(System.IO.Path.Combine(Paths.data, "layouts.bif"));
            lytBif.AttachKey(key, "data\\layouts.bif");
            var lytVREs = lytBif.VariableResourceTable.Where(vre => vre.ResourceType == ResourceType.LYT).ToList();
            var lytFiles = new Dictionary<string, LYT>();   // lytFiles[ResRef] = lytObj; ResRef == RimFilename

            // Create LYT objects
            for (var i = 0; i < lytVREs.Count; i++)
            {
                GameDataWorker.ReportProgress(100 * i / lytVREs.Count);

                var lyt = new LYT(lytVREs[i].EntryData);
                if (lyt.Rooms.Any())    // Only save the LYT if it has rooms.
                    lytFiles.Add(lytVREs[i].ResRef.ToLower(), lyt);
            }

            return lytFiles;
        }

        #endregion // END REGION Game Selection Methods

        #region Swap Game Methods

        /// <summary>
        /// Perform steps to allow the user to swap to a different game.
        /// </summary>
        private void SwapGame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // If selected game panel is not visible, ignore the command request.
            if (pnlSelectedGame.Visibility != Visibility.Visible) return;

            // If any walkmeshes are active, remove them.
            if (OnRims.Any()) RemoveAll_Executed(this, null);

            // Reset values.
            OffRims.Clear();
            SelectedGame = DEFAULT;

            // Reset coordinate matches.
            HideBothPoints();
            ClearBothPointMatches();

            ShowGameButtons();
        }

        /// <summary>
        /// Show the game selection button panel and hide the selected game panel.
        /// </summary>
        private void ShowGameButtons()
        {
            pnlSelectedGame.Visibility = Visibility.Collapsed;
            pnlSelectGame.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Swap Game can execute if the Selected Game panel is visible (i.e., a game is selected).
        /// </summary>
        private void SwapGame_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsBusy && pnlSelectedGame.Visibility == Visibility.Visible;
        }

        #endregion // END REGION Swap Game Methods

        #region RIM Data Info (DLZ) Methods

        private void lvRimDataInfo_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            HandleRimDataInfo((sender as ListViewItem).Content as RimDataInfo);
        }

        private void lvRimDataInfo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) HandleRimDataInfo((sender as ListViewItem).Content as RimDataInfo);
        }

        private void ShowAllRimDataInfo(ObservableCollection<RimDataInfo> rdis)
        {
            foreach (var rdi in rdis.Where(rdi => !rdi.MeshVisible))
            {
                BuildRimDataInfoMesh(rdi);
                SetRimDataInfoMeshBrush(rdi);
                ShowDlzLinesMethod(rdi);
            }
        }

        private void HandleRimDataInfo(RimDataInfo rdi)
        {
            if (rdi.MeshVisible)
            {
                HideRimDataInfoMesh(rdi);
                HideDlzLines(rdi);
            }
            else
            {
                BuildRimDataInfoMesh(rdi);
                SetRimDataInfoMeshBrush(rdi);
                ShowDlzLinesMethod(rdi);
            }

            VisibleRimDoors = RimDoors.Count(d => d.MeshVisible);
            HiddenRimDoors = RimDoors.Count(d => !d.MeshVisible);
            VisibleRimTriggers = RimTriggers.Count(d => d.MeshVisible);
            HiddenRimTriggers = RimTriggers.Count(d => !d.MeshVisible);
            VisibleRimTraps = RimTraps.Count(d => d.MeshVisible);
            HiddenRimTraps = RimTraps.Count(d => !d.MeshVisible);
            VisibleRimZones = RimZones.Count(d => d.MeshVisible);
            HiddenRimZones = RimZones.Count(d => !d.MeshVisible);
            VisibleRimEncounters = RimEncounters.Count(d => d.MeshVisible);
            HiddenRimEncounters = RimEncounters.Count(d => !d.MeshVisible);
        }

        private void HideRimDataInfoMesh(RimDataInfo rdi)
        {
            rdi.MeshColor = Brushes.Transparent;
        }

        private void ShowDlzLinesMethod(RimDataInfo rdi)
        {
            if (ShowDlzLines)
            {
                switch (rdi.RimDataType)
                {
                    case RimDataType.Door:
                        if (ShowRimDataDoorsDlzLines)
                            rdi.LineColor = rdi.MeshColor;
                        break;
                    case RimDataType.Trigger:
                        if (ShowRimDataTriggersDlzLines)
                            rdi.LineColor = rdi.MeshColor;
                        break;
                    case RimDataType.Encounter:
                        if (ShowRimDataEncountersDlzLines)
                            rdi.LineColor = rdi.MeshColor;
                        break;
                    case RimDataType.Trap:
                        if (ShowRimDataTrapsDlzLines)
                            rdi.LineColor = rdi.MeshColor;
                        break;
                    case RimDataType.Zone:
                        if (ShowRimDataZonesDlzLines)
                            rdi.LineColor = rdi.MeshColor;
                        break;
                    case RimDataType.Unknown:
                    default:
                        break;
                }
            }
        }

        private void HideDlzLines(RimDataInfo rdi)
        {
            rdi.LineColor = Brushes.Transparent;
        }

        private void SetRimDataInfoMeshBrush(RimDataInfo rdi)
        {
            rdi.PaletteUsed = SelectedPalette;
            var setColor = GetNextRimDataInfoBrush();
            if (setColor == RimToBrushUsed[rdi.Module]) setColor = GetNextRimDataInfoBrush();
            rdi.MeshColor = setColor;
        }

        private void BuildRimDataInfoMesh(RimDataInfo rdi)
        {
            if (rdi.AreVisualsBuilt == false)
            {
                rdi.AreVisualsBuilt = true;
                rdi.PaletteUsed = SelectedPalette;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    rdi.LineCanvas = new Canvas
                    {
                        Opacity = 0.9,
                        Visibility = Visibility.Visible,
                        RenderTransform = content.Resources["CartesianTransform"] as Transform,
                    };
                });

                foreach (var corner in rdi.Geometry)
                {
                    Application.Current.Dispatcher.Invoke(() => {
                        var l = new Line()
                        {
                            Visibility = Visibility.Visible,
                            Stroke = Brushes.Transparent,
                            StrokeThickness = .05,
                            X1 = corner.Item1,
                            Y1 = corner.Item2,
                            X2 = corner.Item1,
                            Y2 = -1000,
                        };
                        rdi.Lines.Add(l);
                        rdi.LineCanvas.Children.Add(l);
                    });
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    rdi.Polygon = new Polygon
                    {
                        Visibility = Visibility.Visible,
                        Stroke = Brushes.Transparent,
                        StrokeThickness = 0.05,
                        Points = new PointCollection(rdi.Geometry.Select(g => new Point(g.Item1, g.Item2))),
                        Fill = Brushes.Transparent,
                        Opacity = 0.8
                    };
                });

                foreach (var point in rdi.SpawnPoints)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var size = rdi.EllipseRadius * 2;
                        var e = new Ellipse
                        {
                            Stroke = Brushes.Black,
                            StrokeThickness = 0.1,
                            Height = size,
                            Width = size,
                            Opacity = 0.8,
                            RenderTransform = content.Resources["CartesianTransform"] as Transform,
                            Fill = Brushes.Transparent,
                        };
                        Canvas.SetLeft(e, point.Item1 - (e.Width / 2));
                        Canvas.SetTop(e, -point.Item2 + (e.Height / 2));
                        rdi.Ellipses.Add(e);
                    });
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var canvas = RimDataCanvasLookup[rdi.Module];
                    _ = canvas.Children.Add(rdi.Polygon);
                    foreach (var ellipse in rdi.Ellipses)
                        _ = canvas.Children.Add(ellipse);
                    _ = canvas.Children.Add(rdi.LineCanvas);
                });
            }
        }

        /// <summary>
        /// Filter for RimDataInfo listview containers.
        /// </summary>
        /// <param name="item"><see cref="RimDataInfo"/> object</param>
        /// <returns>Returns true if filter is off, the filter box is empty, or the Module strings match (ignoring case).</returns>
        private bool RimDataFilter(object item) => !DoRimDataFilter || RimDataFilter_SelectedItem == null
            || (item as RimDataInfo).Module.Equals(RimDataFilter_SelectedItem.FileName, StringComparison.OrdinalIgnoreCase);

        private void RefreshRimDataFilters()
        {
            CollectionViewSource.GetDefaultView(lvRimDoor.ItemsSource).Refresh();
            CollectionViewSource.GetDefaultView(lvRimTrigger.ItemsSource).Refresh();
            CollectionViewSource.GetDefaultView(lvRimTrap.ItemsSource).Refresh();
            CollectionViewSource.GetDefaultView(lvRimZone.ItemsSource).Refresh();
            CollectionViewSource.GetDefaultView(lvRimEncounter.ItemsSource).Refresh();
        }

        private void RimDataFilter_ToggleChanged(object sender, RoutedEventArgs e) => RefreshRimDataFilters();

        private void RimDataFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshRimDataFilters();

        #endregion

        #region Add Methods

        /// <summary>
        /// Add a walkmesh to the display.
        /// </summary>
        private void LvOff_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (IsBusy) return;

            try
            {
                AddRim((sender as ListViewItem).Content as RimModel);
            }
            catch (InvalidCastException)
            {
                // Ignore this request and reset IsBusy.
                IsBusy = false;
            }
        }

        /// <summary>
        /// Moves RimModel to the On list and runs AddPolyWorker.
        /// </summary>
        private void AddRim(RimModel rim)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                if (SelectedGatherPartyRim == null)
                    SelectedGatherPartyRim = rim;

                // Disable regions if needed.
                if (ShowTransAbortRegions && OnRims.Count == 1)
                {
                    ShowTransAbortRegions = false;
                    UpdateTransAbortRegionVisibility();
                }

                // Move the clicked item from OFF to ON.
                _ = OffRims.Remove(rim);
                var sorted = OnRims.ToList();
                sorted.Add(rim);

                // Sort the ON list and update collection.
                sorted.Sort();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnRims.Clear();
                    foreach (var r in sorted) OnRims.Add(r);
                });

                // Add RIM data collections.
                var rdiModule = RimDataSet.RimData.First(m => m.Module == rim.FileName);
                RimDoors.AddRangeAndSort(rdiModule.Doors);
                RimTriggers.AddRangeAndSort(rdiModule.Triggers);
                RimTraps.AddRangeAndSort(rdiModule.Traps);
                RimZones.AddRangeAndSort(rdiModule.Zones);
                RimEncounters.AddRangeAndSort(rdiModule.Encounters);

                // Start worker to add polygons to the canvas.
                _ = content.Focus();
                AddPolyWorker.RunWorkerAsync(new AddPolyWorkerArgs { rimToAdd = rim, getLeastUsedBrush = true });
            }
            catch (Exception ex)
            {
                // Ignore this request and reset IsBusy.
                IsBusy = false;

                var sb = new StringBuilder();
                _ = sb.AppendLine("An unexpected error occurred while adding walkmesh data.")
                      .AppendLine($"-- {ex.Message}");
                if (ex.InnerException != null)
                    _ = sb.AppendLine($"-- {ex.InnerException.Message}");

                _ = MessageBox.Show(
                    this,
                    sb.ToString(),
                    "Add Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Perform steps to add walkmesh polygons to the canvas.
        /// </summary>
        private void AddPolyWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = (AddPolyWorkerArgs)e.Argument;

            // Code for multiple module display with GP regions.
            //if (OnRims.Count == 1) SelectedGatherPartyRim = OnRims.First();

            BuildNewWalkmeshes(AddPolyWorker);

            ResizeCanvas();

            ShowWalkmeshOnCanvas(args.rimToAdd, AddPolyWorker, args.getLeastUsedBrush, args.cycleColor);

            Application.Current.Dispatcher.Invoke(() =>
            {
                var needsUpdate = RimDoors.ToList()
                    .Concat(RimTriggers)
                    .Concat(RimTraps)
                    .Concat(RimZones)
                    .Concat(RimEncounters)
                    .Where(rdi => rdi.MeshVisible
                        && rdi.AreVisualsBuilt
                        && rdi.PaletteUsed != null
                        && rdi.PaletteUsed != SelectedPalette)
                    .ToList();
                foreach (var rdi in needsUpdate) SetRimDataInfoMeshBrush(rdi);

                if (ShowDoorsOnAddRim) ShowAllRimDataInfo(RimDoors);
                if (ShowTriggersOnAddRim) ShowAllRimDataInfo(RimTriggers);
                if (ShowTrapsOnAddRim) ShowAllRimDataInfo(RimTraps);
                if (ShowZonesOnAddRim) ShowAllRimDataInfo(RimZones);
                if (ShowEncountersOnAddRim) ShowAllRimDataInfo(RimEncounters);
            });

            VisibleRimDoors = RimDoors.Count(d => d.MeshVisible);
            HiddenRimDoors = RimDoors.Count(d => !d.MeshVisible);
            VisibleRimTriggers = RimTriggers.Count(d => d.MeshVisible);
            HiddenRimTriggers = RimTriggers.Count(d => !d.MeshVisible);
            VisibleRimTraps = RimTraps.Count(d => d.MeshVisible);
            HiddenRimTraps = RimTraps.Count(d => !d.MeshVisible);
            VisibleRimZones = RimZones.Count(d => d.MeshVisible);
            HiddenRimZones = RimZones.Count(d => !d.MeshVisible);
            VisibleRimEncounters = RimEncounters.Count(d => d.MeshVisible);
            HiddenRimEncounters = RimEncounters.Count(d => !d.MeshVisible);

            if (LeftClickPointVisible) BringLeftPointToTop();
            if (RightClickPointVisible) BringRightPointToTop();
            if (ShowLivePosition) BringLivePositionPointToTop();
        }

        public struct AddPolyWorkerArgs
        {
            public RimModel rimToAdd;
            public bool getLeastUsedBrush;
            public bool cycleColor;
        }

        /// <summary>
        /// Build any new walkmesh in the ON collection.
        /// </summary>
        private void BuildNewWalkmeshes(BackgroundWorker bw, bool useModuleBrush = false)
        {
            Brush brushToUse = null;
            content.Dispatcher.Invoke(() =>
            {
                content.Background  = ShowTransAbortRegions ? Brushes.Black : BackgroundColors[SelectedBackgroundColor];
                CoordinateTextBrush = ShowTransAbortRegions ? Brushes.White : ForegroundColors[SelectedBackgroundColor];
            });

            // Build unbuilt RIM walkmeshes.
            var unbuilt = OnRims.ToList();
            for (var i = 0; i < unbuilt.Count; i++)
            {
                var rimmodel = unbuilt[i];
                if (useModuleBrush) brushToUse = rimmodel.MeshColor;
                if (ShowTransAbortRegions) brushToUse = GrayScaleBrush;

                // Code for multiple module display with GP regions.
                //if (ShowTransAbortRegions && SelectedGatherPartyRim == rimmodel) brushToUse = GrayScaleBrush;

                var name = rimmodel.FileName;
                Canvas walkCanvas = null, nonWalkCanvas = null, transAbortCanvas = null, transBorderCanvas = null, defaultSpawnCanvas = null, rimDataCanvas = null, fullCanvas = null;

                if (RimFullCanvasLookup.ContainsKey(name))
                {
                    fullCanvas = RimFullCanvasLookup[name];
                    walkCanvas = RimWalkableCanvasLookup[name];
                    nonWalkCanvas = RimNonwalkableCanvasLookup[name];
                    transAbortCanvas = RimTransAbortPointCanvasLookup[name];
                    transBorderCanvas = RimTransAbortRegionCanvasLookup[name];
                    defaultSpawnCanvas = RimDefaultSpawnPointCanvasLookup[name];
                    rimDataCanvas = RimDataCanvasLookup[name];
                }
                else Application.Current.Dispatcher.Invoke(() =>
                {
                    fullCanvas         = new Canvas();
                    walkCanvas         = new Canvas { Opacity = 0.8, Visibility = Visibility.Hidden, };
                    nonWalkCanvas      = new Canvas { Opacity = 0.8, Visibility = Visibility.Hidden, };
                    transBorderCanvas  = new Canvas { Opacity = 0.4, Visibility = Visibility.Hidden, };
                    rimDataCanvas      = new Canvas { Opacity = 0.9, Visibility = Visibility.Visible, };
                    defaultSpawnCanvas = new Canvas { Opacity = 0.8, Visibility = Visibility.Hidden, };
                    transAbortCanvas   = new Canvas { Opacity = 0.8, Visibility = Visibility.Hidden, };

                    _ = fullCanvas.Children.Add(walkCanvas);
                    _ = fullCanvas.Children.Add(nonWalkCanvas);
                    _ = fullCanvas.Children.Add(transBorderCanvas);
                    _ = fullCanvas.Children.Add(rimDataCanvas);
                    _ = fullCanvas.Children.Add(defaultSpawnCanvas);
                    _ = fullCanvas.Children.Add(transAbortCanvas);

                    RimFullCanvasLookup.Add(name, fullCanvas);
                    RimWalkableCanvasLookup.Add(name, walkCanvas);
                    RimNonwalkableCanvasLookup.Add(name, nonWalkCanvas);
                    RimTransAbortRegionCanvasLookup.Add(name, transBorderCanvas);
                    RimDataCanvasLookup.Add(name, rimDataCanvas);
                    RimDefaultSpawnPointCanvasLookup.Add(name, defaultSpawnCanvas);
                    RimTransAbortPointCanvasLookup.Add(name, transAbortCanvas);
                });

                // Create walkable polygons.
                if (ShowWalkableFaces && !RimPolyLookup.ContainsKey(name))
                {
                    var polys = new List<Polygon>();    // walkable polygons
                    var walkFaces = RimWoksLookup[name].SelectMany(w => w.Faces).Where(f => f.IsWalkable).ToList();
                    for (int j = 0; j < walkFaces.Count; j++)
                    {
                        bw.ReportProgress(100 * j / walkFaces.Count);
                        var points = walkFaces[j].ToPoints();
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var poly = new Polygon { Points = new PointCollection(points), Fill = brushToUse };
                            polys.Add(poly);
                            _ = walkCanvas.Children.Add(poly);
                        });
                    }
                    RimPolyLookup.Add(name, polys); // Cache the created polygons.
                }

                // Create nonwalkable polygons.
                if (ShowNonWalkableFaces && !RimOutlinePolyLookup.ContainsKey(name))
                {
                    var unpolys = new List<Polygon>();  // unwalkable polygons
                    var nonwalkFaces = RimWoksLookup[name].SelectMany(w => w.Faces).Where(f => !f.IsWalkable).ToList();
                    for (int j = 0; j < nonwalkFaces.Count; j++)
                    {
                        bw.ReportProgress(100 * j / nonwalkFaces.Count);
                        var points = nonwalkFaces[j].ToPoints();
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var poly = new Polygon
                            {
                                Points = new PointCollection(points),
                                Stroke = brushToUse,
                                StrokeThickness = .1,
                                StrokeLineJoin = PenLineJoin.Round,
                            };
                            unpolys.Add(poly);
                            _ = nonWalkCanvas.Children.Add(poly);
                        });
                    }
                    RimOutlinePolyLookup.Add(name, unpolys);    // Cache the created polygons.
                }

                // Create default spawn points.
                if (ShowDefaultSpawnPoints && !RimDefaultSpawnPoint.ContainsKey(name))
                {
                    Polygon dspStar = null;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        dspStar = new Polygon
                        {
                            Stroke = Brushes.Black,
                            StrokeThickness = 0.25,
                            Fill = brushToUse,
                            Points = new PointCollection
                            {
                                new Point(rimmodel.EntryPoint.X-1, rimmodel.EntryPoint.Y  ),
                                new Point(rimmodel.EntryPoint.X  , rimmodel.EntryPoint.Y+1),
                                new Point(rimmodel.EntryPoint.X+1, rimmodel.EntryPoint.Y  ),
                                new Point(rimmodel.EntryPoint.X  , rimmodel.EntryPoint.Y-1),
                            },
                        };
                        _ = defaultSpawnCanvas.Children.Add(dspStar);
                    });
                    RimDefaultSpawnPoint.Add(name, dspStar);    // Cache the created polygons.
                }

                // Create trans_abort points.
                if ((ShowTransAbortPoints || ShowTransAbortRegions) && !RimTransAborts.ContainsKey(name))
                {
                    var tas = new List<Ellipse>();  // trans_abort points
                    var taWaypoints = RimGitLookup[name].Waypoints.Structs.Where(s => s.Fields.FirstOrDefault(f => f.Label == "Tag") is GFF.CExoString t && t.CEString == "wp_transabort").ToList();
                    var transAbortPoints = new List<Point>();
                    foreach (var waypoint in taWaypoints)
                    {
                        var x = (waypoint.Fields.FirstOrDefault(f => f.Label == "XPosition") is GFF.FLOAT xf) ? xf.Value : double.NaN;
                        var y = (waypoint.Fields.FirstOrDefault(f => f.Label == "YPosition") is GFF.FLOAT yf) ? yf.Value : double.NaN;
                        var z = (waypoint.Fields.FirstOrDefault(f => f.Label == "ZPosition") is GFF.FLOAT zf) ? zf.Value : double.NaN;

                        if (double.IsNaN(x) || double.IsNaN(y)) continue;
                        transAbortPoints.Add(new Point(x, y));
                    }

                    foreach (var point in transAbortPoints)
                    {
                        // Create TransAbort points as ellipses.
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var e = new Ellipse
                            {
                                Stroke = Brushes.Black,
                                Fill = brushToUse,
                                StrokeThickness = 0.25,
                                Height = 2,
                                Width = 2,
                                RenderTransform = content.Resources["CartesianTransform"] as Transform,
                            };
                            Canvas.SetLeft(e, point.X - (e.Width / 2));
                            Canvas.SetTop(e, -point.Y + (e.Height / 2));
                            tas.Add(e);
                            _ = transAbortCanvas.Children.Add(e);
                        });
                    }
                    RimTransAborts.Add(name, tas);  // Cache the created polygons.
                    RimTransAbortPoints.Add(name, transAbortPoints);
                }

                // Calculate border lines between each pair of trans_abort points.
                if (ShowTransAbortRegions && !RimTransRegions.ContainsKey(name))
                    CalculateTransAbortBorders(transBorderCanvas, name, RimTransAbortPoints[name].ToList());

                Application.Current.Dispatcher.Invoke(() =>
                {
                    walkCanvas.Visibility = ShowWalkableFaces ? Visibility.Visible : Visibility.Collapsed;
                    nonWalkCanvas.Visibility = ShowNonWalkableFaces ? Visibility.Visible : Visibility.Collapsed;
                    transBorderCanvas.Visibility = ShowTransAbortRegions ? Visibility.Visible : Visibility.Collapsed;
                    // Code for multiple module display with GP regions.
                    //transBorderCanvas.Visibility = ShowTransAbortRegions && SelectedGatherPartyRim == rimmodel ? Visibility.Visible : Visibility.Collapsed;
                    defaultSpawnCanvas.Visibility = ShowDefaultSpawnPoints ? Visibility.Visible : Visibility.Collapsed;
                    transAbortCanvas.Visibility = ShowTransAbortPoints ? Visibility.Visible : Visibility.Collapsed;
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void CalculateTransAbortBorders(Canvas transBorderCanvas, string rimName, List<Point> transAbortPoints)
        {
            // Calculate minimum and maximum x and y values of this module.
            var woks = RimWoksLookup[rimName];
            var minx = woks.Min(w => w.MinX);
            var maxx = woks.Max(w => w.MaxX);
            var miny = woks.Min(w => w.MinY);
            var maxy = woks.Max(w => w.MaxY);

            // Create collections to use later.
            var equations = new List<List<GeneralLineEquation>>();
            var regions = new List<List<Point>>();
            var boundingBox = new List<GeneralLineEquation>
            {
                new GeneralLineEquation { A = 1, B = 0, C = -(minx - BaseOffset.X) },
                new GeneralLineEquation { A = 0, B = 1, C = -(maxy + BaseOffset.Y) },
                new GeneralLineEquation { A = 1, B = 0, C = -(maxx + BaseOffset.X) },
                new GeneralLineEquation { A = 0, B = 1, C = -(miny - BaseOffset.Y) },
            };

            // Create an equation for each pair of trans_abort points.
            for (var i = 0; i < transAbortPoints.Count; i++)
            {
                var iPoint = transAbortPoints[i];
                var iEquations = new List<GeneralLineEquation>();
                for (var j = 0; j < transAbortPoints.Count; j++)
                {
                    if (i == j)
                    {
                        iEquations.Add(null);   // skip self comparison
                    }
                    else
                    {
                        var jPoint = transAbortPoints[j];
                        iEquations.Add(GeneralLineEquation.FindMidline(iPoint, jPoint));
                    }
                }
                iEquations.AddRange(boundingBox);
                equations.Add(iEquations);
            }

            // For each point, calculate the bouding region.
            for (var i = 0; i < transAbortPoints.Count; i++)
            {
                var iPoint = transAbortPoints[i];
                var searched = new bool[equations[i].Count];

                // Find line L closest to point I.
                GeneralLineEquation L = null;
                var lineIndex = -1;
                for (var j = 0; j < equations[i].Count; j++)
                {
                    if (i == j || equations[i][j] == null || !equations[i][j].IsALine)
                    {
                        searched[j] = true;
                        continue;
                    }
                    if (L == null || (equations[i][j].Distance(iPoint) < L.Distance(iPoint)))
                    {
                        L = equations[i][j];
                        lineIndex = j;
                    }
                }

                if (L == null) continue;    // No line found.
                searched[lineIndex] = true;
                var mPoint = L.FindNearestPoint(iPoint).Value;
                var thisLine = L;       // Current line.
                var cPoint = iPoint;    // Compare point.
                var regionPoints = new List<Point>();

                // Going clockwise, check each other equation for the nearest intersection.
                while (true)
                {
                    var normal = thisLine.NormalVector(iPoint).Value;
                    normal.Normalize();

                    // Direction is based on the direction of the normal vector.
                    var direction = thisLine.IsVertical
                        ? Math.Sign(normal.X)
                        : -Math.Sign(normal.Y);

                    // Find next intersection in the current direction.
                    var distance = double.NaN;
                    Point? nextIntersect = null;
                    GeneralLineEquation nextLine = null;
                    var nextIndex = -1;
                    for (var j = 0; j < equations[i].Count; j++)
                    {
                        if (i == j || equations[i][j] == null || thisLine == equations[i][j]) continue;   // skip self comparison
                        if (searched[j] && L != equations[i][j]) continue;  // skip lines we've already hit, except the start line

                        var thisIntersect = thisLine.Intersection(equations[i][j]);
                        if (!thisIntersect.HasValue ||
                            (thisIntersect.Value - mPoint).Length < GeneralLineEquation.SMALL_VALUE ||  // Don't stop at the same point.
                            double.IsNaN(thisIntersect.Value.X) ||
                            double.IsNaN(thisIntersect.Value.Y))
                        {
                            continue;
                        }

                        // Skip intersection if in the wrong direction.
                        if (thisLine.IsVertical)
                        {
                            if (Math.Sign(thisIntersect.Value.Y - mPoint.Y) != direction) continue;
                        }
                        else
                        {
                            if (Math.Sign(thisIntersect.Value.X - mPoint.X) != direction) continue;
                        }

                        var thisDistance = mPoint.Distance(thisIntersect.Value);
                        if (nextIntersect == null || thisDistance < distance)
                        {
                            distance = thisDistance;
                            nextIntersect = thisIntersect;
                            nextLine = equations[i][j];
                            nextIndex = j;
                        }
                    }

                    // Break out of search if no intersection was found.
                    if (!nextIntersect.HasValue) break;

                    // Save intersection point and new line.
                    cPoint = mPoint;
                    mPoint = nextIntersect.Value;
                    regionPoints.Add(mPoint);
                    thisLine = nextLine;
                    searched[nextIndex] = true;

                    // Break out if you've returned to the starting line.
                    if (nextLine == L) break;
                }

                regions.Add(regionPoints);
            }

            // Draw regions.
            var polys = new List<Polygon>();
            var fillIdx = 0;
            for (var i = 0; i < regions.Count; i++)
            {
                var region = regions[i];
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var poly = new Polygon
                    {
                        Stroke = TransAbortBorderBrush,
                        StrokeThickness = .3,
                        Fill = BrushCycle[fillIdx],
                        RenderTransform = content.Resources["CartesianTransform"] as Transform,
                        Points = new PointCollection(region),
                    };
                    _ = transBorderCanvas.Children.Add(poly);
                    polys.Add(poly);
                });
                fillIdx = (fillIdx + 1) % BrushCycle.Count;
            }
            RimTransRegions.Add(rimName, polys);
        }

        /// <summary>
        /// Resize the canvas to fit all displayed faces.
        /// </summary>
        private void ResizeCanvas()
        {
            // Determine size of canvas and offset.
            var woks = RimWoksLookup
                .Where(kvp => OnRims.Any(r => r.FileName == kvp.Key))
                .SelectMany(kvp => kvp.Value);

            // Do nothing if there is no visible walkmesh.
            if (!woks.Any()) return;

            // Calculate bounding box.
            var MinX = woks.Min(w => w.MinX);
            var MinY = woks.Min(w => w.MinY);
            var RngX = woks.Max(w => w.MaxX) - MinX;
            var RngY = woks.Max(w => w.MaxY) - MinY;

            // Handle taris swoops exception.
            if (OnRims.Any(r => r.FileName == "tar_m03mg"))
            {
                MinX = (float)Math.Min(MinX,   -3.294533);
                MinY = (float)Math.Min(MinY,  -80.829834);
                RngX = (float)Math.Max(RngX,  200);
                RngY = (float)Math.Max(RngY, 4000);
            }

            // Resize canvas for active walkmeshes.
            theGrid.Dispatcher.Invoke(() =>
            {
                theGrid.Height = RngY + (BaseOffset.Y * 2);
                theGrid.Width = RngX + (BaseOffset.X * 2);
            });

            // Offset the polygons so there is a whitespace border.
            var prevLeftOffset = LeftOffset;
            var prevBottomOffset = BottomOffset;

            LeftOffset = -MinX + BaseOffset.X;
            BottomOffset = -MinY + BaseOffset.Y;

            var diffLeft = LeftOffset - prevLeftOffset;
            var diffBottom = BottomOffset - prevBottomOffset;

            LeftClickPoint = new Point(LeftClickPoint.X + diffLeft, LeftClickPoint.Y + diffBottom);
            RightClickPoint = new Point(RightClickPoint.X + diffLeft, RightClickPoint.Y + diffBottom);
        }

        private void ResizePathPointPairs(List<Tuple<Point, Point>> pairs, double diffLeft, double diffBottom)
        {
            for (int i = 0; i < pairs.Count; i++)
            {
                var p1 = pairs[i].Item1;
                var p2 = pairs[i].Item2;
                p1.X += diffLeft;
                p1.Y += diffBottom;
                p2.X += diffLeft;
                p2.Y += diffBottom;
            }
        }

        /// <summary>
        /// Add or make visible all faces in the newly active walkmesh.
        /// </summary>
        private void ShowWalkmeshOnCanvas(RimModel rimToAdd, BackgroundWorker bw = null, bool getLeastUsed = true, bool cycleColor = false)
        {
            // Determine next brush to use.
            var brushChanged = true;
            Brush brush;

            // If added to the canvas before, just add.
            if (RimToBrushUsed.ContainsKey(rimToAdd.FileName) && !cycleColor)
            {
                brush = GetLeastUsedWalkmeshBrush();
                var oldBrush = RimToBrushUsed[rimToAdd.FileName];

                var isGrayscale = false;
                Application.Current.Dispatcher.Invoke(() => isGrayscale = (RimPolyLookup[rimToAdd.FileName].FirstOrDefault()?.Fill ?? null) == GrayScaleBrush);

                brushChanged = brush != oldBrush || (ShowTransAbortRegions ^ isGrayscale);
                PolyBrushCount[brush]++;
            }
            // Else, if get least used color...
            else if (getLeastUsed)
            {
                brush = GetLeastUsedWalkmeshBrush();
                PolyBrushCount[brush]++;
                BrushIndex = BrushCycle.IndexOf(GetLeastUsedWalkmeshBrush());
            }
            // Else, get next color in cycle.
            else if (cycleColor)
            {
                brush = GetNextWalkmeshBrush();
                BrushIndex = (BrushIndex + 1) % PolyBrushCount.Count;
                PolyBrushCount[brush]++;
            }
            else
            {
                throw new Exception("This code should be unreachable. Error Code: 11738");
            }

            // Remember which brush was used.
            rimToAdd.MeshColor = brush;
            if (RimToBrushUsed.ContainsKey(rimToAdd.FileName))
            {
                RimToBrushUsed[rimToAdd.FileName] = brush;
            }
            else
            {
                RimToBrushUsed.Add(rimToAdd.FileName, brush);
            }

            // Update the fill color.
            if (brushChanged)
            {
                // Code for multiple module display with GP regions.
                //if (ShowTransAbortRegions && SelectedGatherPartyRim == rimToAdd)
                if (ShowTransAbortRegions)
                {
                    UpdateRimFillColor(bw, GrayScaleBrush, rimToAdd);
                }
                else
                {
                    UpdateRimFillColor(bw, brush, rimToAdd);
                }
            }

            // Add all surfaces to the canvas.
            if (RimPolysCreated.Contains(rimToAdd.FileName))
            {
                UpdateRimVisibility(rimToAdd);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _ = content.Children.Add(RimFullCanvasLookup[rimToAdd.FileName]);
                });
            }

            // Remember that the polygons have been added to the canvas.
            if (!RimPolysCreated.Contains(rimToAdd.FileName))
            {
                RimPolysCreated.Add(rimToAdd.FileName);
            }

            Console.WriteLine($"OFF: {LeftOffset:N2}, {BottomOffset:N2}");
        }

        /// <summary>
        /// 
        /// </summary>
        private void UpdateRimFillColor(BackgroundWorker bw, Brush brush, RimModel rimModel)
        {
            List<Shape> fills = new List<Shape>();
            if (RimPolyLookup.ContainsKey(rimModel.FileName)) fills.AddRange(RimPolyLookup[rimModel.FileName]); // walkable
            if (RimTransAborts.ContainsKey(rimModel.FileName)) fills.AddRange(RimTransAborts[rimModel.FileName]);   // trans_abort points
            if (RimDefaultSpawnPoint.ContainsKey(rimModel.FileName)) fills.Add(RimDefaultSpawnPoint[rimModel.FileName]);    // default spawn point

            List<Polygon> strokes = new List<Polygon>();
            if (RimOutlinePolyLookup.ContainsKey(rimModel.FileName)) strokes.AddRange(RimOutlinePolyLookup[rimModel.FileName]); // non-walkable

            // If the brush is different, update with new color.
            RimFullCanvasLookup[rimModel.FileName].Dispatcher.Invoke(() =>
            {
                var i = 0;
                fills.Where(f => f.Fill != brush).ToList().ForEach((Shape p) =>
                {
                    bw?.ReportProgress(100 * i++ / fills.Count);
                    p.Fill = brush;
                });
                i = 0;  // reset count
                strokes.Where(s => s.Stroke != brush).ToList().ForEach((Polygon p) =>
                {
                    bw?.ReportProgress(100 * i++ / strokes.Count);
                    p.Stroke = brush;
                });
            });
        }

        private Brush GetLeastUsedWalkmeshBrush() => PolyBrushCount.First(kvp => kvp.Value == PolyBrushCount.Values.Min()).Key;

        private Brush GetNextWalkmeshBrush() => BrushCycle[BrushIndex];

        private Brush GetNextRimDataInfoBrush() => CurrentRimDataInfoBrush = BrushCycle[(BrushCycle.IndexOf(CurrentRimDataInfoBrush) + 1) % BrushCycle.Count];

        #endregion // END REGION Add Methods

        #region Remove Methods

        /// <summary>
        /// Remove a walkmesh from the display.
        /// </summary>
        private void LvOn_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (IsBusy) return;

            try
            {
                // Move the clicked item from ON to OFF.
                RemoveRim((sender as ListViewItem).Content as RimModel);
            }
            catch (InvalidCastException)
            {
                // Ignore this request and reset IsBusy.
                IsBusy = false;
            }
        }

        private void RemoveRim(RimModel rim)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                // Move the clicked item from ON to OFF.
                _ = OnRims.Remove(rim);
                var sorted = OffRims.ToList();
                sorted.Add(rim);

                // Sort the OFF list and update collection.
                sorted.Sort();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OffRims.Clear();
                    foreach (var r in sorted) OffRims.Add(r);
                });

                // Remove RIM data collections.
                var rimModule = RimDataSet.RimData.First(m => m.Module == rim.FileName);
                foreach (var door in rimModule.Doors)
                    RimDoors.Remove(door);
                foreach (var trigger in rimModule.Triggers)
                    RimTriggers.Remove(trigger);
                foreach (var trap in rimModule.Traps)
                    RimTraps.Remove(trap);
                foreach (var zone in rimModule.Zones)
                    RimZones.Remove(zone);
                foreach (var encounter in rimModule.Encounters)
                    RimEncounters.Remove(encounter);

                // Start worker to remove polygons from the canvas.
                _ = content.Focus();
                RemovePolyWorker.RunWorkerAsync(rim);
            }
            catch (Exception ex)
            {
                // Ignore this request and reset IsBusy.
                IsBusy = false;

                var sb = new StringBuilder();
                _ = sb.AppendLine("An unexpected error occurred while removing walkmesh data.")
                      .AppendLine($"-- {ex.Message}");
                if (ex.InnerException != null)
                    _ = sb.AppendLine($"-- {ex.InnerException.Message}");

                _ = MessageBox.Show(
                    this,
                    sb.ToString(),
                    "Remove Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Perform steps to remove walkmesh polygons from the canvas.
        /// </summary>
        private void RemovePolyWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Code for multiple module display with GP regions.
            //if (OnRims.Count == 1) SelectedGatherPartyRim = OnRims.First();
            var rimToRemove = (RimModel)e.Argument;   // grab rim info
            HideWalkmeshOnCanvas(rimToRemove);
            ResizeCanvas();
        }

        /// <summary>
        /// Hide all faces in the newly disabled walkmesh.
        /// </summary>
        private void HideWalkmeshOnCanvas(RimModel rimToRemove)
        {
            // Adjust count of times this rim's brush was used.
            PolyBrushCount[RimToBrushUsed[rimToRemove.FileName]]--;
            UpdateRimVisibility(rimToRemove);
        }

        /// <summary>
        /// Delete all walkmesh faces associated with a specific RIM file.
        /// </summary>
        private void DeleteWalkmeshFromCanvas(string rimToRemove)
        {
            // Adjust count of times this rim's brush was used.
            PolyBrushCount[RimToBrushUsed[rimToRemove]]--;

            // Retrieve all polygons from the cache.
            var polys = RimPolyLookup[rimToRemove].ToList();
            polys.AddRange(RimOutlinePolyLookup[rimToRemove]);

            // Clear bindings for each polygon.
            polys.ForEach((Polygon p) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var rt = p.RenderTransform as TransformGroup;
                    p.Visibility = Visibility.Hidden;

                    // Remove bindings for each of the transforms.
                    foreach (var transform in rt.Children)
                    {
                        BindingOperations.ClearAllBindings(transform);
                    }

                    // Remove bindings on the polygon.
                    BindingOperations.ClearBinding(p, RenderTransformProperty);
                });
            });

            // Remove all polygons from the canvas.
            content.Dispatcher.Invoke(() =>
            {
                content.Children.RemoveRange(content.Children.IndexOf(polys[0]), polys.Count);
            });

            // Clear the caches.
            _ = RimPolyLookup.Remove(rimToRemove);
            _ = RimOutlinePolyLookup.Remove(rimToRemove);
            _ = RimPolysCreated.Remove(rimToRemove);
        }

        #endregion // END REGION Remove Methods

        #region Remove All Methods

        /// <summary>
        /// Remove all active walkmeshes from the canvas.
        /// </summary>
        private void RemoveAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            IsBusy = true;

            HideBothPoints();
            ClearBothPointMatches();

            foreach (var rimModule in RimDataSet.RimData.Where(m => OnRims.Any(n => m.Module == n.FileName)))
            {
                foreach (var door in rimModule.Doors)
                    RimDoors.Remove(door);
                foreach (var trigger in rimModule.Triggers)
                    RimTriggers.Remove(trigger);
                foreach (var trap in rimModule.Traps)
                    RimTraps.Remove(trap);
                foreach (var zone in rimModule.Zones)
                    RimZones.Remove(zone);
                foreach (var encounter in rimModule.Encounters)
                    RimEncounters.Remove(encounter);
            }

            // Move all ON names to the OFF collection.
            foreach (var rim in OnRims)
            {
                OffRims.Add(rim);
            }
            OnRims.Clear();

            // Sort the OFF collection.
            var sorted = OffRims.ToList();
            sorted.Sort();
            Application.Current.Dispatcher.Invoke(() =>
            {
                OffRims.Clear();
                foreach (var r in sorted) OffRims.Add(r);
            });

            // Hide all polygons in the canvas.
            content.Dispatcher.Invoke(() =>
            {
                foreach (var child in content.Children.OfType<Canvas>())
                {
                    child.Visibility = Visibility.Collapsed;
                }
            });

            // Set brush count to 0 since all walkmeshes are now hidden.
            foreach (var key in PolyBrushCount.Keys.ToList())
            {
                PolyBrushCount[key] = 0;
            }

            IsBusy = false;
        }

        /// <summary>
        /// Remove all can execute only if there are names in the ON collection.
        /// </summary>
        private void RemoveAll_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsBusy && (OnRims?.Any() ?? false);
        }

        #endregion // END REGION Remove All Methods

        #region Find Matching Coord Methods

        /// <summary>
        /// Perform steps to find modules with matching coordinates.
        /// </summary>
        private void FindMatchingCoords_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            IsBusy = true;

            if (!LeftClickPointVisible) LeftClickModuleCoords = new Point();
            if (!RightClickPointVisible) RightClickModuleCoords = new Point();

            LastLeftClickModuleCoords = LeftClickModuleCoords;
            LastRightClickModuleCoords = RightClickModuleCoords;

            FindMatchingCoords();

            IsBusy = false;
        }

        /// <summary>
        /// Find all modules whose walkmesh contains the currently selected point.
        /// </summary>
        private void FindMatchingCoords()
        {
            // Initialize values and lists used for this code.
            LeftPointMatches.Clear();
            RightPointMatches.Clear();
            BothPointMatches.Clear();
            bool foundLeftPoint, foundRightPoint;
            WalkabilityModel left, right;
            var allRims = OnRims.Concat(OffRims);

            // Determine each rim to see if it contains the active points.
            var rimwoks = RimWoksLookup.Where(kvp => CurrentGame.Rims.Any(xr => xr.FileName == kvp.Key));
            foreach (var rimKvp in rimwoks)
            {
                foundLeftPoint = foundRightPoint = false;
                left = right = null;

                // Check each room walkmesh within the rim.
                foreach (var wok in rimKvp.Value)
                {
                    // Left point.
                    if (LeftClickPointVisible && !foundLeftPoint &&
                        wok.FaceAtPoint((float)LeftClickModuleCoords.X, (float)LeftClickModuleCoords.Y) is WOK.Face fl)
                    {
                        left = new WalkabilityModel
                        {
                            Rim = allRims.First(r => r.FileName == rimKvp.Key),
                            Walkability = fl.IsWalkable ? "w" : "n",
                        };
                        LeftPointMatches.Add(left);
                        foundLeftPoint = true;
                    }

                    // Right point.
                    if (RightClickPointVisible && !foundRightPoint &&
                        wok.FaceAtPoint((float)RightClickModuleCoords.X, (float)RightClickModuleCoords.Y) is WOK.Face fr)
                    {
                        right = new WalkabilityModel
                        {
                            Rim = allRims.First(r => r.FileName == rimKvp.Key),
                            Walkability = fr.IsWalkable ? "w" : "n",
                        };
                        RightPointMatches.Add(right);
                        foundRightPoint = true;
                    }

                    // Both points.
                    if (foundLeftPoint && foundRightPoint)
                    {
                        BothPointMatches.Add(new WalkabilityModel
                        {
                            Rim = left.Rim,
                            Walkability = left.Walkability == right.Walkability
                                ? left.Walkability  // left and white match
                                : left.Walkability == "w"
                                    ? "w|n"         // left walkable, right not
                                    : "n|w"         // right walkable, left not
                        });
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Toggle the selected item's visibility on the canvas.
        /// </summary>
        private void MatchItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Get the selected RimModel.
            var wm = (sender as ListViewItem).Content as WalkabilityModel;

            // Is this rim in the OnRims list?
            if (OnRims.FirstOrDefault(r => r.FileName == wm.Rim.FileName) is RimModel on)
            {
                // Remove the item from the ON list.
                RemoveRim(on);
            }
            else if (OffRims.FirstOrDefault(r => r.FileName == wm.Rim.FileName) is RimModel off)
            {
                // Add the item to the ON list.
                AddRim(off);
            }
        }

        private void RimColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                var rim = (sender as Button).DataContext as RimModel;
                PolyBrushCount[rim.MeshColor]--;
                _ = content.Focus();
                AddPolyWorker.RunWorkerAsync(new AddPolyWorkerArgs { rimToAdd = rim, getLeastUsedBrush = false, cycleColor = true });
            }
            catch (Exception ex)
            {
                // Ignore this request and reset IsBusy.
                IsBusy = false;

                var sb = new StringBuilder();
                _ = sb.AppendLine("An unexpected error occurred while adding walkmesh data.")
                      .AppendLine($"-- {ex.Message}");
                if (ex.InnerException != null)
                    _ = sb.AppendLine($"-- {ex.InnerException.Message}");

                _ = MessageBox.Show(
                    this,
                    sb.ToString(),
                    "Add Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void RimDataInfoColorButton_Click(object sender, RoutedEventArgs e)
        {
            var info = (sender as Button).DataContext as RimDataInfo;
            if (info.MeshVisible == false) return;
            SetRimDataInfoMeshBrush(info);
            ShowDlzLinesMethod(info);
        }

        //private void ClrPcker_Background_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        //{
        //    //TextBox.Text = "#" + ClrPcker_Background.SelectedColor.R.ToString() + ClrPcker_Background.SelectedColor.G.ToString() + ClrPcker_Background.SelectedColor.B.ToString();
        //    return;
        //}

        private void LeftCoordButton_Click(object sender, RoutedEventArgs e)
        {
            var cid = new CoordinateInputDialog(LeftClickModuleCoords.X, LeftClickModuleCoords.Y)
            {
                Owner = this,
                PointName = "Black",
                PointFill = Brushes.Black,
                PointStroke = Brushes.Black,
            };

            if (cid.ShowDialog() == true)
            {
                ClearLeftPointMatches();
                var x = double.Parse(cid.X) + LeftOffset;
                var y = theGrid.Height - double.Parse(cid.Y) - BottomOffset;
                HandleLeftDoubleClick(new Point(x, y));
                LastLeftClickModuleCoords = LeftClickModuleCoords;
                CalculatePointDistance();
            }
        }

        private void RightCoordButton_Click(object sender, RoutedEventArgs e)
        {
            var cid = new CoordinateInputDialog(RightClickModuleCoords.X, RightClickModuleCoords.Y)
            {
                Owner = this,
                PointName = "White",
                PointFill = Brushes.White,
                PointStroke = Brushes.Black,
            };

            if (cid.ShowDialog() == true)
            {
                ClearRightPointMatches();
                var x = double.Parse(cid.X) + LeftOffset;
                var y = theGrid.Height - double.Parse(cid.Y) - BottomOffset;
                HandleRightDoubleClick(new Point(x, y));
                LastRightClickModuleCoords = RightClickModuleCoords;
                CalculatePointDistance();
            }
        }

        #endregion Find Matching Coord Methods

        #region Common Background Worker Methods

        /// <summary>
        /// Finalize background worker operations.
        /// </summary>
        private void Bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsBusy = false;
            CurrentProgress = 0;
        }

        /// <summary>
        /// Report current progress of background worker.
        /// </summary>
        private void Bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            CurrentProgress = e.ProgressPercentage;
        }

        #endregion // END REGION Common Background Worker Methods

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

        #region Menu Related Command Methods

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            var waitTime = 0;
            if (LivePositionWorker.IsBusy)
            {
                LivePositionWorker.CancelAsync();
                waitTime = LivePositionUpdateDelay * 2;
            }
            if (MouseHoverWorker.IsBusy)
            {
                MouseHoverWorker.CancelAsync();
                waitTime = Math.Max(waitTime, MouseHoverUpdateDelay * 2);
            }

            if (waitTime > 0) Thread.Sleep(waitTime);
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            var settings = Properties.Settings.Default;
            settings.ShowWalkableFaces = ShowWalkableFaces;
            settings.ShowNonWalkableFaces = ShowNonWalkableFaces;
            settings.ShowTransAbortPoints = ShowTransAbortPoints;
            settings.ShowTransAbortRegions = ShowTransAbortRegions;
            settings.ShowDefaultSpawnPoints = ShowDefaultSpawnPoints;
            settings.ShowLeftClickPointCoordinates = ShowLeftClickPointCoordinates;
            settings.ShowRightClickPointCoordinates = ShowRightClickPointCoordinates;
            settings.ShowDlzLines = ShowDlzLines;
            settings.ShowDoorsOnAddRim = ShowDoorsOnAddRim;
            settings.ShowTriggersOnAddRim = ShowTriggersOnAddRim;
            settings.ShowTrapsOnAddRim = ShowTrapsOnAddRim;
            settings.ShowZonesOnAddRim = ShowZonesOnAddRim;
            settings.ShowEncountersOnAddRim = ShowEncountersOnAddRim;
            settings.ShowLivePositionCoordinates = ShowLivePositionCoordinates;
            settings.HidePreviousLiveModule = HidePreviousLiveModule;
            settings.ShowCurrentLiveModule = ShowCurrentLiveModule;
            settings.HotswapToLiveGame = HotswapToLiveGame;
            settings.ViewFollowsLivePosition = ViewFollowsLivePosition;
            settings.LivePositionUpdateDelay = LivePositionUpdateDelay;
            settings.ShowRimDataUnderMouse = ShowRimDataUnderMouse;
            settings.ShowGatherPartyRange = ShowGatherPartyRange;
            settings.ShowLeftClickGatherPartyRange = ShowLeftClickGatherPartyRange;
            settings.ShowCoordinatePanel = ShowCoordinatePanel;
            settings.ShowRimDataPanel = ShowRimDataPanel;
            settings.ShowDistancePanel = ShowDistancePanel;
            settings.ShowToolsPanel = ShowToolsPanel;
            settings.ShowWireTargetPanel = ShowWireTargetPanel;
            settings.PrevLeftPanelSize = (ShowCoordinatePanel || ShowRimDataPanel || ShowDistancePanel || ShowToolsPanel || ShowWireTargetPanel)
                ? columnLeftPanel.ActualWidth
                : prevLeftPanelSize;
            settings.ShowWalkmeshPanel = ShowWalkmeshPanel;
            settings.PrevRightPanelSize = ShowWalkmeshPanel
                ?  columnRightPanel.ActualWidth
                : prevRightPanelSize;
            settings.SelectedPaletteName = SelectedPalette.FileName;
            settings.SelectedBackgroundColor = (int)SelectedBackgroundColor;
            settings.ShowRimDataDoors = ShowRimDataDoors;
            settings.ShowRimDataTriggers = ShowRimDataTriggers;
            settings.ShowRimDataTraps = ShowRimDataTraps;
            settings.ShowRimDataZones = ShowRimDataZones;
            settings.ShowRimDataEncounters = ShowRimDataEncounters;
            settings.ShowTypeUnderMouse = ShowTypeUnderMouse;
            settings.ShowResRefUnderMouse = ShowResRefUnderMouse;
            settings.ShowTagUnderMouse = ShowTagUnderMouse;
            settings.ShowLocalizedNameUnderMouse = ShowLocalizedNameUnderMouse;
            settings.ShowOnEnterUnderMouse = ShowOnEnterUnderMouse;
            settings.Save();
        }

        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void ExitCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsBusy;
        }

        private void SaveShownCanvas_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (zoomAndPanControl.ContentViewportWidth > theGrid.ActualWidth ||
                zoomAndPanControl.ContentViewportHeight > theGrid.ActualHeight)
            {
                SaveImageFromCanvas(20);
            }
            else
            {
                var scale = 20 + zoomAndPanControl.ContentScale;
                SaveImageFromCanvas(scale,
                    new Int32Rect(
                        (int)(zoomAndPanControl.ContentOffsetX * scale),
                        (int)(zoomAndPanControl.ContentOffsetY * scale),
                        (int)(zoomAndPanControl.ContentViewportWidth * scale),
                        (int)(zoomAndPanControl.ContentViewportHeight * scale)));
            }
        }

        private void SaveShownCanvas_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsBusy && (OnRims?.Any() ?? false);
        }

        private void SaveEntireCanvas_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveToXps();
        }

        private void SaveToXps()
        {
            CurrentProgress = 100;
            CurrentProgressStatus = "Saving to XPS...";
            IsBusy = true;

            var dlg = new SaveFileDialog
            {
                Title = "Save XPS As",
                FileName = "screenshot",
                DefaultExt = ".xps",
                Filter = "XPS Documents (.xps)|*.xps"
            };

            var result = dlg.ShowDialog();
            if (result == true)
            {
                var fd = new FixedDocument();
                var pc = new PageContent();
                var fp = new FixedPage();

                fp.Height = theGrid.Height - 10;
                fp.Width = theGrid.Width - 10;
                theGrid.Margin = new Thickness(LeftOffset - 5, -BottomOffset - 5, 0, 0);

                zoomAndPanControl.Content = null;
                fp.Children.Add(theGrid);
                ((System.Windows.Markup.IAddChild)pc).AddChild(fp);
                fd.Pages.Add(pc);

                var filename = dlg.FileName;
                if (File.Exists(filename)) File.Delete(filename);
                var xpsd = new XpsDocument(filename, FileAccess.ReadWrite);
                var xw = XpsDocument.CreateXpsDocumentWriter(xpsd);
                xw.Write(fd);
                xpsd.Close();

                fp.Children.Remove(theGrid);
                theGrid.Margin = new Thickness(0);
                zoomAndPanControl.Content = theGrid;
            }

            CurrentProgress = 0;
            CurrentProgressStatus = string.Empty;
            IsBusy = false;
        }

        private void SaveEntireCanvas_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsBusy && (OnRims?.Any() ?? false);
        }

        private string GetImagePath()
        {
            var sfd = new SaveFileDialog
            {
                Title = "Save Image As",
                FileName = "screenshot",
                DefaultExt = ".png",
                Filter = "PNG File|*.png",
            };
            return sfd.ShowDialog() == true ? sfd.FileName : null;
        }

        private void SaveImageFromCanvas(double zoom, Int32Rect? cropRect = null)
        {
            try
            {
                // request save path from user
                var path = GetImagePath();
                if (string.IsNullOrEmpty(path)) return;

                // render the canvas
                var rect = new Rect(new Size(theGrid.ActualWidth * zoom, theGrid.ActualHeight * zoom));
                var dpi = 96 * zoom;
                var rtb = new RenderTargetBitmap((int)rect.Right, (int)rect.Bottom, dpi, dpi, PixelFormats.Default);
                rtb.Render(theGrid);

                // encode as png
                var encoder = new PngBitmapEncoder();
                if (cropRect.HasValue)
                {
                    // crop if requested
                    var crop = new CroppedBitmap(rtb, cropRect.Value);
                    encoder.Frames.Add(BitmapFrame.Create(crop));
                }
                else
                {
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                }

                // save to file
                using (var fs = File.OpenWrite(path))
                {
                    encoder.Save(fs);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(
                    this,
                    $"Unexpected error encountered while saving.{Environment.NewLine}-- {ex.Message}{Environment.NewLine}Please try again.",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ViewHelpCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var htws = OwnedWindows.OfType<HelpTextWindow>();
            if (htws.Any())
            {
                htws.First().Show();
            }
            else
            {
                var htw = new HelpTextWindow
                {
                    Left = Left + Width + 5,
                    Top = Top,
                    Owner = this
                };
                htw.Show();
            }
        }

        private void ViewHelpCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void SetColorPreferences_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var sctws = OwnedWindows.OfType<SetColorPreferencesWindow>();
            if (sctws.Any())
            {
                sctws.First().Show();
            }
            else
            {
                var sctw = new SetColorPreferencesWindow(SelectedPalette, SelectedBackgroundColor)
                {
                    Owner = this
                };

                // Show dialog. If "Ok" is selected...
                if (sctw.ShowDialog() ?? false)
                {
                    SetBackgroundColor(sctw.SelectedBackground);

                    // If no palette is selected anymore, reset to previously selected palette.
                    var pal = PaletteManager.GetSelectedPalette();
                    if (pal == null)
                    {
                        pal = PaletteManager.Instance.Palettes.FirstOrDefault(p => p.Name == SelectedPalette.Name);
                        if (pal != null) pal.IsSelected = true;
                    }
                    // Otherwise, set the new palette.
                    else SetPalette(pal);
                }
                // If "Cancel" is selected and no palette is selected, reset to previously selected palette.
                else if (!PaletteManager.Instance.Palettes.Any(p => p.IsSelected))
                {
                    var pal = PaletteManager.Instance.Palettes.FirstOrDefault(p => p.Name == SelectedPalette.Name);
                    if (pal != null) pal.IsSelected = true;
                }
            }
        }

        private void SetBackgroundColor(BackgroundColor newBackground)
        {
            if (newBackground == BackgroundColor.Unknown || newBackground == SelectedBackgroundColor) return;
            SelectedBackgroundColor = newBackground;
            Application.Current.Dispatcher.Invoke(() =>
            {
                content.Background  = ShowTransAbortRegions ? Brushes.Black : BackgroundColors[SelectedBackgroundColor];
                CoordinateTextBrush = ShowTransAbortRegions ? Brushes.White : ForegroundColors[SelectedBackgroundColor];
            });
        }

        private async void SetPalette(Palette newPalette)
        {
            // Set up new brush palette.
            SelectedPalette = newPalette;
            BrushToName = SelectedPalette.ToDictionary();
            PolyBrushCount.Clear();
            foreach (var kvp in BrushToName) PolyBrushCount.Add(kvp.Key, 0);

            BrushCycle = new List<Brush>(PolyBrushCount.Keys);
            CurrentRimDataInfoBrush = BrushCycle.First();

            BrushToName.Add(Brushes.Black, "Black");
            BrushToName.Add(Brushes.White, "White");

            // Calculate border lines between each pair of trans_abort points.
            IsBusy = true;
            if (ShowTransAbortRegions && OnRims.Count == 1)
            {
                var rim = OnRims.First();
                var polys = RimTransRegions[rim.FileName];
                var index = 0;
                foreach (var poly in polys)
                {
                    Application.Current.Dispatcher.Invoke(() => poly.Fill = BrushCycle[index]);
                    index = (index + 1) % BrushCycle.Count;
                }
            }

            // foreach active walkmesh, redraw color
            foreach (var rim in OnRims) await Task.Run(() => { ShowWalkmeshOnCanvas(rim); });
            IsBusy = false;

            // foreach active rimdata, redraw color
            var rimInfos = RimDoors
                .Concat(RimTriggers)
                .Concat(RimTraps)
                .Concat(RimZones)
                .Concat(RimEncounters)
                .Where(rdi => rdi.MeshVisible);
            foreach (var info in rimInfos)
                SetRimDataInfoMeshBrush(info);

        }

        private void SetColorPreferences_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsBusy;
        }

        // Code for multiple module display with GP regions.
        //private async void SelectedGatherPartyRim_Changed(object sender, SelectionChangedEventArgs e)
        //{
        //    if (IsBusy || !ShowTransAbortRegions) return;
        //    if (ShowTransAbortRegions)
        //    {
        //        ShowTransAbortRegions = false;
        //        await Task.Run(() => { ShowTransAbortRegionCommand_Executed(this, null); });

        //        ShowTransAbortRegions = true;
        //        await Task.Run(() => { ShowTransAbortRegionCommand_Executed(this, null); });
        //    }
        //}

        #endregion

        #region Left Panel Methods

        private void CoordinatePanelButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide other panels in the Left Panel
            ShowRimDataPanel = false;
            ShowDistancePanel = false;
            ShowToolsPanel = false;
            ShowWireTargetPanel = false;
        }

        private void RimDataPanelButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide other panels in the Left Panel
            ShowCoordinatePanel = false;
            ShowDistancePanel = false;
            ShowToolsPanel = false;
            ShowWireTargetPanel = false;
        }

        private void DistancePanelButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide other panels in the Left Panel
            ShowCoordinatePanel = false;
            ShowRimDataPanel = false;
            ShowToolsPanel = false;
            ShowWireTargetPanel = false;
        }

        private void ToolsPanelButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide other panels in the Left Panel
            ShowCoordinatePanel = false;
            ShowRimDataPanel = false;
            ShowDistancePanel = false;
            ShowWireTargetPanel = false;
        }

        private void WireTargetPanelButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide other panels in the Left Panel
            ShowCoordinatePanel = false;
            ShowRimDataPanel = false;
            ShowDistancePanel = false;
            ShowToolsPanel = false;
        }

        private void gsLeftPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ShowRimDataPanel || ShowCoordinatePanel || ShowDistancePanel || ShowToolsPanel || ShowWireTargetPanel)
            {
                columnLeftPanel.MinWidth = 240;
                columnLeftPanel.Width = new GridLength(prevLeftPanelSize, GridUnitType.Pixel);
            }
            else
            {
                columnLeftPanel.MinWidth = 0;
                prevLeftPanelSize = columnLeftPanel.ActualWidth;
                columnLeftPanel.Width = new GridLength(1, GridUnitType.Auto);
            }
        }

        private void SetRimDataTypePanelVisibility(bool isVisible, string type)
        {
            if (type == "Door")
            {
                ShowRimDataDoors = isVisible;
                lvRimDoor.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                rowRimDoor.Height = isVisible ? new GridLength(4, GridUnitType.Star) : new GridLength(1, GridUnitType.Auto);
            }
            if (type == "Trigger")
            {
                ShowRimDataTriggers = isVisible;
                lvRimTrigger.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                rowRimTrigger.Height = isVisible ? new GridLength(5, GridUnitType.Star) : new GridLength(1, GridUnitType.Auto);
            }
            if (type == "Trap")
            {
                ShowRimDataTraps = isVisible;
                lvRimTrap.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                rowRimTrap.Height = isVisible ? new GridLength(3, GridUnitType.Star) : new GridLength(1, GridUnitType.Auto);
            }
            if (type == "Zone")
            {
                ShowRimDataZones = isVisible;
                lvRimZone.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                rowRimZone.Height = isVisible ? new GridLength(3, GridUnitType.Star) : new GridLength(1, GridUnitType.Auto);
            }
            if (type == "Encounter")
            {
                ShowRimDataEncounters = isVisible;
                lvRimEncounter.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                rowRimEncounter.Height = isVisible ? new GridLength(3, GridUnitType.Star) : new GridLength(1, GridUnitType.Auto);
            }
        }

        private void RimDataShowHideButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as ToggleButton;
            if (!btn.IsChecked.HasValue) return;
            SetRimDataTypePanelVisibility(btn.IsChecked.Value, btn.Tag.ToString());
        }

        private void DistanceSpeedButton_Click(object sender, RoutedEventArgs e)
        {
            DistanceToTimeMultiplier = double.Parse((sender as ToggleButton).Tag.ToString());
            DurationLeftRight   = DistanceLeftRight   / SPEED_UNITS_PER_SECOND / DistanceToTimeMultiplier;
            DurationLiveLeft    = DistanceLiveLeft    / SPEED_UNITS_PER_SECOND / DistanceToTimeMultiplier;
            DurationLiveRight   = DistanceLiveRight   / SPEED_UNITS_PER_SECOND / DistanceToTimeMultiplier;
            DurationTempSegment = DistanceTempSegment / SPEED_UNITS_PER_SECOND / DistanceToTimeMultiplier;
            DurationBluePath    = DistanceBluePath    / SPEED_UNITS_PER_SECOND / DistanceToTimeMultiplier;
            DurationRedPath     = DistanceRedPath     / SPEED_UNITS_PER_SECOND / DistanceToTimeMultiplier;
            DurationGreenPath   = DistanceGreenPath   / SPEED_UNITS_PER_SECOND / DistanceToTimeMultiplier;

            foreach (ToggleButton btn in distPanelSpeedButtons.Children)
            {
                if (btn == sender) continue;
                btn.IsChecked = false;
            }
        }

        private void ClearPathButton_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button).Tag.ToString();
            if (tag.ToString() == "Black")
            {
                TempSegmentVisible = false;
                DistanceTempSegment = 0.0;
                DurationTempSegment = 0.0;
            }
            else if (tag.ToString() == "Blue")
            {
                BluePathPointPairs.Clear();
                BluePathGrid.Children.Clear();
                BluePathVisible = false;
                DistanceBluePath = 0.0;
                DurationBluePath = 0.0;
            }
            else if (tag == "Red")
            {
                RedPathPointPairs.Clear();
                RedPathGrid.Children.Clear();
                RedPathVisible = false;
                DistanceRedPath = 0.0;
                DurationRedPath = 0.0;
            }
            else if (tag == "Green")
            {
                GreenPathPointPairs.Clear();
                GreenPathGrid.Children.Clear();
                GreenPathVisible = false;
                DistanceGreenPath = 0.0;
                DurationGreenPath = 0.0;
            }
        }

        private void AddToPathButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TempSegmentVisible) return;

            var tag = (sender as Button).Tag.ToString();
            if (tag == "Blue")
            {
                // Bring segment to front.
                Application.Current.Dispatcher.Invoke(() =>
                {
                    content.Children.Remove(BluePathGrid);
                    _ = content.Children.Add(BluePathGrid);
                });

                BluePathPointPairs.Add(new Tuple<Point, Point>(TempSegmentStart, TempSegmentEnd));
                BluePathGrid.Children.Add(new Line
                {
                    X1 = TempSegmentStart.X,
                    Y1 = TempSegmentStart.Y,
                    X2 = TempSegmentEnd.X,
                    Y2 = TempSegmentEnd.Y
                });
                BluePathVisible = true;
                DistanceBluePath += DistanceTempSegment;
                DurationBluePath += DurationTempSegment;
            }
            else if (tag == "Red")
            {
                // Bring segment to front.
                Application.Current.Dispatcher.Invoke(() =>
                {
                    content.Children.Remove(RedPathGrid);
                    _ = content.Children.Add(RedPathGrid);
                });

                RedPathPointPairs.Add(new Tuple<Point, Point>(TempSegmentStart, TempSegmentEnd));
                RedPathGrid.Children.Add(new Line
                {
                    X1 = TempSegmentStart.X,
                    Y1 = TempSegmentStart.Y,
                    X2 = TempSegmentEnd.X,
                    Y2 = TempSegmentEnd.Y
                });
                RedPathVisible = true;
                DistanceRedPath += DistanceTempSegment;
                DurationRedPath += DurationTempSegment;
            }
            else if (tag == "Green")
            {
                // Bring segment to front.
                Application.Current.Dispatcher.Invoke(() =>
                {
                    content.Children.Remove(GreenPathGrid);
                    _ = content.Children.Add(GreenPathGrid);
                });

                GreenPathPointPairs.Add(new Tuple<Point, Point>(TempSegmentStart, TempSegmentEnd));
                GreenPathGrid.Children.Add(new Line
                {
                    X1 = TempSegmentStart.X,
                    Y1 = TempSegmentStart.Y,
                    X2 = TempSegmentEnd.X,
                    Y2 = TempSegmentEnd.Y
                });
                GreenPathVisible = true;
                DistanceGreenPath += DistanceTempSegment;
                DurationGreenPath += DurationTempSegment;
            }

            DistanceTempSegment = 0.0;
            DurationTempSegment = 0.0;
            TempSegmentVisible = false;
        }

        private void MinusPathButton_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button).Tag.ToString();
            Tuple<Point,Point> last = null;
            if (tag == "Blue" && BluePathPointPairs.Any())
            {
                last = BluePathPointPairs.Last();
                BluePathPointPairs.RemoveAt(BluePathPointPairs.Count - 1);
                BluePathGrid.Children.RemoveAt(BluePathGrid.Children.Count - 1);
                DistanceTempSegment = (last.Item2 - last.Item1).Length;

                if (BluePathPointPairs.Count == 0)
                {
                    DistanceBluePath = 0.0;
                    DurationBluePath = 0.0;
                    BluePathVisible = false;
                }
                else
                {
                    DistanceBluePath -= DistanceTempSegment;
                    DurationBluePath = DistanceBluePath / SPEED_UNITS_PER_SECOND / DistanceToTimeMultiplier;
                }
            }
            else if (tag == "Red")
            {
                last = RedPathPointPairs.Last();
                RedPathPointPairs.RemoveAt(RedPathPointPairs.Count - 1);
                RedPathGrid.Children.RemoveAt(RedPathGrid.Children.Count - 1);
                DistanceTempSegment = (last.Item2 - last.Item1).Length;

                if (RedPathPointPairs.Count == 0)
                {
                    DistanceRedPath = 0.0;
                    DurationRedPath = 0.0;
                    RedPathVisible = false;
                }
                else
                {
                    DistanceRedPath -= DistanceTempSegment;
                    DurationRedPath = DistanceRedPath / SPEED_UNITS_PER_SECOND / DistanceToTimeMultiplier;
                }
            }
            else if (tag == "Green")
            {
                last = GreenPathPointPairs.Last();
                GreenPathPointPairs.RemoveAt(GreenPathPointPairs.Count - 1);
                GreenPathGrid.Children.RemoveAt(GreenPathGrid.Children.Count - 1);
                DistanceTempSegment = (last.Item2 - last.Item1).Length;

                if (GreenPathPointPairs.Count == 0)
                {
                    DistanceGreenPath = 0.0;
                    DurationGreenPath = 0.0;
                    GreenPathVisible = false;
                }
                else
                {
                    DistanceGreenPath -= DistanceTempSegment;
                    DurationGreenPath = DistanceGreenPath / SPEED_UNITS_PER_SECOND / DistanceToTimeMultiplier;
                }
            }

            DurationTempSegment = DistanceTempSegment / SPEED_UNITS_PER_SECOND / DistanceToTimeMultiplier;
            TempSegmentStart = last.Item1;
            TempSegmentEnd = last.Item2;
            TempSegmentVisible = true;

            // Bring segment to front.
            Application.Current.Dispatcher.Invoke(() =>
            {
                content.Children.Remove(tempSegment);
                _ = content.Children.Add(tempSegment);
            });
        }

        private void BuildSegmentFromBlackWhitePoints_Click(object sender, RoutedEventArgs e)
        {
            TempSegmentStart = new Point(LeftClickPoint.X  + .5, theGrid.Height - LeftClickPoint.Y  - .5);
            TempSegmentEnd   = new Point(RightClickPoint.X + .5, theGrid.Height - RightClickPoint.Y - .5);
            DistanceTempSegment = (TempSegmentEnd - TempSegmentStart).Length;
            DurationTempSegment = DistanceTempSegment / SPEED_UNITS_PER_SECOND / DistanceToTimeMultiplier;
            TempSegmentVisible = true;
        }

        private void AddBlackPointToPathButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddWhitePointToPathButton_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion // END REGION Left Panel Methods

        #region Right Panel Methods

        private void gsRightPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ShowWalkmeshPanel)
            {
                columnRightPanel.MinWidth = 215;
                columnRightPanel.Width = new GridLength(prevRightPanelSize, GridUnitType.Pixel);
            }
            else
            {
                columnRightPanel.MinWidth = 0;
                prevRightPanelSize = columnRightPanel.ActualWidth;
                columnRightPanel.Width = new GridLength(1, GridUnitType.Auto);
            }
        }

        private void TxtOffRimsFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(lvOn.ItemsSource).Refresh();
            CollectionViewSource.GetDefaultView(lvOff.ItemsSource).Refresh();
        }

        private bool HandleListFilter(object item)
        {
            if (string.IsNullOrEmpty(txtRimsFilter.Text)) return true;
            else return (item as RimModel).FileName.IndexOf(txtRimsFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (item as RimModel).Planet.IndexOf(txtRimsFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (item as RimModel).CommonName.IndexOf(txtRimsFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ClearOffRimsFilter_Click(object sender, RoutedEventArgs e)
        {
            txtRimsFilter.Clear();
        }

        #endregion

        #region Live Position Methods

        private void ShowLivePosition_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ShowLivePosition = true;

            if (LivePositionWorker.IsBusy)
            {
                LivePositionWorker.CancelAsync();
            }
            else
            {
                BringLivePositionPointToTop();
                LivePositionWorker.RunWorkerAsync();
            }
        }

        private void ShowLivePosition_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !(LivePositionWorker.IsBusy && LivePositionWorker.CancellationPending);
        }

        private void LivePositionWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var sw = new Stopwatch();
            sw.Start();
            var bw = sender as BackgroundWorker;
            int lastVersion = 0;
            int version = 0;
            KotorManager km = null;
            var thisModuleName = string.Empty;

            while (true)
            {
                try
                {
                    if (bw.CancellationPending) break;
                    if (km == null)
                    {
                        thisModuleName = string.Empty;
                        version = GetRunningKotor();
                        if (version != 0)
                        {
                            if (version != lastVersion) Application.Current.Dispatcher.Invoke(() =>
                            {
                                KotorClasses = version == 1 ? Kotor1Classes : Kotor2Classes;
                                KotorFeats = version == 1 ? Kotor1Feats : Kotor2Feats;
                                KotorPowers = version == 1 ? Kotor1Powers : Kotor2Powers;
                                txtInfluence.Visibility = version == 2 ? Visibility.Visible : Visibility.Collapsed;
                                cbInfluence.Visibility = version == 2 ? Visibility.Visible : Visibility.Collapsed;
                                tbInfluenceValue.Visibility = version == 2 ? Visibility.Visible : Visibility.Collapsed;
                                btnInfluence.Visibility = version == 2 ? Visibility.Visible : Visibility.Collapsed;
                            });
                            lastVersion = version;

                            km = new KotorManager(version);
                            if (!km.TestRead())
                            {
                                km = null;
                                continue;
                            }
                            else if(!km.SetLoadDirection())
                            {
                                km = null;
                                Thread.Sleep(Math.Max(LivePositionUpdateDelay - (int)sw.ElapsedMilliseconds, 0));
                                sw.Restart();
                            }
                        }
                    }
                    if (km != null)
                    {
                        try
                        {
                            if (!IsBusy && UpdateLookingAtId) Application.Current.Dispatcher.Invoke(() =>
                            {
                                try
                                {
                                    GetTargetID_Click(null, null);
                                }
                                catch (Exception) { }
                            });

                            string nextModuleName = string.Empty;

                            if (ShowCurrentLiveModule || HidePreviousLiveModule)
                            {
                                // Get current module
                                km.pr.ReadAreaName(version, km.ka.AREA_NAME, out nextModuleName);
                                if (nextModuleName == null) { km = null; continue; }
                                var null_index = nextModuleName.IndexOf('\0');
                                if (null_index >= 0) nextModuleName = nextModuleName.Substring(0, null_index);
                            }
                            else
                            {
                                thisModuleName = string.Empty;
                                nextModuleName = string.Empty;
                            }

                            // If HidePreviousLiveModule
                            if (HidePreviousLiveModule && thisModuleName != nextModuleName)
                            {
                                // Hide previous module.
                                var lastRim = OnRims.FirstOrDefault(rm => rm.FileName == thisModuleName.ToLower());
                                if (lastRim != null) Application.Current.Dispatcher.Invoke(() => RemoveRim(lastRim));
                                while (IsBusy) { Thread.Sleep(10); }
                            }

                            // If ShowCurrentLiveModule
                            if (ShowCurrentLiveModule)
                            {
                                var thisRim = OffRims.FirstOrDefault(rm => rm.FileName == nextModuleName.ToLower());
                                if (thisRim != null) Application.Current.Dispatcher.Invoke(() => AddRim(thisRim));
                            }

                            // If current module not shown, display it
                            if (thisModuleName != nextModuleName)
                            {
                                // Update address information
                                thisModuleName = nextModuleName;
                                km.RefreshAddresses();
                            }

                            // Get current position and bearing
                            km.pr.ReadUint(km.GetPartyAddress(), out uint partyCount);
                            LivePartyCount = partyCount;
                            var partyPositions3D = km.GetPartyPositions3D();
                            var partyBearings = km.GetPartyBearings();
                            var partyInRange = true;

                            // Handle party leader
                            LivePositionPoint = new Point(partyPositions3D[0].X, partyPositions3D[0].Y);

                            if (LeftClickPointVisible)
                            {
                                DistanceLiveLeft = (LeftClickModuleCoords - LivePositionPoint).Length;
                                DurationLiveLeft  = DistanceLiveLeft  / SPEED_UNITS_PER_SECOND / DistanceToTimeMultiplier;
                            }
                            if (RightClickPointVisible)
                            {
                                DistanceLiveRight = (RightClickModuleCoords - LivePositionPoint).Length;
                                DurationLiveRight = DistanceLiveRight / SPEED_UNITS_PER_SECOND / DistanceToTimeMultiplier;
                            }

                            LiveLeaderBearing = partyBearings[0];
                            LivePositionEllipsePoint = new Point(LivePositionPoint.X + LeftOffset - 0.5, LivePositionPoint.Y + BottomOffset - 0.5);

                            if (!LockGatherPartyRange)
                            {
                                LiveGatherPartyRangePoint = LivePositionEllipsePoint;
                                LastGatherPartyRangePosition = partyPositions3D[0];
                                LastGatherPartyRangePoint = LivePositionEllipsePoint;
                            }
                            else
                            {
                                var leaderDistSq = (LastGatherPartyRangePosition - partyPositions3D[0]).LengthSquared;
                                partyInRange = partyInRange && leaderDistSq <= 900.0;
                            }

                            // Handle party member 1
                            if (partyCount > 1)
                            {
                                double leaderDistSq = 0;
                                if (!LockGatherPartyRange)
                                    leaderDistSq = (partyPositions3D[0] - partyPositions3D[1]).LengthSquared;
                                else
                                    leaderDistSq = (LastGatherPartyRangePosition - partyPositions3D[1]).LengthSquared;
                                partyInRange = partyInRange && leaderDistSq <= 900.0;
                                LivePositionEllipsePointPC1 = new Point(partyPositions3D[1].X + LeftOffset - 0.5, partyPositions3D[1].Y + BottomOffset - 0.5);
                                LiveBearingPC1 = partyBearings[1];
                            }

                            // Handle party member 2
                            if (partyCount > 2)
                            {
                                double leaderDistSq = 0;
                                if (!LockGatherPartyRange)
                                    leaderDistSq = (partyPositions3D[0] - partyPositions3D[2]).LengthSquared;
                                else
                                    leaderDistSq = (LastGatherPartyRangePosition - partyPositions3D[2]).LengthSquared;
                                partyInRange = partyInRange && leaderDistSq <= 900.0;
                                LivePositionEllipsePointPC2 = new Point(partyPositions3D[2].X + LeftOffset - 0.5, partyPositions3D[2].Y + BottomOffset - 0.5);
                                LiveBearingPC2 = partyBearings[2];
                            }

                            LiveGatherPartyRangeStrokeBrush = partyInRange ? gprStrokeGreen : gprStrokeRed;
                            LiveGatherPartyRangeFillBrush   = partyInRange ? gprFillGreen   : gprFillRed;

                            // Follow live position
                            if (ViewFollowsLivePosition)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    var scale = zoomAndPanControl.ContentScale;
                                    var yExtent = zoomAndPanControl.ExtentHeight;
                                    var snapPoint = new Point(
                                        LivePositionEllipsePoint.X,
                                        (yExtent/scale)-LivePositionEllipsePoint.Y);
                                    zoomAndPanControl.SnapTo(snapPoint);
                                });
                            }

                            // If position not found, refresh.
                            if (km.pr.hasFailed) km.RefreshAddresses();
                        }
                        catch (NullReferenceException ex)
                        {
                            Console.WriteLine($"NullReferenceException caught: {ex.Message}");
                            km = null;
                            continue;
                        }
                        catch (Win32Exception ex)
                        {
                            Console.WriteLine($"Win32Exception caught: {ex.Message}");
                            km = null;
                            continue;
                        }
                    }
                    //Console.WriteLine(sw.ElapsedMilliseconds);
                    Thread.Sleep(Math.Max(LivePositionUpdateDelay - (int)sw.ElapsedMilliseconds, 0));
                    sw.Restart();
                }
                catch (NullReferenceException ex)
                {
                    Console.WriteLine($"NullReferenceException caught: {ex.Message}");
                    km = null;
                    continue;
                }
                catch (Win32Exception ex)
                {
                    Console.WriteLine($"Win32Exception caught: {ex.Message}");
                    continue;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unknown exception caught: {ex}");
                    break;
                }
            }
        }

        private static void WriteToConsoleAllDoorsInLiveModule(KotorManager km, string nextModuleName)
        {
            var doors = km.GetDoorCorners();
            Console.WriteLine();
            Console.WriteLine($"\t\t\"Module\": \"{nextModuleName.ToLower()}\",");
            Console.WriteLine($"\t\t\"Doors\": [");
            for (int i = 0; i < doors.Count; i++)
            {
                var door = doors[i];
                var cornersX = string.Empty;
                var cornersY = string.Empty;
                for (int j = 0; j < door.Item2.Count; j++)
                {
                    var c = door.Item2[j];
                    cornersX += $"\"{c.x}\"";
                    cornersY += $"\"{c.y}\"";
                    if (j+1 < door.Item2.Count)
                    {
                        cornersX += ", ";
                        cornersY += ", ";
                    }
                }

                Console.WriteLine("\t\t\t{");
                Console.WriteLine($"\t\t\t\t\"ResRef\": \"{door.Item1.ToLower()}\",");
                Console.WriteLine($"\t\t\t\t\"CornersX\": [{cornersX}],");
                Console.WriteLine($"\t\t\t\t\"CornersY\": [{cornersY}]");
                var text = "\t\t\t}";
                if (i+1 != doors.Count) text += ",";
                Console.WriteLine(text);
            }
            Console.WriteLine($"\t\t]");
            Console.WriteLine();
        }

        private int GetRunningKotor()
        {
            while (IsBusy) { Thread.Sleep(10); }

            var version = 0;
            var gameName = string.Empty;
            var otherGameName = string.Empty;
            XmlGame xmlGame = null;
            var process = Process.GetProcessesByName("swkotor").FirstOrDefault() ??
                          Process.GetProcessesByName("swkotor2").FirstOrDefault();
            if (process == null) return version;

            if (process.ProcessName == "swkotor")
            {
                version = 1;
                gameName = K1_NAME;
                otherGameName = K2_NAME;
                xmlGame = XmlGameData.Kotor1Data;
            }

            if (process.ProcessName == "swkotor2")
            {
                version = 2;
                gameName = K2_NAME;
                otherGameName = K1_NAME;
                xmlGame = XmlGameData.Kotor2Data;
            }

            if (HotswapToLiveGame)
            {
                // Close other game if needed.
                if (Game == otherGameName)
                {
                    Application.Current.Dispatcher.Invoke(() => SwapGame_Executed(null, null));
                    while (IsBusy) { Thread.Sleep(10); }
                }

                // Load live game
                if (Game != gameName)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var exeFile = new FileInfo(process.MainModule.FileName);
                        CurrentGame = xmlGame;
                        RimDataSet.LoadGameData(exeFile.DirectoryName);
                        LoadGameFiles(exeFile.DirectoryName, gameName);
                    });
                    while (IsBusy) { Thread.Sleep(10); }
                }
            }

            return version;
        }

        private void LivePositionWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ShowLivePosition = false;
        }

        #endregion  // ENDREGION Live Position Methods

        #region Mouse Hover Methods

        private void RunMouseHoverWorker_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ShowRimDataUnderMouse = true;

            if (MouseHoverWorker.IsBusy)
            {
                MouseHoverToggleButton.IsEnabled = false;
                MouseHoverWorker.CancelAsync();
            }
            else
            {
                MouseHoverWorker.RunWorkerAsync();
            }
        }

        private void MouseHoverWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var sw = new Stopwatch();
            sw.Start();
            var mousePosition = new Point();
            IEnumerable<RimDataInfo> visibleRimData = new List<RimDataInfo>();
            var bw = sender as BackgroundWorker;

            while (true)
            {
                if (bw.CancellationPending) break;

                content.Dispatcher.Invoke(() =>
                {
                    mousePosition = Mouse.GetPosition(content);
                    mousePosition.X -= LeftOffset;
                    mousePosition.Y = theGrid.Height - mousePosition.Y - BottomOffset;
                    visibleRimData = RimDoors
                        .Concat(RimTriggers)
                        .Concat(RimTraps)
                        .Concat(RimZones)
                        .Concat(RimEncounters)
                        .Where(r => r.MeshVisible)
                        .Where(r => r.IsTouching(mousePosition));
                });

                RimDataUnderMouse = visibleRimData.ToList();
                Thread.Sleep(Math.Max(LivePositionUpdateDelay - (int)sw.ElapsedMilliseconds, 0));
                sw.Restart();
            }
        }

        private string GetBrushName(Brush brush) => BrushToName.ContainsKey(brush) ? BrushToName[brush] : "Unknown";

        private void MouseHoverWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ShowRimDataUnderMouse = false;
            RimDataUnderMouse = new List<RimDataInfo>();
            MouseHoverToggleButton.IsEnabled = true;
        }

        #endregion

        #region Canvas Visibility

        private void UpdateRimVisibility(RimModel rim)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RimFullCanvasLookup[rim.FileName].Visibility = OnRims.Contains(rim) ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        private void UpdateWalkableVisibility()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RimWalkableCanvasLookup.Values.ToList().ForEach((Canvas c) =>
                {
                    c.Visibility = ShowWalkableFaces ? Visibility.Visible : Visibility.Collapsed;
                });
            });
        }

        private void UpdateNonWalkableVisibility()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RimNonwalkableCanvasLookup.Values.ToList().ForEach((Canvas c) =>
                {
                    c.Visibility = ShowNonWalkableFaces ? Visibility.Visible : Visibility.Collapsed;
                });
            });
        }

        private void UpdateDefaultSpawnPointVisibility()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RimDefaultSpawnPointCanvasLookup.Values.ToList().ForEach((Canvas c) =>
                {
                    c.Visibility = ShowDefaultSpawnPoints ? Visibility.Visible : Visibility.Collapsed;
                });
            });
        }

        private void UpdateTransAbortPointVisibility()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RimTransAbortPointCanvasLookup.Values.ToList().ForEach((Canvas c) =>
                {
                    c.Visibility = ShowTransAbortPoints ? Visibility.Visible : Visibility.Collapsed;
                });
            });
        }

        private void UpdateTransAbortRegionVisibility()
        {
            var rimmodel = OnRims.FirstOrDefault();
            if (ShowTransAbortRegions)
            {
                content.Background = Brushes.Black;
                CoordinateTextBrush = Brushes.White;
                if (rimmodel == null) return;
                UpdateRimFillColor(null, GrayScaleBrush, rimmodel);
            }
            else
            {
                content.Background = BackgroundColors[SelectedBackgroundColor];
                CoordinateTextBrush = ForegroundColors[SelectedBackgroundColor];
                if (rimmodel == null) return;
                UpdateRimFillColor(null, RimToBrushUsed[rimmodel.FileName], rimmodel);
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                RimTransAbortRegionCanvasLookup.Values.ToList().ForEach((Canvas c) =>
                {
                    c.Visibility = ShowTransAbortRegions ? Visibility.Visible : Visibility.Collapsed;
                });
            });
            // Code for multiple module display with GP regions.
            //foreach (var rimmodel in OnRims)
            //{
            //    if (ShowTransAbortRegions && SelectedGatherPartyRim == rimmodel)
            //    {
            //        Application.Current.Dispatcher.Invoke(() => content.Background = Brushes.Black);
            //        CoordinateTextBrush = Brushes.White;
            //        if (rimmodel == null) return;
            //        UpdateRimFillColor(null, GrayScaleBrush, rimmodel);
            //    }
            //    else
            //    {
            //        Application.Current.Dispatcher.Invoke(() => content.Background = BackgroundColors[SelectedBackgroundColor]);
            //        CoordinateTextBrush = ForegroundColors[SelectedBackgroundColor];
            //        if (rimmodel == null) return;
            //        UpdateRimFillColor(null, RimToBrushUsed[rimmodel.FileName], rimmodel);
            //    }

            //    Application.Current.Dispatcher.Invoke(() => RimTransAbortRegionCanvasLookup[rimmodel.FileName].Visibility =
            //        ShowTransAbortRegions && SelectedGatherPartyRim == rimmodel ? Visibility.Visible : Visibility.Collapsed);
            //}
        }

        private void UpdateLayerVisibilityWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BuildNewWalkmeshes(UpdateLayerVisibilityWorker, true);
        }

        private void ShowWalkableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is VisualizerWindow) ShowWalkableFaces = !ShowWalkableFaces;

            // If any OnRim is not in Keys, build walkmeshes...
            if (OnRims.Any(r => !RimPolyLookup.ContainsKey(r.FileName)))
            {
                IsBusy = true;
                UpdateLayerVisibilityWorker.RunWorkerAsync();
            }

            UpdateWalkableVisibility();
        }

        private void ShowNonWalkableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is VisualizerWindow) ShowNonWalkableFaces = !ShowNonWalkableFaces;

            // If any OnRim is not in Keys, build walkmeshes...
            if (OnRims.Any(r => !RimOutlinePolyLookup.ContainsKey(r.FileName)))
            {
                IsBusy = true;
                UpdateLayerVisibilityWorker.RunWorkerAsync();
            }

            UpdateNonWalkableVisibility();
        }

        private void ShowTransAbortCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is VisualizerWindow) ShowTransAbortPoints = !ShowTransAbortPoints;

            // If any OnRim is not in Keys, build walkmeshes...
            if (OnRims.Any(r => !RimTransAbortPoints.ContainsKey(r.FileName)))
            {
                IsBusy = true;
                UpdateLayerVisibilityWorker.RunWorkerAsync();
            }

            UpdateTransAbortPointVisibility();
        }

        private void ShowTransAbortRegionCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e?.OriginalSource is VisualizerWindow) ShowTransAbortRegions = !ShowTransAbortRegions;

            // If any OnRim is not in Keys, build walkmeshes...
            if (OnRims.Any(r => !RimTransRegions.ContainsKey(r.FileName)))
            {
                IsBusy = true;
                UpdateLayerVisibilityWorker.RunWorkerAsync();
            }

            UpdateTransAbortRegionVisibility();
        }

        private void ShowDefaultSpawnPoints_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is VisualizerWindow) ShowDefaultSpawnPoints = !ShowDefaultSpawnPoints;

            // If any OnRim is not in Keys, build walkmeshes...
            if (OnRims.Any(r => !RimDefaultSpawnPoint.ContainsKey(r.FileName)))
            {
                IsBusy = true;
                UpdateLayerVisibilityWorker.RunWorkerAsync();
            }

            UpdateDefaultSpawnPointVisibility();
        }

        private void ShowDlzLines_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is VisualizerWindow) ShowDlzLines = !ShowDlzLines;
            var visibleRimDataInfo = RimDoors.Where(i => ShowRimDataDoorsDlzLines && i.MeshVisible)
                .Concat(RimTriggers.Where(i => ShowRimDataTriggersDlzLines && i.MeshVisible))
                .Concat(RimTraps.Where(i => ShowRimDataTrapsDlzLines && i.MeshVisible))
                .Concat(RimZones.Where(i => ShowRimDataZonesDlzLines && i.MeshVisible))
                .Concat(RimEncounters.Where(i => ShowRimDataEncountersDlzLines && i.MeshVisible));
            if (ShowDlzLines)
                foreach (var rdi in visibleRimDataInfo) rdi.LineColor = rdi.MeshColor;
            else
                foreach (var rdi in visibleRimDataInfo) HideDlzLines(rdi);
        }

        private void ShowAllOfRimData_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var rdis = ((e.Source as Button)
                .Tag as ObservableCollection<RimDataInfo>)
                .Where(rdi => !rdi.MeshVisible)
                .ToList();
            foreach (var rdi in rdis) HandleRimDataInfo(rdi);
        }

        private void ShowAllOfRimData_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var anyHidden = ((e.Source as Button)
                ?.Tag as ObservableCollection<RimDataInfo>)
                ?.Any(rdi => !rdi.MeshVisible) ?? false;
            e.CanExecute = !IsBusy && anyHidden;
        }

        private void HideAllOfRimData_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var rdis = ((e.Source as Button)
                .Tag as ObservableCollection<RimDataInfo>)
                .Where(rdi => rdi.MeshVisible)
                .ToList();
            foreach (var rdi in rdis) HandleRimDataInfo(rdi);
        }

        private void HideAllOfRimData_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var anyVisible = ((e.Source as Button)
                ?.Tag as ObservableCollection<RimDataInfo>)
                ?.Any(rdi => rdi.MeshVisible) ?? false;
            e.CanExecute = !IsBusy && anyVisible;
        }

        private void ShowRimDataDlzLines_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!ShowDlzLines) return;
            var btn = e.Source as ToggleButton;
            var visibleRimDataInfo = (btn.Tag as ObservableCollection<RimDataInfo>).Where(i => i.MeshVisible);
            if (btn.IsChecked.HasValue && btn.IsChecked.Value)
                foreach (var rdi in visibleRimDataInfo) rdi.LineColor = rdi.MeshColor;
            else
                foreach (var rdi in visibleRimDataInfo) HideDlzLines(rdi);
        }

        #endregion

        #region Live Tools Panel Methods
        private KotorManager GetKotorManager()
        {
            KotorManager km = null;
            try
            {
                km = new KotorManager(GetRunningKotor());
                if (!km.TestRead() || !km.SetLoadDirection()) km = null;
            }
            catch (Exception) { }
            return km;
        }

        /*
         * Movement
         */
        private void TeleportPlayerToPoint_Click(object sender, RoutedEventArgs e)
        {
            var point = ((e.Source as Button).Tag as Point?).Value;
            var km = GetKotorManager();
            if (km == null) return;
            kmia.SendMessage(
                km.pr.h,
                kmia.TeleportPlayer(
                    km.GetClientPlayerID(),
                    (float)point.X,
                    (float)point.Y,
                    0f));
        }

        private void SetMoveSpeedMultiplier_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;
            var isFloat = float.TryParse(MoveSpeedMultiplier, out float msm);
            if (isFloat)
                kmih.setRunrate(km.pr.h, kmia.GetPlayerServerObject(km.pr.h), DEFAULT_MOVEMENT_SPEED * msm);
            else
                MessageBox.Show("Move speed multiplier must be a floating point number.");
        }

        private void WarpCheat_Click(object sender, RoutedEventArgs e)
        {
            var module = cbbWarpToRim.SelectedItem.ToString();
            if (string.IsNullOrEmpty(module)) return;
            var km = GetKotorManager();
            if (km == null) return;
            kmia.Warp(km.pr.h, module);
        }

        /*
         * Party Members
         */
        private void HealLeaderCheat_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;
            kmia.SendMessage(km.pr.h, kmia.Heal());
        }

        private void StartGatherPartyDialog_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;
            var script = (e.Source as Button).Tag.ToString() == "Warp"
                ? "k_trg_transfail1"
                : "k_trg_transfail";
            kmia.SendMessage(
                km.pr.h,
                kmia.RunScript(
                    script,
                    km.pr.h));
        }

        private void UnlockFullParty_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;
            var script = km.version == 1
                ? "k_cheat_01"
                : "a_debugparty";
            kmia.SendMessage(
                km.pr.h,
                kmia.RunScript(
                    script,
                    km.pr.h));
        }

        private void ShowPartySelect_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;
            kmia.ShowPartySelection(km.pr.h);
        }

        private void SwapToTargetCreature_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;
            var target = kmih.getLookingAtClientID(km.pr.h);
            kmia.SendMessage(km.pr.h, kmia.SwapToTarget(target));
        }

        private void SetAlignment_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;

            uint id;
            if (cbAlignmentTarget.Text == "Player")
                id = kmih.getPlayerServerID(km.pr.h);
            else
                id = kmih.getLookingAtServerID(km.pr.h);
            var obj = kmia.GetServerObject(km.pr.h, id);
            km.RefreshAddresses();

            kmih.SetAlignment(km.pr.h, obj, (short)ValueInAlignmentBox);
        }

        private void SetInfluence_Click(object sender, RoutedEventArgs e)
        {
            if (cbInfluence.Text == string.Empty || Game != K2_NAME) return;    // Only works for K2. Exit if no influence target is selected.
            var km = GetKotorManager();
            if (km == null) return;

            // Get party members to adjust influence.
            var npcs = new List<string>();
            if (cbInfluence.Text == ALL_ABILITIES)
                npcs = [.. KotorPartyMembers];
            else npcs.Add(cbInfluence.Text);

            // Adjust influence.
            foreach (var npc in npcs)
            {
                if (npc == ALL_ABILITIES) continue;
                km.RefreshAddresses();
                kmia.SetPCInfluenceKotor2(km.pr.h, npc.ToEnum<kmih.PARTY_NPCS_K2>(), ValueInInfluenceBox);
            }
        }

        /*
         * Attributes, Skills, Feats, and Powers (Abilities)
         */
        private void tbPreviewIntegerInput(object sender, TextCompositionEventArgs e)
        {
            // TODO: Consider implementing paste handling for any text box that should be limited.
            //DataObject.AddPastingHandler
            var r = new Regex(@"^-$|^-?[0-9]+$");
            if (!r.IsMatch(e.Text))
                e.Handled = true;
        }

        private void tbPreviewIntegerKeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Space) e.Handled = true; }

        private void AddClass_Click(object sender, RoutedEventArgs e)
        {
            if (cbClass.Text == string.Empty) return;           // Exit if no attribute selected
            var km = GetKotorManager();
            if (km == null) return;

            // Set class
            var player = kmia.GetPlayerServerObject(km.pr.h);
            km.RefreshAddresses();
            kmia.AddCreatureClass(km.pr.h, player, KotorClassLookup[cbClass.Text]);
        }

        private void AddExperience_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;

            // Add experience
            var player = kmia.GetPlayerServerObject(km.pr.h);
            km.RefreshAddresses();
            kmia.AddCreatureExp(km.pr.h, player, (uint)ValueInExperienceBox);
        }

        private void MaximumAbilities_Click(object sender, RoutedEventArgs e)
        {
            tbExperienceValue.Text = SelectedGame == K1_NAME ? "250000" : "1250000";
            cbAttribute.SelectedItem = KotorAttributes.FirstOrDefault();
            tbAttributeValue.Text = "255";
            cbSkill.SelectedItem = KotorAttributes.FirstOrDefault();
            tbSkillValue.Text = "127";
            cbFeat.SelectedItem = KotorFeats.FirstOrDefault();
            cbPower.SelectedItem = KotorPowers.FirstOrDefault();
        }

        private void AllAbilities_Click(object sender, RoutedEventArgs e)
        {
            SetAttribute_Click(sender, e);
            SetSkill_Click(sender, e);
            AddFeat_Click(sender, e);
        }

        private void SetAttribute_Click(object sender, RoutedEventArgs e)
        {
            if (cbAttribute.Text == string.Empty) return;           // Exit if no attribute selected
            var km = GetKotorManager();
            if (km == null) return;

            // Get attributes to set
            var attrs = new List<string>();
            if (cbAttribute.Text == ALL_ABILITIES)
                attrs = KotorAttributes.ToList();
            else
                attrs.Add(cbAttribute.Text);

            // Set attributes
            var player = kmia.GetPlayerServerObject(km.pr.h);
            km.RefreshAddresses();
            foreach (var attr in attrs)
            {
                if (attr == ALL_ABILITIES) continue;
                kmia.SetCreatureAttribute(
                    km.pr.h, player,
                    attr.ToEnum<kmih.ATTRIBUTES>(),
                    (byte)ValueInAttributeBox);
                km.RefreshAddresses();
            }
        }

        private void SetSkill_Click(object sender, RoutedEventArgs e)
        {
            if (cbSkill.Text == string.Empty) return;               // Exit if no skill selected
            var km = GetKotorManager();
            if (km == null) return;

            // Get skills to set
            var skills = new List<string>();
            if (cbSkill.Text == ALL_ABILITIES)
                skills = KotorSkills.ToList();
            else skills.Add(cbSkill.Text);

            // Get skill value
            byte value;
            if (ValueInSkillBox >= 0)
                value = (byte)ValueInSkillBox;
            else value = (byte)(ValueInSkillBox + 256);             // Convert signed to unsigned

            // Set skills
            var player = kmia.GetPlayerServerObject(km.pr.h);
            km.RefreshAddresses();
            foreach (var skill in skills)
            {
                if (skill == ALL_ABILITIES) continue;
                kmia.SetCreatureSkill(km.pr.h, player, skill.ToEnum<kmih.SKILLS>(), value);
                km.RefreshAddresses();
            }
        }

        private void AddFeat_Click(object sender, RoutedEventArgs e)
        {
            if (cbFeat.Text == string.Empty) return;                // Exit if no feat selected
            var km = GetKotorManager();
            if (km == null) return;

            // Get feats to add
            var feats = new List<string>();
            if (cbFeat.Text == ALL_ABILITIES)
                feats = KotorFeats.ToList();
            else feats.Add(cbFeat.Text);

            // Add feats
            var player = kmia.GetPlayerServerObject(km.pr.h);
            km.RefreshAddresses();
            foreach (var feat in feats)
            {
                if (feat == ALL_ABILITIES) continue;
                kmia.AddCreatureFeat(km.pr.h, player, feat.ToEnum<kmih.FEATS>());
                km.RefreshAddresses();
            }
        }

        private void ResetFeats_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;

            // Get feats to add as default
            var feats = new List<kmih.FEATS>()
            {
                kmih.FEATS.ARMOUR_PROF_LIGHT,
                kmih.FEATS.WEAPON_PROF_BLASTER,
                kmih.FEATS.WEAPON_PROF_BLASTER_RIFLE,
                kmih.FEATS.WEAPON_PROF_MELEE_WEAPONS,
            };

            // Add feats
            var player = kmia.GetPlayerServerObject(km.pr.h);
            km.RefreshAddresses();
            kmia.SetCreatureFeats(km.pr.h, player, feats);
        }

        private void AddPower_Click(object sender, RoutedEventArgs e)
        {
            if (cbPower.Text == string.Empty) return;               // Exit if no power selected
            var km = GetKotorManager();
            if (km == null) return;

            // Get powers to add
            var powers = new List<string>();
            if (cbPower.Text == ALL_ABILITIES)
                powers = KotorPowers.ToList();
            else powers.Add(cbPower.Text);

            // Add powers
            var player = kmia.GetPlayerServerObject(km.pr.h);
            km.RefreshAddresses();
            foreach (var power in powers)
            {
                if (power == ALL_ABILITIES) continue;
                kmia.AddCreatureSpell(km.pr.h, player, (byte)cbPowerTarget.SelectedIndex, power.ToEnum<kmih.SPELLS>());
                km.RefreshAddresses();
            }
        }

        /*
         * Inventory
         */
        private void SetCredits_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;

            var player = kmia.GetPlayerServerObject(km.pr.h);
            km.RefreshAddresses();
            kmia.SetCreatureCredits(km.pr.h, player, ValueInSetCreditsBox);
        }

        private void ShowItemCreate_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;
            kmia.ShowItemCreateMenu(km.pr.h);
        }

        private void GiveItem_Click(object sender, RoutedEventArgs e)
        {
            // 
        }

        /*
         * Rendering
         */
        private void SetGameGui_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;
            km.SetRenderGui((sender as Button).Tag.ToString() == "on");
        }

        private void SetGameAABB_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;
            km.SetRenderAABB((sender as Button).Tag.ToString() == "on");
        }

        private void SetGameWireframe_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;
            km.SetRenderWireframe((sender as Button).Tag.ToString() == "on");
        }

        private void SetGameTrigger_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;
            km.SetRenderTrigger((sender as Button).Tag.ToString() == "on");
        }

        private void SetGameTriggerColor_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;
            km.SetRenderTriggerColor(ValueInTriggerRedBox / 100f, ValueInTriggerGreenBox / 100f,
                                     ValueInTriggerBlueBox / 100f, ValueInTriggerAlphaBox / 100f);
        }

        private void SetGamePersonalSpace_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;
            km.SetRenderPersonalSpace((sender as Button).Tag.ToString() == "on");
        }

        private void SetGamePlaceholders_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;
            km.SetRenderPlaceholders((sender as Button).Tag.ToString() == "on");
        }

        /*
         * Free Camera
         */
        private void TurnOnFreeCam_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;
            km.SetFreeCamSpeed(FreeCamSpeed);
            kmia.SendMessage(km.pr.h, kmia.FreeCamOn());
            km.RefreshAddresses();
            kmia.SetVisibilityGraph(km.pr.h, false);
        }

        private void TurnOffFreeCam_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;
            km.SetFreeCamSpeed(10f);
            kmia.SendMessage(km.pr.h, kmia.FreeCamOff());
            km.RefreshAddresses();
            kmia.SetVisibilityGraph(km.pr.h, true);
        }

        //private void ResetFreeCamSpeed_Click(object sender, RoutedEventArgs e)
        //{
        //    FreeCamSpeed = 10f;
        //}

        //private void FreeCamSpeed_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        //{
        //    var version = GetRunningKotor();
        //    if (version == 0) return;
        //    var km = GetKotorManager();
        //    if (km == null) return;
        //    km.SetFreeCamSpeed(FreeCamSpeed);
        //}

        private void TurnOffFog_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;
            kmia.SetSceneFogOff(km.pr.h);
        }

        /*
         * Affect Target
         */
        private void DeleteTargetDoor_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Update kmi adapter to handle getLokingAtClientID.
            var km = GetKotorManager();
            if (km == null) return;
            var target = kmih.getLookingAtClientID(km.pr.h);
            kmia.SendMessage(km.pr.h, kmia.DeleteTargetDoor(target));
        }

        private void KillTargetCreature_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;
            var handle = km.pr.h;
            var player = kmih.getPlayerServerID(handle);
            var target = kmih.getLookingAtServerID(handle);
            kmia.SendMessage(handle, kmia.KillTargetCreature(player, target, handle));
        }

        private void PeekTargetContainer_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;
            var target = kmih.getLookingAtClientID(km.pr.h);
            kmia.SendMessage(km.pr.h, kmia.PeekContainerContents(target));
        }

        /*
         * Cheats
         */
        private void InvulnerabilityCheat_Click(object sender, RoutedEventArgs e)
        {
            var km = GetKotorManager();
            if (km == null) return;
            kmia.SendMessage(km.pr.h, kmia.Invulnerability());
        }

        private void ShowCustomMessageBox(string message, bool showCancel = false)
        {
            var km = GetKotorManager();
            if (km == null) return;
            kmia.CreatePopUp(km.pr.h, message, showCancel);
        }

        #endregion // Live Tools Panel Methods

        #region Wire Targeting Panel Methods

        private void GetTargetID_Click(object sender, RoutedEventArgs e)
        {
            txtTargetID.Text = string.Empty;
            var km = GetKotorManager();
            if (km == null) return;
            var id = kmih.getLookingAtClientID(km.pr.h);
            //var id = kmih.getLookingAtServerID(km.pr.h);
            txtTargetID.Text = "0x" + id.ToString("X");
        }

        public class KotorGameObject
        {
            public uint VTable { get; set; }
            public uint ID { get; set; }
            public GameObjectTypes Type { get; set; }
            public string Tag { get; set; }
            public string Name { get; set; }

            public override string ToString() =>
                "id: 0x" + ID.ToString("X") +
                ", type: " + Type +
                ", tag: " + Tag;
        }

        private void GetAllIDs_Click(object sender, RoutedEventArgs e)
        {
            AreaGameObjects.Clear();
            var km = GetKotorManager();
            if (km == null) return;

            var objPtrs = km.GetAllObjectsInArea();
            var objs = new List<KotorGameObject>();
            foreach (var objPtr in objPtrs)
            {
                var obj = km.GetGameObjectByServerID(objPtr);
                km.pr.ReadUint(obj, out uint vtable);
                km.pr.ReadUint(obj + km.ka.OFFSET_GAME_OBJECT_ID, out uint serverID);
                var clientID = kmih.serverToClientId(serverID);
                km.pr.ReadByte(obj + km.ka.OFFSET_GAME_OBJECT_TYPE, out byte type);
                var serverObject = kmia.GetServerObject(km.pr.h, serverID);
                km.RefreshAddresses();
                objs.Add(new KotorGameObject
                {
                    VTable = vtable,
                    ID = clientID,
                    Type = (GameObjectTypes)type,
                    Tag = kmih.getServerObjectTag(km.pr.h, serverObject),
                    Name = kmia.GetClientObjectName(km.pr.h, clientID),
                });
                km.RefreshAddresses();
            }

            foreach (var obj in objs.OrderBy(o => o.ID))
                AreaGameObjects.Add(obj);
        }

        private void TxtAreaIdsFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(lvAreaIds.ItemsSource).Refresh();
        }

        private bool HandleAreaIdsFilter(object item)
        {
            if (string.IsNullOrEmpty(txtAreaIdsFilter.Text)) return true;
            var kgo = item as KotorGameObject;
            return kgo.Type.ToString().Contains(txtAreaIdsFilter.Text, StringComparison.OrdinalIgnoreCase)
                || ("0x" + kgo.ID.ToString("X")).Contains(txtAreaIdsFilter.Text, StringComparison.OrdinalIgnoreCase)
                || kgo.Tag.Contains(txtAreaIdsFilter.Text, StringComparison.OrdinalIgnoreCase)
                || kgo.Name.Contains(txtAreaIdsFilter.Text, StringComparison.OrdinalIgnoreCase);
        }

        private void ClearAreaIdsFilter_Click(object sender, RoutedEventArgs e)
        {
            txtAreaIdsFilter.Clear();
        }

        private void SearchForId_Click(object sender, RoutedEventArgs e)
        {
            txtAreaIdsFilter.Text = txtTargetID.Text;
        }

        #endregion // Wire Targeting Panel Methods
    }
}
