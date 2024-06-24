namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            // type id = 0, val = "800920"
            public class Type(Parser container) : IdentifiableSection(container)
            {
                [SectionProperty("val")]
                public string Value => this.TakeString("val");
            }
        }
    }
}