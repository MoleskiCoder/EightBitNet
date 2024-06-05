namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            public class NamedSection : IdentifiableSection
            {
                public string Name => this.TakeString("name");

                protected NamedSection() => _ = this._string_keys.Add("name");
            }
        }
    }
}