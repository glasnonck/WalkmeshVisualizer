using System;
using System.Collections.Generic;
using System.Linq;
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
            WorkingPalette = new Palette();
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

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!WorkingPalette.Colors.Any())
            {
                MessageBox.Show(this, "Palette must have at least one color.", "Palette Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (WorkingPalette.Colors.Any(c => string.IsNullOrWhiteSpace(c.Name)))
            {
                MessageBox.Show(this, "Palette colors must have a name.", "Palette Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
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
            WorkingPalette.Colors.Remove((sender as Button).DataContext as PaletteColor);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            WorkingPalette.Colors.Add(new PaletteColor()
            {
                Name = "Name",
                ColorText = "#000000",
            });
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Color value must be a hex value in the format \"#RRGGBB\".", "Palette Help", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
