using System.ComponentModel;

namespace WalkmeshVisualizerWpf.Helpers
{
    public enum SupportedGame
    {
        [Description("Not Supported")]
        NotSupported = 0,
        [Description("KotOR 1")]
        Kotor1,
        [Description("KotOR 2")]
        Kotor2,
    }
}
