using KotOR_IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WalkmeshVisualizerWpf.Helpers;
using WalkmeshVisualizerWpf.Models;

namespace WalkmeshVisualizerWpf.Views
{
    /// <summary>
    /// Interaction logic for RimView.xaml
    /// </summary>
    public partial class RimView : UserControl, INotifyPropertyChanged
    {
        #region Constructors

        public RimView()
        {
            InitializeComponent();
            DataContext = this;
        }

        #endregion // Constructors

        #region Properties

        /// <summary>
        /// Canvas containing all of the displayed RIM data.
        /// </summary>
        public Canvas DisplayCanvas { get; private set; } = new Canvas();

        /// <summary>
        /// Lookup from RIM filename to the canvas containing its walkmesh.
        /// </summary>
        private Dictionary<string, Canvas> RimFullCanvasLookup { get; set; } = new Dictionary<string, Canvas>();

        /// <summary>
        /// Lookup from RIM filename to the canvas containing its walkable faces.
        /// </summary>
        private Dictionary<string, Canvas> RimWalkableCanvasLookup { get; set; } = new Dictionary<string, Canvas>();

        /// <summary>
        /// Lookup from RIM filename to the canvas containing its non-walkable faces.
        /// </summary>
        private Dictionary<string, Canvas> RimNonwalkableCanvasLookup { get; set; } = new Dictionary<string, Canvas>();

        /// <summary>
        /// Lookup from RIM filename to the canvas containing its trans_abort waypoints.
        /// </summary>
        private Dictionary<string, Canvas> RimTransAbortPointCanvasLookup { get; set; } = new Dictionary<string, Canvas>();

        /// <summary>
        /// Lookup from RIM filename to the canvas containing its gather party regions.
        /// </summary>
        private Dictionary<string, Canvas> RimTransAbortRegionCanvasLookup { get; set; } = new Dictionary<string, Canvas>();

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

        /// <summary> Lookup from RIM filename to a collection of walkmesh face polygons. </summary>
        private Dictionary<string, IEnumerable<Polygon>> RimPolyLookup { get; set; } = new Dictionary<string, IEnumerable<Polygon>>();

        /// <summary> Lookup from RIM filename to a collection of unwalkable face polygons. </summary>
        private Dictionary<string, IEnumerable<Polygon>> RimOutlinePolyLookup { get; set; } = new Dictionary<string, IEnumerable<Polygon>>();

        /// <summary> Lookup from RIM filename to a collection of trans_abort points. </summary>
        private Dictionary<string, IEnumerable<Ellipse>> RimTransAborts { get; set; } = new Dictionary<string, IEnumerable<Ellipse>>();

        public double ZoomLevel
        {
            get => (double)GetValue(ZoomLevelProperty);
            set => SetValue(ZoomLevelProperty, value);
        }
        public static readonly DependencyProperty ZoomLevelProperty = DependencyProperty.Register("ZoomLevel", typeof(double), typeof(RimView), new PropertyMetadata(3d));

        #endregion // Properties

        #region Notifying Properties

        /// <summary>
        /// Rim models that are not displayed. (VisibleRims)
        /// </summary>
        public ObservableCollection<RimModel> OffRims
        {
            get => _hiddenRims;
            set => SetField(ref _hiddenRims, value);
        }
        private ObservableCollection<RimModel> _hiddenRims;

        /// <summary>
        /// Rim models that are currently displayed. (HiddenRims)
        /// </summary>
        public ObservableCollection<RimModel> OnRims
        {
            get => _visibleRims;
            set => SetField(ref _visibleRims, value);
        }
        private ObservableCollection<RimModel> _visibleRims;

        /// <summary>
        /// Are walkable faces visible?
        /// </summary>
        public bool ShowWalkableFaces
        {
            get => _showWalkableFaces;
            set => SetField(ref _showWalkableFaces, value);
        }
        private bool _showWalkableFaces;

        /// <summary>
        /// Are non-walkable faces visible?
        /// </summary>
        public bool ShowNonWalkableFaces
        {
            get => _showNonWalkableFaces;
            set => SetField(ref _showNonWalkableFaces, value);
        }
        private bool _showNonWalkableFaces;

        /// <summary>
        /// Are trans_abort waypoints visible?
        /// </summary>
        public bool ShowTransAbortPoints
        {
            get => _showTransAbortPoints;
            set => SetField(ref _showTransAbortPoints, value);
        }
        private bool _showTransAbortPoints = false;

        /// <summary>
        /// Are GP warp regions visible?
        /// </summary>
        public bool ShowTransAbortRegions
        {
            get => _showTransAbortRegions;
            set => SetField(ref _showTransAbortRegions, value);
        }
        private bool _showTransAbortRegions = false;

        #endregion // Notifying Properties

        #region Delegates

        /// <summary>
        /// Delegate of the ReportProgress method of a <see cref="BackgroundWorker"/>.
        /// </summary>
        /// <param name="percentProgress">The percentage, from 0 to 100, of the background operation that is complete.</param>
        public delegate void ReportProgressDelegate(int percentProgress);

        #endregion

        #region Public Methods

        public void AddRim(RimModel rim, Transform tf, ReportProgressDelegate report = null)
        {
            // Disable regions if needed.
            if (ShowTransAbortRegions && OnRims.Count == 1)
            {
                ShowTransAbortRegions = false;
                UpdateTransAbortRegionVisibility();
            }

            // Move the rim from OFF to ON.
            _ = OffRims.Remove(rim);
            var sorted = OnRims.ToList();
            sorted.Add(rim);

            // Sort the ON list and update collection.
            sorted.Sort();
            OnRims = new ObservableCollection<RimModel>(sorted);

            // Start worker to add polygons to the canvas.
            ShowRim(rim, tf, report);
        }

        private void UpdateTransAbortRegionVisibility()
        {
            throw new NotImplementedException();
        }

        private void ShowRim(RimModel rim, Transform tf, ReportProgressDelegate report)
        {
            if (!RimFullCanvasLookup.ContainsKey(rim.FileName))
                BuildNewWalkmesh(rim, tf, report);
            ResizeCanvas(rim, report);
            ShowWalkmeshOnCanvas(rim, report);
            //if (LeftClickPointVisible) BringLeftPointToTop();
            //if (RightClickPointVisible) BringRightPointToTop();
        }

        /// <summary>
        /// Builds displayed information for the new walkmesh.
        /// </summary>
        /// <param name="rim">Rim to build.</param>
        /// <param name="tf">Cartesian Transform to apply to the objects.</param>
        /// <param name="report">Delegate used to report progress.</param>
        private void BuildNewWalkmesh(RimModel rim, Transform tf, ReportProgressDelegate report)
        {
            CreateNewCanvases(rim);

            CreateNewWalkmeshFaces(rim, report);

            CreateNewTransAbortWaypoints(rim, tf, report);
        }

        /// <summary>
        /// Builds an ellipse for each trans_abort waypoint and the gather party regions in this <see cref="RimModel"/>.
        /// </summary>
        private void CreateNewTransAbortWaypoints(RimModel rim, Transform tf, ReportProgressDelegate report)
        {
            // Read trans_abort waypoints from the game data.
            var taWaypoints = KotorDataFactory.CurrentGameData
                    .RimNameToGit[rim.FileName].Waypoints.Structs
                    .Where(s => s.Fields.FirstOrDefault(f => f.Label == "Tag") is GFF.CExoString t
                             && t.CEString == "wp_transabort").ToList();

            // Create an ellipse for each trans_abort waypoint.
            var transAbortPoints = new List<Point>();
            var tas = new List<Ellipse>();  // trans_abort points
            for (var i = 0; i < taWaypoints.Count; i++)
            {
                report?.Invoke(100 * i / taWaypoints.Count);
                var waypoint = taWaypoints[i];

                // Calculate point location.
                var x = (waypoint.Fields.FirstOrDefault(f => f.Label == "XPosition") is GFF.FLOAT xf) ? xf.Value : double.NaN;
                var y = (waypoint.Fields.FirstOrDefault(f => f.Label == "YPosition") is GFF.FLOAT yf) ? yf.Value : double.NaN;
                var z = (waypoint.Fields.FirstOrDefault(f => f.Label == "ZPosition") is GFF.FLOAT zf) ? zf.Value : double.NaN;

                if (double.IsNaN(x) || double.IsNaN(y)) continue;
                var point = new Point(x, y);
                transAbortPoints.Add(point);

                // Create ellipse and add to the canvas.
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var e = new Ellipse
                    {
                        Stroke = Brushes.Black,
                        StrokeThickness = 0.25,
                        Height = 2,
                        Width = 2,
                        RenderTransform = tf,
                    };
                    Canvas.SetLeft(e, point.X - (e.Width / 2));
                    Canvas.SetTop(e, -point.Y + (e.Height / 2));
                    tas.Add(e);
                    _ = RimTransAbortPointCanvasLookup[rim.FileName].Children.Add(e);
                });
            }

            // Cache the created points.
            RimTransAborts.Add(rim.FileName, tas);

            // Calculate border lines between each pair of trans_abort points.
            CalculateTransAbortBorders(rim.FileName, tf, transAbortPoints);
        }

        /// <summary>
        /// Builds a polygon for each face of this <see cref="RimModel"/> walkmesh.
        /// </summary>
        private void CreateNewWalkmeshFaces(RimModel rim, ReportProgressDelegate report)
        {
            // Select all faces from mesh.
            var allfaces = KotorDataFactory.CurrentGameData.RimNameToWoks[rim.FileName]
                .SelectMany(w => w.Faces).ToList();

            // Create a polygon for each face.
            var polys = new List<Polygon>();    // walkable polygons
            var unpolys = new List<Polygon>();  // unwalkable polygons
            for (var j = 0; j < allfaces.Count; j++)
            {
                report?.Invoke(100 * j / allfaces.Count);
                var points = allfaces[j].ToPoints();    // points of this face

                // Create polygons, sorted based on walkability.
                if (allfaces[j].IsWalkable)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var poly = new Polygon { Points = new PointCollection(points) };
                        polys.Add(poly);
                        _ = RimWalkableCanvasLookup[rim.FileName].Children.Add(poly);
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
                        _ = RimNonwalkableCanvasLookup[rim.FileName].Children.Add(poly);
                    });
                }
            }

            // Cache the created polygons.
            RimPolyLookup.Add(rim.FileName, polys);
            RimOutlinePolyLookup.Add(rim.FileName, unpolys);
        }

        /// <summary>
        /// Create canvases for the given RimModel for each type of module visualization.
        /// </summary>
        private void CreateNewCanvases(RimModel rim)
        {
            DisplayCanvas.Dispatcher.Invoke(() =>
            {
                RimWalkableCanvasLookup.Add(rim.FileName, new Canvas
                {
                    Opacity = 0.8,
                    Visibility = ShowWalkableFaces.ToVisibility(),
                });
                RimNonwalkableCanvasLookup.Add(rim.FileName, new Canvas
                {
                    Opacity = 0.8,
                    Visibility = ShowNonWalkableFaces.ToVisibility(),
                });
                RimTransAbortPointCanvasLookup.Add(rim.FileName, new Canvas
                {
                    Opacity = 0.8,
                    Visibility = ShowTransAbortPoints.ToVisibility(),
                });
                RimTransAbortRegionCanvasLookup.Add(rim.FileName, new Canvas
                {
                    Opacity = 0.5,
                    Visibility = ShowTransAbortRegions.ToVisibility(),
                });
                RimFullCanvasLookup.Add(rim.FileName, new Canvas
                {
                    Children =
                    {
                        RimWalkableCanvasLookup[rim.FileName],
                        RimNonwalkableCanvasLookup[rim.FileName],
                        RimTransAbortPointCanvasLookup[rim.FileName],
                        RimTransAbortRegionCanvasLookup[rim.FileName],
                    }
                });
            });
        }

        private void CalculateTransAbortBorders(string name, Transform tf, List<Point> transAbortPoints)
        {
            throw new NotImplementedException();
        }

        private void ResizeCanvas(RimModel rim, ReportProgressDelegate report)
        {
            throw new NotImplementedException();
        }

        private void ShowWalkmeshOnCanvas(RimModel rim, ReportProgressDelegate report)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Hide all active <see cref="RimModel"/>s on the canvas.
        /// </summary>
        public void HideAllRims()
        {
            // Move all rims to the hidden collection, and then sort.
            foreach (var rim in OnRims)
                OffRims.Add(rim);
            OnRims.Clear();
            OffRims = OffRims.Sort();

            // Hide all polygons in the canvas.
            DisplayCanvas.Dispatcher.Invoke(() =>
            {
                foreach (var child in DisplayCanvas.Children.OfType<Canvas>())
                {
                    child.Visibility = Visibility.Collapsed;
                }
            });

            // Set brush count to 0 since all walkmeshes are now hidden.
            foreach (var key in PolyBrushCount.Keys.ToList())
            {
                PolyBrushCount[key] = 0;
            }
        }

        #endregion // Public Methods

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Invokes the PropertyChanged event with the name of the property that has changed.
        /// </summary>
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// If <paramref name="value"/> is different than the current value of <paramref name="field"/>,
        /// set the new value, raise the <see cref="PropertyChanged"/> event, and return true. Otherwise,
        /// just return false.
        /// </summary>
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }

        #endregion // INotifyPropertyChanged Implementation

        #region Event Handlers

        private void RemoveAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void RemoveAll_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {

        }

        private void ShowWalkableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void ShowWalkableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {

        }

        private void ShowNonWalkableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void ShowNonWalkableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {

        }

        private void ShowTransAbortCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void ShowTransAbortCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {

        }

        private void ShowTransAbortRegionCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void ShowTransAbortRegionCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {

        }

        private void LvOn_DoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void LvOff_DoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        #endregion // Event Handlers
    }
}
