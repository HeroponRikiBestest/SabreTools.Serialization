#if NET6_0_OR_GREATER
using System.Text.Json.Nodes;

namespace SabreTools.Data.Models.VDF
{
    /// <summary>
    /// Valve Data File
    /// </summary>
    /// <see href="https://github.com/ValveResourceFormat/ValveKeyValue"/>
    /// <see href="https://developer.valvesoftware.com/wiki/VDF"/>
    public class File
    {   
        /// <summary>
        /// A byte array representing the signature/top level item
        /// </summary>
        public byte[]? Signature { get; set; }
        
        /// <summary>
        /// A JSON Object representing the VDF structure.
        /// </summary>
        public JsonObject? VDFObject { get; set; }
    }
}
#endif