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

        public override string ToString() => $"\"{Name}\": {Colors.Count} color(s)";

        #region Serialization
        public void WriteToFile(string filename = null)
        {
            if (string.IsNullOrWhiteSpace(filename)) filename = FileName;
            if (string.IsNullOrWhiteSpace(filename)) filename = Name + ".json";

            var path = Path.Combine(
                PaletteManager.PALETTE_DIRECTORY,
                filename);
            var text = SerializeJson();
            var di = new FileInfo(path).Directory;
            if (!di.Exists) di.Create();
            File.WriteAllText(path, text);
        }

        public static Palette ReadJsonFromFile(string path)
        {
            var palette = DeserializeJson(path);
            if (palette == null) return null;

            palette.FileName = Path.GetFileName(path);

            // Check for a valid number of colors.
            if (palette.Colors.Count == 0)
            {
                palette.IsInvalid = true;
                palette.Errors.Insert(0, $"{palette.FileName}: File has no defined colors.");
            }
            // Check for a valid palette name.
            if (string.IsNullOrWhiteSpace(palette.Name))
            {
                palette.IsInvalid = true;
                palette.Errors.Insert(0, $"{palette.FileName}: File is missing the \"Name\" element.");
            }

            return palette;
        }

        public static Palette DeserializeJson(string path)
        {
            var filename = Path.GetFileName(path);
            var toDeserialize = File.ReadAllText(path);
            try
            {
                var pal = JsonConvert.DeserializeObject<Palette>(toDeserialize);
                if (pal == null) return null;

                // Check for valid colors.
                for (int i = 0; i < pal.Colors.Count; i++)
                {
                    // Color must have a "Name"
                    if (string.IsNullOrWhiteSpace(pal.Colors[i].Name))
                    {
                        pal.IsInvalid = true;
                        pal.Errors.Add($"{filename}: Color #{i+1} is missing a valid \"Name\" element.");
                    }

                    // Color must have a "Color"
                    if (pal.Colors[i].Color == null)
                    {
                        pal.IsInvalid = true;
                        pal.Errors.Add($"{filename}: Color #{i+1} is missing a valid \"Color\" element.");
                    }
                }

                return pal;
            }
            catch (Exception)
            {
                // Ignore exceptions.
                return null;
            }
        }

        public string SerializeJson()
            => JsonConvert.SerializeObject(this);

        internal Dictionary<Brush, string> ToDictionary()
        {
            return Colors.ToDictionary(c => c.Color, c => c.Name);
        }
        #endregion
    }
}
