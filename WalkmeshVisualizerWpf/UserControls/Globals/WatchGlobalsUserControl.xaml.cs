using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using WalkmeshCompareWpf.Models;
using WalkmeshVisualizerWpf.Helpers;
using WalkmeshVisualizerWpf.Models;

namespace WalkmeshVisualizerWpf.UserControls.Globals
{
    /// <summary>
    /// Interaction logic for WatchGlobalsUserControl.xaml
    /// </summary>
    public partial class WatchGlobalsUserControl : UserControl
    {
        #region Constructors
        public WatchGlobalsUserControl()
        {
            InitializeComponent();
            GM.WatchListView = lvGlobalsWatch;
        }
        #endregion  // Constructors

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
        #endregion  // INotifyPropertyChanged Implementation

        #region Properties
        private GlobalsManager GM => GlobalsManager.Instance;
        private SortAdorner _lvGlobalsWatchAdorner;
        private GridViewColumnHeader _lvGlobalsWatchHeader;

        /// <summary>
        /// Default globals to add to the watch globals panel for KOTOR 1.
        /// </summary>
        public List<string> Kotor1DefaultWatchGlobals =
        [
            "K_STAR_MAP",
            "K_KALO_BANDON",
            "K_CURRENT_PLANET",
            "K_FUTURE_PLANET",
            "K_KOTOR_MASTER",
            "G_PazzakDeck",
        ];

        /// <summary>
        /// Default globals to add to the watch globals panel for KOTOR 2.
        /// </summary>
        public List<string> Kotor2DefaultWatchGlobals =
        [
            "K_CURRENT_PLANET",
            "K_FUTURE_PLANET",
            "003EBO_RETURN_DEST",
            "003EBO_Atton_Talk",
            "003EBO_BACKGROUND",
            "401DXN_Visited",
            "900MAL_Open",
        ];
        #endregion  // Properties


        #region Methods
        private void RemoveGlobalWatch_Click(object sender, RoutedEventArgs e)
        {
            GM.RemoveGlobalWatch((sender as Button).DataContext as KotorGlobal);
        }

        private void ClearGlobalsWatchFilter_Click(object sender, RoutedEventArgs e)
        {
            txtGlobalsWatchFilter.Clear();
        }

        private void TxtGlobalsWatchFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(lvGlobalsWatch.ItemsSource).Refresh();
        }

        private bool HandleGlobalsWatchFilter(object item)
        {
            if (string.IsNullOrEmpty(txtGlobalsWatchFilter.Text)) return true;
            var kg = item as KotorGlobal;
            return kg.Name.Contains(txtGlobalsWatchFilter.Text, StringComparison.OrdinalIgnoreCase);
        }

        private void lviGlobalWatch_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            GM.SelectAndReadGlobal((sender as ListViewItem).DataContext as KotorGlobal);
        }

        private void lviGlobalWatch_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                case Key.Space:
                    GM.SelectAndReadGlobal((sender as ListViewItem).DataContext as KotorGlobal);
                    break;
                case Key.Back:
                case Key.Delete:
                    GM.RemoveGlobalWatch((sender as ListViewItem).DataContext as KotorGlobal);
                    break;
                default:
                    return;
            }
        }

        private void LoadGlobalWatch_Click(object sender, RoutedEventArgs e)
        {
            var dlg = GlobalsManager.GetDialog_OpenGlobalsFile();
            if (dlg.ShowDialog() == false) return;  // skip if no file selected

            GM.ClearGlobalWatch();
            var globals = GlobalsManager.LoadGlobalsFile(dlg.FileName);
            if (globals == null) return;    // stop if file is empty

            foreach (var name in globals.Select(g => g.Name))
            {
                var global = GM.KotorGlobals.FirstOrDefault(g => g.Name == name);
                if (global == null) continue;
                global.IsWatched = true;
                GM.KotorWatchGlobals.Add(global);
            }
        }

        private void SaveGlobalWatch_Click(object sender, RoutedEventArgs e)
        {
            var dlg = GlobalsManager.GetDialog_SaveGlobalsFile();
            if (dlg.ShowDialog() == true)
                GlobalsManager.SaveGlobalsFile(GM.KotorWatchGlobals, dlg.FileName);
        }

        private void ClearGlobalWatch_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBoxResult.Yes == MessageBox.Show(
                "Are you sure you want to clear the global watch list?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question))
            {
                GM.ClearGlobalWatch();
            }
        }

        private void ResetGlobalWatch_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBoxResult.Yes != MessageBox.Show(
                "Are you sure you want to reset the global watch list to defaults?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question))
            {
                return;
            }

            GM.ClearGlobalWatch();

            IOrderedEnumerable<KotorGlobal> watchGlobals = null;
            if (GM.CurrentGame == K1GameData.GAME_NAME)
            {
                watchGlobals =  GM.Kotor1Globals
                    .Where(g => Kotor1DefaultWatchGlobals.Contains(g.Name))
                    .OrderBy(g => g.Name);
            }
            else if (GM.CurrentGame == K2GameData.GAME_NAME)
            {
                watchGlobals = GM.Kotor2Globals
                    .Where(g => Kotor2DefaultWatchGlobals.Contains(g.Name))
                    .OrderBy(g => g.Name);
            }

            foreach (var item in watchGlobals)
            {
                item.IsWatched = true;
                GM.KotorWatchGlobals.Add(item);
            }
        }

        private void LvGlobalsWatch_ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GlobalsManager.ColumnHeaderSort(
                sender,
                ref lvGlobalsWatch,
                ref _lvGlobalsWatchHeader,
                ref _lvGlobalsWatchAdorner);
            GM.WatchListViewHeader = _lvGlobalsWatchHeader;
        }

        private void RefreshGlobalWatch_Click(object sender, RoutedEventArgs e)
        {
            GM.RefreshGlobalWatch();
        }
        #endregion  // Methods
    }
}
