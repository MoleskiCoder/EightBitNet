namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            //span id = 351, seg = 7, start = 0, size = 2, type = 2
            public class Span : IdentifiableSection
            {
                public Symbols.Segment Segment => this.TakeSegmentReference();
                public int Start => this.TakeInteger("start");
                public int Size => this.TakeInteger("size");
                public Symbols.Type Type => this.TakeTypeReference();

                public Span()
                {
                    _ = this._integer_keys.Add("seg");
                    _ = this._integer_keys.Add("start");
                    _ = this._integer_keys.Add("size");
                    _ = this._integer_keys.Add("type");
                }
            }
        }
    }
}