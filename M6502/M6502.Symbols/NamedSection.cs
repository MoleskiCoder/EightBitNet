namespace EightBit.Files.Symbols
{
    public class NamedSection : IdentifiableSection
    {
        [SectionProperty("name")]
        public string? Name { get; protected set; }

        protected NamedSection(Parser container)
        : base(container) { }
    }
}