namespace M6502.Symbols
{
    public class NamedSection : IdentifiableSection
    {
        [SectionProperty("name")]
        public string? Name { get; protected set; }

        protected NamedSection(Parser container)
        : base(container) { }
    }
}