using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WalkmeshCompareWpf.Models;
using WalkmeshVisualizerWpf.Models;
using WalkmeshVisualizerWpf.UserControls.Globals;
using KMIAdapter = KotorMessageInjector.Adapter;
using KMIHelpers = KotorMessageInjector.KotorHelpers;

namespace WalkmeshVisualizerWpf.Helpers
{
    internal class GlobalsManager : INotifyPropertyChanged
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

        #endregion // INotifyPropertyChanged Implementation



        #region Singleton
        private readonly static GlobalsManager _instance = new();

        static GlobalsManager()
        {
        }

        private GlobalsManager()
        {
        }

        public static GlobalsManager Instance
        {
            get
            {
                return _instance;
            }
        }
        #endregion  // Singleton



        #region Manager Properties
        internal readonly List<string> _popupDisplayLines = [];
        internal readonly Logger GlobalsLogger = new("Globals");
        public const string K1_GLOBALS_LOG_FILE_PREFIX = "K1_Globals";
        public const string K2_GLOBALS_LOG_FILE_PREFIX = "K2_Globals";
        internal List<KotorGlobal> Kotor1Globals = [];
        internal List<KotorGlobal> Kotor2Globals = [];
        public List<KotorGlobal> KotorGlobals = [];

        /// <summary>
        /// Automatically read the values of all globals in the watch globals panel.
        /// </summary>
        public bool DoGlobalWatchAutoRefresh
        {
            get => _doGlobalAutoRefresh;
            set => SetField(ref _doGlobalAutoRefresh, value);
        }
        private bool _doGlobalAutoRefresh = false;

        /// <summary>
        /// Create in-game popup notification for all changed globals in the watch globals panel.
        /// </summary>
        public bool DoPopupAllGlobalWatch
        {
            get => _doPopupAllGlobalWatch;
            set
            {
                SetField(ref _doPopupAllGlobalWatch, value);
                _popupDisplayLines.Clear();
            }
        }
        private bool _doPopupAllGlobalWatch = false;

        /// <summary>
        /// Log changes to all globals in the watch globals panel to a file.
        /// </summary>
        public bool DoLogChangedGlobals
        {
            get => _doLogChangedGlobals;
            set
            {
                GlobalsLogger.NewLogFile();
                SetField(ref _doLogChangedGlobals, value);
            }
        }
        private bool _doLogChangedGlobals = false;

        /// <summary>
        /// The collection of globals displayed in the watch globals panel.
        /// </summary>
        public ObservableCollection<KotorGlobal> KotorWatchGlobals
        {
            get => _kotorWatchGlobals;
            set => SetField(ref _kotorWatchGlobals, value);
        }
        private ObservableCollection<KotorGlobal> _kotorWatchGlobals = [];

        /// <summary>
        /// Automatically read the value of the selected global in the find globals panel.
        /// </summary>
        public bool DoGlobalReadAutoRefresh
        {
            get => _doGlobalReadAutoRefresh;
            set => SetField(ref _doGlobalReadAutoRefresh, value);
        }
        private bool _doGlobalReadAutoRefresh = false;

        public string CurrentGame
        {
            get => _currentGame;
            set => SetField(ref _currentGame, value);
        }
        private string _currentGame = "";

        /// <summary>
        /// Refresh rate in milliseconds for auto-refreshing values in the watch and global panels.
        /// </summary>
        public int GlobalAutoRefreshRate
        {
            get => _globalAutoRefreshRate;
            set => SetField(ref _globalAutoRefreshRate, value);
        }

        public string CurrentLiveModuleName { get; set; }
        public Point LivePositionPoint { get; set; }
        public string CoordinateSeparator { get; set; }

        private int _globalAutoRefreshRate = 5000;
        internal ListView FindListView;
        internal ListView WatchListView;
        internal GridViewColumnHeader FindListViewHeader;
        internal GridViewColumnHeader WatchListViewHeader;
        internal FindGlobalsUserControl FindGlobalsUserControl;
        #endregion  // Manager Properties

        #region Both Find and Watch Globals Panel Methods
        internal void RemoveGlobalWatch(KotorGlobal global)
        {
            var idx = KotorWatchGlobals.IndexOf(global);
            KotorWatchGlobals.RemoveAt(idx);
            global.IsWatched = false;
            if (WatchListView != null)
                WatchListView.SelectedItem = null;
        }

        internal void RefreshSort_Globals()
        {
            var tag = FindListViewHeader?.Tag.ToString();
            if (tag != null && tag != "Name" && tag != "Type")
                CollectionViewSource.GetDefaultView(FindListView.ItemsSource).Refresh();

            tag = WatchListViewHeader?.Tag.ToString();
            if (tag != null && tag != "Name" && tag != "Type")
                CollectionViewSource.GetDefaultView(WatchListView.ItemsSource).Refresh();
        }

        // Not for both, per se, but it connects the watch panel to the find panel.
        internal void SelectAndReadGlobal(KotorGlobal global)
        {
            //if (!ShowLivePosition) return;
            FindGlobalsUserControl.GlobalsFindSelectedItem = global;
            FindListView.ScrollIntoView(global);
            if (!DoGlobalReadAutoRefresh) ReadGlobal(global);
        }

        public static void ColumnHeaderSort(object sender, ref ListView lv, ref GridViewColumnHeader header, ref SortAdorner adorner)
        {
            var column = sender as GridViewColumnHeader;
            var sortby = column.Tag.ToString();
            SortAdorner.SortColumn(lv, ref header, ref adorner, column, sortby);
        }

        #endregion  // Both Find and Watch Globals Panel Methods

        #region Stays in Visualizer Window
        //private void GlobalWatchRadioButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (sender is not MenuItem mi) return;
        //    GlobalAutoRefreshRate = int.Parse(mi.Tag.ToString(), CultureInfo.InvariantCulture);
        //}
        #endregion  // Stays in Visualizer Window

        #region Globals Manager Methods
        internal static void RefreshSort(ListView lv, GridViewColumnHeader gvch)
        {
            var tag = gvch?.Tag.ToString();
            if (tag != null && tag != "Name" && tag != "Type")
                CollectionViewSource.GetDefaultView(lv.ItemsSource).Refresh();
        }

        internal void LogChangedGlobals(IEnumerable<KotorGlobal> changedGlobals)
        {
            if (!changedGlobals.Any()) return;
            GlobalsLogger.LogLine($"Module: {CurrentLiveModuleName}");
            GlobalsLogger.LogLine($"Position: {LivePositionPoint.X}{CoordinateSeparator}{LivePositionPoint.Y}");
            GlobalsLogger.LogLines(
                changedGlobals
                    .OrderBy(g => g.LastChangeAt)
                    .Select(g => $"{g.LastChangeAt:yyyy-MM-dd HH:mm:ss} - {g.Name}: {g.LastValue} -> {g.Value}")
                , false);
        }

        internal void PopupChangedGlobals()
        {
            if (_popupDisplayLines.Count == 0) return;

            // TODO: Consider limiting displaying to only 512 characters at a time.
            // TODO: Is there a way to check if there is a popup already open?

            if (ShowCustomMessageBox("Globals have changed!\n-------------------------------------\n" + string.Join("\n", _popupDisplayLines)))
            {
                _popupDisplayLines.Clear();
            }
        }

        private bool ShowCustomMessageBox(string message, bool showCancel = false)
        {
            var km = GetKotorManager();
            if (km == null) return false;
            if (KMIHelpers.getGuiManager(km.pr.h) == 0u) return false; // GUI not initialized
            KMIAdapter.CreatePopUp(km.pr.h, message, showCancel, 0u);
            return true;
        }

        internal void QueueChangedGlobalsForPopup(IEnumerable<KotorGlobal> changedGlobals)
        {
            //if (!changedGlobals.Any()) return;
            _popupDisplayLines.AddRange(changedGlobals.Select(g => $"({g.LastChangeAt:HH:mm:ss}) {g.Name}: {g.LastValue} -> {g.Value}"));
            //ShowCustomMessageBox(
            //    "Globals have changed!\n"
            //    +"-------------------------------------\n"
            //    + string.Join("\n", _popupDisplayLines));
        }

        private int GetRunningKotor()
        {
            if (Process.GetProcessesByName("swkotor").FirstOrDefault() is not null)
            {
                CurrentGame = K1GameData.GAME_NAME;
                return 1;
            }

            if (Process.GetProcessesByName("swkotor2").FirstOrDefault() is not null)
            {
                CurrentGame = K2GameData.GAME_NAME;
                return 2;
            }

            CurrentGame = "Unknown";
            return 0;

            //while (IsBusy) { Thread.Sleep(10); }

            //var version = 0;
            //var gameName = string.Empty;
            //var otherGameName = string.Empty;
            //XmlGame xmlGame = null;
            //var process = Process.GetProcessesByName("swkotor").FirstOrDefault() ??
            //              Process.GetProcessesByName("swkotor2").FirstOrDefault();
            //if (process == null) return version;

            //if (process.ProcessName == "swkotor")
            //{
            //    version = 1;
            //    CurrentGame = K1GameData.GAME_NAME;
            //    //otherGameName = K2GameData.GAME_NAME;
            //    //xmlGame = XmlGameData.Kotor1Data;
            //}

            //if (process.ProcessName == "swkotor2")
            //{
            //    version = 2;
            //    CurrentGame = K2GameData.GAME_NAME;
            //    //otherGameName = K1GameData.GAME_NAME;
            //    //xmlGame = XmlGameData.Kotor2Data;
            //}

            //if (HotswapToLiveGame)
            //{
            //    // Close other game if needed.
            //    if (Game == otherGameName)
            //    {
            //        Application.Current.Dispatcher.Invoke(() => SwapGame_Executed(null, null));
            //        while (IsBusy) { Thread.Sleep(10); }
            //    }

            //    // Load live game
            //    if (Game != gameName)
            //    {
            //        Application.Current.Dispatcher.Invoke(() =>
            //        {
            //            var exeFile = new FileInfo(process.MainModule.FileName);
            //            CurrentGame = xmlGame;
            //            RimDataSet.LoadGameData(exeFile.DirectoryName);
            //            LoadGameFiles(exeFile.DirectoryName, gameName);
            //        });
            //        while (IsBusy) { Thread.Sleep(10); }
            //    }
            //}

            //return version;
        }

        private KotorManager GetKotorManager()
        {
            KotorManager km = null;
            try
            {
                km = new KotorManager(GetRunningKotor());
                if (!km.TestRead() || !km.SetLoadDirection()) km = null;
            }
            catch (Exception) { }
            return km;
        }

        internal void ReadGlobal(KotorGlobal global, KotorManager km = null, bool refreshSort = true)
        {
            var kmExternal = km != null;
            if (km == null)
            {
                km = GetKotorManager();
                if (km == null) return;
            }

            global.LastValue = global.Value;
            global.LastReadAt = DateTime.Now;

            if (global.Type == KotorGlobalType.Boolean)
                global.Value = KMIAdapter.GetGlobalBoolean(km.pr.h, global.Name)?.ToString() ?? "N/A";
            
            if (global.Type == KotorGlobalType.Number)
                global.Value = KMIAdapter.GetGlobalNumber(km.pr.h, global.Name).ToString();

            if (global.LastValue != global.Value)
                global.LastChangeAt = global.LastReadAt;

            if (kmExternal) km.RefreshAddresses();
            if (refreshSort) RefreshSort_Globals();
        }

        internal bool WriteGlobal(KotorGlobal global, string valueText, KotorManager km = null, bool refreshSort = true)
        {
            var valueSet = false;
            var value = string.Empty;
            var kmExternal = km != null;
            if (km == null)
            {
                km = GetKotorManager();
                if (km == null) return false;
            }

            if (global.Type == KotorGlobalType.Boolean)
            {
                if (bool.TryParse(valueText, out var boolValue))
                {
                    KMIAdapter.SetGlobalBoolean(km.pr.h, global.Name, boolValue);
                    value = boolValue.ToString();
                    valueSet = true;
                }
                else return false;
            }

            if (global.Type == KotorGlobalType.Number)
            {
                if (int.TryParse(global.Value, out var intValue))
                {
                    KMIAdapter.SetGlobalNumber(km.pr.h, global.Name, intValue);
                    value = intValue.ToString();
                    valueSet = true;
                }
                else return false;
            }

            if (valueSet)
            {
                global.LastValue = global.Value;
                global.Value = value;
                global.LastReadAt = DateTime.Now;
                if (global.LastValue != global.Value)
                    global.LastChangeAt = global.LastReadAt;
                if (refreshSort) RefreshSort_Globals();
            }

            if (kmExternal) km.RefreshAddresses();

            return valueSet;
        }

        internal static OpenFileDialog GetDialog_OpenGlobalsFile()
        {
            return new OpenFileDialog()
            {
                Title = "Open Globals Watch File",
                DefaultExt = ".json",
                Filter = "JSON Documents (.json)|*.json",
            };
        }

        internal static IEnumerable<KotorGlobal> LoadGlobalsFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            return JsonConvert.DeserializeObject<IEnumerable<KotorGlobal>>(File.ReadAllText(path));
        }

        internal static SaveFileDialog GetDialog_SaveGlobalsFile()
        {
            return new SaveFileDialog()
            {
                Title = "Save Globals Watch File",
                FileName = "watch",
                DefaultExt = ".json",
                Filter = "JSON Documents (.json)|*.json",
            };
        }

        internal static void SaveGlobalsFile(IEnumerable<KotorGlobal> globals, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            var text = JsonConvert.SerializeObject(globals);
            var di = new FileInfo(path).Directory;
            if (!di.Exists) di.Create();
            File.WriteAllText(path, text);
        }

        internal void ClearGlobalWatch()
        {
            foreach (var item in KotorWatchGlobals)
                item.IsWatched = false;
            KotorWatchGlobals.Clear();
        }

        internal bool RefreshGlobalWatch()
        {
            var km = GetKotorManager();
            if (km == null) return false;

            var globals = KotorWatchGlobals.ToList();
            if (KMIHelpers.getServer(km.pr.h) == 0) return false;

            foreach (var global in globals)
                ReadGlobal(global, km, false);

            QueueChangedGlobalsForPopup(KotorWatchGlobals.Where(g => (DoPopupAllGlobalWatch || g.DoPopupOnChange) && g.HasChanged));
            if (DoLogChangedGlobals)
                LogChangedGlobals(KotorWatchGlobals.Where(g => g.HasChanged));

            RefreshSort_Globals();
            return true;
        }

        #endregion  // Globals Manager Methods
    }
}
