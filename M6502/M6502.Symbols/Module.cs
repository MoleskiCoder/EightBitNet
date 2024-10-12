namespace M6502.Symbols
{
    // mod	id=0,name="sudoku.o",file=0
    [Section("mod", "Modules")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "External file format")]
    public sealed class Module(Parser container) : NamedSection(container)
    {
        [SectionReference("file")]
        public File? File { get; private set; }
    }
}