namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            //info csym = 0, file = 3, lib = 0, line = 380, mod = 1, scope = 12, seg = 8, span = 356, sym = 61, type = 3
            public sealed class Information : Section
            {
                public Information(Parser container)
                : base(container)
                {
                    this.ProcessAttributesOfProperties();
                    var properties = _sectionPropertiesCache[this.GetType()];
                    properties.AddIntegerKey("csym");
                    properties.AddIntegerKey("file");
                    properties.AddIntegerKey("lib");
                    properties.AddIntegerKey("line");
                    properties.AddIntegerKey("mod");
                    properties.AddIntegerKey("scope");
                    properties.AddIntegerKey("seg");
                    properties.AddIntegerKey("span");
                    properties.AddIntegerKey("sym");
                    properties.AddIntegerKey("type");
                }

                public int Count(string key) => this.TakeInteger(key);
            }
        }
    }
}