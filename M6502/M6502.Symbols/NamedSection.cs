namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            public class NamedSection : IdentifiableSection
            {
                [SectionProperty("name")]
                public string? Name => this.TakeString("name");

                protected NamedSection(Parser container)
                : base(container) { }
            }
        }
    }
}