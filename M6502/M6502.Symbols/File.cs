namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            // file id=0,name="sudoku.s",size=9141,mtime=0x6027C7F0,mod=0
            public sealed class File(Parser container) : NamedSection(container)
            {
                [SectionProperty("size")]
                public int Size => this.TakeInteger("size");

                [SectionProperty("mtime")]
                public DateTime ModificationTime => this.TakeDateTime("mtime");

                [SectionReference("mod")]
                public Symbols.Module Module => this.TakeModuleReference();
            }
        }
    }
}