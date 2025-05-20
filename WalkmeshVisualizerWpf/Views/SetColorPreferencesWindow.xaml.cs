using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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

        public bool IsErrorPanelExpanded
        {
            get => _isErrorPanelExpanded;
            set => SetField(ref _isErrorPanelExpanded, value);
        }
        private bool _isErrorPanelExpanded = false;

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

            RefreshPalettes();
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = _initialPalette != PaletteManager.GetSelectedPalette()
                || _initialBackground != SelectedBackground;
        }

        private void OpenPalettesFolder_Click(object sender, RoutedEventArgs e)
            => PaletteManager.ShowPalettesDirectory();

        private void AddPalette_Click(object sender, RoutedEventArgs e)
        {
            var pdws = OwnedWindows.OfType<PaletteDesignerWindow>();
            if (pdws.Any()) pdws.First().Show();
            else
            {
                var pdw = new PaletteDesignerWindow()
                {
                    Owner = this,
                };

                // Show dialog. If "Ok" is selected...
                if (pdw.ShowDialog() ?? false)
                {
                    // verify valid filename
                    var wp = pdw.WorkingPalette;
                    var fn = wp.FileName;
                    if (fn.EndsWith(".json"))
                        fn = wp.FileName.Substring(0, wp.FileName.Length - 5);
                    var suffix = "";
                    var index = 0;

                    // find a valid, unused filename
                    while (File.Exists(Path.Combine(PaletteManager.PALETTE_DIRECTORY, $"{fn}{suffix}.json")))
                    {
                        index++;
                        suffix = $" ({index})";
                    }
                    pdw.WorkingPalette.FileName = $"{fn}{suffix}.json";

                    // save new WorkingPalette to file
                    wp.WriteToFile();

                    // add WorkingPalette to PaletteManager
                    PM.Palettes.Add(wp);
                }
                // If "Cancel is selected, do nothing.
                else { }
            }
        }

        private void EditPalette_Click(object sender, RoutedEventArgs e)
        {
            var pdws = OwnedWindows.OfType<PaletteDesignerWindow>();
            if (pdws.Any()) pdws.First().Show();
            else
            {
                var pdw = new PaletteDesignerWindow(PaletteManager.GetSelectedPalette())
                {
                    Owner = this,
                };

                // Show dialog. If "Ok" is selected...
                if (pdw.ShowDialog() ?? false)
                {
                    // replace TargetPalette with WorkingPalette
                    PM.Palettes[PM.Palettes.IndexOf(pdw.TargetPalette)] = pdw.WorkingPalette;

                    // save new TargetPalette to file
                    pdw.TargetPalette.WriteToFile();
                }
                // If "Cancel is selected, do nothing.
                else { }
            }
        }

        private void ReloadPalettesFolder_Click(object sender, RoutedEventArgs e)
        {
            RefreshPalettes();
        }

        private void RefreshPalettes()
        {
            var selectedFileName = PaletteManager.GetSelectedPalette().FileName;
            PM.RefreshPalettes();
            var selectedPalette = PM.Palettes.FirstOrDefault(p => p.FileName == selectedFileName);
            if (selectedPalette == null)
            {
                if (PaletteManager.GetSelectedPalette() == null)
                    PM.Palettes.First().IsSelected = true;
            }
            else selectedPalette.IsSelected = true;
        }
    }
}
