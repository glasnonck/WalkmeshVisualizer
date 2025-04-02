using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using WalkmeshVisualizerWpf.Views;

namespace WalkmeshVisualizerWpf.Models
{
    public class PaletteManager
    {
        public const string PALETTE_DIRECTORY = "Resources\\Palettes";
        public const string DEFAULT_PALETTE_STRING = "{\"Name\":\"Bright\",\"Colors\":[{\"ColorText\":\"#FF0000FF\",\"Name\":\"Blue\"},{\"ColorText\":\"#FF00FF00\",\"Name\":\"Green\"},{\"ColorText\":\"#FFFF0000\",\"Name\":\"Red\"},{\"ColorText\":\"#FF00FFFF\",\"Name\":\"Cyan\"},{\"ColorText\":\"#FFFF00FF\",\"Name\":\"Magenta\"},{\"ColorText\":\"#FFFFFF00\",\"Name\":\"Yellow\"}]}";

        #region Singleton Implementation
        private static PaletteManager instance = null;
        public static PaletteManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new PaletteManager();
                return instance;
            }
        }
        #endregion

        public ObservableCollection<Palette> Palettes { get; set; } = new ObservableCollection<Palette>();

        public static Palette GetSelectedPalette() => Instance.Palettes.FirstOrDefault(p => p.IsSelected);

        public PaletteManager()
        {
            // Read .json files from ./Resources/Palettes/
            var di = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, PALETTE_DIRECTORY));
            if (di.Exists)
            {
                foreach (var fi in di.GetFiles("*.json", SearchOption.TopDirectoryOnly))
                    Palettes.Add(Palette.ReadJsonFromFile(fi.FullName));
            }

            // If no palettes found, create the default palette file.
            if (Palettes.Count == 0)
            {
                if (!di.Exists) di.Create();
                var defaultPalette = Palette.DeserializeJson(DEFAULT_PALETTE_STRING);
                defaultPalette.WriteToFile();
                Palettes.Add(defaultPalette);
            }
        }

        /// <summary>
        /// Searches the Palettes directory for new .json palette files.
        /// </summary>
        internal void CheckPalettesDirectory()
        {
            var di = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, PALETTE_DIRECTORY));
            if (!di.Exists) return;
            foreach (var fi in di.GetFiles("*.json", SearchOption.TopDirectoryOnly))
            {
                if (!Palettes.Any(p => p.FileName == fi.Name))
                    Palettes.Add(Palette.ReadJsonFromFile(fi.FullName));
            }
        }

        /// <summary>
        /// Downloads any new or missing palettes from GitHub.
        /// </summary>
        internal void DownloadMissingPalettes()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Writes all currently loaded palettes to the Palettes directory.
        /// </summary>
        internal void WritePalettes()
        {
            foreach (var palette in Palettes)
                palette.WriteToFile();
        }

        public override string ToString() => $"{Palettes.Count} palette(s)";
    }
}
