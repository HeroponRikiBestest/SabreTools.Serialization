﻿using System;
using System.IO;
using SabreTools.IO.Extensions;
using SabreTools.Matching;
using SabreTools.Serialization.Interfaces;

namespace SabreTools.Serialization.Wrappers
{
    public partial class NewExecutable : IExtractable
    {
        /// <inheritdoc/>
        /// <remarks>
        /// This extracts the following data:
        /// - Archives and executables in the overlay
        /// - Wise installers
        /// </remarks>
        public bool Extract(string outputDirectory, bool includeDebug)
        {
            bool overlay = ExtractFromOverlay(outputDirectory, includeDebug);
            bool wise = ExtractWise(outputDirectory, includeDebug);

            return overlay | wise;
        }

        /// <summary>
        /// Extract data from the overlay
        /// </summary>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if extraction succeeded, false otherwise</returns>
        public bool ExtractFromOverlay(string outputDirectory, bool includeDebug)
        {
            try
            {
                // Cache the overlay data for easier reading
                var overlayData = OverlayData;
                if (overlayData.Length == 0)
                    return false;

                // Set the output variables
                int overlayOffset = 0;
                string extension = string.Empty;

                // Only process the overlay if it is recognized
                for (; overlayOffset < 0x400 && overlayOffset < overlayData.Length; overlayOffset++)
                {
                    int temp = overlayOffset;
                    byte[] overlaySample = overlayData.ReadBytes(ref temp, 0x10);

                    if (overlaySample.StartsWith([0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C]))
                    {
                        extension = "7z";
                        break;
                    }
                    else if (overlaySample.StartsWith([0x3B, 0x21, 0x40, 0x49, 0x6E, 0x73, 0x74, 0x61, 0x6C, 0x6C]))
                    {
                        // 7-zip SFX script -- ";!@Install" to ";!@InstallEnd@!"
                        overlayOffset = overlayData.FirstPosition([0x3B, 0x21, 0x40, 0x49, 0x6E, 0x73, 0x74, 0x61, 0x6C, 0x6C, 0x45, 0x6E, 0x64, 0x40, 0x21]);
                        if (overlayOffset == -1)
                            return false;

                        overlayOffset += 15;
                        extension = "7z";
                        break;
                    }
                    else if (overlaySample.StartsWith([0x42, 0x5A, 0x68]))
                    {
                        extension = "bz2";
                        break;
                    }
                    else if (overlaySample.StartsWith([0x1F, 0x8B]))
                    {
                        extension = "gz";
                        break;
                    }
                    else if (overlaySample.StartsWith(Models.MicrosoftCabinet.Constants.SignatureBytes))
                    {
                        extension = "cab";
                        break;
                    }
                    else if (overlaySample.StartsWith(Models.PKZIP.Constants.LocalFileHeaderSignatureBytes))
                    {
                        extension = "zip";
                        break;
                    }
                    else if (overlaySample.StartsWith(Models.PKZIP.Constants.EndOfCentralDirectoryRecordSignatureBytes))
                    {
                        extension = "zip";
                        break;
                    }
                    else if (overlaySample.StartsWith(Models.PKZIP.Constants.EndOfCentralDirectoryRecord64SignatureBytes))
                    {
                        extension = "zip";
                        break;
                    }
                    else if (overlaySample.StartsWith(Models.PKZIP.Constants.DataDescriptorSignatureBytes))
                    {
                        extension = "zip";
                        break;
                    }
                    else if (overlaySample.StartsWith([0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00]))
                    {
                        extension = "rar";
                        break;
                    }
                    else if (overlaySample.StartsWith([0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00]))
                    {
                        extension = "rar";
                        break;
                    }
                    else if (overlaySample.StartsWith([0x55, 0x48, 0x41, 0x06]))
                    {
                        extension = "uha";
                        break;
                    }
                    else if (overlaySample.StartsWith([0xFD, 0x37, 0x7A, 0x58, 0x5A, 0x00]))
                    {
                        extension = "xz";
                        break;
                    }
                    else if (overlaySample.StartsWith(Models.MSDOS.Constants.SignatureBytes))
                    {
                        extension = "bin"; // exe/dll
                        break;
                    }
                }

                // If the extension is unset
                if (extension.Length == 0)
                    return false;

                // Create the temp filename
                string tempFile = $"embedded_overlay.{extension}";
                if (Filename != null)
                    tempFile = $"{Path.GetFileName(Filename)}-{tempFile}";

                tempFile = Path.Combine(outputDirectory, tempFile);
                var directoryName = Path.GetDirectoryName(tempFile);
                if (directoryName != null && !Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);

                // Write the resource data to a temp file
                using var tempStream = File.Open(tempFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                tempStream?.Write(overlayData, overlayOffset, overlayData.Length - overlayOffset);

                return true;
            }
            catch (Exception ex)
            {
                if (includeDebug) Console.Error.WriteLine(ex);
                return false;
            }
        }

        /// <summary>
        /// Extract data from a Wise installer
        /// </summary>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if extraction succeeded, false otherwise</returns>
        public bool ExtractWise(string outputDirectory, bool includeDebug)
        {
            // Get the source data for reading
            Stream source = _dataSource;
            if (Filename != null)
            {
                // Try to open a multipart file
                if (WiseOverlayHeader.OpenFile(Filename, includeDebug, out var temp) && temp != null)
                    source = temp;
            }

            // Try to find the overlay header
            long offset = FindWiseOverlayHeader();
            if (offset > 0 && offset < Length)
                return ExtractWiseOverlay(outputDirectory, includeDebug, source, offset);

            // Everything else could not extract
            return false;
        }

        /// <summary>
        /// Extract using Wise overlay
        /// </summary>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <param name="source">Potentially multi-part stream to read</param>
        /// <param name="offset">Offset to the start of the overlay header</param>
        /// <returns>True if extraction succeeded, false otherwise</returns>
        private bool ExtractWiseOverlay(string outputDirectory, bool includeDebug, Stream source, long offset)
        {
            // Seek to the overlay and parse
            source.Seek(offset, SeekOrigin.Begin);
            var header = WiseOverlayHeader.Create(source);
            if (header == null)
            {
                if (includeDebug) Console.Error.WriteLine("Could not parse the overlay header");
                return false;
            }

            // Extract the header-defined files
            bool extracted = header.ExtractHeaderDefinedFiles(outputDirectory, includeDebug);
            if (!extracted)
            {
                if (includeDebug) Console.Error.WriteLine("Could not extract header-defined files");
                return false;
            }

            // Open the script file from the output directory
            var scriptStream = File.OpenRead(Path.Combine(outputDirectory, "WiseScript.bin"));
            var script = WiseScript.Create(scriptStream);
            if (script == null)
            {
                if (includeDebug) Console.Error.WriteLine("Could not parse WiseScript.bin");
                return false;
            }

            // Get the source directory
            string? sourceDirectory = null;
            if (Filename != null)
                sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(Filename));

            // Process the state machine
            return script.ProcessStateMachine(header, sourceDirectory, outputDirectory, includeDebug);
        }
    }
}
