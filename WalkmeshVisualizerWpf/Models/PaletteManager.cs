using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media;
using WalkmeshVisualizerWpf.Views;

namespace WalkmeshVisualizerWpf.Models
{
    public class PaletteManager
    {
        private const string DEFAULT_PALETTE_STRING = "{\"Name\":\"Bright\",\"Colors\":[{\"ColorText\":\"#FF0000FF\",\"Name\":\"Blue\"},{\"ColorText\":\"#FF00FF00\",\"Name\":\"Green\"},{\"ColorText\":\"#FFFF0000\",\"Name\":\"Red\"},{\"ColorText\":\"#FF00FFFF\",\"Name\":\"Cyan\"},{\"ColorText\":\"#FFFF00FF\",\"Name\":\"Magenta\"},{\"ColorText\":\"#FFFFFF00\",\"Name\":\"Yellow\"}]}";
        private const string DEFAULT_PALETTE_NAME = "Bright";
        private const string PALETTES_ERROR_MESSAGE = "Errors have been identified in some of your palette JSON files. Please review the expected format at \"https://github.com/glasnonck/WalkmeshVisualizer?tab=readme-ov-file#palettes\"";
        private const string RELATIVE_PALETTE_DIRECTORY = "Resources\\Palettes";

        public static string PALETTE_DIRECTORY => Path.Combine(Environment.CurrentDirectory, RELATIVE_PALETTE_DIRECTORY);

        #region Singleton Implementation
        private static PaletteManager _instance = null;
        public static PaletteManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PaletteManager();

                    // If no palettes found, create the default palette file.
                    if (_instance.Palettes.Count == 0) _instance.CreateDefaultPalette();
                }
                return _instance;
            }
        }
        #endregion

        public bool ErrorsFound { get; private set; }

        public string ErrorMessages { get; private set; }

        public ObservableCollection<Palette> Palettes { get; set; } = new ObservableCollection<Palette>();

        public PaletteManager()
        {
            // Read .json files from ./Resources/Palettes/
            var di = new DirectoryInfo(PALETTE_DIRECTORY);
            if (di.Exists)
            {
                foreach (var fi in di.GetFiles("*.json", SearchOption.TopDirectoryOnly))
                {
                    var pal = Palette.DeserializeJsonFromFile(fi.FullName);
                    // Invalid files return null -- ignore them.
                    if (pal != null) Palettes.Add(pal);
                }

                // Collect error messages.
                var invalidPalettes = Palettes.Where(p => p.IsInvalid).ToList();
                if (invalidPalettes.Any())
                {
                    ErrorsFound = true;
                    var messages = new List<string> { PALETTES_ERROR_MESSAGE };
                    messages.AddRange(invalidPalettes.Select(p => string.Join(Environment.NewLine, p.Errors)));
                    ErrorMessages = string.Join(Environment.NewLine + Environment.NewLine, messages);   // separate files with two lines for readability
                }

                // Remove invalid palettes.
                foreach (var invalidPalette in invalidPalettes)
                    Palettes.Remove(invalidPalette);
            }
        }

        private void CreateDefaultPalette()
        {
            var di = new DirectoryInfo(PALETTE_DIRECTORY);
            if (!di.Exists) di.Create();
            var defaultPalette = Palette.DeserializeJsonString(DEFAULT_PALETTE_STRING);
            defaultPalette.IsSelected = true;
            defaultPalette.WriteToFile();
            _instance.Palettes.Add(defaultPalette);
        }

        public static Palette GetSelectedPalette()
        {
            if (Instance.Palettes.Count == 0) Instance.CreateDefaultPalette();
            return Instance.Palettes.FirstOrDefault(p => p.IsSelected);
            //var pal = Instance.Palettes.FirstOrDefault(p => p.IsSelected);
            //if (pal == null)
            //{
            //    Instance.Palettes.First(p => p.Name == DEFAULT_PALETTE_NAME);
            //    pal.IsSelected = true;
            //}
            //return pal;
        }

        public static void ShowPalettesDirectory()
            => Process.Start("explorer.exe", PALETTE_DIRECTORY);

        ///// <summary>
        ///// Searches the Palettes directory for new .json palette files.
        ///// </summary>
        //internal void CheckPalettesDirectory()
        //{
        //    var di = new DirectoryInfo(PALETTE_DIRECTORY);
        //    if (!di.Exists) return;
        //    foreach (var fi in di.GetFiles("*.json", SearchOption.TopDirectoryOnly))
        //    {
        //        if (!Palettes.Any(p => p.FileName == fi.Name))
        //            Palettes.Add(Palette.ReadJsonFromFile(fi.FullName));
        //    }
        //}

        ///// <summary>
        ///// Downloads any new or missing palettes from GitHub.
        ///// </summary>
        //internal void DownloadMissingPalettes()
        //{
        //    throw new NotImplementedException();
        //}

        ///// <summary>
        ///// Writes all currently loaded palettes to the Palettes directory.
        ///// </summary>
        //internal void WritePalettes()
        //{
        //    foreach (var palette in Palettes)
        //        palette.WriteToFile();
        //}

        public override string ToString() => $"{Palettes.Count} palette(s)";
    }
}
