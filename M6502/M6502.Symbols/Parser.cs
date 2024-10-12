namespace M6502.Symbols
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    public sealed class Parser
    {
        #region Variables, properties etc.

        private Version? _version;
        private Information? _information;

        // Section -> Unique ID list of dictionary entries
        public Dictionary<string, List<Dictionary<string, string>>> Parsed { get; } = [];

        public List<Section> SectionEntries { get; } = [];

        public List<File> Files = [];

        public List<Line> Lines = [];

        public List<Module> Modules = [];

        public List<Segment> Segments = [];

        public List<Span> Spans = [];

        public List<Scope> Scopes = [];

        public List<Symbol> Symbols = [];

        public List<Type> Types = [];


        // Symbol sub-types
        public IEnumerable<Symbol> Labels => this.SelectSymbolsByType("lab");
        public IEnumerable<Symbol> Equates => this.SelectSymbolsByType("equ");

        // Scope clarification
        public List<Scope> AddressableScopes = [];

        // Scope cache for precomputed ranges
        private readonly int?[] _scopeAddressCache = new int?[0x10000];

        #endregion

        #region Lookups

        private static IEnumerable<T> SelectIdMatching<T>(int id, IEnumerable<T>? items) where T : IdentifiableSection => from item in items where item.ID == id select item;

        private IEnumerable<Symbol> SelectSymbolsByType(string type) => from symbol in this.Symbols where symbol.Type == type select symbol;

        #region Label lookup

        private IEnumerable<Symbol> LookupLabelsByAddress(int address) => from label in this.Labels where label.Value == address select label;

        public Symbol? LookupLabelByAddress(int address)
        {
            var labels = this.LookupLabelsByAddress(address).ToList();
            return labels.Count > 0 ? labels[0] : null;
        }

        private IEnumerable<Symbol> LookupLabelsByID(int id) => SelectIdMatching(id, this.Labels);

        public Symbol? LookupLabelByID(int id)
        {
            var labels = this.LookupLabelsByID(id).ToList();
            return labels.Count > 0 ? labels[0] : null;
        }

        #endregion

        #region Constant lookup

        private IEnumerable<Symbol> LookupEquatesByValue(int constant) => from equate in this.Equates where equate.Value == constant select equate;

        public Symbol? LookupEquateByValue(int constant)
        {
            var equates = this.LookupEquatesByValue(constant).ToList();
            return equates.Count > 0 ? equates[0] : null;
        }

        #endregion

        #region Scope lookup

        private int LocateScopeByAddress(int address)
        {
            var low = 0;
            var high = this.AddressableScopes.Count - 1;

            while (low <= high)
            {
                var mid = low + ((high - low) / 2);

                var scope = this.AddressableScopes[mid];

                var symbol = scope.Symbol;
                Debug.Assert(symbol is not null);
                var start = symbol.Value;

                if (address < start)
                {
                    high = mid - 1;
                }
                else
                {
                    var end = start + scope.Size;
                    if (address >= end)
                    {
                        low = mid + 1;
                    }
                    else
                    {
                        return mid;
                    }
                }
            }

            // If we reach here, then scope was not present
            return -1;
        }

        public Scope? LookupScopeByAddress(int address)
        {
            var index = this._scopeAddressCache[address] ?? (this._scopeAddressCache[address] = this.LocateScopeByAddress(address));
            return index == -1 ? null : this.AddressableScopes[index.Value];
        }

        private IEnumerable<Scope> LookupScopesByID(int id) => SelectIdMatching(id, this.Scopes);

        public Scope? LookupScopeByID(int id)
        {
            var scopes = this.LookupScopesByID(id).ToList();
            return scopes.Count > 0 ? scopes[0] : null;
        }

        #endregion

        #region Scope evaluation

        private static List<Scope> EvaluateScope(Scope? start)
        {
            Debug.Assert(start is not null);
            var returned = new List<Scope>();
            for (var current = start; current.Parent is not null; current = current.Parent)
            {
                returned.Add(current);
            }
            return returned;
        }

        private static List<Scope> EvaluateScope(Symbol symbol) => EvaluateScope(symbol.Scope);

        #endregion

        #region Namespace evaluation from scope

        private static string BuildNamespace(Symbol symbol)
        {
            var returned = string.Empty;
            var scopes = EvaluateScope(symbol);
            for (var i = scopes.Count - 1; i >= 0; i--)
            {
                var scope = scopes[i];
                var name = scope.Name;
                Debug.Assert(!string.IsNullOrEmpty(name));
                returned += name;
                var last = i == 0;
                if (!last)
                {
                    returned += '.';
                }
            }
            return returned;
        }

        private static string PrefixNamespace(Symbol? symbol)
        {
            if (symbol is null)
            {
                return string.Empty;
            }
            var prefix = BuildNamespace(symbol);
            var name = symbol.Name;
            Debug.Assert(!string.IsNullOrEmpty(name));
            return string.IsNullOrEmpty(prefix) ? name : $"{prefix}.{name}";
        }

        #endregion

        #region Qualified symbol lookup

        public bool TryGetQualifiedLabelByAddress(ushort absolute, out string label)
        {
            var symbol = this.LookupLabelByAddress(absolute);
            label = PrefixNamespace(symbol);
            return symbol is not null;
        }

        public string MaybeGetQualifiedLabelByAddress(ushort absolute) => this.TryGetQualifiedLabelByAddress(absolute, out var label) ? label : string.Empty;

        public bool TryGetQualifiedEquateValue(ushort value, out string name)
        {
            var symbol = this.LookupEquateByValue(value);
            name = PrefixNamespace(symbol);
            return symbol is not null;
        }

        #endregion

        #endregion

        #region Section extractors

        private void ExtractSections()
        {
            this.ExtractFiles();
            this.ExtractLines();
            this.ExtractModules();
            this.ExtractSegments();
            this.ExtractSpans();
            this.ExtractScopes();
            this.ExtractSymbols();
            this.ExtractTypes();
        }

        private void ExtractFiles() => this.Extract<File>("file", this.Files);

        private void ExtractLines() => this.Extract<Line>("line", this.Lines);

        private void ExtractModules() => this.Extract<Module>("mod", this.Modules);

        private void ExtractSegments() => this.Extract<Segment>("seg", this.Segments);

        private void ExtractSpans() => this.Extract<Span>("span", this.Spans);

        private void ExtractScopes() => this.Extract<Scope>("scope", this.Scopes);

        private void ExtractSymbols() => this.Extract<Symbol>("sym", this.Symbols);

        private void ExtractTypes() => this.Extract<Type>("type", this.Types);

        private void Extract<T>(string key, List<T> into) where T : IdentifiableSection
        {
            if (!this.Parsed.TryGetValue(key, out var parsed))
            {
                throw new ArgumentOutOfRangeException(nameof(key), key, "Debugging section is unavailable");
            }
            this.ExtractIdentifiableSection(key, into, parsed);
        }

        private void ExtractIdentifiableSection<T>(string key, List<T> into, List<Dictionary<string, string>> parsed) where T : IdentifiableSection
        {
            into.Capacity = parsed.Count;
            for (var id = 0; id < parsed.Count; ++id)
            {
                this.ExtractIdentifiableEntry(id, parsed, into);
            }
            Debug.Assert(into.Count == parsed.Count);
            this.VerifyInformationCount(key, into.Count);
        }

        private void ExtractIdentifiableEntry<T>(int id, List<Dictionary<string, string>> parsed, List<T> into) where T : IdentifiableSection
        {
            Debug.Assert(into.Count == id);
            var information = parsed[id];
            var entry = this.ExtractEntry<T>(information);
            Debug.Assert(into.Capacity > id);
            into.Add(entry);
        }

        private T ExtractEntry<T>(Dictionary<string, string> information) where T : Section
        {
            var entry = (T?)Activator.CreateInstance(typeof(T), this);
            Debug.Assert(entry is not null);
            entry.Parse(information);
            return entry;
        }

        private void VerifyInformationCount(string key, int actual)
        {
            Debug.Assert(this._information is not null);
            var expected = this._information.Count(key);
            if (expected != actual)
            {
                throw new InvalidOperationException($"information count mismatch for {key}.  Expected {expected}, actual {actual}");
            }
        }

        #endregion

        private void ExtractReferences()
        {
            foreach (var entry in this.SectionEntries)
            {
                var identifiableEntry = entry as IdentifiableSection;
                identifiableEntry?.ExtractReferences();
            }
        }

        private void BuildAddressableScopes()
        {
            var scopes = from scope in this.Scopes where scope.Symbol is not null select scope;
            this.AddressableScopes.AddRange(scopes);
        }

        #region Parser driver

        public async Task ParseAsync(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            using var reader = new StreamReader(path);
            await this.ParseAsync(reader).ConfigureAwait(false);

            this.ExtractSections();
            this.ExtractReferences();

            this.Parsed.Clear();

            this.BuildAddressableScopes();
        }

        private async Task ParseAsync(StreamReader reader)
        {
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                this.ParseLine(line.Split(' ', '\t'));
            }
        }

        private void ParseLine(string[] elements)
        {
            ArgumentNullException.ThrowIfNull(elements, nameof(elements));
            ArgumentOutOfRangeException.ThrowIfNotEqual(elements.Length, 2, nameof(elements));

            var key = elements[0];
            var parts = elements[1].Split(',');

            if (key is "version")
            {
                if (this._version is not null)
                {
                    throw new InvalidOperationException("Version object has already been parsed");
                }
                this._version = new(this);
                this._version.Parse(BuildDictionary(parts));
                this.VerifyVersion();
            }
            else if (key is "info")
            {
                if (this._information is not null)
                {
                    throw new InvalidOperationException("Information object has already been parsed");
                }
                this._information = new(this);
                this._information.Parse(BuildDictionary(parts));
            }
            else
            {
                this.Parse(key, parts);
            }
        }

        private void Parse(string key, string[] parts)
        {
            if (!this.Parsed.TryGetValue(key, out var section))
            {
                this.Parsed.Add(key, section = []);
            }
            section.Add(BuildDictionary(parts));
        }

        private static Dictionary<string, string> BuildDictionary(string[] parts)
        {
            var dictionary = new Dictionary<string, string>(parts.Length);
            foreach (var part in parts)
            {
                var definition = part.Split('=');
                if (definition.Length != 2)
                {
                    throw new InvalidOperationException("Invalid symbol file format (definition is missing equals)");
                }

                var key = definition[0];
                var value = definition[1];
                dictionary.Add(key, value);
            }

            return dictionary;
        }

        private void VerifyVersion()
        {
            if (this._version is null)
            {
                throw new InvalidOperationException("Version has not yet been parsed");
            }

            var major = this._version.Major;
            var minor = this._version.Minor;
            var valid = major == 2 && minor == 0;
            if (!valid)
            {
                throw new InvalidOperationException($"Unknown symbol file version: {major}.{minor}");
            }
        }

        #endregion
    }
}