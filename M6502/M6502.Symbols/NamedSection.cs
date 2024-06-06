#define FAST_NAME_LOOKUP

namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            public class NamedSection : IdentifiableSection
            {
#if FAST_NAME_LOOKUP
                public string? Name { get; private set; }
#else
                public string Name => this.TakeString("name");
#endif

                protected NamedSection() => _ = this._string_keys.Add("name");

#if FAST_NAME_LOOKUP
                public override void Parse(Parser parent, Dictionary<string, string> entries)
                {
                    base.Parse(parent, entries);
                    this.Name = this.TakeString("name");
                }
#endif
            }
        }
    }
}