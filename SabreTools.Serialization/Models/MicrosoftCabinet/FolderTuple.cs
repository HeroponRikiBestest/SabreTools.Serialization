using System;

namespace SabreTools.Data.Models.MicrosoftCabinet
{
    // TODO: Surely there's a better way to do this
    /// <summary>
    /// Tuple to hold what's needed to open a specific folder
    /// </summary>
    public sealed class FolderTuple
    {
        /// <summary>
        /// Filename for one cabinet in the set, if available
        /// </summary>
        public string Filename { get; set; } = String.Empty;

        /// <summary>
        /// Folder containing the blocks to decompress
        /// </summary>
        public CFFOLDER Folder { get; set; } = new CFFOLDER();

        /// <summary>
        /// A series of one or more cabinet file (CFFILE) entries
        /// </summary>
        public int FolderIndex { get; set; }
    }
}