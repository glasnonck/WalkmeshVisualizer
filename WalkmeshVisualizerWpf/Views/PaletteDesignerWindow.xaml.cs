using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WalkmeshVisualizerWpf.Models;

namespace WalkmeshVisualizerWpf.Views
{
    /// <summary>
    /// Interaction logic for PaletteDesignerWindow.xaml
    /// </summary>
    public partial class PaletteDesignerWindow : Window
    {
        public Palette TargetPalette { get; set; }
        public Palette WorkingPalette { get; set; }

        public PaletteDesignerWindow()
        {
            InitializeComponent();
            WorkingPalette = new Palette()
            {
                FileName = "Palette",
                Name = "Palette",
                Colors = new System.Collections.ObjectModel.ObservableCollection<PaletteColor>
                {
                    new PaletteColor
                    {
                        Name = "Color",
                        ColorText = "#000000",
                    }
                }
            };
            DataContext = WorkingPalette;
        }

        public PaletteDesignerWindow(Palette palette)
        {
            InitializeComponent();
            TargetPalette = palette;
            WorkingPalette = new Palette(palette);
            DataContext = WorkingPalette;
            tbxFileName.IsEnabled = false;
            lblJsonExtension.Visibility = Visibility.Collapsed;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (TargetPalette == null)
            {
                tbxFileName.Focus();
                tbxFileName.SelectAll();
            }
            else
            {
                tbxDisplayName.Focus();
                tbxDisplayName.SelectAll();
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(WorkingPalette.Name))
            {
                MessageBox.Show(this, "Palette must have a name.", "Palette Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!WorkingPalette.Colors.Any())
            {
                MessageBox.Show(this, "Palette must have at least one color.", "Palette Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AddColor_Executed(this, null);
                return;
            }

            if (WorkingPalette.Colors.Any(c => string.IsNullOrWhiteSpace(c.Name)))
            {
                MessageBox.Show(this, "Palette colors must have a name.", "Palette Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Only need to verify filename if creating a new Palette.
            if (TargetPalette == null)
            {
                var invalidChars = System.IO.Path.GetInvalidFileNameChars();
                var filtered = string.Join(null, WorkingPalette.FileName.Where(c => !invalidChars.Contains(c)));
                if (filtered.Length > 5 && filtered.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
                    filtered = filtered.Substring(0, filtered.Length - 5);

                if (string.IsNullOrEmpty(filtered))
                {
                    MessageBox.Show(this, $"Filename cannot be blank or contain only invalid characters: \" < > | : * ? \\ /");
                    return;
                }

                WorkingPalette.FileName = filtered;
            }

            DialogResult = true;
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            var colorToMove = (sender as Button).DataContext as PaletteColor;
            if (colorToMove == WorkingPalette.Colors.First()) return;

            var index = WorkingPalette.Colors.IndexOf(colorToMove);
            WorkingPalette.Colors.RemoveAt(index);
            WorkingPalette.Colors.Insert(index - 1, colorToMove);
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            var colorToMove = (sender as Button).DataContext as PaletteColor;
            if (colorToMove == WorkingPalette.Colors.Last()) return;

            var index = WorkingPalette.Colors.IndexOf(colorToMove);
            WorkingPalette.Colors.RemoveAt(index);
            WorkingPalette.Colors.Insert(index + 1, colorToMove);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBoxResult.Yes == MessageBox.Show(
                "Do you want to delete this Palette Color?",
                "Delete Confirmation",
                MessageBoxButton.YesNo))
            {
                WorkingPalette.Colors.Remove((sender as Button).DataContext as PaletteColor);
            }
        }

        private void AddColor_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            WorkingPalette.Colors.Add(new PaletteColor()
            {
                Name = "Color",
                ColorText = "#000000",
            });
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Filename cannot contain invalid characters: \" < > | : * ? \\ /\r\n" +
                "Name and Color name cannot be empty.\r\n" +
                "Color value must be a hex value in the format \"#RRGGBB\".",
                "Palette Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
