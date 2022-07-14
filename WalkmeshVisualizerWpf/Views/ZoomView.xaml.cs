using System;
using System.Collections.Generic;
using System.Linq;
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

namespace WalkmeshVisualizerWpf.Views
{
    /// <summary>
    /// Interaction logic for ZoomView.xaml
    /// </summary>
    public partial class ZoomView : UserControl
    {
        #region Constructors

        public ZoomView()
        {
            InitializeComponent();
            DataContext = this;
        }

        #endregion // Constructors

        #region Properties

        public double BottomOffset
        {
            get => (double)GetValue(BottomOffsetProperty);
            set => SetValue(BottomOffsetProperty, value);
        }

        public double LeftOffset
        {
            get => (double)GetValue(LeftOffsetProperty);
            set => SetValue(LeftOffsetProperty, value);
        }

        public bool LeftClickPointVisible
        {
            get => (bool)GetValue(LeftClickPointVisibleProperty);
            set => SetValue(LeftClickPointVisibleProperty, value);
        }

        public bool RightClickPointVisible
        {
            get => (bool)GetValue(RightClickPointVisibleProperty);
            set => SetValue(RightClickPointVisibleProperty, value);
        }

        public Point LeftClickPoint
        {
            get => (Point)GetValue(LeftClickPointProperty);
            set => SetValue(LeftClickPointProperty, value);
        }

        public Point RightClickPoint
        {
            get => (Point)GetValue(RightClickPointProperty);
            set => SetValue(RightClickPointProperty, value);
        }

        public Point LeftClickModuleCoords
        {
            get => (Point)GetValue(LeftClickModuleCoordsProperty);
            set => SetValue(LeftClickModuleCoordsProperty, value);
        }

        public Point RightClickModuleCoords
        {
            get => (Point)GetValue(RightClickModuleCoordsProperty);
            set => SetValue(RightClickModuleCoordsProperty, value);
        }

        public double ZoomLevel
        {
            get => (double)GetValue(ZoomLevelProperty);
            set => SetValue(ZoomLevelProperty, value);
        }

        public double MinZoomLevel
        {
            get => (double)GetValue(MinZoomLevelProperty);
            set => SetValue(MinZoomLevelProperty, value);
        }

        public double MaxZoomLevel
        {
            get => (double)GetValue(MaxZoomLevelProperty);
            set => SetValue(MaxZoomLevelProperty, value);
        }

        #endregion // Properties

        #region Dependency Property Definitions

        public static readonly DependencyProperty BottomOffsetProperty = DependencyProperty.Register("BottomOffset", typeof(double), typeof(ZoomView), new PropertyMetadata(0d));
        public static readonly DependencyProperty LeftOffsetProperty = DependencyProperty.Register("LeftOffset", typeof(double), typeof(ZoomView), new PropertyMetadata(0d));
        public static readonly DependencyProperty LeftClickPointVisibleProperty = DependencyProperty.Register("LeftClickPointVisible", typeof(bool), typeof(ZoomView), new PropertyMetadata(false));
        public static readonly DependencyProperty RightClickPointVisibleProperty = DependencyProperty.Register("RightClickPointVisible", typeof(bool), typeof(ZoomView), new PropertyMetadata(false));
        public static readonly DependencyProperty LeftClickPointProperty = DependencyProperty.Register("LeftClickPoint", typeof(Point), typeof(ZoomView), new PropertyMetadata(new Point()));
        public static readonly DependencyProperty RightClickPointProperty = DependencyProperty.Register("RightClickPoint", typeof(Point), typeof(ZoomView), new PropertyMetadata(new Point()));
        public static readonly DependencyProperty LeftClickModuleCoordsProperty = DependencyProperty.Register("LeftClickModuleCoords", typeof(Point), typeof(ZoomView), new PropertyMetadata(new Point()));
        public static readonly DependencyProperty RightClickModuleCoordsProperty = DependencyProperty.Register("RightClickModuleCoords", typeof(Point), typeof(ZoomView), new PropertyMetadata(new Point()));
        public static readonly DependencyProperty ZoomLevelProperty = DependencyProperty.Register("ZoomLevel", typeof(double), typeof(ZoomView), new PropertyMetadata(3d));
        public static readonly DependencyProperty MinZoomLevelProperty = DependencyProperty.Register("MinZoomLevel", typeof(double), typeof(ZoomView), new PropertyMetadata(0.1d));
        public static readonly DependencyProperty MaxZoomLevelProperty = DependencyProperty.Register("MaxZoomLevel", typeof(double), typeof(ZoomView), new PropertyMetadata(20d));

        #endregion // Dependency Property Definitions

        #region Event Handlers

        private void ZoomOut_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void ZoomIn_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void JumpBackToPrevZoom_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void JumpBackToPrevZoom_CanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {

        }

        private void Fill_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void OneHundredPercent_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void FifteenHundredPercent_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void ZoomAndPanControl_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void ZoomAndPanControl_MouseUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void ZoomAndPanControl_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void ZoomAndPanControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }

        private void ZoomAndPanControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        #endregion // Event Handlers

    }
}
