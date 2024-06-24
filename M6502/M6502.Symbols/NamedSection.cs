namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            public class NamedSection : IdentifiableSection
            {
                [SectionProperty("name")]
                public string? Name { get; private set; }

                protected NamedSection(Parser container)
                : base(container) { }

                public override void Parse(IDictionary<string, string> entries)
                {
                    base.Parse(entries);
                    this.Name = this.TakeString("name");
                }
            }
        }
    }
}