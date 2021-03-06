using System;
using System.Windows.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using WalkmeshVisualizerWpf.Models;
using System.Collections.ObjectModel;
using System.Linq;

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

    public class AndBoolToVisibilityMultiConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Members
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Visibility)) throw new InvalidOperationException("The target must be of type Visibility.");
            var combined = true;
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
            if (targetType != typeof(Visibility)) throw new InvalidOperationException("The target must be of type Visibility.");
            var combined = false;
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

    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean.");
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean.");
            return !(bool)value;
        }
        #endregion
    }
}
