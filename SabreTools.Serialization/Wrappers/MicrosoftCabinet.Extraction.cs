using System;
using System.Collections.Generic;
using System.IO;
using SabreTools.Data.Models.MicrosoftCabinet;
using SabreTools.IO.Extensions;
using SabreTools.IO.Compression.MSZIP;

namespace SabreTools.Serialization.Wrappers
{
    public partial class MicrosoftCabinet : IExtractable
    {
        #region Extension Properties

        /// <summary>
        /// Reference to the next cabinet header
        /// </summary>
        /// <remarks>Only used in multi-file</remarks>
        public MicrosoftCabinet? Next { get; set; }

        /// <summary>
        /// Reference to the next previous header
        /// </summary>
        /// <remarks>Only used in multi-file</remarks>
        public MicrosoftCabinet? Prev { get; set; }

        #endregion

        #region Cabinet Set

        /// <summary>
        /// Open a cabinet set for reading, if possible
        /// </summary>
        /// <param name="filename">Filename for one cabinet in the set</param>
        /// <returns>Wrapper representing the set, null on error</returns>
        private static MicrosoftCabinet? OpenSet(string? filename)
        {
            // If the file is invalid
            if (string.IsNullOrEmpty(filename))
                return null;
            else if (!File.Exists(filename!))
                return null;

            // Get the full file path and directory
            filename = Path.GetFullPath(filename);

            // Read in the current file and try to parse
            var stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var current = Create(stream);
            if (current?.Header == null)
                return null;

            // Seek to the first part of the cabinet set
            while (current.CabinetPrev != null)
            {
                // Attempt to open the previous cabinet
                var prev = current.OpenPrevious(filename);
                if (prev?.Header == null)
                    break;

                // Assign previous as new current
                current = prev;
            }

            // Cache the current start of the cabinet set
            var start = current;

            // Read in the cabinet parts sequentially
            while (current.CabinetNext != null)
            {
                // If the current and next filenames match
                if (Path.GetFileName(filename) == current.CabinetNext)
                    break;

                // Open the next cabinet and try to parse
                var next = current.OpenNext(filename);
                if (next?.Header == null)
                    break;

                // Add the next and previous links, resetting current
                next.Prev = current;
                current.Next = next;
                current = next;
            }

            // Return the start of the set
            return start;
        }

        /// <summary>
        /// Open the next archive, if possible
        /// </summary>
        /// <param name="filename">Filename for one cabinet in the set</param>
        private MicrosoftCabinet? OpenNext(string? filename)
        {
            // Ignore invalid archives
            if (string.IsNullOrEmpty(filename))
                return null;

            // Normalize the filename
            filename = Path.GetFullPath(filename);

            // Get if the cabinet has a next part
            string? next = CabinetNext;
            if (string.IsNullOrEmpty(next))
                return null;

            // Get the full next path
            string? folder = Path.GetDirectoryName(filename);
            if (folder != null)
                next = Path.Combine(folder, next);

            // Open and return the next cabinet
            var fs = File.Open(next, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return Create(fs);
        }

        /// <summary>
        /// Open the previous archive, if possible
        /// </summary>
        /// <param name="filename">Filename for one cabinet in the set</param>
        private MicrosoftCabinet? OpenPrevious(string? filename)
        {
            // Ignore invalid archives
            if (string.IsNullOrEmpty(filename))
                return null;

            // Normalize the filename
            filename = Path.GetFullPath(filename);

            // Get if the cabinet has a previous part
            string? prev = CabinetPrev;
            if (string.IsNullOrEmpty(prev))
                return null;

            // Get the full next path
            string? folder = Path.GetDirectoryName(filename);
            if (folder != null)
                prev = Path.Combine(folder, prev);

            // Open and return the previous cabinet
            var fs = File.Open(prev, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return Create(fs);
        }

        #endregion

        #region Extraction

        /// <inheritdoc/>
        public bool Extract(string outputDirectory, bool includeDebug)
        {
            // Display warning in debug runs
            if (includeDebug) Console.WriteLine("WARNING: LZX and Quantum compression schemes are not supported so some files may be skipped!");

            // Do not ignore previous links by default
            bool ignorePrev = false;

            // Open the full set if possible
            var cabinet = this;
            if (Filename != null)
            {
                cabinet = OpenSet(Filename);
                ignorePrev = true;
            }
            
            // TODO: first folder idk

            // If the archive is invalid
            if (cabinet?.Folders == null || cabinet.Folders.Length == 0)
                return false;

            try
            {
                // Loop through the folders
                bool allExtracted = true;
                while (true)
                {
                    // Loop through the current folders
                    for (int f = 0; f < cabinet.Folders.Length; f++)
                    {
                        if (f == 0 && (cabinet.Files[0].FolderIndex == FolderIndex.CONTINUED_PREV_AND_NEXT
                            || cabinet.Files[0].FolderIndex == FolderIndex.CONTINUED_FROM_PREV))
                            continue;

                        var folder = cabinet.Folders[f];
                        allExtracted &= cabinet.ExtractFolder(Filename, outputDirectory, folder, f, ignorePrev, includeDebug);
                    }

                    // Move to the next cabinet, if possible

                    /*
                    Array.ForEach(cabinet.Folders, folder => folder.DataBlocks = []);
                    */

                    cabinet = cabinet.Next;
                    /*cabinet?.Prev = null;*/

                    // TODO: already-extracted data isn't being cleared from memory, at least not nearly enough.
                    if (cabinet?.Folders == null || cabinet.Folders.Length == 0)
                        break;
                }

                return allExtracted;
            }
            catch (Exception ex)
            {
                if (includeDebug) Console.Error.WriteLine(ex);
                return false;
            }
        }

        /// <summary>
        /// Extract the contents of a single folder
        /// </summary>
        /// <param name="filename">Filename for one cabinet in the set, if available</param>
        /// <param name="outputDirectory">Path to the output directory</param>
        /// <param name="folder">Folder containing the blocks to decompress</param>
        /// <param name="folderIndex">Index of the folder in the cabinet</param>
        /// <param name="ignorePrev">True to ignore previous links, false otherwise</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if all files extracted, false otherwise</returns>
        private bool ExtractFolder(string? filename,
            string outputDirectory,
            CFFOLDER? folder,
            int folderIndex,
            bool ignorePrev,
            bool includeDebug)
        {

            // Loop through the files
            bool allExtracted = true;
            var filterFiles = GetSpannedFiles(filename, folderIndex, ignorePrev);
            List<CFFILE> fileList = [];

            // Filtering, add debug output eventually
            for (int i = 0; i < filterFiles.Length; i++)
            {
                var file = filterFiles[i];

                if (file.FolderIndex == FolderIndex.CONTINUED_PREV_AND_NEXT ||
                    file.FolderIndex == FolderIndex.CONTINUED_FROM_PREV)
                {
                    // debug output for inconsistencies would go here
                    continue;
                }

                fileList.Add(file);
            }

            CFFILE[] files = fileList.ToArray();
            byte[] leftoverBytes = [];
            if (folder == null) // TODO: this should never happen
                return false;
            
            this._dataSource.SeekIfPossible(folder.CabStartOffset, SeekOrigin.Begin);
            // Setup decompressors
            var mszip = Decompressor.Create();
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                allExtracted &= ExtractFiles(outputDirectory, folder, file, ref leftoverBytes, mszip, includeDebug);
            }

            return allExtracted;
        }

        // TODO: this will apparently improve memory usage/performance, but it's not clear if this implementation is enough for that to happen
        /// <summary>
        /// Extract the contents of a single file, intended to be used with all files in a straight shot
        /// </summary>
        /// <param name="outputDirectory">Path to the output directory</param>
        /// <param name="blockStream">Stream representing the uncompressed block data</param>
        /// <param name="file">File information</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if the file extracted, false otherwise</returns>
        private bool ExtractFiles(string outputDirectory, CFFOLDER? folder, CFFILE file, ref byte[] leftoverBytes, Decompressor mszip, bool includeDebug)
        {
            try
            {
                // byte[] fileData = blockStream.ReadBytes((int)file.FileSize);

                // Ensure directory separators are consistent
                string filename = file.Name;
                if (Path.DirectorySeparatorChar == '\\')
                    filename = filename.Replace('/', '\\');
                else if (Path.DirectorySeparatorChar == '/')
                    filename = filename.Replace('\\', '/');

                // Ensure the full output directory exists
                filename = Path.Combine(outputDirectory, filename);
                var directoryName = Path.GetDirectoryName(filename);
                if (directoryName != null && !Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);

                // Open the output file for writing
                using var fs = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.None);
                
#region workregion

                // Ensure folder contains data
                // TODO: does this ever fail on spanned only folders or something
                if (folder == null || folder.DataCount == 0)
                    return false;

                // Get the compression type
                var compressionType = GetCompressionType(folder!);

                //uint quantumWindowBits = (uint)(((ushort)folder.CompressionType >> 8) & 0x1f);

                // Loop through the data blocks

                MicrosoftCabinet cabinet = this;
                
                int cabinetCount = 1;
                if (this.Files[this.FileCount - 1].FolderIndex == FolderIndex.CONTINUED_TO_NEXT)
                {
                    cabinetCount++;
                    MicrosoftCabinet? tempCabinet = this.Next;  // TODO: what do you do if this is null, it shouldn't be
                    while (tempCabinet?.Files[0].FolderIndex == FolderIndex.CONTINUED_PREV_AND_NEXT)
                    {
                        cabinetCount++;
                        tempCabinet = tempCabinet.Next;
                    }
                }
                
                // TODO: do continued spanned folders ever contain another file beyond the one spanned one

                
                CFFOLDER currentFolder = folder;
                int currentCabinetCount = 0;
                bool continuedBlock = false;
                bool fileFinished = false;
                CFDATA continuedDataBlock = new CFDATA(); // TODO: this wont work because it resets i think. Another ref? do in main buffer
                // TODO: these probably dont need to be longs, they were ints before
                int filesize = (int)file.FileSize;
                int extractedSize = 0;
                while (currentCabinetCount < cabinetCount)
                {
                    lock (cabinet._dataSourceLock)
                    {
                        if (currentFolder.CabStartOffset <= 0)
                            return false;   // TODO: why is a CabStartOffset of 0 not acceptable? header? 
                        
                        /*long currentPosition = cabinet._dataSource.Position;*/

                        for (int i = 0; i < currentFolder.DataCount; i++)
                        {
                            if (leftoverBytes.Length > 0)
                            {
                                int writeSize = Math.Min(leftoverBytes.Length, filesize - extractedSize);
                                byte[] tempLeftoverBytes = (byte[])leftoverBytes.Clone();
                                if (writeSize < leftoverBytes.Length)
                                {
                                    leftoverBytes = new byte[leftoverBytes.Length - writeSize];
                                    Array.Copy(tempLeftoverBytes, writeSize, leftoverBytes, 0, leftoverBytes.Length);
                                }
                                else
                                {
                                    leftoverBytes = [];
                                }
                                fs.Write(tempLeftoverBytes, 0, writeSize);
                                extractedSize += tempLeftoverBytes.Length;
                                if (extractedSize >= filesize)
                                {
                                    fileFinished = true;
                                    break;
                                }
                            }
                            // TODO: wire up
                            var db = new CFDATA();
                            
                            var dataReservedSize = cabinet.Header.DataReservedSize;

                            db.Checksum = cabinet._dataSource.ReadUInt32LittleEndian();
                            db.CompressedSize = cabinet._dataSource.ReadUInt16LittleEndian();
                            db.UncompressedSize = cabinet._dataSource.ReadUInt16LittleEndian();

                            if (dataReservedSize > 0)
                                db.ReservedData = cabinet._dataSource.ReadBytes(dataReservedSize);

                            if (db.CompressedSize > 0)
                                db.CompressedData = cabinet._dataSource.ReadBytes(db.CompressedSize);
                            
                            /*data.SeekIfPossible(currentPosition, SeekOrigin.Begin);*/

                            // Get the data to be processed
                            byte[] blockData = db.CompressedData;

                            // If the block is continued, append
                            if (db.UncompressedSize == 0)
                            {
                                // TODO: is this a correct assumption at all

                                continuedBlock = true;
                                continuedDataBlock = db;
                                
                                // TODO: these really need to never happen
                                if (cabinet.Next == null) 
                                    break;

                                if (currentCabinetCount == cabinetCount - 1)
                                    break;
                    
                                cabinet = cabinet.Next;
                                cabinet._dataSource.SeekIfPossible(currentFolder.CabStartOffset, SeekOrigin.Begin);
                                currentFolder = cabinet.Folders[0];
                                currentCabinetCount++;
                            }
                            else
                            {
                                if (continuedBlock)
                                {
                                    var nextBlock = db;
                                    db = continuedDataBlock;
                                    // TODO: why was there a continue if compressed data is null here
                                    continuedBlock = false;
                                    byte[]? nextData = nextBlock.CompressedData;
                                    blockData = [.. blockData, .. nextData];
                                    db.CompressedSize += nextBlock.CompressedSize;
                                    db.UncompressedSize = nextBlock.UncompressedSize;
                                    continuedDataBlock = new CFDATA();
                                }
                                
                                // Get the uncompressed data block
                                byte[] data = compressionType switch
                                {
                                    CompressionType.TYPE_NONE => blockData,
                                    CompressionType.TYPE_MSZIP => DecompressMSZIPBlock(currentCabinetCount, mszip, i, db, blockData,
                                        includeDebug),

                                    // TODO: Unsupported
                                    CompressionType.TYPE_QUANTUM => [],
                                    CompressionType.TYPE_LZX => [],

                                    // Should be impossible
                                    _ => [],
                                };
                                int writeSize = Math.Min(data.Length, filesize - extractedSize );
                                if (writeSize < data.Length)
                                {
                                    leftoverBytes = new byte[data.Length - writeSize];
                                    Array.Copy(data, writeSize, leftoverBytes, 0, leftoverBytes.Length);
                                }
                                fs.Write(data, 0, writeSize);
                                extractedSize += data.Length;
                                if (extractedSize >= filesize)
                                {
                                    fileFinished = true;
                                    break;
                                }
                                // TODO: do i ever need to flush before the end of the file?
                            }
                        }
                    }

                    if (fileFinished)
                        break;
                    
                    // TODO: does this running unnecessarily on unspanned folders cause issues
                    // TODO: spanned folders are only across cabs and never within cabs, right

                    if (cabinet.Next == null) 
                        break;

                    if (currentCabinetCount == cabinetCount - 1)
                        break;
                    
                    cabinet = cabinet.Next;
                    cabinet._dataSource.SeekIfPossible(currentFolder.CabStartOffset, SeekOrigin.Begin);
                    currentFolder = cabinet.Folders[0];
                    currentCabinetCount++;
                }

#endregion
                
                //fs.Write(fileData, 0, fileData.Length);
                fs.Flush();
            }
            catch (Exception ex)
            {
                if (includeDebug) Console.Error.WriteLine(ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Extract the contents of a single file
        /// </summary>
        /// <param name="outputDirectory">Path to the output directory</param>
        /// <param name="blockStream">Stream representing the uncompressed block data</param>
        /// <param name="file">File information</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if the file extracted, false otherwise</returns>
        private static bool ExtractFile(string outputDirectory, Stream blockStream, CFFILE file, bool includeDebug)
        {
            try
            {
                blockStream.SeekIfPossible(file.FolderStartOffset, SeekOrigin.Begin);
                byte[] fileData = blockStream.ReadBytes((int)file.FileSize);

                // Ensure directory separators are consistent
                string filename = file.Name;
                if (Path.DirectorySeparatorChar == '\\')
                    filename = filename.Replace('/', '\\');
                else if (Path.DirectorySeparatorChar == '/')
                    filename = filename.Replace('\\', '/');

                // Ensure the full output directory exists
                filename = Path.Combine(outputDirectory, filename);
                var directoryName = Path.GetDirectoryName(filename);
                if (directoryName != null && !Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);

                // Open the output file for writing
                using var fs = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.None);
                fs.Write(fileData, 0, fileData.Length);
                fs.Flush();
            }
            catch (Exception ex)
            {
                if (includeDebug) Console.Error.WriteLine(ex);
                return false;
            }

            return true;
        }

        #endregion

        #region Checksumming

        /// <summary>
        /// The computation and verification of checksums found in CFDATA structure entries cabinet files is
        /// done by using a function described by the following mathematical notation. When checksums are
        /// not supplied by the cabinet file creating application, the checksum field is set to 0 (zero). Cabinet
        /// extracting applications do not compute or verify the checksum if the field is set to 0 (zero).
        /// </summary>
        private static uint ChecksumData(byte[] data)
        {
            uint[] C =
            [
                S(data, 1, data.Length),
                S(data, 2, data.Length),
                S(data, 3, data.Length),
                S(data, 4, data.Length),
            ];

            return C[0] ^ C[1] ^ C[2] ^ C[3];
        }

        /// <summary>
        /// Individual algorithmic step
        /// </summary>
        private static uint S(byte[] a, int b, int x)
        {
            int n = a.Length;

            if (x < 4 && b > n % 4)
                return 0;
            else if (x < 4 && b <= n % 4)
                return a[n - b + 1];
            else // if (x >= 4)
                return a[n - x + b] ^ S(a, b, x - 4);
        }

        #endregion
    }
}
