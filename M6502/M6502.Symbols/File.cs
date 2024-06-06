namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            // file id=0,name="sudoku.s",size=9141,mtime=0x6027C7F0,mod=0
            public class File : NamedSection
            {
                public int Size { get; private set; }

                public long ModificationTime { get; private set; }
                public Symbols.Module Module => this.TakeModuleReference();

                public File()
                {
                    _ = this._integer_keys.Add("size");
                    _ = this._hex_long_keys.Add("mtime");
                    _ = this._integer_keys.Add("mod");
                }

                public override void Parse(Parser parent, Dictionary<string, string> entries)
                {
                    base.Parse(parent, entries);
                    this.Size = this.TakeInteger("size");
                    this.ModificationTime = this.TakeLong("mtime");
                }
            }
        }
    }
}