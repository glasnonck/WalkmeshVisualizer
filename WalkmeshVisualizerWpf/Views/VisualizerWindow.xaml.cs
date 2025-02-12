using System;
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
using KotOR_IO;
using KotOR_IO.GffFile;
using KotOR_IO.Helpers;
using Microsoft.Win32;
using WalkmeshVisualizerWpf.Helpers;
using WalkmeshVisualizerWpf.Models;
using ZoomAndPan;

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

            // Grab current brush theme.
            BrushToName = BrushThemeMuted;
            foreach (var kvp in BrushToName) PolyBrushCount.Add(kvp.Key, 0);

            BrushCycle = new List<Brush>(PolyBrushCount.Keys);
            CurrentRimDataInfoBrush = BrushCycle.First();

            BrushToName.Add(Brushes.Black, "Black");
            BrushToName.Add(Brushes.White, "White");

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
            ShowWalkmeshPanel = settings.ShowWalkmeshPanel;
            prevLeftPanelSize = settings.PrevLeftPanelSize;
            prevRightPanelSize = settings.PrevRightPanelSize;

            if (ShowTransAbortRegions) content.Background = Brushes.Black;
            if (ShowTransAbortRegions) CoordinateTextBrush = Brushes.White;
            if (ShowRimDataUnderMouse) RunMouseHoverWorker_Executed(this, null);

            if (ShowCoordinatePanel || ShowRimDataPanel) columnLeftPanel.Width = new GridLength(prevLeftPanelSize, GridUnitType.Pixel);
            if (ShowWalkmeshPanel) columnRightPanel.Width = new GridLength(prevRightPanelSize, GridUnitType.Pixel);

            SetRimDataTypePanelVisibility(ShowRimDataDoors, "Door");
            SetRimDataTypePanelVisibility(ShowRimDataTriggers, "Trigger");
            SetRimDataTypePanelVisibility(ShowRimDataTraps, "Trap");
            SetRimDataTypePanelVisibility(ShowRimDataZones, "Zone");
            SetRimDataTypePanelVisibility(ShowRimDataEncounters, "Encounter");
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

        private Dictionary<Brush, string> BrushThemeRainbow { get; set; } = new Dictionary<Brush, string>
        {
            { new SolidColorBrush(new Color { R = 0xff, G = 0x00, B = 0x00, A = 0xFF }), "Red" },
            { new SolidColorBrush(new Color { R = 0xe2, G = 0x98, B = 0x18, A = 0xFF }), "Orange" },
            { new SolidColorBrush(new Color { R = 0xff, G = 0xd7, B = 0x00, A = 0xFF }), "Yellow" },
            { new SolidColorBrush(new Color { R = 0x00, G = 0x80, B = 0x00, A = 0xFF }), "Green" },
            { new SolidColorBrush(new Color { R = 0x00, G = 0x00, B = 0xff, A = 0xFF }), "Blue" },
            { new SolidColorBrush(new Color { R = 0x4b, G = 0x00, B = 0x82, A = 0xFF }), "Indigo" },
            { new SolidColorBrush(new Color { R = 0xee, G = 0x82, B = 0xee, A = 0xFF }), "Violet" },
        };

        private Dictionary<Brush, string> BrushThemeMuted { get; set; } = new Dictionary<Brush, string>
        {
            { new SolidColorBrush(new Color { R = 0x00, G = 0x00, B = 0xFF, A = 0xFF }), "Blue" },
            { new SolidColorBrush(new Color { R = 0x33, G = 0xCC, B = 0x33, A = 0xFF }), "Green" },
            { new SolidColorBrush(new Color { R = 0xDD, G = 0x11, B = 0x11, A = 0xFF }), "Red" },
            { new SolidColorBrush(new Color { R = 0x40, G = 0xE0, B = 0xD0, A = 0xFF }), "Turquoise" },
            { new SolidColorBrush(new Color { R = 0xFF, G = 0x69, B = 0xB4, A = 0xFF }), "Hot Pink" },
            { new SolidColorBrush(new Color { R = 0xFF, G = 0xD7, B = 0x00, A = 0xFF }), "Gold" },
        };

        private Dictionary<Brush, string> BrushThemeOriginal { get; set; } = new Dictionary<Brush, string>
        {
            { new SolidColorBrush(new Color { R = 0x00, G = 0x00, B = 0xFF, A = 0xFF }), "Blue" },
            { new SolidColorBrush(new Color { R = 0x00, G = 0xFF, B = 0x00, A = 0xFF }), "Green" },
            { new SolidColorBrush(new Color { R = 0xFF, G = 0x00, B = 0x00, A = 0xFF }), "Red" },
            { new SolidColorBrush(new Color { R = 0x00, G = 0xFF, B = 0xFF, A = 0xFF }), "Cyan" },
            { new SolidColorBrush(new Color { R = 0xFF, G = 0x00, B = 0xFF, A = 0xFF }), "Magenta" },
            { new SolidColorBrush(new Color { R = 0xFF, G = 0xFF, B = 0x00, A = 0xFF }), "Yellow" },
        };

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
        private const string MOUSE_OVER_DEFAULT_STRING = "Mouse is above these items:";
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

        private string _selectedGame = DEFAULT;
        public string SelectedGame
        {
            get => _selectedGame;
            set => SetField(ref _selectedGame, value);
        }

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

        public Point CurrentMousePosition
        {
            get => _currentMousePosition;
            set => SetField(ref _currentMousePosition, value);
        }
        private Point _currentMousePosition = new Point();

        public string RimDataUnderMouse
        {
            get => _rimDataUnderMouse;
            set => SetField(ref _rimDataUnderMouse, value);
        }
        private string _rimDataUnderMouse = MOUSE_OVER_DEFAULT_STRING;

        public int MouseHoverUpdateDelay
        {
            get => _mouseHoverUpdateDelay;
            set => SetField(ref _mouseHoverUpdateDelay, value);
        }
        private int _mouseHoverUpdateDelay = 50;

        #endregion // END REGION DataBinding Members

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
                // Don't set points if a game is not selected.
                if (!(SelectedGame == K1_NAME || SelectedGame == K2_NAME)) return;

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
            var distanceSq = (LeftClickPoint - RightClickPoint).LengthSquared;

            // If right click point is not visible OR if in range...
            if (!RightClickPointVisible || distanceSq <= 900.0)
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

                OffRims = new ObservableCollection<RimModel>(rimModels);

                SelectedGame = e.Argument?.ToString() ?? DEFAULT;

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
                if (ShowDlzLines) rdi.LineColor = rdi.MeshColor;
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
                if (ShowDlzLines)
                    rdi.LineColor = rdi.MeshColor;
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

        private void HideDlzLines(RimDataInfo rdi)
        {
            rdi.LineColor = Brushes.Transparent;
        }

        private void SetRimDataInfoMeshBrush(RimDataInfo rdi, Brush setColor = null)
        {
            if (setColor != null)
            {
                rdi.MeshColor = setColor;
                return;
            }

            setColor = GetNextRimDataInfoBrush();
            if (setColor == RimToBrushUsed[rdi.Module]) setColor = GetNextRimDataInfoBrush();
            rdi.MeshColor = setColor;
        }

        private void BuildRimDataInfoMesh(RimDataInfo rdi)
        {
            if (rdi.Lines.Count == 0)
            {
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
                        const double SIZE = 1;
                        var e = new Ellipse
                        {
                            Stroke = Brushes.Black,
                            StrokeThickness = 0.1,
                            Height = SIZE,
                            Width = SIZE,
                            Opacity = 0.8,
                            RenderTransform = content.Resources["CartesianTransform"] as Transform,
                            Fill = Brushes.Transparent,
                        };
                        Canvas.SetLeft(e, point.Item1 - (e.Width / SIZE));
                        Canvas.SetTop(e, -point.Item2 + (e.Height / SIZE));
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
                OnRims = new ObservableCollection<RimModel>(sorted);

                // Add RIM data collections.
                var rdiModule = RimDataSet.RimData.First(m => m.Module == rim.FileName);
                var rdiSort = RimDoors.Concat(rdiModule.Doors).ToList();
                rdiSort.Sort();
                RimDoors = new ObservableCollection<RimDataInfo>(rdiSort);

                rdiSort = RimTriggers.Concat(rdiModule.Triggers).ToList();
                rdiSort.Sort();
                RimTriggers = new ObservableCollection<RimDataInfo>(rdiSort);

                rdiSort = RimTraps.Concat(rdiModule.Traps).ToList();
                rdiSort.Sort();
                RimTraps = new ObservableCollection<RimDataInfo>(rdiSort);

                rdiSort = RimZones.Concat(rdiModule.Zones).ToList();
                rdiSort.Sort();
                RimZones = new ObservableCollection<RimDataInfo>(rdiSort);

                rdiSort = RimEncounters.Concat(rdiModule.Encounters).ToList();
                rdiSort.Sort();
                RimEncounters = new ObservableCollection<RimDataInfo>(rdiSort);

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

            BuildNewWalkmeshes(AddPolyWorker);

            ResizeCanvas();

            ShowWalkmeshOnCanvas(args.rimToAdd, args.getLeastUsedBrush, args.cycleColor);

            Application.Current.Dispatcher.Invoke(() =>
            {
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
            content.Dispatcher.Invoke(() => content.Background = ShowTransAbortRegions ? Brushes.Black : Brushes.White);

            // Build unbuilt RIM walkmeshes.
            //var unbuilt = OnRims.Where(n => !RimPolyLookup.ContainsKey(n.FileName)).ToList();
            var unbuilt = OnRims.ToList();
            for (var i = 0; i < unbuilt.Count; i++)
            {
                var rimmodel = unbuilt[i];
                if (useModuleBrush) brushToUse = rimmodel.MeshColor;
                if (ShowTransAbortRegions) brushToUse = GrayScaleBrush;

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
                        Fill = PolyBrushCount.Keys.ElementAt(fillIdx),
                        RenderTransform = content.Resources["CartesianTransform"] as Transform,
                        Points = new PointCollection(region),
                    };
                    _ = transBorderCanvas.Children.Add(poly);
                    polys.Add(poly);
                });
                fillIdx = (fillIdx + 1) % PolyBrushCount.Count;
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

        /// <summary>
        /// Add or make visible all faces in the newly active walkmesh.
        /// </summary>
        private void ShowWalkmeshOnCanvas(RimModel rimToAdd, bool getLeastUsed = true, bool cycleColor = false)
        {
            // Determine next brush to use.
            var brushChanged = true;
            Brush brush;

            // If added to the canvas before, just add.
            if (RimToBrushUsed.ContainsKey(rimToAdd.FileName) && !cycleColor)
            {
                brush = GetLeastUsedWalkmeshBrush();
                var oldBrush = RimToBrushUsed[rimToAdd.FileName];
                brushChanged = brush != oldBrush;
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
                if (ShowTransAbortRegions)
                {
                    UpdateRimFillColor(AddPolyWorker, GrayScaleBrush, rimToAdd);
                }
                else
                {
                    UpdateRimFillColor(AddPolyWorker, brush, rimToAdd);
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
                OffRims = new ObservableCollection<RimModel>(sorted);

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
            OffRims = new ObservableCollection<RimModel>(sorted);

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
            if (ShowDlzLines) info.LineColor = info.MeshColor;
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
            settings.PrevLeftPanelSize = (ShowCoordinatePanel || ShowRimDataPanel) ? columnLeftPanel.ActualWidth : prevLeftPanelSize;
            settings.ShowWalkmeshPanel = ShowWalkmeshPanel;
            settings.PrevRightPanelSize = ShowWalkmeshPanel ?  columnRightPanel.ActualWidth : prevRightPanelSize;
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
            SaveImageFromCanvas(20);
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
                InitialDirectory = Environment.CurrentDirectory,
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

        #endregion

        #region Left Panel Methods

        private void CoordinatePanelButton_Click(object sender, RoutedEventArgs e)
            => ShowRimDataPanel = false;    // Hide other panels in the Left Panel

        private void RimDataPanelButton_Click(object sender, RoutedEventArgs e)
            => ShowCoordinatePanel = false; // Hide other panels in the Left Panel

        private void gsLeftPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ShowRimDataPanel || ShowCoordinatePanel)
            {
                columnLeftPanel.MinWidth = 215;
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

        #endregion

        #region Live Position Methods

        private void ShowLivePosition_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ShowLivePosition = true;

            if (LivePositionWorker.IsBusy)
            {
                LivePositionToggleButton.IsEnabled = false;
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
                            //var partyPositions = km.GetPartyPositions();
                            var partyPositions3D = km.GetPartyPositions3D();
                            var partyBearings = km.GetPartyBearings();
                            var partyInRange = true;

                            // Handle party leader
                            //LivePositionPoint = partyPositions[0];
                            LivePositionPoint = new Point(partyPositions3D[0].X, partyPositions3D[0].Y);
                            LiveLeaderBearing = partyBearings[0];
                            LivePositionEllipsePoint = new Point(LivePositionPoint.X + LeftOffset - 0.5, LivePositionPoint.Y + BottomOffset - 0.5);
                            if (!LockGatherPartyRange)
                            {
                                LiveGatherPartyRangePoint = LivePositionEllipsePoint;
                                //LastGatherPartyRangePosition = LivePositionPoint;
                                LastGatherPartyRangePosition = partyPositions3D[0];
                                LastGatherPartyRangePoint = LivePositionEllipsePoint;
                            }
                            else
                            {
                                //var leaderDist = (LastGatherPartyRangePosition - partyPositions[0]).Length;
                                var leaderDistSq = (LastGatherPartyRangePosition - partyPositions3D[0]).LengthSquared;
                                partyInRange = partyInRange && leaderDistSq <= 900.0;
                            }

                            // Handle party member 1
                            if (partyCount > 1)
                            {
                                //var leaderDist = (partyPositions[0] - partyPositions[1]).Length;
                                var leaderDistSq = (partyPositions3D[0] - partyPositions3D[1]).LengthSquared;
                                partyInRange = partyInRange && leaderDistSq <= 900.0;
                                //LivePositionEllipsePointPC1 = new Point(partyPositions[1].X + LeftOffset - 0.5, partyPositions[1].Y + BottomOffset - 0.5);
                                LivePositionEllipsePointPC1 = new Point(partyPositions3D[1].X + LeftOffset - 0.5, partyPositions3D[1].Y + BottomOffset - 0.5);
                                LiveBearingPC1 = partyBearings[1];
                            }

                            // Handle party member 2
                            if (partyCount > 2)
                            {
                                //var leaderDist = (partyPositions[0] - partyPositions[2]).Length;
                                var leaderDistSq = (partyPositions3D[0] - partyPositions3D[2]).LengthSquared;
                                partyInRange = partyInRange && leaderDistSq <= 900.0;
                                //LivePositionEllipsePointPC2 = new Point(partyPositions[2].X + LeftOffset - 0.5, partyPositions[2].Y + BottomOffset - 0.5);
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
            //ShowLivePosition = false;
            LivePositionToggleButton.IsEnabled = true;
        }

        #endregion

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
            var message = string.Empty;

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
                        .Where(r => r.MeshVisible);
                    message = string.Join(Environment.NewLine, visibleRimData
                        .Where(r => r.IsTouching(mousePosition))
                        .Select(r => $"{r.RimDataType,-12}\t{BrushToName[r.MeshColor],-7}\t      {r.ResRef}")
                        .ToArray());
                });

                RimDataUnderMouse = $"Mouse is above these items:{Environment.NewLine}{message}".TrimEnd();
                Thread.Sleep(Math.Max(LivePositionUpdateDelay - (int)sw.ElapsedMilliseconds, 0));
                sw.Restart();
            }
        }

        private void MouseHoverWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ShowRimDataUnderMouse = false;
            RimDataUnderMouse = MOUSE_OVER_DEFAULT_STRING;
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
                content.Background = Brushes.White;
                CoordinateTextBrush = Brushes.Black;
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
            if (e.OriginalSource is VisualizerWindow) ShowTransAbortRegions = !ShowTransAbortRegions;

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
            var visibleRimDataInfo = RimDoors.Where(i => i.MeshVisible)
                .Concat(RimTriggers.Where(i => i.MeshVisible))
                .Concat(RimTraps.Where(i => i.MeshVisible))
                .Concat(RimZones.Where(i => i.MeshVisible))
                .Concat(RimEncounters.Where(i => i.MeshVisible));
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

        #endregion
    }
}
