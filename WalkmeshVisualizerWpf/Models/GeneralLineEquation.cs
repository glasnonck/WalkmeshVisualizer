using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WalkmeshVisualizerWpf.Models
{
    /// <summary>
    /// Linear equation: Ax + By + C = 0
    /// </summary>
    public class GeneralLineEquation
    {
        public const double SMALL_VALUE = 0.00001;

        /// <summary>
        /// X scale
        /// </summary>
        public double A { get; set; }

        /// <summary>
        /// Y scale
        /// </summary>
        public double B { get; set; }

        /// <summary>
        /// Constant
        /// </summary>
        public double C { get; set; }

        public bool IsALine => Math.Abs(A) >= SMALL_VALUE || Math.Abs(B) >= SMALL_VALUE;
        public bool IsHorizontal => Math.Abs(A) < SMALL_VALUE && Math.Abs(B) >= SMALL_VALUE;
        public bool IsVertical => Math.Abs(B) < SMALL_VALUE && Math.Abs(A) >= SMALL_VALUE;

        public double Slope => Math.Abs(B) < SMALL_VALUE ? double.NaN : -A/B;

        public double SolveForX(double y)
        {
            // horizontal, or not a line
            if (Math.Abs(A) < SMALL_VALUE)
                return double.NaN;
            return ((-B * y) - C) / A;
        }

        public double SolveForY(double x)
        {
            // vertical, or not a line
            if (Math.Abs(B) < SMALL_VALUE)
                return double.NaN;
            return ((-A * x) - C) / B;
        }

        public double Distance(Point point)
        {
            if (!IsALine) return double.NaN;
            var numerator = Math.Abs(A * point.X + B * point.Y + C);
            var denominator = Math.Sqrt(Math.Pow(A, 2) + Math.Pow(B, 2));
            return numerator / denominator;
        }

        public Vector? NormalVector(Point p)
        {
            var m = FindNearestPoint(p);
            if (m.HasValue)
            {
                var x = p - m;
                if (x.HasValue) return x;
            }
            return null;
        }

        public Point? FindNearestPoint(Point p)
        {
            if (!IsALine) return null;
            var xNumerator = (B * ((B * p.X) - (A * p.Y))) - (A * C);
            var yNumerator = (A * ((A * p.Y) - (B * p.X))) - (B * C);
            var denominator = Math.Pow(A, 2) + Math.Pow(B, 2);
            return new Point
            {
                X = xNumerator / denominator,
                Y = yNumerator / denominator,
            };
        }

        public Point? Intersection(GeneralLineEquation other)
        {
            if (!IsALine || !other.IsALine) // some equation isn't linear
                return null;

            if ((IsHorizontal && other.IsHorizontal) || // both horizontal
                (IsVertical && other.IsVertical) ||     // both vertical
                Math.Abs((other.A / other.B) - (A / B)) < SMALL_VALUE)  // same slope
            {
                // Lines are parallel. They are either the same line or never intersect.
                return null;
            }

            if (IsHorizontal && other.IsVertical ||
                IsVertical && other.IsHorizontal)
            {
                if (IsHorizontal)
                {
                    return new Point(-other.C, -C);
                }
                else
                {
                    return new Point(-C, -other.C);
                }
            }

            double m1, m2, i1, i2;
            if (IsVertical || other.IsVertical)
            {
                m1 = -B / A;
                m2 = -other.B / other.A;
                i1 = -C / A;
                i2 = -other.C / other.A;

                var y = (i2 - i1) / (m1 - m2);
                return new Point(SolveForX(y), y);
            }

            // Solve as slope-intercept. (lines cannot be vertical)
            m1 = -A / B;
            m2 = -other.A / other.B;
            i1 = -C / B;
            i2 = -other.C / other.B;

            var x = (i2 - i1) / (m1 - m2);
            return new Point(x, SolveForY(x));
        }

        public override string ToString()
        {
            return $"{A:N5} * x + {B:N5} * y + {C:N5} = 0";
        }

        public static GeneralLineEquation FindMidline(Point p1, Point p2)
        {
            return new GeneralLineEquation
            {
                A = 2 * (p1.X - p2.X),
                B = 2 * (p1.Y - p2.Y),
                C = Math.Pow(p2.X, 2) + Math.Pow(p2.Y, 2)
                  - Math.Pow(p1.X, 2) - Math.Pow(p1.Y, 2),
            };
        }
    }
}
