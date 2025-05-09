using KotOR_IO;
using KotOR_IO.GffFile;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using WalkmeshVisualizerWpf.Models;

namespace WalkmeshVisualizerWpf.Helpers
{
    public static class ExtensionMethods
    {
        public static double Distance(this Point point, Point other)
        {
            return Math.Sqrt(Math.Pow(other.X - point.X, 2) + Math.Pow(other.Y - point.Y, 2));
        }

        public static Point ToPoint(this GIT.Placeable p)
        {
            return new Point(p.X, p.Y);
        }

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

        public static void AddRangeAndSort(this ObservableCollection<RimDataInfo> data, List<RimDataInfo> list)
        {
            list = list.ToList();
            list.AddRange(data);
            list.Sort();
            data.Clear();
            foreach (var item in list)
                data.Add(item);
        }

        public static T ToEnum<T>(this string value) where T : Enum
        {
            return (T) Enum.Parse(typeof(T), value);
        }
    }
}
