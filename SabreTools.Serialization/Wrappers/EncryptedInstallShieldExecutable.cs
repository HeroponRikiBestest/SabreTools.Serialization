using System.IO;
using SabreTools.Data.Models.InstallShieldExecutable;

namespace SabreTools.Serialization.Wrappers
{
    public partial class EncryptedInstallShieldExecutable : WrapperBase<EncryptedSFX>
    {
        #region Descriptive Properties

        /// <inheritdoc/>
        public override string DescriptionString => "Encrypted InstallShield Executable";

        #endregion

        #region Extension Properties

        /// <inheritdoc cref="EncryptedSFX.Signature"/>
        public string? Signature => Model.Signature;

        /// <inheritdoc cref="EncryptedSFX.EntryCount"/>
        public uint EntryCount => Model.EntryCount;

        /// <inheritdoc cref="EncryptedSFX.Type"/>
        public uint Type => Model.Type;

        /// <inheritdoc cref="EncryptedSFX.UnknownX4"/>
        public byte[]? UnknownX4 => Model.UnknownX4;

        /// <inheritdoc cref="EncryptedSFX.UnknownX5"/>
        public uint UnknownX5 => Model.UnknownX5;

        /// <inheritdoc cref="EncryptedSFX.UnknownX6"/>
        public byte[]? UnknownX6 => Model.UnknownX6;

        /// <inheritdoc cref="EncryptedSFX.Entries"/>
        public EncryptedFileEntry[]? Entries => Model.Entries;

        #endregion

        #region Constructors

        /// <inheritdoc/>
        public EncryptedInstallShieldExecutable(EncryptedSFX model, byte[] data) : base(model, data) { }

        /// <inheritdoc/>
        public EncryptedInstallShieldExecutable(EncryptedSFX model, byte[] data, int offset) : base(model, data, offset) { }

        /// <inheritdoc/>
        public EncryptedInstallShieldExecutable(EncryptedSFX model, byte[] data, int offset, int length) : base(model, data, offset, length) { }

        /// <inheritdoc/>
        public EncryptedInstallShieldExecutable(EncryptedSFX model, Stream data) : base(model, data) { }

        /// <inheritdoc/>
        public EncryptedInstallShieldExecutable(EncryptedSFX model, Stream data, long offset) : base(model, data, offset) { }

        /// <inheritdoc/>
        public EncryptedInstallShieldExecutable(EncryptedSFX model, Stream data, long offset, long length) : base(model, data, offset, length) { }

        #endregion

        #region Static Constructors

        /// <summary>
        /// Create an encrypted InstallShield Executable from a byte array and offset
        /// </summary>
        /// <param name="data">Byte array representing the executable</param>
        /// <param name="offset">Offset within the array to parse</param>
        /// <returns>An encrypted InstallShield Executable  wrapper on success, null on failure</returns>
        public static EncryptedInstallShieldExecutable? Create(byte[]? data, int offset)
        {
            // If the data is invalid
            if (data == null || data.Length == 0)
                return null;

            // If the offset is out of bounds
            if (offset < 0 || offset >= data.Length)
                return null;

            // Create a memory stream and use that
            var dataStream = new MemoryStream(data, offset, data.Length - offset);
            return Create(dataStream);
        }

        /// <summary>
        /// Create an encrypted InstallShield Executable from a Stream
        /// </summary>
        /// <param name="data">Stream representing the executable</param>
        /// <returns>An encrypted InstallShield Executable  wrapper on success, null on failure</returns>
        public static EncryptedInstallShieldExecutable? Create(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                // Cache the current offset
                long currentOffset = data.Position;

                var model = new Readers.EncryptedInstallShieldExecutable().Deserialize(data);
                if (model == null)
                    return null;

                return new EncryptedInstallShieldExecutable(model, data, currentOffset);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
