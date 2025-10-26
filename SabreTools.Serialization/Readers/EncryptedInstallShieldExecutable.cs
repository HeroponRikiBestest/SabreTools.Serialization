using System.IO;
using System.Text;
using SabreTools.Data.Models.InstallShieldExecutable;
using SabreTools.IO.Extensions;
using static SabreTools.Data.Models.InstallShieldExecutable.Constants;

namespace SabreTools.Serialization.Readers
{
    public class EncryptedInstallShieldExecutable : BaseBinaryReader<EncryptedSFX>
    {
        
        public override EncryptedSFX? Deserialize(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                // Cache the initial offset
                long initialOffset = data.Position;

                // Try to parse the header
                var sfx = ParseEncryptedSFX(data);
                if (sfx == null)
                    return null;

                // Try to parse the entries
                sfx.Entries = ParseEntries(data, sfx.EntryCount);

                return sfx;
            }
            catch
            {
                // Ignore the actual error
                return null;
            }
        }

        /// <summary>
        /// Parse a Stream into a EncryptedSFX
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled EncryptedSFX on success, null on error</returns>
        public static EncryptedSFX? ParseEncryptedSFX(Stream data)
        {
            var obj = new EncryptedSFX();
            
            // TODO

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a EncryptedSFX array
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="entryCount">Number of entries in the array</param>
        /// <returns>Filled EncryptedSFX array on success, null on error</returns>
        private static EncryptedSFX[] ParseEntries(Stream data, uint entryCount)
        {
            var obj = new EncryptedSFX[entryCount];

            // TODO
            
            return obj;
        }
    }
}

