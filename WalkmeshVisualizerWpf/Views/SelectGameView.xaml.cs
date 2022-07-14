using KotOR_IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using WalkmeshVisualizerWpf.Helpers;
using WalkmeshVisualizerWpf.Models;

namespace WalkmeshVisualizerWpf.Views
{
    /// <summary>
    /// Interaction logic for SelectGameView.xaml
    /// </summary>
    public partial class SelectGameView : UserControl
        //, INotifyPropertyChanged
    {
        #region Constants

        private const string DEFAULT = "N/A";
        private const string LOADING = "Loading";

        #endregion // Constants

        #region Constructor

        public SelectGameView()
        {
            InitializeComponent();
            DataContext = this;

            SelectGameWorker.WorkerReportsProgress = true;
            SelectGameWorker.DoWork += SelectGameWorker_DoWork;
            SelectGameWorker.ProgressChanged += SelectGameWorker_ProgressChanged;
            SelectGameWorker.RunWorkerCompleted += SelectGameWorker_RunWorkerCompleted;

            //LoadGame = new DelegatedCommand<object>();
            
        }

        #endregion // Constructor

        #region Properties

        /// <summary>
        /// Background worker used to load game data.
        /// </summary>
        private BackgroundWorker SelectGameWorker { get; set; } = new BackgroundWorker();

        /// <summary>
        /// Data of the currently selected game.
        /// </summary>
        public KotorDataModel CurrentGameData
        {
            get => (KotorDataModel)GetValue(CurrentGameDataProperty);
            set => SetValue(CurrentGameDataProperty, value);
        }

        /// <summary>
        /// Xml information of the currently selected game.
        /// </summary>
        public XmlGame CurrentGameXml
        {
            get => (XmlGame)GetValue(CurrentGameXmlProperty);
            set => SetValue(CurrentGameXmlProperty, value);
        }

        /// <summary>
        /// Current <see cref="BackgroundWorker"/> progress.
        /// </summary>
        public double CurrentProgress
        {
            get => (double)GetValue(CurrentProgressProperty);
            set => SetValue(CurrentProgressProperty, value);
        }

        /// <summary>
        /// Is the <see cref="BackgroundWorker"/> currently busy?
        /// </summary>
        public bool IsBusy
        {
            get => (bool)GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        /// <summary>
        /// Currently selected <see cref="SupportedGame"/>.
        /// </summary>
        public SupportedGame SelectedGame
        {
            get => (SupportedGame)GetValue(SelectedGameProperty);
            set => SetValue(SelectedGameProperty, value);
        }

        /// <summary>
        /// Name of the currently selected <see cref="SupportedGame"/> or a description of the current busy action.
        /// </summary>
        public string SelectedLabel
        {
            get => (string)GetValue(SelectedLabelProperty);
            set => SetValue(SelectedLabelProperty, value);
        }

        public ICommand LoadGame
        {
            get => (ICommand)GetValue(LoadGameProperty);
            set => SetValue(LoadGameProperty, value);
        }

        public static readonly DependencyProperty LoadGameProperty = DependencyProperty.Register(nameof(LoadGame), typeof(ICommand), typeof(SelectGameView), new PropertyMetadata(null));

        #endregion // Properties

        #region Dependency Property Definitions

        public static readonly DependencyProperty CurrentGameDataProperty = DependencyProperty.Register(nameof(CurrentGameData), typeof(KotorDataModel), typeof(SelectGameView), new PropertyMetadata(null));
        public static readonly DependencyProperty CurrentGameXmlProperty = DependencyProperty.Register(nameof(CurrentGameXml), typeof(XmlGame), typeof(SelectGameView), new PropertyMetadata(null));
        public static readonly DependencyProperty CurrentProgressProperty = DependencyProperty.Register(nameof(CurrentProgress), typeof(double), typeof(SelectGameView), new PropertyMetadata(0d));
        public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(nameof(IsBusy), typeof(bool), typeof(SelectGameView), new PropertyMetadata(false));
        public static readonly DependencyProperty SelectedGameProperty = DependencyProperty.Register(nameof(SelectedGame), typeof(SupportedGame), typeof(SelectGameView), new PropertyMetadata(SupportedGame.NotSupported));
        public static readonly DependencyProperty SelectedLabelProperty = DependencyProperty.Register(nameof(SelectedLabel), typeof(string), typeof(SelectGameView), new PropertyMetadata(DEFAULT));

        #endregion // Dependency Property Definitions

        #region Event Handlers

        /// <summary>
        /// Performs steps to load required game data.
        /// </summary>
        private void SelectGameWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var gsa = (GameSelectArgs)e.Argument;

            // If path is null, load data from cache. Otherwise, load from that path.
            CurrentGameData = string.IsNullOrEmpty(gsa.Path)
                ? KotorDataFactory.GetKotorDataByGame(gsa.Game, report: SelectGameWorker.ReportProgress)
                : KotorDataFactory.GetKotorDataByPath(gsa.Path, report: SelectGameWorker.ReportProgress);

            // Set label.
            SelectedLabel = CurrentGameData.Game.ToDescription();
        }

        /// <summary>
        /// Report current progress of background worker.
        /// </summary>
        private void SelectGameWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            CurrentProgress = e.ProgressPercentage;
        }

        /// <summary>
        /// Finalize background worker operations.
        /// </summary>
        private void SelectGameWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                SelectedGame = SupportedGame.NotSupported;
                SelectedLabel = DEFAULT;
                var sb = new StringBuilder();
                _ = sb.AppendLine("An unexpected error occurred while loading game data.")
                      .AppendLine($"-- {e.Error.Message}");
                if (e.Error.InnerException != null)
                    _ = sb.AppendLine($"-- {e.Error.InnerException.Message}");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _ = MessageBox.Show(sb.ToString(), "Loading Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }

            CurrentProgress = 0;
            IsBusy = false;
        }

        /// <summary>
        /// Request game path from user and load game files.
        /// </summary>
        private void LoadGame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is SupportedGame game)
            {
                // Create OpenFileDialog for this game.
                OpenFileDialog ofd;
                switch (game)
                {
                    case SupportedGame.Kotor1:
                        ofd = new OpenFileDialog
                        {
                            Title = "Select KotOR 1 Executable File",
                            Filter = "Exe File (swkotor.exe)|swkotor.exe",
                            InitialDirectory = KotorDataFactory.K1_DEFAULT_PATH,
                            FileName = KotorDataFactory.K1_EXE_NAME,
                            CheckFileExists = true,
                        };
                        break;
                    case SupportedGame.Kotor2:
                        ofd = new OpenFileDialog
                        {
                            Title = "Select KotOR 2 Executable File",
                            Filter = "Exe File (swkotor2.exe)|swkotor2.exe",
                            InitialDirectory = KotorDataFactory.K2_DEFAULT_PATH,
                            FileName = KotorDataFactory.K2_EXE_NAME,
                            CheckFileExists = true,
                        };
                        break;
                    case SupportedGame.NotSupported:
                    default:
                        throw new InvalidEnumArgumentException($"Event parameter '{game}' is not supported.");
                }

                // Show dialog and load selected game.
                if (ofd?.ShowDialog() == true)
                {
                    LoadSupportedGame(game, new FileInfo(ofd.FileName).DirectoryName);
                }
            }
            else
            {
                throw new ArgumentException($"Invalid event parameter: {e.Parameter}.");
            }
        }

        /// <summary>
        /// Load game data for a <see cref="SupportedGame"/>. If directory is null, game will be loaded from cache.
        /// </summary>
        /// <param name="game">Game to load.</param>
        /// <param name="directory">Path to game files. If null, cache will be used.</param>
        private void LoadSupportedGame(SupportedGame game, string directory = null)
        {
            CurrentGameXml = XmlGameData.GetKotorXml(game);
            LoadGameFiles(new GameSelectArgs { Game = game, Path = directory, });
        }

        /// <summary>
        /// Load game files based on the given game directory.
        /// </summary>
        /// <param name="path">Directory full path. Null if loading from cache.</param>
        /// <param name="game">Name of the selected game.</param>
        private void LoadGameFiles(GameSelectArgs gsa)
        {
            SetGameSelectVisibility(false);

            // Initialize game path and set game as Loading.
            SelectedGame = gsa.Game;
            SelectedLabel = LOADING;

            //// If any walkmeshes are active, remove them.
            //if (OnRims.Any()) RemoveAll_Executed(this, null);

            // Clear the cache before loading game files.
            IsBusy = true;
            SelectGameWorker.RunWorkerAsync(gsa);
        }

        /// <summary>
        /// If true, show Game Select panel and hide Selected Game / Rim Select panels.
        /// If false, hide Game Select panel and show Selected Game / Rim Select panels.
        /// </summary>
        /// <param name="isVisible">Should the Game Select panel be visible?</param>
        private void SetGameSelectVisibility(bool isVisible)
        {
            pnlGameSelect.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            pnlSelectedGame.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
            //pnlRimSelect.Visibility = isVisible ? Visibility.Hidden : Visibility.Visible;
        }

        /// <summary>
        /// Game files can be loaded if the application is not busy and the game files aren't already cached.
        /// </summary>
        private void LoadGame_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is SupportedGame game && !KotorDataFactory.IsGameCached(game);
        }

        /// <summary>
        /// Load game files from cache.
        /// </summary>
        private void LoadCache_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is SupportedGame game)
            {
                LoadSupportedGame(game);
            }
            else
            {
                // Throw exception?
            }
        }

        /// <summary>
        /// Game cache can be loaded if the application is not busy and the game files are cached.
        /// </summary>
        private void LoadCache_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is SupportedGame game && KotorDataFactory.IsGameCached(game);
        }

        /// <summary>
        /// Clear the requested cache of game files.
        /// </summary>
        private void ClearCache_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is SupportedGame game)
            {
                switch (game)
                {
                    case SupportedGame.Kotor1:
                    case SupportedGame.Kotor2:
                        KotorDataFactory.DeleteCachedGameData(game);
                        break;
                    case SupportedGame.NotSupported:
                    default:
                        // Throw exception?
                        break;
                }
            }
            else
            {
                // Throw exception?
            }
        }

        /// <summary>
        /// Can clear cache if the cache exists and the application is not busy.
        /// </summary>
        private void ClearCache_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is SupportedGame game && KotorDataFactory.IsGameCached(game);
        }

        /// <summary>
        /// Perform steps to allow the user to swap to a different game.
        /// </summary>
        private void SwapGame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //// If any walkmeshes are active, remove them.
            //if (OnRims.Any()) RemoveAll_Executed(this, null);

            // Reset values.
            //OffRims.Clear();
            SelectedGame = SupportedGame.NotSupported;
            SelectedLabel = DEFAULT;

            //// Reset coordinate matches.
            //HideBothPoints();
            //ClearBothPointMatches();

            SetGameSelectVisibility(true);
        }

        /// <summary>
        /// Swap Game can execute if the Selected Game panel is visible (i.e., a game is selected).
        /// </summary>
        private void SwapGame_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsBusy && pnlSelectedGame.Visibility == Visibility.Visible;
        }

        #endregion // Event Handlers

        #region Nested Classes

        /// <summary>
        /// Arguments to pass to the GameSelectWorker.
        /// </summary>
        private struct GameSelectArgs
        {
            public SupportedGame Game;
            public string Path;

            public override string ToString()
            {
                return $"{nameof(Game)}: {Game.ToDescription()}, {nameof(Path)}: {Path}";
            }
        }

        #endregion // Nested Classes

        //#region INotifyPropertyChanged Implementation

        //public event PropertyChangedEventHandler PropertyChanged;

        //protected void NotifyPropertyChanged(string propertyName)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}

        //protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        //{
        //    if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        //    field = value;
        //    NotifyPropertyChanged(propertyName);
        //    return true;
        //}

        //#endregion // INotifyPropertyChanged Implementation
    }
}
