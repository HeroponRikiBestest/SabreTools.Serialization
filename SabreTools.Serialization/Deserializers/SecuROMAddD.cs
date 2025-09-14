using System.IO;
using System.Text;
using SabreTools.IO.Extensions;
using SabreTools.Models.PortableExecutable;

namespace SabreTools.Serialization.Deserializers
{
    public class SecuROMAddD : BaseBinaryDeserializer<Models.PortableExecutable.SecuROMAddD>
    {
        /// <inheritdoc/>
        public override Models.PortableExecutable.SecuROMAddD? Deserialize(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                // Cache the current offset
                long initialOffset = data.Position;

                var addD = ParseSecuROMAddD(data);
                if (addD.Signature != 0x44646441)
                    return null;

                return addD;
            }
            catch
            {
                // Ignore the actual error
                return null;
            }
        }

        /// <summary>
        /// Parse a Stream into an SecuROMAddD
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled SecuROMAddD on success, null on error</returns>
        private static Models.PortableExecutable.SecuROMAddD ParseSecuROMAddD(Stream data)
        {
            var obj = new Models.PortableExecutable.SecuROMAddD();

            obj.Signature = data.ReadUInt32LittleEndian();
            obj.EntryCount = data.ReadUInt32LittleEndian();
            obj.Version = data.ReadNullTerminatedAnsiString();
            byte[] buildBytes = data.ReadBytes(4);
            string buildStr = Encoding.ASCII.GetString(buildBytes);
            obj.Build = buildStr.ToCharArray();
            obj.Unknown14h = data.ReadBytes(1); // TODO: Figure out how to determine how many bytes are here consistently

            obj.Entries = new SecuROMAddDEntry[obj.EntryCount];
            for (int i = 0; i < obj.Entries.Length; i++)
            {
                var entry = ParseSecuROMAddDEntry(data);
                obj.Entries[i] = entry;
            }

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an SecuROMAddDEntry
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled SecuROMAddDEntry on success, null on error</returns>
        private static SecuROMAddDEntry ParseSecuROMAddDEntry(Stream data)
        {
            var obj = new SecuROMAddDEntry();

            obj.PhysicalOffset = data.ReadUInt32LittleEndian();
            obj.Length = data.ReadUInt32LittleEndian();
            obj.Unknown08h = data.ReadUInt32LittleEndian();
            obj.Unknown0Ch = data.ReadUInt32LittleEndian();
            obj.Unknown10h = data.ReadUInt32LittleEndian();
            obj.Unknown14h = data.ReadUInt32LittleEndian();
            obj.Unknown18h = data.ReadUInt32LittleEndian();
            obj.Unknown1Ch = data.ReadUInt32LittleEndian();
            obj.FileName = data.ReadNullTerminatedAnsiString();
            obj.Unknown2Ch = data.ReadUInt32LittleEndian();

            return obj;
        }
    }
}
