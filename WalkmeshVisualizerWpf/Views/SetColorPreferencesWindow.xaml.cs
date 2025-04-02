using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using WalkmeshVisualizerWpf.Models;

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

        public PaletteManager PM => PaletteManager.Instance;

        private Palette _initialPalette = null;
        private BackgroundColor _initialBackground = BackgroundColor.Unknown;

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

        public SetColorPreferencesWindow(Palette initialPalette, BackgroundColor initialBackground)
        {
            InitializeComponent();

            _initialPalette = initialPalette;

            _initialBackground = initialBackground;
            SelectedBackground = initialBackground;

            DataContext = this;
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = _initialPalette != PaletteManager.GetSelectedPalette()
                || _initialBackground != SelectedBackground;
        }
    }
}
