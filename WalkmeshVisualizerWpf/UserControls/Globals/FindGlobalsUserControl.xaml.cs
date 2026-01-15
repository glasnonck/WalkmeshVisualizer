using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
using WalkmeshVisualizerWpf.Helpers;
using WalkmeshVisualizerWpf.Models;

namespace WalkmeshVisualizerWpf.UserControls.Globals
{
    /// <summary>
    /// Interaction logic for FindGlobalsUserControl.xaml
    /// </summary>
    public partial class FindGlobalsUserControl : UserControl, INotifyPropertyChanged
    {
        #region Constructors
        public FindGlobalsUserControl()
        {
            InitializeComponent();
            GM.FindListView = lvGlobalsFind;
            GM.FindGlobalsUserControl = this;
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

        private const string GLOBAL_READ_MESSAGE_DEFAULT = "Double-click a global to read / write.";
        private SortAdorner _lvGlobalsFindAdorner;
        private GridViewColumnHeader _lvGlobalsFindHeader;

        /// <summary>
        /// Indicates if the selected global in the find globals panel has changed.
        /// </summary>
        public bool GlobalReadChanged
        {
            get => _globalReadChanged;
            set => SetField(ref _globalReadChanged, value);
        }
        private bool _globalReadChanged = true;

        /// <summary>
        /// The collection of globals displayed in the find globals panel.
        /// </summary>
        public ObservableCollection<KotorGlobal> KotorFindGlobals
        {
            get => _kotorGlobals;
            set => SetField(ref _kotorGlobals, value);
        }
        private ObservableCollection<KotorGlobal> _kotorGlobals = [];

        /// <summary>
        /// The currently selected global in the find globals panel.
        /// </summary>
        public KotorGlobal GlobalsFindSelectedItem
        {
            get => _globalsFindSelectedItem;
            set => SetField(ref _globalsFindSelectedItem, value);
        }
        private KotorGlobal _globalsFindSelectedItem;

        /// <summary>
        /// The global read message displayed in the find globals panel.
        /// </summary>
        public string GlobalReadMessage
        {
            get => _globalReadMessage;
            set => SetField(ref _globalReadMessage, value);
        }
        private string _globalReadMessage = GLOBAL_READ_MESSAGE_DEFAULT;
        #endregion  // Properties



        #region Find Globals Panel Methods
        private void RemoveGlobalWatch_Click(object sender, RoutedEventArgs e)
        {
            GM.RemoveGlobalWatch((sender as Button).DataContext as KotorGlobal);
        }

        private void ClearGlobalsFindFilter_Click(object sender, RoutedEventArgs e)
        {
            txtGlobalsFindFilter.Clear();
        }

        private void TxtGlobalsFindFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(lvGlobalsFind.ItemsSource).Refresh();
        }

        private bool HandleGlobalsFindFilter(object item)
        {
            if (string.IsNullOrEmpty(txtGlobalsFindFilter.Text)) return true;
            var kg = item as KotorGlobal;
            return kg.Name.Contains(txtGlobalsFindFilter.Text, StringComparison.OrdinalIgnoreCase);
        }

        private void ReadGlobal_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalsFindSelectedItem == null)
            {
                GlobalReadMessage = GLOBAL_READ_MESSAGE_DEFAULT;
                return;
            }

            GM.ReadGlobal(GlobalsFindSelectedItem);
            GlobalReadMessage = $"[{GlobalsFindSelectedItem.LastReadAt:HH:mm:ss}] read successful";
        }

        private void WriteGlobal_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalsFindSelectedItem == null)
            {
                GlobalReadMessage = GLOBAL_READ_MESSAGE_DEFAULT;
                return;
            }
            if (GM.WriteGlobal(GlobalsFindSelectedItem, txtGlobalSetValue.Text))
            {
                // Write successful
                GlobalReadMessage = $"[{GlobalsFindSelectedItem.LastReadAt:HH:mm:ss}] value set";
            }
            else
            {
                // Write failed
                if (GlobalsFindSelectedItem.Type == KotorGlobalType.Boolean)
                    GlobalReadMessage = $"[{DateTime.Now:HH:mm:ss}] Invalid value: must be True or False";
                if (GlobalsFindSelectedItem.Type == KotorGlobalType.Number)
                    GlobalReadMessage = $"[{DateTime.Now:HH:mm:ss}] Invalid value: must be an integer between -2b and +2b";
            }
        }

        private void ToggleGlobalWatch_Click(object sender, RoutedEventArgs e)
        {
            var globalSender = (sender as Button).DataContext as KotorGlobal;

            // Sender is in selection: multi-toggle.
            if (lvGlobalsFind.SelectedItems.Contains(globalSender))
            {
                var doRemoveWatch = globalSender.IsWatched;

                foreach (KotorGlobal global in lvGlobalsFind.SelectedItems)
                {
                    // Remove all selected globals.
                    if (doRemoveWatch)
                    {
                        if (!global.IsWatched) continue;    // skip globals not watched
                        global.IsWatched = false;
                        GM.KotorWatchGlobals.Remove(global);
                    }

                    // Add all selected globals.
                    else
                    {
                        if (global.IsWatched) continue;     // skip globals watched
                        global.IsWatched = true;
                        GM.KotorWatchGlobals.Add(global);
                    }
                }
            }

            // Sender is not in selection: single toggle.
            else
            {
                if (GM.KotorWatchGlobals.Contains(globalSender))
                {
                    globalSender.IsWatched = false;
                    GM.KotorWatchGlobals.Remove(globalSender);
                }
                else
                {
                    GM.ReadGlobal(globalSender);
                    GlobalReadMessage = $"[{GlobalsFindSelectedItem.LastReadAt:HH:mm:ss}] read successful";
                    globalSender.IsWatched = true;
                    GM.KotorWatchGlobals.Add(globalSender);
                }
            }
        }

        private void LvFindGlobal_DoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void LvGlobalsFind_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GlobalReadChanged = true;
        }

        private void LvGlobalsFind_ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GlobalsManager.ColumnHeaderSort(
                sender,
                ref lvGlobalsFind,
                ref _lvGlobalsFindHeader,
                ref _lvGlobalsFindAdorner);
            GM.FindListViewHeader = _lvGlobalsFindHeader;
        }
        #endregion  // Find Globals Panel Methods
    }
}
