namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            // seg	id=1,name="RODATA",start=0x00F471,size=0x0000,addrsize=absolute,type=ro,oname="sudoku.65b",ooffs=1137
            public sealed class Segment(Parser container) : NamedSection(container)
            {
                [SectionProperty("start", hexadecimal: true)]
                public int Start => this.TakeInteger("start");

                [SectionProperty("size", hexadecimal: true)]
                public int Size => this.TakeInteger("size");

                [SectionEnumeration("addrsize")]
                public string AddressSize => this.TakeString("addrsize");

                [SectionEnumeration("type")]
                public string Type => this.TakeString("type");

                [SectionProperty("oname")]
                public string OName => this.TakeString("oname");

                [SectionProperty("ooffs")]
                public int? OOFFS => this.MaybeTakeInteger("ooffs");  // ?? Offsets, perhaps?
            }
        }
    }
}