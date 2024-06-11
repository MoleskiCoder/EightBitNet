namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            // seg	id=1,name="RODATA",start=0x00F471,size=0x0000,addrsize=absolute,type=ro,oname="sudoku.65b",ooffs=1137
            public class Segment : NamedSection
            {
                public int Start { get; private set; }
                public int Size { get; private set; }
                public string AddressSize => this.TakeString("addrsize");
                public string Type => this.TakeString("type");
                public string OName => this.TakeString("oname");
                public int? OOFFS { get; private set; }  // ?? Offsets, perhaps?

                public Segment()
                {
                    _ = this._hex_integer_keys.Add("start");
                    _ = this._hex_integer_keys.Add("size");
                    _ = this._enumeration_keys.Add("addrsize");
                    _ = this._enumeration_keys.Add("type");
                    _ = this._string_keys.Add("oname");
                    _ = this._integer_keys.Add("ooffs");
                }

                public override void Parse(Parser parent, IDictionary<string, string> entries)
                {
                    base.Parse(parent, entries);
                    this.Start = this.TakeInteger("start");
                    this.Size = this.TakeInteger("size");
                    this.OOFFS = this.MaybeTakeInteger("ooffs");
                }
            }
        }
    }
}