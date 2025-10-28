using System.IO;
using System.Text;
using SabreTools.Data.Models.PlayJ;
using SabreTools.IO.Extensions;
using static SabreTools.Data.Models.PlayJ.Constants;

namespace SabreTools.Serialization.Readers
{
    public class PlayJAudio : BaseBinaryReader<AudioFile>
    {
        /// <inheritdoc/>
        public override AudioFile? Deserialize(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                // Cache the current offset
                long initialOffset = data.Position;

                // Create a new audio file to fill
                var audioFile = new AudioFile();

                #region Audio Header

                // Try to parse the audio header
                var audioHeader = ParseAudioHeader(data);
                if (audioHeader == null)
                    return null;

                // Set the audio header
                audioFile.Header = audioHeader;

                #endregion

                #region Unknown Block 1

                uint unknownOffset1 = (audioHeader.Version == 0x00000000)
                    ? (audioHeader as AudioHeaderV1)?.UnknownOffset1 ?? 0
                    : ((audioHeader as AudioHeaderV2)?.UnknownOffset1 ?? 0) + 0x54;

                // If we have an unknown block 1 offset
                if (unknownOffset1 > 0)
                {
                    // Get the unknown block 1 offset
                    long offset = initialOffset + unknownOffset1;
                    if (offset < initialOffset || offset >= data.Length)
                        return null;

                    // Seek to the unknown block 1
                    data.SeekIfPossible(offset, SeekOrigin.Begin);
                }

                // Try to parse the unknown block 1
                var unknownBlock1 = ParseUnknownBlock1(data);
                if (unknownBlock1 == null)
                    return null;

                // Set the unknown block 1
                audioFile.UnknownBlock1 = unknownBlock1;

                #endregion

                #region V1 Only

                // If we have a V1 file
                if (audioHeader.Version == 0x00000000)
                {
                    #region Unknown Value 2

                    // Get the V1 unknown offset 2
                    uint? unknownOffset2 = (audioHeader as AudioHeaderV1)?.UnknownOffset2;

                    // If we have an unknown value 2 offset
                    if (unknownOffset2 != null && unknownOffset2 > 0)
                    {
                        // Get the unknown value 2 offset
                        long offset = initialOffset + unknownOffset2.Value;
                        if (offset < initialOffset || offset >= data.Length)
                            return null;

                        // Seek to the unknown value 2
                        data.SeekIfPossible(offset, SeekOrigin.Begin);
                    }

                    // Set the unknown value 2
                    audioFile.UnknownValue2 = data.ReadUInt32LittleEndian();

                    #endregion

                    #region Unknown Block 3

                    // Get the V1 unknown offset 3
                    uint? unknownOffset3 = (audioHeader as AudioHeaderV1)?.UnknownOffset3;

                    // If we have an unknown block 3 offset
                    if (unknownOffset3 != null && unknownOffset3 > 0)
                    {
                        // Get the unknown block 3 offset
                        long offset = initialOffset + unknownOffset3.Value;
                        if (offset < initialOffset || offset >= data.Length)
                            return null;

                        // Seek to the unknown block 3
                        data.SeekIfPossible(offset, SeekOrigin.Begin);
                    }

                    // Try to parse the unknown block 3
                    var unknownBlock3 = ParseUnknownBlock3(data);
                    if (unknownBlock3 == null)
                        return null;

                    // Set the unknown block 3
                    audioFile.UnknownBlock3 = unknownBlock3;

                    #endregion
                }

                #endregion

                #region V2 Only

                // If we have a V2 file
                if (audioHeader.Version == 0x0000000A)
                {
                    #region Data Files Count

                    // Set the data files count
                    audioFile.DataFilesCount = data.ReadUInt32LittleEndian();

                    #endregion

                    #region Data Files

                    // Create the data files array
                    audioFile.DataFiles = new DataFile[audioFile.DataFilesCount];

                    // Try to parse the data files
                    for (int i = 0; i < audioFile.DataFiles.Length; i++)
                    {
                        var dataFile = ParseDataFile(data);
                        if (dataFile == null)
                            return null;

                        audioFile.DataFiles[i] = dataFile;
                    }

                    #endregion
                }

                #endregion

                return audioFile;
            }
            catch
            {
                // Ignore the actual error
                return null;
            }
        }

        /// <summary>
        /// Parse a Stream into an audio header
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled audio header on success, null on error</returns>
        private static AudioHeader? ParseAudioHeader(Stream data)
        {
            // Cache the current offset
            long initialOffset = data.Position;

            AudioHeader audioHeader;

            // Get the common header pieces
            uint signature = data.ReadUInt32LittleEndian();
            if (signature != SignatureUInt32)
                return null;

            uint version = data.ReadUInt32LittleEndian();

            // Build the header according to version
            uint unknownOffset1;
            switch (version)
            {
                // Version 1
                case 0x00000000:
                    AudioHeaderV1 v1 = new AudioHeaderV1();

                    v1.Signature = signature;
                    v1.Version = version;
                    v1.TrackID = data.ReadUInt32LittleEndian();
                    v1.UnknownOffset1 = data.ReadUInt32LittleEndian();
                    v1.UnknownOffset2 = data.ReadUInt32LittleEndian();
                    v1.UnknownOffset3 = data.ReadUInt32LittleEndian();
                    v1.Unknown1 = data.ReadUInt32LittleEndian();
                    v1.Unknown2 = data.ReadUInt32LittleEndian();
                    v1.Year = data.ReadUInt32LittleEndian();
                    v1.TrackNumber = data.ReadByteValue();
                    v1.Subgenre = (Subgenre)data.ReadByteValue();
                    v1.Duration = data.ReadUInt32LittleEndian();

                    audioHeader = v1;
                    unknownOffset1 = v1.UnknownOffset1;
                    break;

                // Version 2
                case 0x0000000A:
                    AudioHeaderV2 v2 = new AudioHeaderV2();

                    v2.Signature = signature;
                    v2.Version = version;
                    v2.Unknown1 = data.ReadUInt32LittleEndian();
                    v2.Unknown2 = data.ReadUInt32LittleEndian();
                    v2.Unknown3 = data.ReadUInt32LittleEndian();
                    v2.Unknown4 = data.ReadUInt32LittleEndian();
                    v2.Unknown5 = data.ReadUInt32LittleEndian();
                    v2.Unknown6 = data.ReadUInt32LittleEndian();
                    v2.UnknownOffset1 = data.ReadUInt32LittleEndian();
                    v2.Unknown7 = data.ReadUInt32LittleEndian();
                    v2.Unknown8 = data.ReadUInt32LittleEndian();
                    v2.Unknown9 = data.ReadUInt32LittleEndian();
                    v2.UnknownOffset2 = data.ReadUInt32LittleEndian();
                    v2.Unknown10 = data.ReadUInt32LittleEndian();
                    v2.Unknown11 = data.ReadUInt32LittleEndian();
                    v2.Unknown12 = data.ReadUInt32LittleEndian();
                    v2.Unknown13 = data.ReadUInt32LittleEndian();
                    v2.Unknown14 = data.ReadUInt32LittleEndian();
                    v2.Unknown15 = data.ReadUInt32LittleEndian();
                    v2.Unknown16 = data.ReadUInt32LittleEndian();
                    v2.Unknown17 = data.ReadUInt32LittleEndian();
                    v2.TrackID = data.ReadUInt32LittleEndian();
                    v2.Year = data.ReadUInt32LittleEndian();
                    v2.TrackNumber = data.ReadUInt32LittleEndian();
                    v2.Unknown18 = data.ReadUInt32LittleEndian();

                    audioHeader = v2;
                    unknownOffset1 = v2.UnknownOffset1 + 0x54;
                    break;

                // No other version are recognized
                default:
                    return null;
            }

            audioHeader.TrackLength = data.ReadUInt16LittleEndian();
            byte[] track = data.ReadBytes(audioHeader.TrackLength);
            audioHeader.Track = Encoding.ASCII.GetString(track);

            audioHeader.ArtistLength = data.ReadUInt16LittleEndian();
            byte[] artist = data.ReadBytes(audioHeader.ArtistLength);
            audioHeader.Artist = Encoding.ASCII.GetString(artist);

            audioHeader.AlbumLength = data.ReadUInt16LittleEndian();
            byte[] album = data.ReadBytes(audioHeader.AlbumLength);
            audioHeader.Album = Encoding.ASCII.GetString(album);

            audioHeader.WriterLength = data.ReadUInt16LittleEndian();
            byte[] writer = data.ReadBytes(audioHeader.WriterLength);
            audioHeader.Writer = Encoding.ASCII.GetString(writer);

            audioHeader.PublisherLength = data.ReadUInt16LittleEndian();
            byte[] publisher = data.ReadBytes(audioHeader.PublisherLength);
            audioHeader.Publisher = Encoding.ASCII.GetString(publisher);

            audioHeader.LabelLength = data.ReadUInt16LittleEndian();
            byte[] label = data.ReadBytes(audioHeader.LabelLength);
            audioHeader.Label = Encoding.ASCII.GetString(label);

            if (data.Position - initialOffset < unknownOffset1)
            {
                audioHeader.CommentsLength = data.ReadUInt16LittleEndian();
                byte[] comments = data.ReadBytes(audioHeader.CommentsLength);
                audioHeader.Comments = Encoding.ASCII.GetString(comments);
            }

            return audioHeader;
        }

        /// <summary>
        /// Parse a Stream into an unknown block 1
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled unknown block 1 on success, null on error</returns>
        private static UnknownBlock1 ParseUnknownBlock1(Stream data)
        {
            var unknownBlock1 = new UnknownBlock1();

            unknownBlock1.Length = data.ReadUInt32LittleEndian();
            unknownBlock1.Data = data.ReadBytes((int)unknownBlock1.Length);

            return unknownBlock1;
        }

        /// <summary>
        /// Parse a Stream into an unknown block 3
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled unknown block 3 on success, null on error</returns>
        private static UnknownBlock3 ParseUnknownBlock3(Stream data)
        {
            var unknownBlock3 = new UnknownBlock3();

            // No-op because we don't even know the length

            return unknownBlock3;
        }

        /// <summary>
        /// Parse a Stream into a data file
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled data file on success, null on error</returns>
        private static DataFile ParseDataFile(Stream data)
        {
            var dataFile = new DataFile();

            dataFile.FileNameLength = data.ReadUInt16LittleEndian();
            byte[] fileName = data.ReadBytes(dataFile.FileNameLength);
            dataFile.FileName = Encoding.ASCII.GetString(fileName);
            dataFile.DataLength = data.ReadUInt32LittleEndian();
            dataFile.Data = data.ReadBytes((int)dataFile.DataLength);

            return dataFile;
        }
    }
}
