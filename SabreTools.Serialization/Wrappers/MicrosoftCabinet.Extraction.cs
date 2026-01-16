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
            else if (!File.Exists(filename))
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
            // Do not ignore previous links by default
            bool ignorePrev = false;

            // Open the full set if possible
            var cabinet = this;
            if (Filename != null)
            {
                cabinet = OpenSet(Filename);
                ignorePrev = true;
                
                // TOOD: reenable after confirming rollback is good
                if (cabinet == null) // TODO: handle better
                    return false;
                
                // If we have anything but the first file, avoid extraction to avoid repeat extracts
                // TODO: handle partial sets
                // TODO: is there any way for this to not spam the logs on large sets? probably not, but idk
                // TODO: if/when full msi support is added, somehow this is going to have to take that into account, while also still handling partial sets
                if (this.Filename != cabinet.Filename)
                {
                    string firstCabName = Path.GetFileName(cabinet.Filename) ?? string.Empty;
                    if (includeDebug) Console.WriteLine($"Only the first cabinet {firstCabName} will be extracted!");
                    return false;
                }

                
                // Display warning in debug runs
                if (includeDebug && cabinet != null)
                {
                    var tempCabinet = cabinet;
                    HashSet<CompressionType> compressionTypes = new HashSet<CompressionType>();
                    while (true) // this feels unsafe, but the existing code already did it
                    {
                        for (int i = 0; i < tempCabinet.FolderCount; i++)
                            compressionTypes.Add(GetCompressionType(tempCabinet.Folders[i]));
                        
                        tempCabinet = tempCabinet.Next;
                        
                        if (tempCabinet == null) // TODO: handle better
                            break;
                        
                        if (tempCabinet.Folders.Length == 0)
                            break;   
                    }
                    
                    string firstLine = "Mscab contains compression:";
                    bool firstFence = true;
                    foreach (CompressionType compressionType in compressionTypes)
                    {
                            if (firstFence)
                                firstFence = false;
                            else
                                firstLine += ",";

                            firstLine += $" {compressionType}";
                    }
                    
                    Console.WriteLine(firstLine);
                    if (compressionTypes.Contains(CompressionType.TYPE_QUANTUM) || compressionTypes.Contains(CompressionType.TYPE_LZX))
                        Console.WriteLine("WARNING: LZX and Quantum compression schemes are not supported so some files may be skipped!");
                }
            }
            
            // If the archive is invalid
            if (cabinet?.Folders == null || cabinet.Folders.Length == 0)
                return false;
            
            return cabinet.ExtractSet(Filename, outputDirectory, ignorePrev, includeDebug);
        }

        /// <summary>
        /// Get filtered array of spanned files for a folder
        /// </summary>
        /// <param name="filename">Filename for one cabinet in the set, if available</param>
        /// <param name="f">Index of the folder in the cabinet</param>
        /// <param name="ignorePrev">True to ignore previous links, false otherwise</param>
        /// <returns>Filtered array of files</returns>
        private CFFILE[] GetSpannedFilesArray(string? filename, int f, bool ignorePrev)
        {
            // Loop through the files
            var filterFiles = GetSpannedFiles(filename, f, ignorePrev);
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

            return fileList.ToArray();
        }

        /// <summary>
        /// Get filestream for a file to be extracted to
        /// </summary>
        /// <param name="filename">Filename for the file that will be extracted to</param>
        /// <param name="outputDirectory">Path to the output directory</param>
        /// <returns>Filestream for the file to be extracted to</returns>
        private FileStream GetFileStream(string filename, string outputDirectory)
        {
            // Ensure directory separators are consistent
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
            return File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.None);
        }

        /// <summary>
        /// Read a datablock from a cabinet
        /// </summary>
        /// <param name="cabinet">Cabinet to be read from</param>
        /// <returns>Read datablock</returns>
        private CFDATA ReadBlock(MicrosoftCabinet cabinet)
        {
            var db = new CFDATA();

            var dataReservedSize = cabinet.Header.DataReservedSize;

            db.Checksum = cabinet._dataSource.ReadUInt32LittleEndian();
            db.CompressedSize = cabinet._dataSource.ReadUInt16LittleEndian();
            db.UncompressedSize = cabinet._dataSource.ReadUInt16LittleEndian();

            if (dataReservedSize > 0)
                db.ReservedData = cabinet._dataSource.ReadBytes(dataReservedSize);

            if (db.CompressedSize > 0)
                db.CompressedData = cabinet._dataSource.ReadBytes(db.CompressedSize);

            return db;
        }

        // TODO: cab stepping, folder stepping (I think?), 0 size continued blocks, find something that triggers exact data size
        /// <summary>
        /// Extract the contents of a cabinet set
        /// </summary>
        /// <param name="cabFilename">Filename for one cabinet in the set, if available</param>
        /// <param name="outputDirectory">Path to the output directory</param>
        /// <param name="ignorePrev">True to ignore previous links, false otherwise</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if all files extracted, false otherwise</returns>
        private bool ExtractSet(string? cabFilename, string outputDirectory, bool ignorePrev, bool includeDebug)
        {
            var cabinet = this;
            var currentCabFilename = cabFilename;
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
                        CFFILE[] files = cabinet.GetSpannedFilesArray(currentCabFilename, f, ignorePrev);
                        var file = files[0];
                        int bytesLeft = (int)file.FileSize;
                        int fileCounter = 0;

                        cabinet._dataSource.SeekIfPossible(folder.CabStartOffset, SeekOrigin.Begin);
                        var mszip = Decompressor.Create();
                        try
                        {
                            // Ensure folder contains data
                            // TODO: does this fail on spanned only folders or something? when would this happen
                            if (folder.DataCount == 0)
                                return false;

                            // Skip unsupported compression types to avoid opening a blank filestream. This can be altered/removed if these types are ever supported.
                            var compressionType = GetCompressionType(folder);
                            if (compressionType == CompressionType.TYPE_QUANTUM || compressionType == CompressionType.TYPE_LZX)
                                continue;
                            
                            var fs = GetFileStream(file.Name, outputDirectory);

                            // TODO: what is this comment here for
                            //uint quantumWindowBits = (uint)(((ushort)folder.CompressionType >> 8) & 0x1f);

                            if (folder.CabStartOffset <= 0)
                                return false; // TODO: why is a CabStartOffset of 0 not acceptable? header? 

                            var tempCabinet = cabinet;
                            int j = 0;

                            // Loop through the data blocks
                            // Has to be a while loop instead of a for loop due to cab spanning continue blocks
                            while (j < folder.DataCount)
                            {
                                // TODO: since i need lock state to be maintained the whole loop, do i need to cache and reset position to be safe?
                                lock (tempCabinet._dataSourceLock)
                                {
                                    var db = ReadBlock(tempCabinet);

                                    // Get the data to be processed
                                    byte[] blockData = db.CompressedData;

                                    // If the block is continued, append
                                    // TODO: this is specifically if and only if it's jumping between cabs on a spanned folder, I think?
                                    bool continuedBlock = false;
                                    if (db.UncompressedSize == 0)
                                    {
                                        tempCabinet = tempCabinet.Next;
                                        if (tempCabinet == null) // TODO: handle better?
                                            return false;
                                        
                                        // Compressiontype not updated because there's no way it's possible that it can swap on continued blocks
                                        folder = tempCabinet.Folders[0];
                                        lock (tempCabinet._dataSourceLock)
                                        {
                                            // TODO: make sure this spans?
                                            tempCabinet._dataSource.SeekIfPossible(folder.CabStartOffset, SeekOrigin.Begin);
                                            var nextBlock = ReadBlock(tempCabinet);
                                            byte[] nextData = nextBlock.CompressedData;
                                            if (nextData.Length == 0) // TODO: null cant happen, is it meant to be if it's empty?
                                                continue;

                                            continuedBlock = true;
                                            blockData = [.. blockData, .. nextData];
                                            db.CompressedSize += nextBlock.CompressedSize;
                                            db.UncompressedSize = nextBlock.UncompressedSize;
                                        }
                                    }
                                    
                                    // Get the uncompressed data block
                                    byte[] data = compressionType switch
                                    {
                                        CompressionType.TYPE_NONE => blockData,
                                        CompressionType.TYPE_MSZIP => DecompressMSZIPBlock(f, mszip, j, db, blockData, includeDebug),

                                        // TODO: Unsupported
                                        CompressionType.TYPE_QUANTUM => [],
                                        CompressionType.TYPE_LZX => [],

                                        // Should be impossible
                                        _ => [],
                                    };
                                    
                                    // TODO: will 0 byte files mess things up
                                    if (bytesLeft > 0 && bytesLeft >= data.Length)
                                    {
                                        fs.Write(data);
                                        bytesLeft -= data.Length;
                                    }
                                    else if (bytesLeft > 0 && bytesLeft < data.Length)
                                    {
                                        int tempBytesLeft = bytesLeft;
                                        fs.Write(data, 0, bytesLeft);
                                        fs.Close();

                                        // reached end of folder
                                        if (fileCounter + 1 == files.Length)
                                            break;

                                        file = files[++fileCounter];
                                        bytesLeft = (int)file.FileSize;
                                        fs = GetFileStream(file.Name, outputDirectory);
                                        // TODO: can I deduplicate this? probably not since I need breaks
                                        while (bytesLeft < data.Length - tempBytesLeft)
                                        {
                                            fs.Write(data, tempBytesLeft, bytesLeft);
                                            tempBytesLeft += bytesLeft;
                                            fs.Close();

                                            // reached end of folder
                                            if (fileCounter + 1 == files.Length)
                                                break;

                                            file = files[++fileCounter];
                                            bytesLeft = (int)file.FileSize;
                                            fs = GetFileStream(file.Name, outputDirectory);
                                        }

                                        fs.Write(data, tempBytesLeft, data.Length - tempBytesLeft);
                                        bytesLeft -= (data.Length - tempBytesLeft);
                                    }
                                    else // TODO: find something that can actually trigger this case
                                    {
                                        int tempBytesLeft = bytesLeft;
                                        fs.Close();

                                        // reached end of folder
                                        if (fileCounter + 1 == files.Length)
                                            break;

                                        file = files[++fileCounter];
                                        bytesLeft = (int)file.FileSize;
                                        fs = GetFileStream(file.Name, outputDirectory);
                                        while (bytesLeft < data.Length - tempBytesLeft)
                                        {
                                            fs.Write(data, tempBytesLeft, bytesLeft);
                                            tempBytesLeft += bytesLeft;
                                            fs.Close();

                                            // reached end of folder
                                            if (fileCounter + 1 == files.Length)
                                                break;

                                            file = files[++fileCounter];
                                            bytesLeft = (int)file.FileSize;
                                            fs = GetFileStream(file.Name, outputDirectory);
                                        }

                                        fs.Write(data, tempBytesLeft, data.Length - tempBytesLeft);
                                        bytesLeft -= (data.Length - tempBytesLeft);
                                    }
                                    
                                    // Top if block occurs on http://redump.org/disc/107833/ , middle on https://dbox.tools/titles/pc/57520FA0 , bottom still unobserved
                                    // While loop since this also handles 0 byte files. Example file seen in http://redump.org/disc/93312/ , cab Group17.cab, file TRACKSLOC6DYNTEX_BIN
                                    // TODO: make sure that file is actually supposed to be 0 bytes. 7z also extracts it as 0 bytes, so it probably is, but it's good to make sure.
                                    while (bytesLeft == 0)
                                    {
                                        fs.Close();

                                        // reached end of folder
                                        if (fileCounter + 1 == files.Length)
                                            break;

                                        file = files[++fileCounter];
                                        bytesLeft = (int)file.FileSize;
                                        fs = GetFileStream(file.Name, outputDirectory);
                                    }

                                    // TODO: do i ever need to flush before the end of the file?
                                    if (continuedBlock)
                                        j = 0;
                                    
                                    j++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (includeDebug) Console.Error.WriteLine(ex);
                            return false;
                        }
                    }

                    // Move to the next cabinet, if possible
                    cabinet = cabinet.Next;
                    if (cabinet == null) // TODO: handle better
                        return false;

                    currentCabFilename = cabinet.Filename;

                    // TODO: already-extracted data isn't being cleared from memory, at least not nearly enough.
                    if (cabinet.Folders.Length == 0)
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