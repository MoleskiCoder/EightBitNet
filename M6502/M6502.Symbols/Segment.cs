namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            // seg	id=1,name="RODATA",start=0x00F471,size=0x0000,addrsize=absolute,type=ro,oname="sudoku.65b",ooffs=1137
            public class Segment(Parser container) : NamedSection(container)
            {
                [SectionProperty("start", hexadecimal: true)]
                public int Start { get; private set; }

                [SectionProperty("size", hexadecimal: true)]
                public int Size { get; private set; }

                [SectionEnumeration("addrsize")]
                public string AddressSize => this.TakeString("addrsize");

                [SectionEnumeration("type")]
                public string Type => this.TakeString("type");

                [SectionProperty("oname")]
                public string OName => this.TakeString("oname");

                [SectionProperty("ooffs")]
                public int? OOFFS { get; private set; }  // ?? Offsets, perhaps?

                public override void Parse(IDictionary<string, string> entries)
                {
                    base.Parse(entries);
                    this.Start = this.TakeInteger("start");
                    this.Size = this.TakeInteger("size");
                    this.OOFFS = this.MaybeTakeInteger("ooffs");
                }
            }
        }
    }
}