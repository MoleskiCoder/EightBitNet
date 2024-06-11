namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            // line id = 268, file = 1, line = 60, type = 2, count = 1, span = 286 + 195
            public class Line : IdentifiableSection
            {
                public Symbols.File File => this.TakeFileReference();

                public int LineNumber { get; private set; }
                public Symbols.Type Type => this.TakeTypeReference();

                public int? Count { get; private set; }
                public List<Span> Spans => this.TakeSpanReferences();
                
                public Line()
                {
                    _ = this._integer_keys.Add("file");
                    _ = this._integer_keys.Add("line");
                    _ = this._integer_keys.Add("type");
                    _ = this._integer_keys.Add("count");
                    _ = this._multiple_keys.Add("span");
                }

                public override void Parse(Parser parent, IDictionary<string, string> entries)
                {
                    base.Parse(parent, entries);
                    this.LineNumber = this.TakeInteger("line");
                    this.Count = this.MaybeTakeInteger("count");
                }
            }
        }
    }
}