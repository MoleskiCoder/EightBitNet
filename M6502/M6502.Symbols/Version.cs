namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            //version major = 2, minor = 0
            public sealed class Version(Parser container) : Section(container)
            {
                [SectionProperty("major")]
                public int Major { get; private set; }

                [SectionProperty("minor")]
                public int Minor { get; private set; }
            }
        }
    }
}