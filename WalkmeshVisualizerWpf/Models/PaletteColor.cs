using Newtonsoft.Json;
using System;
using System.Windows.Media;

namespace WalkmeshVisualizerWpf.Models
{
    [Serializable]
    public class PaletteColor
    {
        private Brush _color;
        private string _colorText;

        [JsonIgnore]
        public Brush Color => _color;

        public string ColorText
        {
            get => _colorText;
            set
            {
                _colorText = value;
                _color = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
            }
        }

        public string Name { get; set; }

        public override string ToString() => $"{ColorText} \"{Name}\"";
    }
}
