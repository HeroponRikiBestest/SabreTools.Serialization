using System.IO;
using System.Text;
using SabreTools.Data.Models.AttractMode;
using SabreTools.IO.Extensions;
using SabreTools.IO.Writers;

namespace SabreTools.Serialization.Writers
{
    public class AttractMode : BaseBinaryWriter<MetadataFile>
    {
        #region Constants

        public const string HeaderWithoutRomname = "#Name;Title;Emulator;CloneOf;Year;Manufacturer;Category;Players;Rotation;Control;Status;DisplayCount;DisplayType;AltRomname;AltTitle;Extra;Buttons";

        public static readonly string[] HeaderArrayWithoutRomname =
        [
            "#Name",
            "Title",
            "Emulator",
            "CloneOf",
            "Year",
            "Manufacturer",
            "Category",
            "Players",
            "Rotation",
            "Control",
            "Status",
            "DisplayCount",
            "DisplayType",
            "AltRomname",
            "AltTitle",
            "Extra",
            "Buttons"
        ];

        public const string HeaderWithRomname = "#Romname;Title;Emulator;Cloneof;Year;Manufacturer;Category;Players;Rotation;Control;Status;DisplayCount;DisplayType;AltRomname;AltTitle;Extra;Buttons;Favourite;Tags;PlayedCount;PlayedTime;FileIsAvailable";

        public static readonly string[] HeaderArrayWithRomname =
        [
            "#Romname",
            "Title",
            "Emulator",
            "Cloneof",
            "Year",
            "Manufacturer",
            "Category",
            "Players",
            "Rotation",
            "Control",
            "Status",
            "DisplayCount",
            "DisplayType",
            "AltRomname",
            "AltTitle",
            "Extra",
            "Buttons",
            "Favourite",
            "Tags",
            "PlayedCount",
            "PlayedTime",
            "FileIsAvailable"
        ];

        #endregion

        #region IByteWriter

        /// <inheritdoc/>
        public override byte[]? SerializeArray(MetadataFile? obj)
            => SerializeArray(obj, false);

        /// <inheritdoc/>
        public byte[]? SerializeArray(MetadataFile? obj, bool longHeader)
        {
            using var stream = SerializeStream(obj, longHeader);
            if (stream == null)
                return null;

            byte[] bytes = new byte[stream.Length];
            int read = stream.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        #endregion

        #region IFileWriter

        /// <inheritdoc/>
        public override bool SerializeFile(MetadataFile? obj, string? path)
            => SerializeFile(obj, path, false);

        /// <inheritdoc/>
        public bool SerializeFile(MetadataFile? obj, string? path, bool longHeader)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            using var stream = SerializeStream(obj, longHeader);
            if (stream == null)
                return false;

            using var fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
            stream.CopyTo(fs);
            fs.Flush();

            return true;
        }

        #endregion

        #region IStreamWriter

        /// <inheritdoc/>
        public override Stream? SerializeStream(MetadataFile? obj)
            => SerializeStream(obj, false);

        /// <inheritdoc/>
        public Stream? SerializeStream(MetadataFile? obj, bool longHeader)
        {
            // If the metadata file is null
            if (obj == null)
                return null;

            // Setup the writer and output
            var stream = new MemoryStream();
            var writer = new SeparatedValueWriter(stream, Encoding.UTF8)
            {
                Separator = ';',
                Quotes = false,
                VerifyFieldCount = false,
            };

            // Write the header
            writer.WriteValues(longHeader ? HeaderArrayWithRomname : HeaderArrayWithoutRomname);

            // Write out the rows, if they exist
            WriteRows(obj.Row, writer, longHeader);

            // Return the stream
            stream.SeekIfPossible(0, SeekOrigin.Begin);
            return stream;
        }

        /// <summary>
        /// Write rows information to the current writer
        /// </summary>
        /// <param name="rows">Array of Row objects representing the rows information</param>
        /// <param name="writer">SeparatedValueWriter representing the output</param>
        /// <param name="longHeader">True if the long variant of the row should be written, false otherwise</param>
        private static void WriteRows(Row[]? rows, SeparatedValueWriter writer, bool longHeader)
        {
            // If the games information is missing, we can't do anything
            if (rows == null || rows.Length == 0)
                return;

            // Loop through and write out the rows
            foreach (var row in rows)
            {
                if (row == null)
                    continue;

                string?[] rowArray;
                if (longHeader)
                {
                    rowArray =
                    [
                        row.Name,
                        row.Title,
                        row.Emulator,
                        row.CloneOf,
                        row.Year,
                        row.Manufacturer,
                        row.Category,
                        row.Players,
                        row.Rotation,
                        row.Control,
                        row.Status,
                        row.DisplayCount,
                        row.DisplayType,
                        row.AltRomname,
                        row.AltTitle,
                        row.Extra,
                        row.Buttons,
                        row.Favorite,
                        row.Tags,
                        row.PlayedCount,
                        row.PlayedTime,
                        row.FileIsAvailable,
                    ];
                }
                else
                {
                    rowArray =
                    [
                        row.Name,
                        row.Title,
                        row.Emulator,
                        row.CloneOf,
                        row.Year,
                        row.Manufacturer,
                        row.Category,
                        row.Players,
                        row.Rotation,
                        row.Control,
                        row.Status,
                        row.DisplayCount,
                        row.DisplayType,
                        row.AltRomname,
                        row.AltTitle,
                        row.Extra,
                        row.Buttons,
                    ];
                }

                writer.WriteValues(rowArray);
                writer.Flush();
            }
        }

        #endregion
    }
}
