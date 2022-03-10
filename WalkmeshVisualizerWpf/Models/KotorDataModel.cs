using KotOR_IO;
using KotOR_IO.GffFile;
using KotOR_IO.Helpers;
using System.Collections.Generic;
using WalkmeshVisualizerWpf.Helpers;

namespace WalkmeshVisualizerWpf.Models
{
    /// <summary>
    /// Encapsulates data retrieved from KotOR game files using the KotOR_IO dll.
    /// </summary>
    public class KotorDataModel
    {
        /// <summary>
        /// Enumerated game identifier.
        /// </summary>
        public SupportedGame Game { get; set; }

        /// <summary>
        /// Name of this game.
        /// </summary>
        public string GameName { get; set; }

        /// <summary>
        /// Directory paths within this installation.
        /// </summary>
        public KPaths KPaths { get; set; }

        /// <summary>
        /// Chitin KEY data file.
        /// </summary>
        public KEY KEY { get; set; }

        ///// <summary>
        ///// Talk Table data file.
        ///// </summary>
        //public TLK TLK { get; set; }

        /// <summary>
        /// Path to the local cache of this data.
        /// </summary>
        public string CachePath { get; set; }

        /// <summary>
        /// Lookup from a RIM filename to a more readable Common Name.
        /// </summary>
        public Dictionary<string, string> RimNameToCommonName { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Lookup from a RIM filename to the associated GIT file.
        /// </summary>
        public Dictionary<string, GIT> RimNameToGit { get; set; } = new Dictionary<string, GIT>();

        /// <summary>
        /// Lookup from a RIM filename to the associated LYT file.
        /// </summary>
        public Dictionary<string, LYT> RimNameToLyt { get; set; } = new Dictionary<string, LYT>();

        /// <summary>
        /// Lookup from a RIM filename to a collection of the WOKs it contains.
        /// </summary>
        public Dictionary<string, IEnumerable<WOK>> RimNameToWoks { get; set; } = new Dictionary<string, IEnumerable<WOK>>();

        /// <summary>
        /// Lookup from a Room name to the room's walkmesh.
        /// </summary>
        public Dictionary<string, WOK> RoomNameToWok { get; set; } = new Dictionary<string, WOK>();

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
            return $"{GameName}, Rims: {RimNameToCommonName.Count}";
        }
    }
}
