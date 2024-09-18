namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            // mod	id=0,name="sudoku.o",file=0
            public sealed class Module(Parser container) : NamedSection(container)
            {
                [SectionReference("file")]
                public Symbols.File File => this.TakeFileReference();
            }
        }
    }
}