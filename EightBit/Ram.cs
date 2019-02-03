namespace EightBit
{
    public class Ram : Rom
    {
        public Ram(int size = 0)
        : base(size)
        {
        }

        public override sealed ref byte Reference(ushort address)
        {
            return ref Bytes()[address];
        }

        public new void Poke(ushort address, byte value)
        {
            base.Poke(address, value);
        }
    }
}
