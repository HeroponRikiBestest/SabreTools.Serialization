using Newtonsoft.Json.Linq;

namespace SabreTools.Data.Models.VDF
{
    /// <summary>
    /// Valve Data File
    /// </summary>
    /// <remarks>
    /// Valve's json-like format, used for a variety of things across Steam.
    /// </remarks>
    /// <see href="https://github.com/ValveResourceFormat/ValveKeyValue"/>
    /// <see href="https://developer.valvesoftware.com/wiki/VDF"/>
    public class File
    {   
        /// <summary>
        /// A byte array representing the signature/top level item.
        /// </summary>
        public byte[]? Signature { get; set; }
        
        /// <summary>
        /// A JSON Object representing the VDF structure.
        /// </summary>
        public JObject? VDFObject { get; set; }
    }
}
