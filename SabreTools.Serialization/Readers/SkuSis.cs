using System;
using System.IO;
using SabreTools.IO.Extensions;
using File = SabreTools.Data.Models.VDF.File;
using static SabreTools.Data.Models.VDF.Constants;
using System.Text;
using Newtonsoft.Json.Linq;

namespace SabreTools.Serialization.Readers
{
    public class SkuSis : BaseBinaryReader<File>
    {
        /// <inheritdoc/>
        public override File? Deserialize(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                // Cache the current offset
                long initialOffset = data.Position;

                // Check if file contains the top level sku value, otherwise return null
                var signatureBytes = data.ReadBytes(5);
                if (!signatureBytes.EqualsExactly(Steam2SisSignatureBytes)
                    && !signatureBytes.EqualsExactly(Steam3SisSignatureBytes))
                    return null;
                
                data.SeekIfPossible(initialOffset, SeekOrigin.Begin);

                var skuSis = ParseSkuSis(data);
                if (skuSis == null)
                    return null;

                if (skuSis.VDFObject == null)
                    return null;

                skuSis.Signature = signatureBytes;
                
                return skuSis;
            }
            catch
            {
                // Ignore the actual error
                return null;
            }
        }

        /// <summary>
        /// Parse a Stream into a Header
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled Header on success, null on error</returns>
        public static File? ParseSkuSis(Stream data)
        {
            var obj = new File();

            string json = "{\n";
            string delimiter = "\"\t\t\"";
            string? line;
            var reader = new StreamReader(data, Encoding.ASCII);

            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                if (line == null)
                    continue;
                
                if (line.Contains("{"))
                {
                    json += "{\n";
                    continue;
                }
                else if (line.Contains("}"))
                {
                    json += line;
                    json += ",\n";
                    continue;
                }
    
                int index = line.IndexOf(delimiter, StringComparison.Ordinal);
                if (index <= -1) // Array
                {
                    json += line;
                    json += ": ";
                }
                else
                {
                    json += line.Replace(delimiter, "\": \"");
                    json += ",\n";
                }
            }
            
            json += "\n}";
            obj.VDFObject = JObject.Parse(json);

            return obj;
        }
    }
}
