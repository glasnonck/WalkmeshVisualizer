using KotOR_IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace WalkmeshVisualizerWpf.Helpers
{
    public static class ExtensionMethods
    {
        public static Point ToPoint(this WOK.Vert vert)
        {
            return new Point(vert.X, vert.Y);
        }

        public static List<Point> ToPoints(this WOK.Face face)
        {
            return new List<Point>
            {
                face.A.ToPoint(),
                face.B.ToPoint(),
                face.C.ToPoint(),
            };
        }

        public static IEnumerable<List<Point>> ToPolys(this WOK wok)
        {
            return wok.Faces.Select(f => f.ToPoints());
        }
    }
}
