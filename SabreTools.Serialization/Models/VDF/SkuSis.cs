using System.Collections.Generic;
using Newtonsoft.Json;
using SabreTools.Data.Models.ISO9660;

namespace SabreTools.Data.Models.VDF
{
    /// <summary>
    /// Contains metadata information about retail Steam discs
    /// Stored in a VDF file on the disc
    /// </summary>
    /// <remarks>Stored in the order it appears in the sku sis file, as it is always the same order.</remarks>
    [JsonObject]
    public class SkuSis
    {

        // TODO: the only ones that matter for my PR here are, as follows:
        // SKU
        // sku
        // apps/Apps
        // depots
        // manifests
        // all others do not matter at all.
        #region Not Numbered

        /// <summary>
        /// "sku"
        /// Top-level value for sim/sid sku.sis files.
        /// Known values: the entire sku.sis object
        /// </summary>
        /// <remarks>sim/sid only</remarks>
        [JsonProperty("SKU", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object>? SKU { get; set; }

        /// <summary>
        /// "sku"
        /// Top-level value for csm/csd sku.sis files.
        /// Known values: the entire sku.sis object
        /// </summary>
        /// <remarks>csm/csd only</remarks>
        [JsonProperty("sku", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object>? sku { get; set; }

        /// <summary>
        /// "name"
        /// Name of the disc/app
        /// Known values: Arbitrary string
        /// </summary>
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string? name { get; set; }

        /// <summary>
        /// "productname"
        /// productname of the retail installer
        /// Known values: Arbitrary string
        /// </summary>
        /// <remarks>sim/sid only</remarks>
        [JsonProperty("productname", NullValueHandling = NullValueHandling.Ignore)]
        public string? productname { get; set; }

        /// <summary>
        /// "subscriptionID"
        /// subscriptionID of the retail installer
        /// Known values: Arbitrary number
        /// </summary>
        /// <remarks>sim/sid only</remarks>
        [JsonProperty("subscriptionID", NullValueHandling = NullValueHandling.Ignore)]
        public long? subscriptionID { get; set; }

        // Both are used interchangeably, but never at the same time
        /// <summary>
        /// "AppID"
        /// AppID of the retail installer
        /// Known values: Arbitrary number
        /// </summary>
        /// <remarks>sim/sid only</remarks>
        [JsonProperty("AppID", NullValueHandling = NullValueHandling.Ignore)]
        public long? AppID { get; set; }

        /// <summary>
        /// "appID"
        /// appID of the retail installer
        /// Known values: Arbitrary number
        /// </summary>
        /// <remarks>sim/sid only</remarks>
        [JsonProperty("appID", NullValueHandling = NullValueHandling.Ignore)]
        public long? appID { get; set; }

        /// <summary>
        /// "disks"
        /// Number of discs of the retail installer set
        /// Known values: 1-5? 10? Unsure what the most discs in a steam retail installer is currently known to be
        /// </summary>
        [JsonProperty("disks", NullValueHandling = NullValueHandling.Ignore)]
        public uint? disks { get; set; }

        /// <summary>
        /// "language"
        /// language of the retail installer
        /// Known values: english, russian
        /// </summary>
        /// <remarks>sim/sid only</remarks>
        [JsonProperty("language", NullValueHandling = NullValueHandling.Ignore)]
        public string? language { get; set; }

        /// <summary>
        /// "disk"
        /// Numbered disk of the retail installer set
        /// Known values: 1-5? 10? Unsure what the most discs in a steam retail installer is currently known to be
        /// </summary>
        /// <remarks>csm/csd only</remarks>
        [JsonProperty("disk", NullValueHandling = NullValueHandling.Ignore)]
        public uint? disk { get; set; }

        /// <summary>
        /// "backup"
        /// Unknown. This is probably a boolean?
        /// Known values: 0
        /// </summary>
        [JsonProperty("backup", NullValueHandling = NullValueHandling.Ignore)]
        public uint? backup { get; set; }

        /// <summary>
        /// "contenttype"
        /// Unknown.
        /// Known values: 3
        /// </summary>
        [JsonProperty("contenttype", NullValueHandling = NullValueHandling.Ignore)]
        public uint? contenttype { get; set; }

        #endregion

        // When VDF has an array, they represent it like this, with the left numbers being indexes:
        /// "1"		"1056577072"
        /// "2"		"1056702256"
        /// "3"		"1056203136"
        /// "4"		"1056394576"
        /// "5"		"274355120"
        /// "6"		"1056600656"
        /// "7"		"1056306688"
        /// "8"		"1056771040"
        /// "9"		"1056875824"
        /// "10"		"89495744"
        /// also like this, although this isn't one that needs to be parsed right now
        /// "1 0"		"1493324560"
        /// "1 1"		"1492884912"
        /// "1 2"		"1492755784"
        /// "1 3"		"28749920"
        /// TODO: not sure how you want me to handle this, especially since in implementation it seems easier to treat it
        /// TODO: like a dictionary
        #region Numbered

        // On csm/csd discs, both are used interchangeably, but never at the same time. It's usually still lowercase though.
        // It always seems to be lowercase on sim/sid discs
        /// <summary>
        /// "apps"
        /// AppIDs contained on the disc.
        /// Known values: arbitrary
        /// </summary>
        [JsonProperty("apps", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<long, long>? apps { get; set; }

        /// <summary>
        /// "Apps"
        /// AppIDs contained on the disc.
        /// Known values: arbitrary
        /// </summary>
        /// <remarks>csm/csd only</remarks>
        [JsonProperty("Apps", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<long, long>? Apps { get; set; }

        /// <summary>
        /// "depots"
        /// DepotIDs contained on the disc.
        /// Known values: arbitrary
        /// </summary>
        [JsonProperty("depots", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<long, long>? depots { get; set; }

        // packages goes here, but it's that weird format in the "also like this" that also isn't one of the only 4 values that matter anyways

        /// <summary>
        /// "manifests"
        /// DepotIDs contained on the disc.
        /// Known values: arbitrary pairs of DepotID - Manifest
        /// </summary>
        [JsonProperty("manifests", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<long, long>? manifests { get; set; }

        /// <summary>
        /// "chunkstores"
        /// chunkstores contained on the disc.
        /// Known values: DepotIDs containing arrays of chunkstores, usually just one.
        /// </summary>
        [JsonProperty("manifests", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<long, object>? chunkstores { get; set; }

        #endregion
    }
}
