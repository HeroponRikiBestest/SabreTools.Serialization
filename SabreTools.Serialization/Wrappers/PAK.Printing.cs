using System.Text;
using SabreTools.Data.Extensions;
using SabreTools.Data.Models.PAK;

namespace SabreTools.Serialization.Wrappers
{
    public partial class PAK : IPrintable
    {
#if NETCOREAPP
        /// <inheritdoc/>
        public string ExportJSON() => System.Text.Json.JsonSerializer.Serialize(Model, _jsonSerializerOptions);
#endif

        /// <inheritdoc/>
        public void PrintInformation(StringBuilder builder)
            => Print(builder, Model);

        private static void Print(StringBuilder builder, File file)
        {
            builder.AppendLine("PAK Information:");
            builder.AppendLine("-------------------------");
            builder.AppendLine();

            Print(builder, file.Header);
            Print(builder, file.DirectoryItems);
        }

        private static void Print(StringBuilder builder, Header? header)
        {
            builder.AppendLine("  Header Information:");
            builder.AppendLine("  -------------------------");
            if (header == null)
            {
                builder.AppendLine("  No header");
                builder.AppendLine();
                return;
            }

            builder.AppendLine(header.Signature, "  Signature");
            builder.AppendLine(header.DirectoryOffset, "  Directory offset");
            builder.AppendLine(header.DirectoryLength, "  Directory length");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, DirectoryItem[]? entries)
        {
            builder.AppendLine("  Directory Items Information:");
            builder.AppendLine("  -------------------------");
            if (entries == null || entries.Length == 0)
            {
                builder.AppendLine("  No directory items");
                builder.AppendLine();
                return;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];

                builder.AppendLine($"  Directory Item {i}");
                builder.AppendLine(entry.ItemName, "    Item name");
                builder.AppendLine(entry.ItemOffset, "    Item offset");
                builder.AppendLine(entry.ItemLength, "    Item length");
            }

            builder.AppendLine();
        }
    }
}
