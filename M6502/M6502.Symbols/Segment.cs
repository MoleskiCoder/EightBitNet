namespace EightBit.Files.Symbols
{
    // seg	id=1,name="RODATA",start=0x00F471,size=0x0000,addrsize=absolute,type=ro,oname="sudoku.65b",ooffs=1137
    [Section("seg", "Segments")]
    public sealed class Segment(Parser container) : NamedSection(container)
    {
        [SectionProperty("start", hexadecimal: true)]
        public int Start { get; private set; }

        [SectionProperty("size", hexadecimal: true)]
        public int Size { get; private set; }

        [SectionEnumeration("addrsize")]
        public string? AddressSize { get; private set; }

        [SectionEnumeration("type")]
        public string? Type { get; private set; }

        [SectionProperty("oname")]
        public string? OName { get; private set; }

        [SectionProperty("ooffs")]
        public int? OOFFS { get; private set; }
    }
}