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

                public int LineNumber => this.TakeInteger("line");

                public Symbols.Type Type => this.TakeTypeReference();

                public int Count => this.TakeInteger("count");

                public List<Span> Spans => this.TakeSpanReferences();
                
                public Line()
                {
                    _ = this._integer_keys.Add("file");
                    _ = this._integer_keys.Add("line");
                    _ = this._integer_keys.Add("type");
                    _ = this._integer_keys.Add("count");
                    _ = this._multiple_keys.Add("span");
                }
            }
        }
    }
}