﻿using System.IO;
using System.Text;
using Newtonsoft.Json;
using SabreTools.IO.Extensions;

namespace SabreTools.Serialization.Writers
{
    /// <summary>
    /// Base class for other JSON serializers
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JsonFile<T> : BaseBinaryWriter<T>
    {
        #region IByteWriter

        /// <inheritdoc/>
        public override byte[]? SerializeArray(T? obj)
            => SerializeArray(obj, new UTF8Encoding(false));

        /// <summary>
        /// Serialize a <typeparamref name="T"/> into a byte array
        /// </summary>
        /// <typeparam name="T">Type of object to serialize from</typeparam>
        /// <param name="obj">Data to serialize</param>
        /// <param name="encoding">Encoding to parse text as</param>
        /// <returns>Filled object on success, null on error</returns>
        public byte[]? SerializeArray(T? obj, Encoding encoding)
        {
            using var stream = Serialize(obj, encoding);
            if (stream == null)
                return null;

            byte[] bytes = new byte[stream.Length];
            int read = stream.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        #endregion

        #region IFileWriter

        /// <inheritdoc/>
        public override bool SerializeFile(T? obj, string? path)
            => Serialize(obj, path, new UTF8Encoding(false));

        /// <summary>
        /// Serialize a <typeparamref name="T"/> into a file
        /// </summary>
        /// <typeparam name="T">Type of object to serialize from</typeparam>
        /// <param name="obj">Data to serialize</param>
        /// <param name="path">Path to the file to serialize to</param>
        /// <param name="encoding">Encoding to parse text as</param>
        /// <returns>True on successful serialization, false otherwise</returns>
        public bool Serialize(T? obj, string? path, Encoding encoding)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            using var stream = Serialize(obj, encoding);
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
        public override Stream? SerializeStream(T? obj)
            => Serialize(obj, new UTF8Encoding(false));

        /// <summary>
        /// Serialize a <typeparamref name="T"/> into a Stream
        /// </summary>
        /// <typeparam name="T">Type of object to serialize from</typeparam>
        /// <param name="obj">Data to serialize</param>
        /// <param name="encoding"></param>
        /// <returns>Filled object on success, null on error</returns>
        public Stream? Serialize(T? obj, Encoding encoding)
        {
            // If the object is null
            if (obj == null)
                return null;

            // Setup the serializer and the writer
            var serializer = JsonSerializer.Create();
            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream, encoding);
            var jsonWriter = new JsonTextWriter(streamWriter);

            // Perform the deserialization and return
            serializer.Serialize(jsonWriter, obj);
            stream.SeekIfPossible(0, SeekOrigin.Begin);
            return stream;
        }

        #endregion
    }
}
