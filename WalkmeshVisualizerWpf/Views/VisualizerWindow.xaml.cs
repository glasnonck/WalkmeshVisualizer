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

            // Hide selected game label.
            pnlSelectedGame.Visibility = Visibility.Collapsed;
            pnlRimSelect.Visibility = Visibility.Hidden;

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

            // Set up RemovePolyWorker
            RemovePolyWorker.WorkerReportsProgress = true;
            RemovePolyWorker.ProgressChanged += Bw_ProgressChanged;
            RemovePolyWorker.RunWorkerCompleted += Bw_RunWorkerCompleted;
            RemovePolyWorker.DoWork += RemovePolyWorker_DoWork;

            DataContext = this;
        }

        /// <summary>
        /// Event raised when the Window has loaded.
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e) { }


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

        /// <summary> Data of the currently selected game. </summary>
        private KotorDataModel CurrentGameData { get; set; }

        /// <summary> Lookup from RIM filename to a collection of walkmesh face polygons. </summary>
        private Dictionary<string, IEnumerable<Polygon>> RimPolyLookup { get; set; } = new Dictionary<string, IEnumerable<Polygon>>();

        /// <summary> Lookup from RIM filename to a collection of unwalkable face polygons. </summary>
        private Dictionary<string, IEnumerable<Polygon>> RimOutlinePolyLookup { get; set; } = new Dictionary<string, IEnumerable<Polygon>>();

        /// <summary> Lookup from RIM filename to a collection of trans_abort points. </summary>
        private Dictionary<string, IEnumerable<Ellipse>> RimTransAborts { get; set; } = new Dictionary<string, IEnumerable<Ellipse>>();

        /// <summary>
        /// Lookup of the brushes used to draw and how many meshes are currently using them.
        /// </summary>
        private Dictionary<Brush, int> PolyBrushCount { get; set; } = new Dictionary<Brush, int>
        {
            { new SolidColorBrush(new Color { R = 0x00, G = 0x00, B = 0xFF, A = 0xFF }), 0 },
            { new SolidColorBrush(new Color { R = 0x00, G = 0xFF, B = 0x00, A = 0xFF }), 0 },
            { new SolidColorBrush(new Color { R = 0xFF, G = 0x00, B = 0x00, A = 0xFF }), 0 },
            { new SolidColorBrush(new Color { R = 0x00, G = 0xFF, B = 0xFF, A = 0xFF }), 0 },
            { new SolidColorBrush(new Color { R = 0xFF, G = 0x00, B = 0xFF, A = 0xFF }), 0 },
            { new SolidColorBrush(new Color { R = 0xFF, G = 0xFF, B = 0x00, A = 0xFF }), 0 },
        };

        private Brush GrayScaleBrush { get; set; } = Brushes.White;

        private Brush TransAbortBorderBrush { get; set; } = Brushes.White;

        private Dictionary<string, Brush> RimToBrushUsed { get; set; } = new Dictionary<string, Brush>();

        private List<string> RimPolysCreated { get; set; } = new List<string>();

        public BackgroundWorker GameDataWorker { get; set; } = new BackgroundWorker();
        public BackgroundWorker AddPolyWorker { get; set; } = new BackgroundWorker();
        public BackgroundWorker RemovePolyWorker { get; set; } = new BackgroundWorker();

        public bool K1Loaded { get; set; }
        public bool K2Loaded { get; set; }
        public XmlGame CurrentGame { get; set; }

        private readonly Point BaseOffset = new Point(20, 20);
        public const string DEFAULT = "N/A";
        public const string LOADING = "Loading";
        public const string K1_NAME = "KotOR 1";
        public const string K2_NAME = "KotOR 2";
        private const string K1_DEFAULT_PATH = @"C:\Program Files (x86)\Steam\steamapps\common\swkotor";
        private const string K2_DEFAULT_PATH = @"C:\Program Files (x86)\Steam\steamapps\common\Knights of the Old Republic II";

        #endregion // END REGION KIO Members

        #region DataBinding Members

        public string Game { get; private set; }

        public string WindowTitle
        {
            get
            {
                var v = System.Reflection.Assembly.GetAssembly(typeof(VisualizerWindow)).GetName().Version;
                return $"KotOR Walkmesh Visualizer (v{v.Major}.{v.Minor}.{v.Build})";
            }
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

        private bool _leftClickPointVisible;
        public bool LeftClickPointVisible
        {
            get => _leftClickPointVisible;
            set => SetField(ref _leftClickPointVisible, value);
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

        private bool _rightClickPointVisible;
        public bool RightClickPointVisible
        {
            get => _rightClickPointVisible;
            set => SetField(ref _rightClickPointVisible, value);
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

        public bool ShowTransAbortRegions
        {
            get => _showTransAbortRegions;
            set => SetField(ref _showTransAbortRegions, value);
        }
        private bool _showTransAbortRegions = false;

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
                }
                else if (e.ChangedButton == MouseButton.Right)
                {
                    HandleRightDoubleClick(e.GetPosition(content));
                }
            }

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

        private void BringLeftPointToTop()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
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
            {
                CurrentGame = XmlGameData.GetKotorXml(SupportedGame.Kotor1);
                LoadGameFiles(K1_DEFAULT_PATH, K1_NAME);
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
            if (Directory.Exists(K2_DEFAULT_PATH))
            {
                CurrentGame = XmlGameData.GetKotorXml(SupportedGame.Kotor2);
                LoadGameFiles(K2_DEFAULT_PATH, K2_NAME);
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
                    CurrentGame = XmlGameData.GetKotorXml(SupportedGame.Kotor1);
                    LoadGameFiles(dir.FullName, K1_NAME);
                }
                if (exe.Name.ToLower() == "swkotor2.exe")
                {
                    CurrentGame = XmlGameData.GetKotorXml(SupportedGame.Kotor2);
                    LoadGameFiles(dir.FullName, K2_NAME);
                }
            }
        }

        /// <summary>
        /// Load Custom can execute if no game is busy and the app is not busy.
        /// </summary>
        private void LoadCustom_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsBusy && SelectedGame == DEFAULT;
        }

        /// <summary>
        /// Load game files based on the given game directory.
        /// </summary>
        /// <param name="path">Directory full path. Null if loading from cache.</param>
        /// <param name="name">Name of the selected game.</param>
        private void LoadGameFiles(string path, string name)
        {
            HideGameButtons();

            // Initialize game path and set game as Loading.
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
            pnlGameSelect.Visibility = Visibility.Collapsed;
            pnlSelectedGame.Visibility = Visibility.Visible;
            pnlRimSelect.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Performs steps to load required game data.
        /// </summary>
        private void GameDataWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (Game == K1_NAME)
                {
                    CurrentGameData = KotorDataFactory.GetKotor1Data(GameDataWorker.ReportProgress);
                }
                else if (Game == K2_NAME)
                {
                    CurrentGameData = KotorDataFactory.GetKotor2Data(GameDataWorker.ReportProgress);
                }
                else
                {
                    SelectedGame = DEFAULT;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _ = MessageBox.Show(
                            this,
                            "Unknown game selection error occurred.",
                            "Loading Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    });
                    return;
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
                    });
                }

                OffRims = new ObservableCollection<RimModel>(rimModels);
                SelectedGame = e.Argument?.ToString() ?? DEFAULT;
                if (Game == K1_NAME) K1Loaded = true;
                if (Game == K2_NAME) K2Loaded = true;
            }
            catch (Exception ex)
            {
                SelectedGame = DEFAULT;
                var sb = new StringBuilder();
                _ = sb.AppendLine("An unexpected error occurred while loading game data.")
                      .AppendLine($"-- {ex.Message}");
                if (ex.InnerException != null)
                    _ = sb.AppendLine($"-- {ex.InnerException.Message}");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _ = MessageBox.Show(
                        this,
                        sb.ToString(),
                        "Loading Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
        }

        #endregion // END REGION Game Selection Methods

        #region Swap Game Methods

        /// <summary>
        /// Perform steps to allow the user to swap to a different game.
        /// </summary>
        private void SwapGame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
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
            pnlRimSelect.Visibility = Visibility.Hidden;
            pnlGameSelect.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Swap Game can execute if the Selected Game panel is visible (i.e., a game is selected).
        /// </summary>
        private void SwapGame_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsBusy && pnlSelectedGame.Visibility == Visibility.Visible;
        }

        #endregion // END REGION Swap Game Methods

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

                // Start worker to add polygons to the canvas.
                _ = content.Focus();
                AddPolyWorker.RunWorkerAsync(rim);
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
            var rimToAdd = (RimModel)e.Argument;  // grab rim info

            BuildNewWalkmeshes();

            ResizeCanvas();

            ShowWalkmeshOnCanvas(rimToAdd);

            if (LeftClickPointVisible) BringLeftPointToTop();
            if (RightClickPointVisible) BringRightPointToTop();
        }

        /// <summary>
        /// Build any new walkmesh in the ON collection.
        /// </summary>
        private void BuildNewWalkmeshes()
        {
            // Build unbuilt RIM walkmeshes.
            var unbuilt = OnRims.Where(n => !RimPolyLookup.ContainsKey(n.FileName)).ToList();
            for (var i = 0; i < unbuilt.Count; i++)
            {
                var rimmodel = unbuilt[i];
                var name = rimmodel.FileName;
                Canvas walkCanvas = null, nonWalkCanvas = null, transAbortCanvas = null, transBorderCanvas = null, fullCanvas = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    walkCanvas = new Canvas
                    {
                        Opacity = 0.8,
                        Visibility = ShowWalkableFaces ? Visibility.Visible : Visibility.Collapsed,
                    };
                    nonWalkCanvas = new Canvas
                    {
                        Opacity = 0.8,
                        Visibility = ShowNonWalkableFaces ? Visibility.Visible : Visibility.Collapsed,
                    };
                    transAbortCanvas = new Canvas
                    {
                        Opacity = 0.8,
                        Visibility = ShowTransAbortPoints ? Visibility.Visible : Visibility.Collapsed,
                    };
                    transBorderCanvas = new Canvas
                    {
                        Opacity = 0.5,
                        Visibility = ShowTransAbortRegions ? Visibility.Visible : Visibility.Collapsed,
                    };
                });

                // Select all faces from mesh.
                //var woks = RimWoksLookup[name];
                var allfaces = CurrentGameData.RimNameToWoks[name].SelectMany(w => w.Faces).ToList();

                // Create a polygon for each face.
                var polys = new List<Polygon>();    // walkable polygons
                var unpolys = new List<Polygon>();  // unwalkable polygons
                var tas = new List<Ellipse>();  // trans_abort points
                //var tabs = new List<Line>();    // trans_abort borders
                for (var j = 0; j < allfaces.Count; j++)
                {
                    AddPolyWorker.ReportProgress(100 * j / allfaces.Count);
                    var points = allfaces[j].ToPoints();    // points of this face

                    // Create polygons, sorted based on walkability.
                    if (allfaces[j].IsWalkable)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var poly = new Polygon { Points = new PointCollection(points) };
                            polys.Add(poly);
                            _ = walkCanvas.Children.Add(poly);
                        });
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var poly = new Polygon
                            {
                                Points = new PointCollection(points),
                                StrokeThickness = .1,
                            };
                            unpolys.Add(poly);
                            _ = nonWalkCanvas.Children.Add(poly);
                        });
                    }
                }

                var taWaypoints = CurrentGameData.RimNameToGit[name].Waypoints.Structs
                    .Where(s => s.Fields.FirstOrDefault(f => f.Label == "Tag") is GFF.CExoString t &&
                                t.CEString == "wp_transabort").ToList();
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

                // Calculate border lines between each pair of trans_abort points.
                CalculateTransAbortBorders(transBorderCanvas, name, transAbortPoints);

                // Cache the created polygons.
                RimPolyLookup.Add(name, polys);
                RimOutlinePolyLookup.Add(name, unpolys);
                RimTransAborts.Add(name, tas);

                // Cache the canvases.
                RimWalkableCanvasLookup.Add(name, walkCanvas);
                RimNonwalkableCanvasLookup.Add(name, nonWalkCanvas);
                RimTransAbortPointCanvasLookup.Add(name, transAbortCanvas);
                RimTransAbortRegionCanvasLookup.Add(name, transBorderCanvas);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    fullCanvas = new Canvas();
                    _ = fullCanvas.Children.Add(walkCanvas);
                    _ = fullCanvas.Children.Add(nonWalkCanvas);
                    _ = fullCanvas.Children.Add(transBorderCanvas);
                    _ = fullCanvas.Children.Add(transAbortCanvas);
                });

                RimFullCanvasLookup.Add(name, fullCanvas);
            }
        }

        /// <summary>
        /// Calculates the bounding points for trans_abort regions.
        /// </summary>
        private void CalculateTransAbortBorders(Canvas transBorderCanvas, string rimName, List<Point> transAbortPoints)
        {
            // Calculate minimum and maximum x and y values of this module.
            var woks = CurrentGameData.RimNameToWoks[rimName];
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
                    //var normalNullable = thisLine.NormalVector(iPoint);
                    //if (!normalNullable.HasValue) continue;
                    var normal = thisLine.NormalVector(iPoint).Value;
                    normal.Normalize();
                    //if (!normal.HasValue) continue;

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

            //// Determine text for regions.
            //var froms = RimGitLookup[rimName].Waypoints.Structs.Where(s => s.Fields.Any(
            //    f => f is GFF.CExoString ces &&
            //    (ces.CEString.ToLower().StartsWith("from") ||
            //     ces.CEString.ToLower().Contains("ebon_hawk_transition"))));
            //var fromLabels = new string[transAbortPoints.Count];
            //for (var i = 0; i < transAbortPoints.Count; i++)
            //{
            //    var abortWP = transAbortPoints[i];
            //    Point? fromWP = null;
            //    string fromTag = null;
            //    var distance = double.NaN;

            //    // Find closest from waypoint.
            //    foreach (var from in froms)
            //    {
            //        var x = (from.Fields.FirstOrDefault(f => f.Label == "XPosition") as GFF.FLOAT).Value;
            //        var y = (from.Fields.FirstOrDefault(f => f.Label == "YPosition") as GFF.FLOAT).Value;
            //        var thisWP = new Point(x, y);
            //        var thisDistance = abortWP.Distance(thisWP);

            //        if (!fromWP.HasValue || thisDistance < distance)
            //        {
            //            fromWP = thisWP;
            //            distance = thisDistance;
            //            fromTag = (from.Fields.First(f => f.Label == "Tag") as GFF.CExoString).CEString.ToLower();
            //        }
            //    }

            //    // Determine label from closest waypoint tag.
            //    if (fromTag.Contains("ebon_hawk_transition"))
            //    {
            //        fromLabels[i] = "Ebon Hawk";
            //    }
            //    else
            //    {
            //        fromTag = fromTag.Replace("from", "");  // Remove starting "from"
            //        var fromNum = fromTag.Substring(0, 2);  // First two characters contain the number.
            //        if (fromTag.Length == 2)
            //        {
            //            fromLabels[i] = $"m{fromNum}";
            //        }
            //        else
            //        {
            //            var fromLtr = fromTag.Substring(2, 1);  // Next character is the unique module letter.
            //            fromLabels[i] = $"m{fromNum}a{fromLtr}";
            //        }
            //    }
            //}

            // Draw regions.
            var fillIdx = 0;
            //var foreIdx = 2;
            //foreach (var region in regions)
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

                    //var text = new TextBlock
                    //{
                    //    Text = "Hello world",
                    //    FontSize = 4,
                    //    Foreground = Brushes.White,
                    //    Background = Brushes.Black,
                    //    HorizontalAlignment = HorizontalAlignment.Center,
                    //    TextAlignment = TextAlignment.Center,
                    //    RenderTransform = content.Resources["OffsetTransform"] as Transform,
                    //};
                    //Canvas.SetLeft(text, transAbortPoints[i].X - (BaseOffset.X / 2));
                    //Canvas.SetTop(text, -transAbortPoints[i].Y - BaseOffset.Y);
                    //_ = transBorderCanvas.Children.Add(text);
                });
                fillIdx = (fillIdx + 1) % PolyBrushCount.Count;
                //foreIdx = (foreIdx + 1) % PolyBrushCount.Count;
            }
        }

        /// <summary>
        /// Resize the canvas to fit all displayed faces.
        /// </summary>
        private void ResizeCanvas()
        {
            // Determine size of canvas and offset.
            var woks = CurrentGameData.RimNameToWoks
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
        private void ShowWalkmeshOnCanvas(RimModel rimToAdd)
        {
            // Determine next brush to use.
            var brushChanged = true;
            Brush brush;
            if (RimToBrushUsed.ContainsKey(rimToAdd.FileName))
            {
                // If added to the canvas before, find most frequent brushes.
                var max = PolyBrushCount.Where(kvp => kvp.Value == PolyBrushCount.Values.Max()).ToDictionary(kvp => kvp.Key);

                // If not all brushes are equal AND last used brush is in max frequency, get a new brush.
                if (max.Count != PolyBrushCount.Count && max.ContainsKey(RimToBrushUsed[rimToAdd.FileName]))
                {
                    brush = GetNextBrush();
                    PolyBrushCount[brush]++;
                }
                // Else, don't change the brush.
                else
                {
                    brush = RimToBrushUsed[rimToAdd.FileName];
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
        /// Updates the brush used by a displayed rim.
        /// </summary>
        private void UpdateRimFillColor(BackgroundWorker bw, Brush brush, RimModel rimModel)
        {
            var fills = RimPolyLookup[rimModel.FileName].Select(p => p as Shape).ToList();  // walkable
            fills.AddRange(RimTransAborts[rimModel.FileName]);  // trans_abort points
            var strokes = RimOutlinePolyLookup[rimModel.FileName].ToList();   // non-walkable
            var i = 0;

            // If the brush is different, update with new color.
            fills.ForEach((Shape p) =>
            {
                content.Dispatcher.Invoke(() =>
                {
                    bw?.ReportProgress(100 * i++ / fills.Count);
                    p.Fill = brush;
                });
            });
            i = 0;  // reset count
            strokes.ForEach((Polygon p) =>
            {
                content.Dispatcher.Invoke(() =>
                {
                    bw?.ReportProgress(100 * i++ / strokes.Count);
                    p.Stroke = brush;
                });
            });
        }

        /// <summary>
        /// Determine next brush to use in sequence.
        /// </summary>
        private Brush GetNextBrush()
        {
            var min = PolyBrushCount.Values.Min();
            return PolyBrushCount.First(pair => pair.Value == min).Key;
        }

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
        /// Find matching coords can execute if a point has been selected and there are any modules currently displayed.
        /// </summary>
        private void FindMatchingCoords_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsBusy && (LeftClickPointVisible || RightClickPointVisible) && (OnRims?.Any() ?? false);
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
            var rimwoks = CurrentGameData.RimNameToWoks.Where(kvp => CurrentGame.Rims.Any(xr => xr.FileName == kvp.Key));
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
                SaveImageFromCanvas(zoomAndPanControl.MaxContentScale);
            }
            else
            {
                var scale = zoomAndPanControl.MaxContentScale + zoomAndPanControl.ContentScale;
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
            SaveImageFromCanvas(zoomAndPanControl.MaxContentScale);
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
                if (rimmodel == null) return;
                UpdateRimFillColor(null, GrayScaleBrush, rimmodel);
            }
            else
            {
                content.Background = Brushes.White;
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

        private void ShowWalkableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is VisualizerWindow) ShowWalkableFaces = !ShowWalkableFaces;
            UpdateWalkableVisibility();
        }

        private void ShowWalkableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsBusy;
        }

        private void ShowNonWalkableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is VisualizerWindow) ShowNonWalkableFaces = !ShowNonWalkableFaces;
            UpdateNonWalkableVisibility();
        }

        private void ShowNonWalkableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsBusy;
        }

        private void ShowTransAbortCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is VisualizerWindow) ShowTransAbortPoints = !ShowTransAbortPoints;
            UpdateTransAbortPointVisibility();
        }

        private void ShowTransAbortCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsBusy;
        }

        private void ShowTransAbortRegionCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is VisualizerWindow) ShowTransAbortRegions = !ShowTransAbortRegions;
            UpdateTransAbortRegionVisibility();
        }

        private void ShowTransAbortRegionCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsBusy && OnRims.Count < 2;
        }

        #endregion

        private void LoadSupportedGame(SupportedGame game, string directory = null)
        {
            switch (game)
            {
                case SupportedGame.Kotor1:
                case SupportedGame.Kotor2:
                    CurrentGame = XmlGameData.GetKotorXml(game);
                    LoadGameFiles(directory, game.ToDescription());
                    break;
                case SupportedGame.NotSupported:
                default:
                    break;
            }
        }

        private void LoadGame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is SupportedGame game)
            {
                OpenFileDialog ofd = null;
                switch (game)
                {
                    case SupportedGame.Kotor1:
                        ofd = new OpenFileDialog
                        {
                            Title = "Select KotOR 1 Executable File",
                            Filter = "Exe File (swkotor.exe)|swkotor.exe",
                            InitialDirectory = K1_DEFAULT_PATH,
                            FileName = "swkotor.exe",
                            CheckFileExists = true,
                        };
                        break;
                    case SupportedGame.Kotor2:
                        ofd = new OpenFileDialog
                        {
                            Title = "Select KotOR 2 Executable File",
                            Filter = "Exe File (swkotor2.exe)|swkotor2.exe",
                            InitialDirectory = K2_DEFAULT_PATH,
                            FileName = "swkotor2.exe",
                            CheckFileExists = true,
                        };
                        break;
                    case SupportedGame.NotSupported:
                    default:
                        // Throw exception?
                        break;
                }
                if (ofd?.ShowDialog() == true)
                {
                    LoadSupportedGame(game, new FileInfo(ofd.FileName).DirectoryName);
                }
            }
            else
            {
                // Throw exception?
            }
        }

        private void LoadGame_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter is SupportedGame game)
            {
                switch (game)
                {
                    case SupportedGame.Kotor1:
                        e.CanExecute = !KotorDataFactory.IsKotor1Cached;
                        break;
                    case SupportedGame.Kotor2:
                        e.CanExecute = !KotorDataFactory.IsKotor2Cached;
                        break;
                    case SupportedGame.NotSupported:
                    default:
                        e.CanExecute = false;
                        break;
                }
            }
        }

        private void LoadCache_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is SupportedGame game)
            {
                LoadSupportedGame(game);
            }
            else
            {
                // Throw exception?
            }
        }

        private void LoadCache_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter is SupportedGame game)
            {
                switch (game)
                {
                    case SupportedGame.Kotor1:
                        e.CanExecute = KotorDataFactory.IsKotor1Cached;
                        break;
                    case SupportedGame.Kotor2:
                        e.CanExecute = KotorDataFactory.IsKotor2Cached;
                        break;
                    case SupportedGame.NotSupported:
                    default:
                        e.CanExecute = false;
                        break;
                }
            }
        }

        private void ClearCache_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is SupportedGame game)
            {
                switch (game)
                {
                    case SupportedGame.Kotor1:
                    case SupportedGame.Kotor2:
                        KotorDataFactory.DeleteCachedGameData(game);
                        break;
                    case SupportedGame.NotSupported:
                    default:
                        // Throw exception?
                        break;
                }
            }
            else
            {
                // Throw exception?
            }
        }

        private void ClearCache_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter is SupportedGame game)
            {
                switch (game)
                {
                    case SupportedGame.Kotor1:
                        e.CanExecute = KotorDataFactory.IsKotor1Cached;
                        break;
                    case SupportedGame.Kotor2:
                        e.CanExecute = KotorDataFactory.IsKotor2Cached;
                        break;
                    case SupportedGame.NotSupported:
                    default:
                        e.CanExecute = false;
                        break;
                }
            }
        }
    }
}
