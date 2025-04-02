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
                Environment.CurrentDirectory,
                PaletteManager.PALETTE_DIRECTORY,
                filename);
            var text = SerializeJson();
            var di = new FileInfo(path).Directory;
            if (!di.Exists) di.Create();
            File.WriteAllText(path, text);
        }

        public static Palette ReadJsonFromFile(string path)
        {
            var palette = DeserializeJson(File.ReadAllText(path));
            palette.FileName = Path.GetFileName(path);
            return palette;
        }

        public static Palette DeserializeJson(string toDeserialize)
            => JsonConvert.DeserializeObject<Palette>(toDeserialize);

        public string SerializeJson()
            => JsonConvert.SerializeObject(this);

        internal Dictionary<Brush, string> ToDictionary()
        {
            return Colors.ToDictionary(c => c.Color, c => c.Name);

            //var retval = new Dictionary<Brush, string>();

            //foreach (var color in Colors)
            //    retval.Add(color.Color, color.Name);

            //return retval;
        }
        #endregion
    }
}
