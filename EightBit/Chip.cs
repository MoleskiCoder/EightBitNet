namespace EightBit
{
    public class Chip : Device
    {
        protected Chip() { }

        public static void ClearFlag(ref byte f, byte flag) => f &= (byte)~flag;
        public static void SetFlag(ref byte f, byte flag) => f |= flag;

        public static void SetFlag(ref byte f, byte flag, int condition) => SetFlag(ref f, flag, condition != 0);

        public static void SetFlag(ref byte f, byte flag, bool condition)
        {
            if (condition)
                SetFlag(ref f, flag);
            else
                ClearFlag(ref f, flag);
        }

        public static void ClearFlag(ref byte f, byte flag, int condition) => ClearFlag(ref f, flag, condition != 0);

        public static void ClearFlag(ref byte f, byte flag, bool condition) => SetFlag(ref f, flag, !condition);

        public static byte HighByte(int value) => (byte)(value >> 8);
        public static byte HighByte(ushort value) => HighByte((int)value);
        public static byte LowByte(int value) => (byte)(value & (int)Mask.Mask8);
        public static byte LowByte(ushort value) => LowByte((int)value);
        public static ushort PromoteByte(byte value) => (ushort)(value << 8);
        public static byte DemoteByte(ushort value) => HighByte(value);
        public static ushort HigherPart(ushort value) => (ushort)(value & 0xff00);
        public static byte LowerPart(ushort value) => LowByte(value);

        static public ushort MakeWord(byte low, byte high) => (ushort)(PromoteByte(high) | low);

        public static int HighNibble(byte value) => value >> 4;
        public static int LowNibble(byte value) => value & 0xf;

        public static int HigherNibble(byte value) => value & 0xf0;
        public static int LowerNibble(byte value) => LowNibble(value);

        public static int PromoteNibble(byte value) => LowByte(value << 4);
        public static int DemoteNibble(byte value) => HighNibble(value);
    }
}
