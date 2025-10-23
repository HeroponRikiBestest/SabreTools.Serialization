using System.IO;
using SabreTools.Data.Models.InstallShieldExecutable;
using SabreTools.IO.Extensions;

// According to ISx source, there are two categories of installshield executables, plain and encrypted. Encrypted has
// two different "types" it can be, specified by a header. "InstallShield" and a newer format from 2015?-onwards called
// "ISSetupStream". Plain executables have no central file entry header, and each file is unencrypted. Files in 
// "InstallShield" encrypted executables have encryption applied over block sizes of 1024 bytes, and files in
// "ISSetupStream" encrypted executables are encrypted per-file. There's also something about leading data that
// isn't explained, (at least not clearly), and these encrypted executables can also additionally have their files
// compressed with inflate.
// While not stated in ISx, from experience, executables with "InstallShield" often (if not always?) mainly consist of
// a singular, large MSI installer along with some helper files, wheras plain executables often (if not always?) mainly
// consist of regular installshield cabinets within.
// At the moment, this code only supports and documents the plain variant. Clearer naming and seperation between the
// types is yet to come.

namespace SabreTools.Serialization.Readers
{
    public class InstallShieldExecutableFile : BaseBinaryReader<ExtractableFile>
    {
        public override ExtractableFile? Deserialize(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                // Cache the initial offset
                long initialOffset = data.Position;

                // Try to parse the header
                var header = ParseExtractableFileHeader(data);
                if (header == null)
                    return null;

                return header;
            }
            catch
            {
                // Ignore the actual error
                return null;
            }        
        }
        
        /// <summary>
        /// Parse a Stream into an InstallShield Executable file header
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled InstallShield Executable file header on success, null on error</returns>
        public static ExtractableFile? ParseExtractableFileHeader(Stream? data)
        {
            var obj = new ExtractableFile();
            
            obj.Name = data.ReadNullTerminatedAnsiString();
            if (obj.Name == null)
                return null;
        
            obj.Path = data.ReadNullTerminatedAnsiString();
            if (obj.Path == null)
                return null;
        
            obj.Version = data.ReadNullTerminatedAnsiString();
            if (obj.Version == null)
                return null;
        
            var versionString = data.ReadNullTerminatedAnsiString();
            if (versionString == null || !ulong.TryParse(versionString, out var foundVersion))
                return null;
        
            obj.Length = foundVersion;
            if (obj.Name == null)
                return null;
        
            return obj;
        }
    }
}