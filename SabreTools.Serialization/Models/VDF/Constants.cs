namespace SabreTools.Data.Models.VDF
{
    public static class Constants
    {
        public static readonly byte[] Steam2SisSignatureBytes = [0x22, 0x53, 0x4B, 0x55, 0x22]; // "SKU"

        public static readonly string Steam2SisSignatureString = "\"SKU\"";

        public static readonly byte[] Steam3SisSignatureBytes = [0x22, 0x73, 0x6B, 0x75, 0x22]; // "sku"

        public static readonly string Steam3SisSignatureString = "\"sku\"";
    }
}
