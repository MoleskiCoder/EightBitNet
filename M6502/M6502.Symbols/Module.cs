namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            // mod	id=0,name="sudoku.o",file=0
            public class Module : NamedSection
            {
                public Symbols.File File => this.TakeFileReference();

                public Module() => _ = this._integer_keys.Add("file");
            }
        }
    }
}