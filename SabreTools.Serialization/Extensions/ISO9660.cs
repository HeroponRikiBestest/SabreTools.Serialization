using System;
using SabreTools.Data.Models.ISO9660;

namespace SabreTools.Data.Extensions
{
    public static class ISO9660
    {
        /// <summary>
        /// Get the logical block size from a sector length
        /// </summary>
        /// <param name="bvd">Volume descriptor containing block information</param>
        /// <param name="sectorLength">Defined sector length</param>
        /// <returns>Size of a logical block</returns>
        public static short GetLogicalBlockSize(this BaseVolumeDescriptor bvd, short sectorLength)
        {
            short blockLength = sectorLength;
            if (bvd.LogicalBlockSize.IsValid)
            {
                // Validate logical block length
                if (bvd.LogicalBlockSize >= 512 && bvd.LogicalBlockSize <= sectorLength && (bvd.LogicalBlockSize & (bvd.LogicalBlockSize - 1)) == 0)
                    blockLength = bvd.LogicalBlockSize;
            }
            else
            {
                // If logical block size is ambiguous check if only one is valid, otherwise default to sector length
                short le = bvd.LogicalBlockSize.LittleEndian;
                short be = bvd.LogicalBlockSize.LittleEndian;
                bool le_valid = true;
                bool be_valid = true;
                if (le < 512 || le > sectorLength || (le & (le - 1)) != 0)
                    le_valid = false;
                if (be < 512 || be > sectorLength || (be & (be - 1)) != 0)
                    be_valid = false;
                if (le_valid && !be_valid)
                    blockLength = le;
                else if (be_valid && !le_valid)
                    blockLength = be;
                else
                    blockLength = sectorLength;
            }

            return blockLength;
        }

        /// <summary>
        /// Indicates if an array contains all ASCII numeric digits
        /// </summary>
        /// TODO: Move to IO as an array extension
        public static bool IsNumericArray(this byte[] arr)
            => Array.TrueForAll(arr, b => b >= 0x30 && b <= 0x39);
    }
}
