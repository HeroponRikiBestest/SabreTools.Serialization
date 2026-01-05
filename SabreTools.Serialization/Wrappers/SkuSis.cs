#if NET6_0_OR_GREATER
using System.IO;
using System.Text.Json.Nodes;

namespace SabreTools.Serialization.Wrappers
{
    public partial class SkuSis : WrapperBase<Data.Models.VDF.File>
    {
        #region Descriptive Properties

        /// <inheritdoc/>
        public override string DescriptionString => "Valve Data File";

        #endregion
        
        #region Extension Properties
        
        /// <inheritdoc cref="Models.VDF.File.Signature"/>
        public byte[]? Signature => Model.Signature;
        
        /// <inheritdoc cref="Models.VDF.File.VDFObject"/>
        public JsonObject? VDFObject => Model.VDFObject;

        #endregion

        #region Constructors

        public SkuSis(Data.Models.VDF.File model, byte[] data) : base(model, data) { }

        /// <inheritdoc/>
        public SkuSis(Data.Models.VDF.File model, byte[] data, int offset) : base(model, data, offset) { }

        /// <inheritdoc/>
        public SkuSis(Data.Models.VDF.File model, byte[] data, int offset, int length) : base(model, data, offset, length) { }

        /// <inheritdoc/>
        public SkuSis(Data.Models.VDF.File model, Stream data) : base(model, data) { }

        /// <inheritdoc/>
        public SkuSis(Data.Models.VDF.File model, Stream data, long offset) : base(model, data, offset) { }

        /// <inheritdoc/>
        public SkuSis(Data.Models.VDF.File model, Stream data, long offset, long length) : base(model, data, offset, length) { }

        #endregion

        #region Static Constructors

        /// <summary>
        /// Create an SKU sis from a byte array and offset
        /// </summary>
        /// <param name="data">Byte array representing the SKU sis</param>
        /// <param name="offset">Offset within the array to parse</param>
        /// <returns>An SKU sis wrapper on success, null on failure</returns>
        public static SkuSis? Create(byte[]? data, int offset)
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
        /// Create an SKU sis from a Stream
        /// </summary>
        /// <param name="data">Stream representing the SKU sis</param>
        /// <returns>An SKU sis wrapper on success, null on failure</returns>
        public static SkuSis? Create(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                // Cache the current offset
                long currentOffset = data.Position;

                var model = new Readers.SkuSis().Deserialize(data);
                if (model == null)
                    return null;

                return new SkuSis(model, data, currentOffset);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
#endif