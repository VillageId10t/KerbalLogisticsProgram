using UnityEngine;

namespace KerbalLogisticsProgram
{
    public sealed class KlpPalette
    {
        // Backgrounds
        public Color WindowBackground = FromHex(0x151B22);
        public Color PanelBackground = FromHex(0x202A35);
        public Color HeaderBackground = FromHex(0x27333F);
        public Color RowBackground = FromHex(0x19222B);
        public Color AlternateRowBackground = FromHex(0x1D2731);

        // Borders and separators
        public Color Border = FromHex(0x34414D);

        // Text
        public Color PrimaryText = FromHex(0xE2E8F0);
        public Color SecondaryText = FromHex(0xA8B4C2);
        public Color DisabledText = FromHex(0x788592);
        public Color AccentText = FromHex(0x151B22);

        // Mailbox operations
        public Color MailboxAccent = FromHex(0x56B4E9);
        public Color MailboxSelection = FromHex(0x203E50);
        public Color MailboxHover = FromHex(0x30566D);

        // Shipping operations
        public Color ShippingAccent = FromHex(0xE69F00);
        public Color ShippingSelection = FromHex(0x403725);
        public Color ShippingHover = FromHex(0x594722);

        // Transaction status
        public Color Success = FromHex(0x009E73);
        public Color Warning = FromHex(0xF0E442);
        public Color Error = FromHex(0xE76F51);

        // Disabled controls
        public Color DisabledBackground = FromHex(0x353B41);

        private static Color FromHex(uint hex)
        {
            return new Color32(
                (byte)((hex >> 16) & 0xFF),
                (byte)((hex >> 8) & 0xFF),
                (byte)(hex & 0xFF),
                255);
        }
    }
}