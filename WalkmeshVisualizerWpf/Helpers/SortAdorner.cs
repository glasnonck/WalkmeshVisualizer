using System;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace WalkmeshVisualizerWpf.Helpers
{
    public class SortAdorner(UIElement element, ListSortDirection dir) : Adorner(element)
    {
        private static readonly Geometry ascGeometry  = Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");
        private static readonly Geometry descGeometry = Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");

        public ListSortDirection Direction { get; private set; } = dir;

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (AdornedElement.RenderSize.Width < 20)
                return;

            TranslateTransform transform = new
            (
                AdornedElement.RenderSize.Width - 15,
                (AdornedElement.RenderSize.Height - 5) / 2
            );
            drawingContext.PushTransform(transform);

            Geometry geometry = ascGeometry;
            if (this.Direction == ListSortDirection.Descending)
                geometry = descGeometry;
            drawingContext.DrawGeometry(Brushes.Black, null, geometry);

            drawingContext.Pop();
        }

        internal static void SortColumn(ListView lv, ref GridViewColumnHeader column, ref SortAdorner adorner, GridViewColumnHeader newColumn, string sortBy)
        {
            if (lv == null) return;

            if (column != null)
            {
                AdornerLayer.GetAdornerLayer(column)?.Remove(adorner);
                lv.Items.SortDescriptions.Clear();
            }

            ListSortDirection newDir = ListSortDirection.Ascending;
            if (column == newColumn && adorner?.Direction == newDir)
                newDir = ListSortDirection.Descending;

            column = newColumn;
            adorner = new SortAdorner(column, newDir);
            var layer = AdornerLayer.GetAdornerLayer(column);
            if (layer == null) return;  // The exception kept hopping the catch, so this is a temp fix
            layer.Add(adorner);
            lv.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
        }
    }

    // TODO: Since all values are stored as strings, use a custom comparer to sort numerically where possible.
    
    public class GlobalValueComparer(ListSortDirection direction) : IComparer
    {
        private readonly ListSortDirection _direction = direction;

        public int Compare(object x, object y)
        {
            if (int.TryParse(x.ToString(), out int intX) && int.TryParse(y.ToString(), out int intY))
            {
                // Both values are integers, compare numerically
                return _direction == ListSortDirection.Ascending ? intX.CompareTo(intY) : intY.CompareTo(intX);
            }
            else
            {
                // Fallback to string comparison
                int comparisonResult = string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
                return _direction == ListSortDirection.Ascending ? comparisonResult : -comparisonResult;
            }
        }
    }

    /*
    private void ColumnHeader_Click(object sender, RoutedEventArgs e)
    {
        ICollectionView view = CollectionViewSource.GetDefaultView(myListView.ItemsSource);

        if (view is ListCollectionView listCollectionView)
        {
            // Assign the custom comparer directly
            listCollectionView.CustomSort = new GlobalValueComparer({direction});
        }
    }
    */
}
