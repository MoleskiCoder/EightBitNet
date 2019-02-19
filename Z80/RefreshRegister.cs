namespace EightBit
{
    public struct RefreshRegister
    {
        private readonly byte high;
        private byte variable;

        public RefreshRegister(byte value)
        {
            this.high = (byte)(value & (byte)Bits.Bit7);
            this.variable = (byte)(value & (byte)Mask.Mask7);
        }

        public static implicit operator byte(RefreshRegister input)
        {
            return ToByte(input);
        }

        public static implicit operator RefreshRegister(byte input)
        {
            return FromByte(input);
        }

        public static RefreshRegister operator ++(RefreshRegister value) => Increment(value);

        public static bool operator ==(RefreshRegister left, RefreshRegister right) => left.Equals(right);

        public static bool operator !=(RefreshRegister left, RefreshRegister right) => !(left == right);

        public static byte ToByte(RefreshRegister input)
        {
            return (byte)((input.high << 7) | input.variable);
        }

        public static RefreshRegister FromByte(byte input)
        {
            return new RefreshRegister(input);
        }

        public static RefreshRegister Increment(RefreshRegister value)
        {
            ++value.variable;
            return value;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RefreshRegister))
            {
                return false;
            }

            var rhs = (RefreshRegister)obj;
            return rhs.high == this.high && rhs.variable == this.variable;
        }

        public override int GetHashCode() => this.high + this.variable;
    }
}
