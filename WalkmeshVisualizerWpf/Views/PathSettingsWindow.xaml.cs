using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using WalkmeshVisualizerWpf.Models;

namespace WalkmeshVisualizerWpf.Views
{
    /// <summary>
    /// Interaction logic for PathSettingsWindow.xaml
    /// </summary>
    public partial class PathSettingsWindow : Window, INotifyPropertyChanged
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

        public PathSettingsWindow()
        {
            InitializeComponent();
        }

        public string Kotor1Path
        {
            get => _kotor1Path;
            set => SetField(ref _kotor1Path, value);
        }
        private string _kotor1Path;

        public List<string> Kotor1SuggestedPaths
        {
            get => _kotor1SuggestedPaths;
            set => SetField(ref _kotor1SuggestedPaths, value);
        }
        private List<string> _kotor1SuggestedPaths = ["No installations found."];

        public string Kotor2Path
        {
            get => _kotor2Path;
            set => SetField(ref _kotor2Path, value);
        }
        private string _kotor2Path;

        public List<string> Kotor2SuggestedPaths
        {
            get => _kotor2SuggestedPaths;
            set => SetField(ref _kotor2SuggestedPaths, value);
        }
        private List<string> _kotor2SuggestedPaths = ["No installations found."];

        private void ListView_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var lv = sender as ListView;
            if (lv.Tag?.ToString() == "Kotor1")
            {
                Kotor1Path = lv.SelectedItem.ToString();
            }
            else if (lv.Tag?.ToString() == "Kotor2")
            {
                Kotor2Path = lv.SelectedItem.ToString();
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void FolderButton_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Select KotOR 1 or 2 Game File",
                Filter = "Game File (swkotor(2).exe)|swkotor.exe;swkotor2.exe"
            };

            if (ofd.ShowDialog() == true)
            {
                var dir = new FileInfo(ofd.FileName).Directory;
                var exe = dir.EnumerateFiles().FirstOrDefault(fi =>
                    fi.Name.Equals("swkotor.exe", StringComparison.CurrentCultureIgnoreCase) ||
                    fi.Name.Equals("swkotor2.exe", StringComparison.CurrentCultureIgnoreCase));

                if (exe == null) return;
                if (exe.Name.Equals("swkotor.exe", StringComparison.CurrentCultureIgnoreCase))
                {
                    Kotor1Path = dir.FullName;
                }
                if (exe.Name.Equals("swkotor2.exe", StringComparison.CurrentCultureIgnoreCase))
                {
                    Kotor2Path = dir.FullName;
                }
            }
        }
    }
}
