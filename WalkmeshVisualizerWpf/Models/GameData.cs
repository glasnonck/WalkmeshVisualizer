using KotOR_IO;
using KotOR_IO.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WalkmeshCompareWpf.Models
{
    /// <summary>
    /// Collection of game data.
    /// </summary>
    public abstract class GameData : INotifyPropertyChanged
    {
        #region Fields

        private ObservableCollection<ModuleWalkmesh> _moduleWalkmeshes;
        private string _gameName;

        #endregion

        #region Properties

        /// <summary>
        /// Name of the game this instance represents.
        /// </summary>
        public string GameName
        {
            get => _gameName;
            protected set => SetField(ref _gameName, value);
        }

        /// <summary>
        /// Collection of the ModuleWalkmesh objects.
        /// </summary>
        public ObservableCollection<ModuleWalkmesh> ModuleWalkmeshes
        {
            get => _moduleWalkmeshes;
            set => SetField(ref _moduleWalkmeshes, value);
        }

        public KPaths Paths { get; protected set; }

        #endregion

        #region Methods

        internal void DeleteCachedWokFiles(/*BackgroundWorker bw*/)
        {
            var wokpath = Path.Combine(Environment.CurrentDirectory, GameName, "Woks");
            if (Directory.Exists(wokpath))
            {
                Directory.Delete(wokpath, true);

                //var wokDir = new DirectoryInfo(wokpath);
                //wokDir.Delete(true);

                //var totalFiles = wokDir.EnumerateFiles("*", SearchOption.AllDirectories).Count();
                //var count = 0;
                //foreach (var rimDir in wokDir.EnumerateDirectories())
                //{
                //    var woks = new List<WOK>();
                //    foreach (var file in rimDir.EnumerateFiles())
                //    {
                //        bw.ReportProgress(100 * count++ / totalFiles);
                //        file.Delete();
                //    }
                //}
            }
        }

        internal void InitializeWokData(BackgroundWorker bw)
        {
            var wokPath = Path.Combine(Environment.CurrentDirectory, GameName, "Woks");
            if (Directory.Exists(wokPath))
            {
                ReadWokFiles(wokPath, bw);
            }
            else
            {
                var key = new KEY(Paths.chitin);
                //FetchWokFiles(key);
                //GetRimData(key);
            }
        }

        private void ReadWokFiles(string wokPath, BackgroundWorker bw)
        {
            var wokDir = new DirectoryInfo(wokPath);
            var totalWoks = wokDir.EnumerateFiles("*.wok", SearchOption.AllDirectories).Count();
            var count = 0;
            foreach (var rimDir in wokDir.EnumerateDirectories())
            {
                var woks = new List<WOK>();
                foreach (var wokFile in rimDir.EnumerateFiles("*.wok"))
                {
                    bw?.ReportProgress(100 * count++ / totalWoks);
                    woks.Add(new WOK(wokFile.OpenRead())
                    {
                        RoomName = wokFile.Name.Replace(".wok", "")
                    });
                }

                var nameFile = rimDir.EnumerateFiles("*.txt").First();
                ModuleWalkmeshes.Add(new ModuleWalkmesh
                {
                    RimName = rimDir.Name,
                    CommonName = nameFile.Name.Replace(".txt", ""),
                });
                //ModuleWalkmeshes.
            }
        }

        #endregion

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
    }

    /// <summary>
    /// Collection of KotOR 1 game data.
    /// </summary>
    public class K1GameData : GameData
    {
        public const string GAME_NAME = "KotOR 1";
        public const string DEFAULT_PATH = @"C:\Program Files (x86)\Steam\steamapps\common\swkotor";

        public K1GameData(string gamePath = DEFAULT_PATH)
        {
            GameName = GAME_NAME;
            Paths = new KPaths(gamePath);
        }
    }

    /// <summary>
    /// Collection of KotOR 2 game data.
    /// </summary>
    public class K2GameData : GameData
    {
        public const string GAME_NAME = "KotOR 2";
        public const string DEFAULT_PATH = @"C:\Program Files (x86)\Steam\steamapps\common\Knights of the Old Republic II";

        public K2GameData(string gamePath = DEFAULT_PATH)
        {
            GameName = GAME_NAME;
            Paths = new KPaths(gamePath);
        }
    }
}
