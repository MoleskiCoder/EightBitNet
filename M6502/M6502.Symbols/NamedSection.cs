namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            public class NamedSection : IdentifiableSection
            {
                public string? Name { get; private set; }

                protected NamedSection() => _ = this._string_keys.Add("name");

                public override void Parse(Parser parent, Dictionary<string, string> entries)
                {
                    base.Parse(parent, entries);
                    this.Name = this.TakeString("name");
                }
            }
        }
    }
}