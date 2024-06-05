namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            using System.Collections.Generic;

            // sym id = 16, name = "solve", addrsize = absolute, size = 274, scope = 0, def = 94,ref=144+17+351,val=0xF314,seg=6,type=lab
            public class Symbol : NamedSection
            {
                public string AddressSize => this.TakeString("addrsize");

                public int Size => this.TakeInteger("size");

                public Symbols.Scope Scope => this.TakeScopeReference();

                public List<Line> Definitions => this.TakeLineReferences("def");  // Guess

                public List<Line> References => this.TakeLineReferences("ref"); // Guess

                public int Value => this.TakeInteger("val");

                public Symbols.Segment Segment => this.TakeSegmentReference();

                public string Type => this.TakeString("type");

                public Symbol()
                {
                    _ = this._enumeration_keys.Add("addrsize");
                    _ = this._integer_keys.Add("size");
                    _ = this._integer_keys.Add("scope");
                    _ = this._multiple_keys.Add("def");
                    _ = this._multiple_keys.Add("ref");
                    _ = this._hex_integer_keys.Add("val");
                    _ = this._integer_keys.Add("seg");
                    _ = this._enumeration_keys.Add("type");
                }

                public override void Parse(Parser parent, Dictionary<string, string> entries) 
                {
                    base.Parse(parent, entries);
                    if (this.Type is "lab")
                    {
                        this._parent.Labels.Add(this);
                    }
                    else if (this.Type is "equ")
                    {
                        this._parent.Equates.Add(this);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unknown symbol type: {this.Type}");
                    }
                }
            }
        }
    }
}