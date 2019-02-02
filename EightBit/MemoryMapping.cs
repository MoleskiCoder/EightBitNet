namespace EightBit
{
    public class MemoryMapping
    {
        private Memory memory;
        private ushort begin;
        private ushort mask;
        private AccessLevel access;

        public MemoryMapping(Memory memory, ushort begin, ushort mask, AccessLevel access)
        {
            this.memory = memory;
            this.begin = begin;
            this.mask = mask;
            this.access = access;
        }

        public Memory Memory { get => memory; set => memory = value; }
        public ushort Begin { get => begin; set => begin = value; }
        public ushort Mask { get => mask; set => mask = value; }
        public AccessLevel Access { get => access; set => access = value; }
    }
}
