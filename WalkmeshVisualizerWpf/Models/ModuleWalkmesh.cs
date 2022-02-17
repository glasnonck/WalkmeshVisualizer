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
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using WalkmeshVisualizerWpf.Helpers;

namespace WalkmeshCompareWpf.Models
{
    public class ModuleWalkmesh : INotifyPropertyChanged
    {
        #region Fields

        private string rimName;
        private string commonName;
        private Brush meshBrush;
        private ObservableCollection<WOK> walkmeshes;
        private ObservableCollection<Polygon> walkablePolygons;
        private ObservableCollection<Polygon> nonWalkablePolygons;

        #endregion

        #region Properties

        public string RimName
        {
            get => rimName;
            set => SetField(ref rimName, value);
        }

        public string CommonName
        {
            get => commonName;
            set => SetField(ref commonName, value);
        }

        public Brush MeshBrush
        {
            get => meshBrush;
            set => SetField(ref meshBrush, value);
        }

        public ObservableCollection<WOK> Walkmeshes
        {
            get => walkmeshes;
            set => SetField(ref walkmeshes, value);
        }

        public ObservableCollection<Polygon> WalkablePolygons
        {
            get => walkablePolygons;
            set => SetField(ref walkablePolygons, value);
        }

        public ObservableCollection<Polygon> NonWalkablePolygons
        {
            get => nonWalkablePolygons;
            set => SetField(ref nonWalkablePolygons, value);
        }

        #endregion

        #region Methods

        public void CreatePolygons(BackgroundWorker bw)
        {
            // Return if polygons already created.
            if (WalkablePolygons.Any() || NonWalkablePolygons.Any()) return;

            // Select all faces from mesh.
            var count = 0;
            var allFaces = Walkmeshes.SelectMany(w => w.Faces).ToList();
            allFaces.ForEach((WOK.Face f) =>
            {
                bw.ReportProgress(100 * count++ / allFaces.Count);
                var points = f.ToPoints();

                // Create polygons, sorted based on walkability.
                if (f.IsWalkable)
                {
                    WalkablePolygons.Add(new Polygon
                    {
                        Points = new PointCollection(points)
                    });
                }
                else
                {
                    NonWalkablePolygons.Add(new Polygon
                    {
                        Points = new PointCollection(points),
                        StrokeThickness = .05
                    });
                }
            });
        }

        public void SetPolygonVisibility(Visibility visibility, BackgroundWorker bw)
        {
            // Retrieve all polygons from the cache.
            var polys = WalkablePolygons.ToList();
            polys.AddRange(NonWalkablePolygons);

            // Set visibility of each polygon.
            for (var i = 0; i < polys.Count; i++)
            {
                if (bw != null) bw.ReportProgress(100 * i / polys.Count);
                polys[i].Visibility = visibility;
            }
        }

        public void ClearPolygonCache(BackgroundWorker bw)
        {
            // Retrieve all polygons from the cache.
            var polys = WalkablePolygons.ToList();
            polys.AddRange(NonWalkablePolygons);

            // Clear bindings for each polygon.
            for (var i = 0; i < polys.Count; i++)
            {
                if (bw != null) bw.ReportProgress(100 * i / polys.Count);
                var rt = polys[i].RenderTransform as TransformGroup;

                // Remove bindings for each of the transforms.
                foreach (var transform in rt.Children)
                {
                    BindingOperations.ClearAllBindings(transform);
                }

                // Remove bindings on the polygon.
                BindingOperations.ClearBinding(polys[i], UIElement.RenderTransformProperty);
            }

            // Clear the caches.
            WalkablePolygons.Clear();
            NonWalkablePolygons.Clear();
        }

        #endregion

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
