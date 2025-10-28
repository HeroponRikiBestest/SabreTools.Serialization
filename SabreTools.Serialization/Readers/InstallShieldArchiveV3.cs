﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using SabreTools.Data.Models.InstallShieldArchiveV3;
using SabreTools.IO.Extensions;

namespace SabreTools.Serialization.Readers
{
    public class InstallShieldArchiveV3 : BaseBinaryReader<Archive>
    {
        public override Archive? Deserialize(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                // Cache the current offset
                long initialOffset = data.Position;

                // Create a new archive to fill
                var archive = new Archive();

                #region Header

                // Try to parse the header
                var header = ParseHeader(data);
                if (header.Signature1 != Constants.HeaderSignature)
                    return null;
                if (initialOffset + header.TocAddress >= data.Length)
                    return null;

                // Set the archive header
                archive.Header = header;

                #endregion

                #region Directories

                // Get the directories offset
                long directoriesOffset = initialOffset + header.TocAddress;
                if (directoriesOffset < initialOffset || directoriesOffset >= data.Length)
                    return null;

                // Seek to the directories
                data.SeekIfPossible(directoriesOffset, SeekOrigin.Begin);

                // Try to parse the directories
                var directories = new List<Data.Models.InstallShieldArchiveV3.Directory>();
                for (int i = 0; i < header.DirCount; i++)
                {
                    var directory = ParseDirectory(data);
                    directories.Add(directory);
                    data.SeekIfPossible(directory.ChunkSize - directory.Name!.Length - 6, SeekOrigin.Current);
                }

                // Set the directories
                archive.Directories = [.. directories];

                #endregion

                #region Files

                // Try to parse the files
                var files = new List<Data.Models.InstallShieldArchiveV3.File>();
                for (int i = 0; i < archive.Directories.Length; i++)
                {
                    var directory = archive.Directories[i];
                    for (int j = 0; j < directory.FileCount; j++)
                    {
                        var file = ParseFile(data);
                        files.Add(file);
                        data.SeekIfPossible(file.ChunkSize - file.Name!.Length - 30, SeekOrigin.Current);
                    }
                }

                // Set the files
                archive.Files = [.. files];

                #endregion

                return archive;
            }
            catch
            {
                // Ignore the actual error
                return null;
            }
        }

        /// <summary>
        /// Parse a Stream into a Directory
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled Directory on success, null on error</returns>
        public static Data.Models.InstallShieldArchiveV3.Directory ParseDirectory(Stream data)
        {
            var obj = new Data.Models.InstallShieldArchiveV3.Directory();

            obj.FileCount = data.ReadUInt16LittleEndian();
            obj.ChunkSize = data.ReadUInt16LittleEndian();

            ushort nameLength = data.ReadUInt16LittleEndian();
            byte[] nameBytes = data.ReadBytes(nameLength);
            obj.Name = Encoding.ASCII.GetString(nameBytes);

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a File
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled File on success, null on error</returns>
        public static Data.Models.InstallShieldArchiveV3.File ParseFile(Stream data)
        {
            var obj = new Data.Models.InstallShieldArchiveV3.File();

            obj.VolumeEnd = data.ReadByteValue();
            obj.Index = data.ReadUInt16LittleEndian();
            obj.UncompressedSize = data.ReadUInt32LittleEndian();
            obj.CompressedSize = data.ReadUInt32LittleEndian();
            obj.Offset = data.ReadUInt32LittleEndian();
            obj.DateTime = data.ReadUInt32LittleEndian();
            obj.Reserved0 = data.ReadUInt32LittleEndian();
            obj.ChunkSize = data.ReadUInt16LittleEndian();
            obj.Attrib = (Data.Models.InstallShieldArchiveV3.Attributes)data.ReadByteValue();
            obj.IsSplit = data.ReadByteValue();
            obj.Reserved1 = data.ReadByteValue();
            obj.VolumeStart = data.ReadByteValue();
            obj.Name = data.ReadPrefixedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a Header
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled Header on success, null on error</returns>
        public static Header ParseHeader(Stream data)
        {
            var obj = new Header();

            obj.Signature1 = data.ReadUInt32LittleEndian();
            obj.Signature2 = data.ReadUInt32LittleEndian();
            obj.Reserved0 = data.ReadUInt16LittleEndian();
            obj.IsMultivolume = data.ReadUInt16LittleEndian();
            obj.FileCount = data.ReadUInt16LittleEndian();
            obj.DateTime = data.ReadUInt32LittleEndian();
            obj.CompressedSize = data.ReadUInt32LittleEndian();
            obj.UncompressedSize = data.ReadUInt32LittleEndian();
            obj.Reserved1 = data.ReadUInt32LittleEndian();
            obj.VolumeTotal = data.ReadByteValue();
            obj.VolumeNumber = data.ReadByteValue();
            obj.Reserved2 = data.ReadByteValue();
            obj.SplitBeginAddress = data.ReadUInt32LittleEndian();
            obj.SplitEndAddress = data.ReadUInt32LittleEndian();
            obj.TocAddress = data.ReadUInt32LittleEndian();
            obj.Reserved3 = data.ReadUInt32LittleEndian();
            obj.DirCount = data.ReadUInt16LittleEndian();
            obj.Reserved4 = data.ReadUInt32LittleEndian();
            obj.Reserved5 = data.ReadUInt32LittleEndian();

            return obj;
        }
    }
}
