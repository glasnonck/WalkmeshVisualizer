using System;
using System.Windows.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using WalkmeshVisualizerWpf.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

namespace WalkmeshVisualizerWpf.Helpers
{
    /// <summary>
    /// Used in MainWindow.xaml to converts a scale value to a percentage.
    /// It is used to display the 50%, 100%, etc that appears underneath the zoom and pan control.
    /// </summary>
    public class ScaleToPercentConverter : IValueConverter
    {
        /// <summary>
        /// Convert a fraction to a percentage.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Round to an integer value whilst converting.
            return (double)(int)((double)value * 100.0);
        }

        /// <summary>
        /// Convert a percentage back to a fraction.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value / 100.0;
        }
    }

    public class ScaleTextToPercentConverter : IValueConverter
    {
        /// <summary>
        /// Convert a fraction to a percentage.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Display as percentage.
            return $"{(double)value:P0}";
        }

        /// <summary>
        /// Convert a percentage back to a fraction.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Round to an integer value whilst converting.
            var str = value.ToString().Replace("%", "").Trim();
            return !double.TryParse(str, out var dbl) ? 0 : (object)(double)(int)(dbl / 100.0);
        }
    }

    public class StringEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value.ToString().ToLower() == parameter.ToString().ToLower();

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) // Note: One way by design
            => throw new NotImplementedException();
    }

    public class IntLessEqualConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value <= int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Note: One way by design
            throw new NotImplementedException();
        }
    }

    public class IntGreaterEqualConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value >= int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Note: One way by design
            throw new NotImplementedException();
        }
    }

    public class BoolMultiConverter : IMultiValueConverter
    {
        enum Operator { Unknown = 0, And = 1, Or = 2, }

        #region IMultiValueConverter Members
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool)) throw new InvalidOperationException("The target must be of type bool.");
            foreach (var value in values) if (!bool.TryParse(value.ToString(), out _)) return false;

            var op = (Operator)Enum.Parse(typeof(Operator), parameter.ToString());
            if (op == Operator.And)
            {
                var combined = true;
                foreach (var value in values) combined &= (bool)value;
                return combined;
            }
            if (op == Operator.Or)
            {
                var combined = false;
                foreach (var value in values) combined |= (bool)value;
                return combined;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

    public class AndBoolToVisibilityMultiConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Members
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var combined = true;
            foreach (var value in values) if (!bool.TryParse(value.ToString(), out _)) return false;
            foreach (var value in values) combined &= (bool)value;
            return combined ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

    public class OrInverseBoolToVisibilityMultiConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Members
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var combined = false;
            foreach (var value in values) if (!bool.TryParse(value.ToString(), out _)) return false;
            foreach (var value in values) combined |= (bool)value;
            return combined ? Visibility.Collapsed : Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

    public class MatchRectFillMultiConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Members
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return Brushes.Transparent;

            var walk = values[0] as WalkabilityModel;
            var onrims = values[1] as ObservableCollection<RimModel>;

            if (walk == null || onrims == null) return Brushes.Transparent;

            return onrims.Any(r => r.FileName == walk.Rim.FileName) ? walk.Rim.MeshColor : Brushes.Transparent;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

    public class PointToTextConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Point p ? $"({p.X:N2}, {p.Y:N2})" : throw new InvalidOperationException("The value must be of type Point.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Visibility)) throw new InvalidOperationException("The target must be of type Visibility.");

            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool)) throw new InvalidOperationException("The target must be a boolean.");

            return (Visibility)value == Visibility.Visible;
        }
        #endregion
    }

    [ValueConversion(typeof(Visibility), typeof(bool))]
    public class VisibilityToBoolConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (Visibility)value == Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }
        #endregion
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Visibility)) throw new InvalidOperationException("The target must be of type Visibility.");

            return (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool)) throw new InvalidOperationException("The target must be a boolean.");

            return (Visibility)value == Visibility.Collapsed;
        }
        #endregion
    }

    [ValueConversion(typeof(RimDataInfo), typeof(bool))]
    public class AnyHiddenRdiConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value as IEnumerable<RimDataInfo>)?.Any(rdi => !rdi.MeshVisible) ?? false;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    [ValueConversion(typeof(RimDataInfo), typeof(bool))]
    public class AnyVisibleRdiConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value as IEnumerable<RimDataInfo>)?.Any(rdi => rdi.MeshVisible) ?? false;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }
        #endregion
    }

    [ValueConversion(typeof(int), typeof(Visibility))]
    public class IsNonZeroToVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Visibility)) throw new InvalidOperationException("The target must be of type Visibility.");

            return (int)value != 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(int)) throw new InvalidOperationException("The target must be a int.");

            return (Visibility)value == Visibility.Visible ? 1 : 0;
        }
        #endregion
    }

    [ValueConversion(typeof(int), typeof(Visibility))]
    public class IsZeroToVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Visibility)) throw new InvalidOperationException("The target must be of type Visibility.");

            return (int)value == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(int)) throw new InvalidOperationException("The target must be a int.");

            return (Visibility)value == Visibility.Visible ? 0 : 1;
        }
        #endregion
    }

    [ValueConversion(typeof(Brush), typeof(bool))]
    public class IsNotTransparentConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool)) throw new InvalidOperationException("The target must be of type bool.");

            return (Brush)value != Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Brush)) throw new InvalidOperationException("The target must be a Brush.");

            return (bool)value ? Brushes.Black : Brushes.Transparent;
        }
        #endregion
    }
}
