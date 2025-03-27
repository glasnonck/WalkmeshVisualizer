using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace WalkmeshVisualizerWpf.Views
{
    public enum BackgroundColor : int
    {
        Unknown = 0,
        White = 1,
        LightGray,
        DarkGray,
        Black,
    }

    public enum ColorTheme : int
    {
        Unknown = 0,
        Bright = 1,
        Muted,
        Rainbow,
        Spring,
        Pastel,
        Baby,
    }

    /// <summary>
    /// Interaction logic for SetColorSchemeWindow.xaml
    /// </summary>
    public partial class SetColorPreferencesWindow : Window, INotifyPropertyChanged
    {
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

        private ColorTheme _initialTheme = ColorTheme.Unknown;
        private BackgroundColor _initialBackground = BackgroundColor.Unknown;

        public ColorTheme SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                SetField(ref _selectedTheme, value);
                NotifyPropertyChanged(nameof(IsBrightThemeSelected));
                NotifyPropertyChanged(nameof(IsMutedThemeSelected));
                NotifyPropertyChanged(nameof(IsRainbowThemeSelected));
                NotifyPropertyChanged(nameof(IsSpringThemeSelected));
                NotifyPropertyChanged(nameof(IsPastelThemeSelected));
                NotifyPropertyChanged(nameof(IsBabyThemeSelected));
            }
        }
        private ColorTheme _selectedTheme = ColorTheme.Unknown;

        public bool IsBrightThemeSelected
        {
            get => SelectedTheme == ColorTheme.Bright;
            set => SelectedTheme = ColorTheme.Bright;
        }

        public bool IsMutedThemeSelected
        {
            get => SelectedTheme == ColorTheme.Muted;
            set => SelectedTheme = ColorTheme.Muted;
        }

        public bool IsRainbowThemeSelected
        {
            get => SelectedTheme == ColorTheme.Rainbow;
            set => SelectedTheme = ColorTheme.Rainbow;
        }

        public bool IsSpringThemeSelected
        {
            get => SelectedTheme == ColorTheme.Spring;
            set => SelectedTheme = ColorTheme.Spring;
        }

        public bool IsPastelThemeSelected
        {
            get => SelectedTheme == ColorTheme.Pastel;
            set => SelectedTheme = ColorTheme.Pastel;
        }

        public bool IsBabyThemeSelected
        {
            get => SelectedTheme == ColorTheme.Baby;
            set => SelectedTheme = ColorTheme.Baby;
        }

        public BackgroundColor SelectedBackground
        {
            get => _selectedBackground;
            set
            {
                SetField(ref _selectedBackground, value);
                NotifyPropertyChanged(nameof(IsWhiteBackgroundSelected));
                NotifyPropertyChanged(nameof(IsLightGrayBackgroundSelected));
                NotifyPropertyChanged(nameof(IsDarkGrayBackgroundSelected));
                NotifyPropertyChanged(nameof(IsBlackBackgroundSelected));
            }
        }
        private BackgroundColor _selectedBackground = BackgroundColor.Unknown;

        public bool IsWhiteBackgroundSelected
        {
            get => SelectedBackground == BackgroundColor.White;
            set => SelectedBackground = BackgroundColor.White;
        }

        public bool IsLightGrayBackgroundSelected
        {
            get => SelectedBackground == BackgroundColor.LightGray;
            set => SelectedBackground = BackgroundColor.LightGray;
        }

        public bool IsDarkGrayBackgroundSelected
        {
            get => SelectedBackground == BackgroundColor.DarkGray;
            set => SelectedBackground = BackgroundColor.DarkGray;
        }

        public bool IsBlackBackgroundSelected
        {
            get => SelectedBackground == BackgroundColor.Black;
            set => SelectedBackground = BackgroundColor.Black;
        }

        public SetColorPreferencesWindow(ColorTheme initialTheme, BackgroundColor initialBackground)
        {
            InitializeComponent();

            _initialTheme = initialTheme;
            SelectedTheme = initialTheme;

            _initialBackground = initialBackground;
            SelectedBackground = initialBackground;

            DataContext = this;
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = _initialTheme != SelectedTheme
                || _initialBackground != SelectedBackground;
        }
    }
}
