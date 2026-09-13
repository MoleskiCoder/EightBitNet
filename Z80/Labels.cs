namespace Z80
{
    using System.Diagnostics;
    using System.Globalization;
    using System.Collections.Generic;

    public class Labels(EightBit.ILogger logger)
    {
        private readonly EightBit.ILogger _logger = logger;
        private readonly Dictionary<ushort, string> _symbols = [];   // Address to symbol lookup

        public Dictionary<ushort, string> Symbols => this._symbols;

        public Labels()
        : this(new EightBit.ConsoleLogger())
        {}

        public bool Lookup(ushort address, out string? label) => this.Symbols.TryGetValue(address, out label);

        public void Parse(string path)
        {
            this._logger.Inform($"Labels: reading {path}");
            using var reader = new StreamReader(path);
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                this.ParseLine(line);
            }
        }

        private void ParseLine(string? line)
        {
            if (string.IsNullOrEmpty(line))
            {
                this._logger.Debug($"Ignoring empty line");
                return;
            }
            this._logger.Debug($"Parsing: {line}");
            var elements = line.Split(' ', '\t');
            Debug.Assert(elements is not null);
            if (elements.Length != 2)
            {
                this._logger.Debug("Ignoring invalid line");
                return;
            }

            if (!ushort.TryParse(elements[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var address))
            {
                this._logger.Debug($"First element ({elements[0]}), cannot be parsed as a hexadecimal number");
                return;
            }

            var label = elements[1];
            if (!this.Symbols.TryAdd(address, label))
            {
                this._logger.Debug($"Label {label}, has already been used");
            }
        }
    }
}
