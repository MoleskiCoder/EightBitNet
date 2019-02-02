namespace EightBit
{
    public sealed class Register16
    {
        private byte low;
        private byte high;

        public Register16() { }

        public Register16(ushort value) => Word = value;

        public Register16(byte lowValue, byte highValue)
        {
            Low = lowValue;
            High = highValue;
        }

        public byte Low { get => low; set => low = value; }
        public byte High { get => high; set => high = value; }

        public ushort Word
        {
            get
            {
                return (ushort)(Chip.PromoteByte(high) | low);
            }

            set
            {
                high = Chip.DemoteByte(value);
                low = Chip.LowByte(value);
            }
        }

        public static implicit operator ushort(Register16 value) { return ToUInt16(value); }

        public static Register16 operator ++(Register16 left) => Increment(left);
        public static Register16 operator --(Register16 left) => Decrement(left);

        public static bool operator ==(Register16 left, Register16 right) => Equals(left, right);
        public static bool operator !=(Register16 left, Register16 right) => !(left == right);

        public static Register16 operator +(Register16 left, Register16 right) => Add(left, right);
        public static Register16 operator -(Register16 left, Register16 right) => Subtract(left, right);

        public static ushort ToUInt16(Register16 value) { return value.Word; }
        public ushort ToUInt16() { return ToUInt16(this); }

        public static Register16 Increment(Register16 left)
        {
            ++left.Word;
            return left;
        }

        public static Register16 Decrement(Register16 left)
        {
            --left.Word;
            return left;
        }

        public static Register16 Add(Register16 left, Register16 right)
        {
            left.Word += right.Word;
            return left;
        }

        public static Register16 Subtract(Register16 left, Register16 right)
        {
            left.Word -= right.Word;
            return left;
        }

        public override int GetHashCode() => Word;

        public override bool Equals(object obj)
        {
            var register = obj as Register16;
            return Equals(register);
        }

        public bool Equals(Register16 register)
        {
            return register != null && low == register.low && high == register.high;
        }
    }
}
