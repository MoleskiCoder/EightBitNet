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

        public static void ClearFlag(ref byte f, byte flag, int condition) => ClearFlag(ref f, flag, condition == 0);

        public static void ClearFlag(ref byte f, byte flag, bool condition) => SetFlag(ref f, flag, !condition);

        public static int HighByte(int value) => LowByte(value >> 8);
        public static byte HighByte(ushort value) => (byte)LowByte((int)value);
        public static int LowByte(int value) => value & (int)Mask.Mask8;
        public static byte LowByte(ushort value) => (byte)LowByte((int)value);
        public static int PromoteByte(int value) => value << 8;
        public static ushort PromoteByte(byte value) => (ushort)PromoteByte((int)value);
        public static int DemoteByte(int value) => HighByte(value);
        public static byte DemoteByte(ushort value) => (byte)HighByte((int)value);

        public static int HighNibble(byte value) => value >> 4;
        public static int LowNibble(byte value) => value & 0xf;

        public static int HigherNibble(byte value) => value & 0xf0;
        public static int LowerNibble(byte value) => LowNibble(value);

        public static int PromoteNibble(byte value) => value << 4;
        public static int DemoteNibble(byte value) => HighNibble(value);
    }
}
