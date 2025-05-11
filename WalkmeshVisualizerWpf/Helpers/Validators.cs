using System;
using System.Globalization;
using System.Windows.Controls;

namespace WalkmeshVisualizerWpf.Helpers
{
    public class IntRangeRule : ValidationRule
    {
        public int Min { get; set; } = int.MinValue;
        public int Max { get; set; } = int.MaxValue;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int param = 0;
            try
            {
                var valueString = value.ToString();
                if (valueString.Length > 0)
                    param = int.Parse(valueString);
            }
            catch (Exception e)
            {
                return new ValidationResult(false, "Illegal characters or " + e.Message);
            }

            if (param < Min || param > Max)
                return new ValidationResult(false, $"Please enter value in the range: [{Min},{Max}]");

            return new ValidationResult(true, null);
        }
    }
}
