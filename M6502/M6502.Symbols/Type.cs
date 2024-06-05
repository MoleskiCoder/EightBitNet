namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            // type id = 0, val = "800920"
            public class Type : IdentifiableSection
            {
                public string Value => this.TakeString("val");

                public Type() => _ = this._string_keys.Add("val");
            }
        }
    }
}