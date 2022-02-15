using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using KotOR_IO;
using KotOR_IO.Helpers;
using Microsoft.Win32;
using WalkmeshVisualizerWpf.Helpers;
using ZoomAndPan;

namespace WalkmeshVisualizerWpf.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
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

        /// <summary>
        /// Lookup of the brushes used to draw and how many meshes are currently using them.
        /// </summary>
        private Dictionary<Brush, int> PolyBrushCount { get; set; } = new Dictionary<Brush, int>
        {
            { new SolidColorBrush(new Color { R = 0x00, G = 0x00, B = 0xFF, A = 0xAA }), 0 },
            { new SolidColorBrush(new Color { R = 0x00, G = 0xFF, B = 0x00, A = 0xAA }), 0 },
            { new SolidColorBrush(new Color { R = 0xFF, G = 0x00, B = 0x00, A = 0xAA }), 0 },
            { new SolidColorBrush(new Color { R = 0x00, G = 0xFF, B = 0xFF, A = 0xAA }), 0 },
            { new SolidColorBrush(new Color { R = 0xFF, G = 0x00, B = 0xFF, A = 0xAA }), 0 },
            { new SolidColorBrush(new Color { R = 0xFF, G = 0xFF, B = 0x00, A = 0xAA }), 0 },
        };

        private Dictionary<string, Brush> RimToBrushUsed { get; set; } = new Dictionary<string, Brush>();

        private List<string> RimPolysCreated { get; set; } = new List<string>();

        public BackgroundWorker GameDataWorker { get; set; } = new BackgroundWorker();
        public BackgroundWorker AddPolyWorker { get; set; } = new BackgroundWorker();
        public BackgroundWorker RemovePolyWorker { get; set; } = new BackgroundWorker();
        public BackgroundWorker ClearCacheWorker { get; set; } = new BackgroundWorker();

        private readonly Point BaseOffset = new Point(20, 20);
        public const string DEFAULT = "N/A";
        public const string LOADING = "Loading";
        public const string K1_NAME = "KotOR 1";
        public const string K2_NAME = "KotOR 2";
        private readonly string K1_DEFAULT_PATH = @"C:\Program Files (x86)\Steam\steamapps\common\swkotor";
        private readonly string K2_DEFAULT_PATH = @"C:\Program Files (x86)\Steam\steamapps\common\Knights of the Old Republic II";
        private KPaths Paths;

        #endregion // END REGION KIO Members

        #region DataBinding Members

        public bool PointClicked
        {
            get => (bool)GetValue(PointClickedProperty);
            set => Application.Current.Dispatcher.Invoke(() => SetValue(PointClickedProperty, value));
        }

        public static readonly DependencyProperty PointClickedProperty = DependencyProperty.Register("PointClicked", typeof(bool), typeof(MainWindow));

        //private bool _pointClicked;
        //public bool PointClicked
        //{
        //    get => _pointClicked;
        //    set => SetField(ref _pointClicked, value);
        //}

        public Point LastPoint
        {
            get => (Point)GetValue(LastPointProperty);
            set => Application.Current.Dispatcher.Invoke(() => SetValue(LastPointProperty, value));
        }

        public static readonly DependencyProperty LastPointProperty = DependencyProperty.Register("LastPoint", typeof(Point), typeof(MainWindow));

        //private Point _lastPoint;
        //public Point LastPoint
        //{
        //    get => _lastPoint;
        //    set => SetField(ref _lastPoint, value);
        //}

        public Point LastModuleCoords
        {
            get => (Point)GetValue(LastModuleCoordsProperty);
            set => Application.Current.Dispatcher.Invoke(() => SetValue(LastModuleCoordsProperty, value));
        }

        public static readonly DependencyProperty LastModuleCoordsProperty = DependencyProperty.Register("LastModuleCoords", typeof(Point), typeof(MainWindow));

        //private Point _lastModuleCoords;
        //public Point LastModuleCoords
        //{
        //    get => _lastModuleCoords;
        //    set => SetField(ref _lastModuleCoords, value);
        //}

        public bool IsBusy
        {
            get => (bool)GetValue(IsBusyProperty);
            set => Application.Current.Dispatcher.Invoke(() => SetValue(IsBusyProperty, value));
        }

        public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register("IsBusy", typeof(bool), typeof(MainWindow));

        //private bool _isBusy;
        //public bool IsBusy
        //{
        //    get => _isBusy;
        //    set => SetField(ref _isBusy, value);
        //}

        public string SelectedGame
        {
            get => (string)GetValue(SelectedGameProperty);
            set => Application.Current.Dispatcher.Invoke(() => SetValue(SelectedGameProperty, value));
        }

        public static readonly DependencyProperty SelectedGameProperty = DependencyProperty.Register("SelectedGame", typeof(string), typeof(MainWindow), new PropertyMetadata(DEFAULT));

        //private string _selectedGame = DEFAULT;
        //public string SelectedGame
        //{
        //    get => _selectedGame;
        //    set => SetField(ref _selectedGame, value);
        //}

        private ObservableCollection<string> _onNames;
        public ObservableCollection<string> OnNames
        {
            get => _onNames;
            set => SetField(ref _onNames, value);
        }

        private ObservableCollection<string> _offNames;

        public ObservableCollection<string> OffNames
        {
            get => _offNames;
            set => SetField(ref _offNames, value);
        }

        public double LeftOffset
        {
            get => (double)GetValue(LeftOffsetProperty);
            set => Application.Current.Dispatcher.Invoke(() => SetValue(LeftOffsetProperty, value));
        }

        public static readonly DependencyProperty LeftOffsetProperty = DependencyProperty.Register("LeftOffset", typeof(double), typeof(MainWindow));

        //public double _leftOffset;
        //public double LeftOffset
        //{
        //    get => _leftOffset;
        //    set => SetField(ref _leftOffset, value);
        //}

        public double BottomOffset
        {
            get => (double)GetValue(BottomOffsetProperty);
            set => Application.Current.Dispatcher.Invoke(() => SetValue(BottomOffsetProperty, value));
        }

        public static readonly DependencyProperty BottomOffsetProperty = DependencyProperty.Register("BottomOffset", typeof(double), typeof(MainWindow));

        //public double _bottomOffset;
        //public double BottomOffset
        //{
        //    get => _bottomOffset;
        //    set => SetField(ref _bottomOffset, value);
        //}

        public static readonly DependencyProperty CurrentProgressProperty = DependencyProperty.Register(nameof(CurrentProgress), typeof(double), typeof(MainWindow));
        public double CurrentProgress
        {
            get => (double)GetValue(CurrentProgressProperty);
            set => Application.Current.Dispatcher.Invoke(() => SetValue(CurrentProgressProperty, value));
        }
        public string Game { get; private set; }

        #endregion // END REGION DataBinding Members

        #region Constructors

        public MainWindow()
        {
            InitializeComponent();

            OnNames = new ObservableCollection<string>();
            OffNames = new ObservableCollection<string>();

            GameDataWorker.WorkerReportsProgress = true;
            GameDataWorker.ProgressChanged += Bw_ProgressChanged;
            GameDataWorker.RunWorkerCompleted += Bw_RunWorkerCompleted;
            GameDataWorker.DoWork += GameDataWorker_DoWork;

            AddPolyWorker.WorkerReportsProgress = true;
            AddPolyWorker.ProgressChanged += Bw_ProgressChanged;
            AddPolyWorker.RunWorkerCompleted += Bw_RunWorkerCompleted;
            AddPolyWorker.DoWork += AddPolyWorker_DoWork;

            RemovePolyWorker.WorkerReportsProgress = true;
            RemovePolyWorker.ProgressChanged += Bw_ProgressChanged;
            RemovePolyWorker.RunWorkerCompleted += Bw_RunWorkerCompleted;
            RemovePolyWorker.DoWork += RemovePolyWorker_DoWork;

            ClearCacheWorker.WorkerReportsProgress = true;
            ClearCacheWorker.ProgressChanged += Bw_ProgressChanged;
            ClearCacheWorker.RunWorkerCompleted += Bw_RunWorkerCompleted;
            ClearCacheWorker.DoWork += ClearCacheWorker_DoWork;
        }

        /// <summary>
        /// Event raised when the Window has loaded.
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var htw = new HelpTextWindow
            {
                Left = Left + Width + 5,
                Top = Top,
                Owner = this
            };
            htw.Show();
        }


        #endregion // END REGION Constructors

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
                if (mouseHandlingMode == MouseHandlingMode.Zooming)
                {
                    if (mouseButtonDown == MouseButton.Left)
                    {
                        // Shift + left-click zooms in on the content.
                        ZoomIn(origContentMouseDownPoint);
                    }
                    else if (mouseButtonDown == MouseButton.Right)
                    {
                        // Shift + left-click zooms out from the content.
                        ZoomOut(origContentMouseDownPoint);
                    }
                }
                else if (mouseHandlingMode == MouseHandlingMode.DragZooming)
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
            var doubleClickPoint = e.GetPosition(content);
            doubleClickPoint.Y = theGrid.Height - doubleClickPoint.Y - .5;
            doubleClickPoint.X -= .5;
            LastPoint = doubleClickPoint;

            doubleClickPoint.X -= LeftOffset;
            doubleClickPoint.Y -= BottomOffset;
            LastModuleCoords = doubleClickPoint;

            content.Children.Remove(walkmeshPoint);
            content.Children.Add(walkmeshPoint);

            content.Children.Remove(pointCoords);
            content.Children.Add(pointCoords);

            PointClicked = true;
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
            if (Directory.Exists(K1_DEFAULT_PATH))
                LoadGameFiles(K1_DEFAULT_PATH, K1_NAME);
            else
                _ = MessageBox.Show("Default KotOR 1 path not found. Please use the 'Custom' option instead.");
        }

        /// <summary>
        /// Load K1 can execute if KotOR 2 is selected or if no game has been selected.
        /// </summary>
        private void LoadK1_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedGame == K2_NAME || SelectedGame == DEFAULT;
        }

        /// <summary>
        /// Load KotOR 2 game files.
        /// </summary>
        private void LoadK2_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (Directory.Exists(K2_DEFAULT_PATH))
                LoadGameFiles(K2_DEFAULT_PATH, K2_NAME);
            else
                _ = MessageBox.Show("Default KotOR 2 path not found. Please use the 'Custom' option instead.");
        }

        /// <summary>
        /// Load K2 can execute if KotOR 1 is selected or of no game has been selected.
        /// </summary>
        private void LoadK2_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedGame == K1_NAME || SelectedGame == DEFAULT;
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
                if (exe.Name.ToLower() == "swkotor.exe") LoadGameFiles(dir.FullName, K1_NAME);
                if (exe.Name.ToLower() == "swkotor2.exe") LoadGameFiles(dir.FullName, K2_NAME);
            }
        }

        private void LoadCustom_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedGame == DEFAULT;
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
            if (OnNames.Any()) RemoveAll_Executed(this, null);

            // Clear the cache before loading game files.
            IsBusy = true;
            ClearCacheWorker.RunWorkerCompleted -= Bw_RunWorkerCompleted;
            ClearCacheWorker.RunWorkerCompleted += ClearAndLoad_RunWorkerCompleted;
            ClearCacheWorker.RunWorkerAsync(name);
        }

        private void HideGameButtons()
        {
            bSetGameK1.Visibility = Visibility.Collapsed;
            bSetGameK2.Visibility = Visibility.Collapsed;
            bSetGameCustom.Visibility = Visibility.Collapsed;
            txtGameSelectText.Visibility = Visibility.Collapsed;
            pbLoading.Visibility = Visibility.Visible;
            bFindMatchingCoords.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Resets clear cache worker and starts the game data worker.
        /// </summary>
        private void ClearAndLoad_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ClearCacheWorker.RunWorkerCompleted -= ClearAndLoad_RunWorkerCompleted;
            ClearCacheWorker.RunWorkerCompleted += Bw_RunWorkerCompleted;

            GameDataWorker.RunWorkerAsync(e.Result);
        }

        /// <summary>
        /// Performs steps to load required game data.
        /// </summary>
        private void GameDataWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ClearGameData();

            // Create the KEY file.
            GameDataWorker.ReportProgress(25);
            var key = new KEY(Paths.chitin);

            var path = System.IO.Path.Combine(Environment.CurrentDirectory, $"{Game} Woks");
            if (Directory.Exists(path))
            {
                ReadWokFiles(path);
            }
            else
            {
                FetchWokFiles(key);
                GetRimData(key);
            }

            OffNames = new ObservableCollection<string>(RimNamesLookup.Keys);

            SelectedGame = e.Argument?.ToString() ?? DEFAULT;

            if ((Game == K1_NAME || Game == K2_NAME) && !Directory.Exists(path))
            {
                SaveWokFiles(path);
            }
        }

        /// <summary>
        /// Read walkmesh files we saved previously.
        /// </summary>
        private void ReadWokFiles(string path)
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
            }
        }

        /// <summary>
        /// Persist walkmesh files for future application use.
        /// </summary>
        private void SaveWokFiles(string path)
        {
            var gameDir = Directory.CreateDirectory(path);
            var count = 0;
            var totalWoks = RimWoksLookup.Sum(kvp => kvp.Value.Count());
            foreach (var rim in RimWoksLookup)
            {
                var rimDir = gameDir.CreateSubdirectory(rim.Key);
                foreach (var wok in rim.Value)
                {
                    GameDataWorker.ReportProgress(100 * count++ / totalWoks);
                    var wokPath = System.IO.Path.Combine(rimDir.FullName, $"{wok.RoomName}.wok");
                    if (File.Exists(wokPath))
                        throw new Exception();
                    else
                        wok.WriteToFile(wokPath);
                }

                File.Create(System.IO.Path.Combine(rimDir.FullName, $"{RimNamesLookup[rim.Key]}.txt")).Close();
            }
        }

        /// <summary>
        /// Clear game data related collections.
        /// </summary>
        private void ClearGameData()
        {
            PointClicked = false;
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

        #region Add Polygon Methods

        /// <summary>
        /// Add a walkmesh to the display.
        /// </summary>
        private void LvOff_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            IsBusy = true;
            //content.Visibility = Visibility.Hidden;

            // Move the clicked item from OFF to ON.
            var name = (string)(sender as ListViewItem).Content;
            _ = OffNames.Remove(name);
            var sorted = OnNames.ToList();
            sorted.Add(name);

            // Sort the ON list and update collection.
            sorted.Sort();
            OnNames = new ObservableCollection<string>(sorted);

            // Start worker to add polygons to the canvas.
            _ = content.Focus();
            AddPolyWorker.RunWorkerAsync(name);
        }

        /// <summary>
        /// Perform steps to add walkmesh polygons to the canvas.
        /// </summary>
        private void AddPolyWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            PointClicked = false;   // hide point
            var rimToAdd = (string)e.Argument;  // grab rim name

            BuildNewWalkmeshes();

            ResizeCanvas();

            AddWalkmeshToCanvas(rimToAdd);
        }

        /// <summary>
        /// Build any new walkmesh in the ON collection.
        /// </summary>
        private void BuildNewWalkmeshes()
        {
            // Build unbuilt RIM walkmeshes.
            var unbuilt = OnNames.Where(n => !RimPolyLookup.ContainsKey(n)).ToList();
            for (var i = 0; i < unbuilt.Count; i++)
            {
                var name = unbuilt[i];

                // Select all faces from mesh.
                var allfaces = RimWoksLookup[name].SelectMany(w => w.Faces).ToList();

                // Create a polygon for each face.
                var polys = new List<Polygon>();    // walkable polygons
                var unpolys = new List<Polygon>();  // unwalkable polygons
                for (var j = 0; j < allfaces.Count; j++)
                {
                    AddPolyWorker.ReportProgress(100 * j / allfaces.Count);
                    var points = allfaces[j].ToPoints();    // points of this face

                    // Create polygons, sorted based on walkability.
                    if (allfaces[j].IsWalkable)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            polys.Add(new Polygon { Points = new PointCollection(points) });
                        });
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            unpolys.Add(new Polygon
                            {
                                Points = new PointCollection(points),
                                StrokeThickness = .05,
                            });
                        });
                    }
                }

                // Cache the created polygons.
                RimPolyLookup.Add(name, polys);
                RimOutlinePolyLookup.Add(name, unpolys);
            }
        }

        /// <summary>
        /// Resize the canvas to fit all displayed faces.
        /// </summary>
        private void ResizeCanvas()
        {
            // Determine size of canvas and offset.
            var woks = RimWoksLookup
                .Where(kvp => OnNames.Contains(kvp.Key))
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
            LeftOffset = -MinX + BaseOffset.X;
            BottomOffset = -MinY + BaseOffset.Y;
        }

        /// <summary>
        /// Add or make visible all faces in the newly active walkmesh.
        /// </summary>
        private void AddWalkmeshToCanvas(string rimToAdd)
        {
            // Determine next brush to use.
            var brushChanged = true;
            Brush brush;
            if (RimToBrushUsed.ContainsKey(rimToAdd))
            {
                // If added to the canvas before, find most frequent brushes.
                var max = PolyBrushCount.Where(kvp => kvp.Value == PolyBrushCount.Values.Max()).ToDictionary(kvp => kvp.Key);

                if (max.Count != PolyBrushCount.Count && max.ContainsKey(RimToBrushUsed[rimToAdd]))
                {
                    // If not all brushes are equal AND last used brush is in max frequency, get a new brush.
                    brush = GetNextBrush();
                    PolyBrushCount[brush]++;
                }
                else
                {
                    // Else, don't change the brush.
                    brush = RimToBrushUsed[rimToAdd];
                    brushChanged = false;
                    PolyBrushCount[brush]++;
                }
            }
            else
            {
                brush = GetNextBrush();
                PolyBrushCount[brush]++;
            }

            // Remember which brush was used.
            if (RimToBrushUsed.ContainsKey(rimToAdd))
            {
                RimToBrushUsed[rimToAdd] = brush;
            }
            else
            {
                RimToBrushUsed.Add(rimToAdd, brush);
            }

            // Add walkable surfaces to the canvas.
            var polygons = RimPolyLookup[rimToAdd].ToList();
            var unpolygons = RimOutlinePolyLookup[rimToAdd].ToList();
            var i = 0;
            if (RimPolysCreated.Contains(rimToAdd))
            {
                if (brushChanged)
                {
                    // If the brush is different, update with new color.
                    polygons.ForEach((Polygon p) =>
                    {
                        content.Dispatcher.Invoke(() =>
                        {
                            AddPolyWorker.ReportProgress(100 * i++ / polygons.Count);
                            p.Fill = brush; p.Visibility = Visibility.Visible;
                        });
                    });
                    i = 0;
                    unpolygons.ForEach((Polygon p) =>
                    {
                        content.Dispatcher.Invoke(() =>
                        {
                            AddPolyWorker.ReportProgress(100 * i++ / unpolygons.Count);
                            p.Stroke = brush; p.Visibility = Visibility.Visible;
                        });
                    });
                }
                else
                {
                    // If the brush is the same, just reveal the polygons.
                    polygons.AddRange(unpolygons);
                    polygons.ForEach((Polygon p) =>
                    {
                        content.Dispatcher.Invoke(() =>
                        {
                            AddPolyWorker.ReportProgress(100 * i++ / polygons.Count);
                            p.Visibility = Visibility.Visible;
                        });
                    });
                }
            }
            else
            {
                // If not yet used, update brush and add to canvas.
                polygons.ForEach((Polygon p) =>
                {
                    content.Dispatcher.Invoke(() =>
                    {
                        AddPolyWorker.ReportProgress(100 * i++ / polygons.Count);
                        p.Fill = brush; _ = content.Children.Add(p);
                    });
                });
                i = 0;
                unpolygons.ForEach((Polygon p) =>
                {
                    content.Dispatcher.Invoke(() =>
                    {
                        AddPolyWorker.ReportProgress(100 * i++ / unpolygons.Count);
                        p.Stroke = brush; _ = content.Children.Add(p);
                    });
                });
            }

            //// Add walkable surfaces to the canvas.
            //var polygons = RimPolyLookup[rimToAdd].ToList();
            //for (var i = 0; i < polygons.Count; i++)
            //{
            //    AddPolyWorker.ReportProgress(100 * i / polygons.Count);

            //    var p = polygons[i];
            //    if (RimPolysCreated.Contains(rimToAdd)) // Polygons already added. Set them to visible.
            //    {
            //        Application.Current.Dispatcher.Invoke(() => { p.Fill = brush; p.Visibility = Visibility.Visible; });
            //    }
            //    else    // Polygons not added yet.
            //    {
            //        Application.Current.Dispatcher.Invoke(() => { p.Fill = brush; _ = content.Children.Add(p); });
            //    }
            //}

            //// Add unwalkable surfaces to the canvas.
            //var unpolygons = RimOutlinePolyLookup[rimToAdd].ToList();
            //for (var i = 0; i < unpolygons.Count; i++)
            //{
            //    AddPolyWorker.ReportProgress(100 * i / unpolygons.Count);

            //    var p = unpolygons[i];
            //    if (RimPolysCreated.Contains(rimToAdd)) // Polygons already added. Set them to visible.
            //    {
            //        Application.Current.Dispatcher.Invoke(() => { p.Stroke = brush; p.Visibility = Visibility.Visible; });
            //    }
            //    else    // Polygons not added yet.
            //    {
            //        Application.Current.Dispatcher.Invoke(() => { p.Stroke = brush; _ = content.Children.Add(p); });
            //    }
            //}

            // Remember that the polygons have been added to the canvas.
            if (!RimPolysCreated.Contains(rimToAdd))
            {
                RimPolysCreated.Add(rimToAdd);
            }
        }

        /// <summary>
        /// Determine next brush to use in sequence.
        /// </summary>
        private Brush GetNextBrush()
        {
            var min = PolyBrushCount.Values.Min();
            return PolyBrushCount.First(pair => pair.Value == min).Key;
        }

        #endregion // END REGION Add Polygon Methods

        #region Remove Polygon Methods

        /// <summary>
        /// Remove a walkmesh from the display.
        /// </summary>
        private void LvOn_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            IsBusy = true;
            //content.Visibility = Visibility.Hidden;

            // Move the clicked item from ON to OFF.
            var name = (string)(sender as ListViewItem).Content;
            _ = OnNames.Remove(name);
            var sorted = OffNames.ToList();
            sorted.Add(name);

            // Sort the OFF list and update collection.
            sorted.Sort();
            OffNames = new ObservableCollection<string>(sorted);

            // Start worker to remove polygons from the canvas.
            _ = content.Focus();
            RemovePolyWorker.RunWorkerAsync(name);
        }

        /// <summary>
        /// Perform steps to remove walkmesh polygons from the canvas.
        /// </summary>
        private void RemovePolyWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            PointClicked = false;   // hide point
            var rimToRemove = (string)e.Argument;   // grab rim name

            //DeleteWalkmeshFromCanvas(rimToRemove);
            RemoveWalkmeshFromCanvas(rimToRemove);

            ResizeCanvas();
        }

        /// <summary>
        /// Hide all faces in the newly disabled walkmesh.
        /// </summary>
        private void RemoveWalkmeshFromCanvas(string rimToRemove)
        {
            // Adjust count of times this rim's brush was used.
            PolyBrushCount[RimToBrushUsed[rimToRemove]]--;

            var polys = RimPolyLookup[rimToRemove].ToList();
            polys.AddRange(RimOutlinePolyLookup[rimToRemove]);

            // Hide walkable faces.
            content.Dispatcher.Invoke(() =>
            {
                polys.ForEach((Polygon p) =>
                {
                    p.Visibility = Visibility.Hidden;
                });
            });

            //// Hide walkable faces.
            //var polys = RimPolyLookup[rimToRemove].ToList();
            //for (var i = 0; i < polys.Count; i++)
            //{
            //    RemovePolyWorker.ReportProgress(100 * i / polys.Count);
            //    Application.Current.Dispatcher.Invoke(() =>
            //        { polys[i].Visibility = Visibility.Collapsed; });
            //}

            //// Hide unwalkable faces.
            //polys = RimOutlinePolyLookup[rimToRemove].ToList();
            //for (var i = 0; i < polys.Count; i++)
            //{
            //    RemovePolyWorker.ReportProgress(100 * i / polys.Count);
            //    Application.Current.Dispatcher.Invoke(() =>
            //        { polys[i].Visibility = Visibility.Collapsed; });
            //}
        }

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

            //for (int i = 0; i < polys.Count; i++)
            //{
            //    RemovePolyWorker.ReportProgress(100 * i / polys.Count);
            //    Application.Current.Dispatcher.Invoke(() =>
            //    {
            //        var rt = polys[i].RenderTransform as TransformGroup;
            //        polys[i].Visibility = Visibility.Collapsed;

            //        // Remove bindings for each of the transforms.
            //        foreach (var transform in rt.Children)
            //        {
            //            BindingOperations.ClearAllBindings(transform);
            //        }

            //        // Remove bindings on the polygon.
            //        BindingOperations.ClearBinding(polys[i], RenderTransformProperty);
            //    });
            //}

            content.Dispatcher.Invoke(() =>
            {
                content.Children.RemoveRange(content.Children.IndexOf(polys[0]), polys.Count);
            });

            // Clear the caches.
            _ = RimPolyLookup.Remove(rimToRemove);
            _ = RimOutlinePolyLookup.Remove(rimToRemove);
            _ = RimPolysCreated.Remove(rimToRemove);
        }

        #endregion // END REGION Remove Polygon Methods

        #region Remove All Methods

        /// <summary>
        /// Remove all active walkmeshes from the canvas.
        /// </summary>
        private void RemoveAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PointClicked = false;   // hide point

            // Move all ON names to the OFF collection.
            foreach (var name in OnNames)
            {
                OffNames.Add(name);
            }
            OnNames.Clear();

            // Sort the OFF collection.
            var sorted = OffNames.ToList();
            sorted.Sort();
            OffNames = new ObservableCollection<string>(sorted);

            // Hide all polygons in the canvas.
            content.Dispatcher.Invoke(() =>
            {
                foreach (FrameworkElement child in content.Children)
                {
                    child.Visibility = Visibility.Hidden;
                }
            });

            // Set brush count to 0 since all walkmeshes are now hidden.
            foreach (var key in PolyBrushCount.Keys.ToList())
            {
                PolyBrushCount[key] = 0;
            }
        }

        /// <summary>
        /// Remove all can execute only if there are names in the ON collection.
        /// </summary>
        private void RemoveAll_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsBusy && (OnNames?.Any() ?? false);
        }

        #endregion // END REGION Remove All Methods

        #region Clear Cache Methods

        /// <summary>
        /// Clear the cache of saved polygons.
        /// </summary>
        private void ClearCache_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            IsBusy = true;
            OnNames.Clear();
            _ = content.Focus();
            ClearCacheWorker.RunWorkerAsync();
        }

        /// <summary>
        /// Clear cache can execute if the ON collection is empty and if there are any saved polygons.
        /// </summary>
        private void ClearCache_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (!IsBusy) && (!OnNames?.Any() ?? false) && (RimPolysCreated?.Any() ?? false);
        }

        /// <summary>
        /// Perform steps to clear the cache of saved polygons.
        /// </summary>
        private void ClearCacheWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Save game name sent if started from Game Data load.
            e.Result = e.Argument;

            // Retrieve all polygons from the cache.
            var polys = RimPolyLookup.SelectMany(kvp => kvp.Value).ToList();
            polys.AddRange(RimOutlinePolyLookup.SelectMany(kvp => kvp.Value));

            // Clear bindings for each polygon.
            for (var i = 0; i < polys.Count; i++)
            {
                ClearCacheWorker.ReportProgress(100 * i / polys.Count);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var rt = polys[i].RenderTransform as TransformGroup;

                    // Remove bindings for each of the transforms.
                    foreach (var transform in rt.Children)
                    {
                        BindingOperations.ClearAllBindings(transform);
                    }

                    // Remove bindings on the polygon.
                    BindingOperations.ClearBinding(polys[i], RenderTransformProperty);
                });
            }

            // Clear the caches.
            RimPolyLookup = new Dictionary<string, IEnumerable<Polygon>>();
            RimOutlinePolyLookup = new Dictionary<string, IEnumerable<Polygon>>();
            RimPolysCreated = new List<string>();

            // Clear the canvas.
            Application.Current.Dispatcher.Invoke(() =>
                { content.Children.Clear(); });
        }

        #endregion // END REGION Clear Cache Methods

        #region Find Matching Coord Methods

        private void FindMatchingCoords_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FindMatchingCoords();
        }

        private void FindMatchingCoords_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = PointClicked && (OnNames?.Any() ?? false);
        }

        private void FindMatchingCoords()
        {
            // Search walkmeshes for walkable coordinates at LastModuleCoords
            var matching = new List<string>();
            foreach (var rimKvp in RimWoksLookup)
            {
                var displayed = OnNames.Contains(rimKvp.Key) ? "(displayed)" : "";
                foreach (var wok in rimKvp.Value)
                {
                    if (wok.ContainsWalkablePoint((float)LastModuleCoords.X, (float)LastModuleCoords.Y))
                    {
                        matching.Add($" [w] = {rimKvp.Key} {displayed}");
                        break;
                    }
                    if (wok.ContainsNonWalkablePoint((float)LastModuleCoords.X, (float)LastModuleCoords.Y))
                    {
                        matching.Add($" [n] = {rimKvp.Key} {displayed}");
                        break;
                    }
                }
            }

            // Build a string of all the matching modules.
            var sb = new StringBuilder();
            _ = sb.AppendLine($"The following modules have a walkmesh face at the point ({LastModuleCoords.X:N2}, {LastModuleCoords.Y:N2}):")
                  .AppendLine(" [n] = non-walkable")
                  .AppendLine(" [w] = walkable")
                  .AppendLine(" - - - - - - - - - - - - - -");

            matching.ForEach((string s) =>
            {
                _ = sb.AppendLine(s);
            });

            // Display matching modules in a new window.
            var mws = OwnedWindows.OfType<MatchingWindow>();
            if (mws.Any())
            {
                mws.First().UpdateMessage(sb.ToString());
            }
            else
            {
                var mw = new MatchingWindow(sb.ToString())
                {
                    Left = Left + Width + 5,
                    Top = Top,
                    Owner = this
                };
                mw.Show();
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
            //content.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Report current progress of background worker.
        /// </summary>
        private void Bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            CurrentProgress = e.ProgressPercentage;
        }

        #endregion // END REGION Common Background Worker Methods

        #region Unused Methods

        //private void DrawAllActiveWalkmeshes()
        //{
        //    // Draw selected walkmeshes.
        //    for (var i = 0; i < OnNames.Count; i++)
        //    {
        //        // Add all polys for this RIM to the canvas.
        //        var polys = RimPolyLookup[OnNames[i]].ToList();
        //        for (var j = 0; j < polys.Count; j++)
        //        {
        //            // Reset the polygon fill color.
        //            RemovePolyWorker.ReportProgress(100 * j / OnNames.Count);
        //            var min = PolyBrushCount.Values.Min();
        //            var brush = PolyBrushCount.First(pair => pair.Value == min).Key;
        //            PolyBrushCount[brush]++;
        //            RimToBrushUsed.Add(OnNames[i], brush);

        //            Application.Current.Dispatcher.Invoke(() =>
        //            {
        //                var poly = polys[j];
        //                poly.Fill = brush;
        //                _ = content.Children.Add(poly);
        //            });
        //        }
        //    }
        //}

        //private void ClearMapsAndNames()
        //{
        //    PointClicked = false;

        //    // Remove polygons from canvas.
        //    content.Dispatcher.Invoke(() =>
        //    {
        //        content.Children.Clear();
        //    });

        //    // Clear map names.
        //    OnNames.Clear();
        //    OffNames.Clear();
        //}

        //private void BuildOnlyWalkablePolygons()
        //{
        //    // Build unbuilt RIM walkmeshes.
        //    var unbuilt = OnNames.Where(n => !RimPolyLookup.ContainsKey(n)).ToList();
        //    for (var i = 0; i < unbuilt.Count; i++)
        //    {
        //        var name = unbuilt[i];

        //        // Select all faces from mesh.
        //        var allfaces = RimWoksLookup[name].SelectMany(w => w.Faces).ToList();

        //        var faces = new List<WOK.Face>();
        //        for (var j = 0; j < RimWoksLookup[name].Count(); j++)
        //        {
        //            AddPolyWorker.ReportProgress(100 * j / RimWoksLookup[name].Count());
        //            var wok = RimWoksLookup[name].ElementAt(j);
        //            var walkable = wok.Faces.Where(f => f.IsWalkable);
        //            if (walkable.Any()) faces.AddRange(walkable);
        //        }

        //        // Select walkable faces from mesh.
        //        for (var j = 0; j < RimWoksLookup[name].Count(); j++)
        //        {
        //            AddPolyWorker.ReportProgress(100 * j / RimWoksLookup[name].Count());
        //            var wok = RimWoksLookup[name].ElementAt(j);
        //            var walkable = wok.Faces.Where(f => f.IsWalkable);
        //            if (walkable.Any()) faces.AddRange(walkable);
        //        }

        //        // Create a polygon for each face.
        //        var polys = new List<Polygon>();
        //        for (var j = 0; j < faces.Count; j++)
        //        {
        //            AddPolyWorker.ReportProgress(100 * j / faces.Count);
        //            Application.Current.Dispatcher.Invoke(new Action(() =>
        //            {
        //                polys.Add(new Polygon
        //                {
        //                    Points = new PointCollection(faces[j].ToPoints()),
        //                });
        //            }));
        //        }

        //        RimPolyLookup.Add(name, polys);
        //    }
        //}

        #endregion // END REGION Unused Methods

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
}
