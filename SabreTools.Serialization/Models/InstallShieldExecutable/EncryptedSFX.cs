namespace SabreTools.Data.Models.InstallShieldExecutable
{
    /// <summary>
    /// Represents the layout of of the overlay area of an Encrypted
    /// InstallShield executable.
    ///
    /// The layout of this is derived from the layout in the
    /// physical file.
    /// </summary>
    public class EncryptedSFX
    {
        public string? Signature { get; set; }
        
        public uint EntryCount { get; set; }
        
        public uint Type { get; set; }
        
        public byte[]? UnknownX4 { get; set; }
        
        public uint UnknownX5 { get; set; }

        public byte[]? UnknownX6 { get; set; }

        /// <summary>
        /// Set of file entries
        /// </summary>
        public EncryptedFileEntry[]? Entries { get; set; }
    }
}
