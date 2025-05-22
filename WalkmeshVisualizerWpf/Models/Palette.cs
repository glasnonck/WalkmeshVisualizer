using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace WalkmeshVisualizerWpf.Models
{
    [Serializable]
    public class Palette
    {
        #region Properties
        [JsonIgnore]
        public bool IsSelected { get; set; }

        [JsonIgnore]
        public string FileName { get; set; }

        [JsonIgnore]
        public bool IsInvalid { get; set; } = false;

        [JsonIgnore]
        public List<string> Errors { get; set; } = new List<string>();

        public string Name { get; set; }

        public ObservableCollection<PaletteColor> Colors { get; set; } = new ObservableCollection<PaletteColor>();
        #endregion

        #region Constructors
        public Palette() { }

        public Palette(Palette palette)
        {
            FileName = palette.FileName;
            IsSelected = palette.IsSelected;
            IsInvalid = palette.IsInvalid;
            Errors = palette.Errors.ToList();
            Name = palette.Name;
            Colors = new ObservableCollection<PaletteColor>();
            foreach (var color in palette.Colors)
            {
                Colors.Add(new PaletteColor(color));
            }
        }
        #endregion

        #region Methods
        public override string ToString() => $"\"{Name}\": {Colors.Count} color(s)";

        public Dictionary<Brush, string> ToDictionary()
        {
            return Colors.ToDictionary(c => c.Color, c => c.Name);
        }
        #endregion

        #region Serialization
        public void WriteToFile(string filename = null)
        {
            if (string.IsNullOrWhiteSpace(filename)) filename = FileName;
            if (string.IsNullOrWhiteSpace(filename)) filename = FileName = Name + ".json";

            var path = Path.Combine(
                PaletteManager.PALETTE_DIRECTORY,
                filename);
            var text = SerializeJson();
            var di = new FileInfo(path).Directory;
            if (!di.Exists) di.Create();
            File.WriteAllText(path, text);
        }

        public static Palette DeserializeJsonFromFile(string path)
        {
            var filename = Path.GetFileName(path);
            Palette palette;

            // Attempt deserialization.
            try { palette = DeserializeJsonString(File.ReadAllText(path)); }
            catch (Exception e)
            {
                // If deserialization failed, create blank palette with error message.
                palette = new Palette
                {
                    FileName = filename,
                    IsInvalid = true,
                    Errors = new List<string> {
                        $"{filename}: File is not in valid JSON format. {e.Message}"
                    }
                };
                return palette;
            }

            if (palette == null) return null;   // Ignore any additional errors.
            palette.FileName = filename;        // Set palette filename.

            // Check for a valid palette name.
            if (string.IsNullOrWhiteSpace(palette.Name))
            {
                palette.IsInvalid = true;
                palette.Errors.Insert(0, $"{filename}: File is missing the \"Name\" element.");
            }

            // Check for a valid number of colors.
            if (palette.Colors.Count == 0)
            {
                palette.IsInvalid = true;
                palette.Errors.Insert(0, $"{filename}: File has no defined colors.");
            }

            // Check for valid colors.
            for (int i = 0; i < palette.Colors.Count; i++)
            {
                // Color must have a "Name"
                if (string.IsNullOrWhiteSpace(palette.Colors[i].Name))
                {
                    palette.IsInvalid = true;
                    palette.Errors.Add($"{filename}: Color #{i+1} is missing a valid \"Name\" element.");
                }

                // Color must have a "Color"
                if (palette.Colors[i].Color == null)
                {
                    palette.IsInvalid = true;
                    palette.Errors.Add($"{filename}: Color #{i+1} is missing a valid \"Color\" element.");
                }
            }

            return palette;
        }

        public static Palette DeserializeJsonString(string json)
            => JsonConvert.DeserializeObject<Palette>(json);

        public string SerializeJson()
            => JsonConvert.SerializeObject(this);
        #endregion
    }
}
