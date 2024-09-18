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
                public int Major => this.TakeInteger("major");

                [SectionProperty("minor")]
                public int Minor => this.TakeInteger("minor");
            }
        }
    }
}