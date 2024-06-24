namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            // file id=0,name="sudoku.s",size=9141,mtime=0x6027C7F0,mod=0
            public class File(Parser container) : NamedSection(container)
            {
                [SectionProperty("size")]
                public int Size { get; private set; }

                [SectionProperty("mtime", hexadecimal: true)]
                public long ModificationTime { get; private set; }

                [SectionReference("mod")]
                public Symbols.Module Module => this.TakeModuleReference();

                public override void Parse(IDictionary<string, string> entries)
                {
                    base.Parse(entries);
                    this.Size = this.TakeInteger("size");
                    this.ModificationTime = this.TakeLong("mtime");
                }
            }
        }
    }
}