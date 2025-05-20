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

        public PaletteColor() { }

        public PaletteColor(PaletteColor color)
        {
            ColorText = color.ColorText;
            Name = color.Name;
        }

        [JsonIgnore]
        public Brush Color => _color;

        public string ColorText
        {
            get => _colorText;
            set
            {
                try
                {
                    _color = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
                    _colorText = value;
                }
                catch (Exception)
                {
                    // In the case of an exception, don't save the new value and continue.
                }
            }
        }

        public string Name { get; set; }

        public override string ToString() => $"{ColorText} \"{Name}\"";
    }
}
