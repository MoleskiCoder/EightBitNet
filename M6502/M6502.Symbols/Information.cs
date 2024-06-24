namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            //info csym = 0, file = 3, lib = 0, line = 380, mod = 1, scope = 12, seg = 8, span = 356, sym = 61, type = 3
            public class Information : Section
            {
                public Information(Parser container)
                : base(container)
                {
                    this.AddIntegerKey("csym");
                    this.AddIntegerKey("file");
                    this.AddIntegerKey("lib");
                    this.AddIntegerKey("line");
                    this.AddIntegerKey("mod");
                    this.AddIntegerKey("scope");
                    this.AddIntegerKey("seg");
                    this.AddIntegerKey("span");
                    this.AddIntegerKey("sym");
                    this.AddIntegerKey("type");
                }

                public int Count(string key) => this.TakeInteger(key);
            }
        }
    }
}