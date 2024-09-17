namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            using System;
            using System.Diagnostics;
            using System.Threading.Tasks;

            public sealed class Parser
            {
                #region Variables, properties etc.

                public bool Parsed { get; private set; }

                // Section -> Unique ID list of dictionary entries
                // Being sorted allows us to verify IDs as they arrive
                private readonly Dictionary<string, SortedDictionary<int, Dictionary<string, string>>> _parsed = [];

                private Version? _version;
                private Information? _information;

                public List<File> Files { get; } = [];
                public List<Line> Lines { get; } = [];
                public List<Module> Modules { get; } = [];
                public List<Segment> Segments { get; } = [];
                public List<Span> Spans { get; } = [];
                public List<Scope> Scopes { get; } = [];
                public List<Symbol> Symbols { get; } = [];
                public List<Type> Types { get; } = [];

                // Symbol sub-types
                public IEnumerable<Symbol> Labels => this.SelectSymbolsByType("lab");
                public IEnumerable<Symbol> Equates => this.SelectSymbolsByType("equ");

                // Scope clarification
                public List<Scope> AddressableScopes { get; } = [];

                // Scope cache for precomputed ranges
                private readonly int?[] _scopeAddressCache = new int?[0x10000];

                #endregion

                #region Lookups

                private static IEnumerable<T> SelectNameMatching<T>(string name, IEnumerable<T> items) where T : NamedSection => from item in items where item.Name == name select item;

                private static IEnumerable<T> SelectIdMatching<T>(int id, IEnumerable<T> items) where T : IdentifiableSection => from item in items where item.ID == id select item;

                private IEnumerable<Symbol> SelectSymbolsByType(string type) => from symbol in this.Symbols where symbol.Type == type select symbol;

                #region Label lookup

                private IEnumerable<Symbol> LookupLabelsByAddress(int address) => from label in this.Labels where label.Value == address select label;

                public Symbol? LookupLabelByAddress(int address)
                {
                    var labels = this.LookupLabelsByAddress(address).ToList();
                    return labels.Count > 0 ? labels[0] : null;
                }

                private IEnumerable<Symbol> LookupLabelsByName(string name) => SelectNameMatching(name, this.Labels);

                public Symbol? LookupLabelByName(string name)
                {
                    var labels = this.LookupLabelsByName(name).ToList();
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
                        Debug.Assert(symbol != null);
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
                    var index = _scopeAddressCache[address] ?? (_scopeAddressCache[address] = this.LocateScopeByAddress(address));
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

                private static List<Scope> EvaluateScope(Scope start)
                {
                    var returned = new List<Scope>();
                    for (var current = start; current.Parent != null; current = current.Parent)
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
                    return symbol != null;
                }

                public string MaybeGetQualifiedLabelByAddress(ushort absolute) => this.TryGetQualifiedLabelByAddress(absolute, out var label) ? label : string.Empty;

                public bool TryGetQualifiedEquateValue(ushort value, out string name)
                {
                    var symbol = this.LookupEquateByValue(value);
                    name = PrefixNamespace(symbol);
                    return symbol != null;
                }

                #endregion

                #endregion

                #region Metadata lookup

                private int InformationCount(string key)
                {
                    if (this._information == null)
                    {
                        throw new InvalidOperationException("Information section has not been initialised");
                    }
                    return this._information.Count(key);
                }

                private void VerifyInformationCount(string key, int extracted)
                {
                    if (extracted != this.InformationCount(key))
                    {
                        throw new InvalidOperationException($"Invalid symbol file format (Information/{key} section count mismatch)");
                    }
                }

                #endregion

                #region Section extractors

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
                    if (this._parsed == null)
                    {
                        throw new InvalidOperationException("Parsed dictionary has not been frozen");
                    }

                    if (!this._parsed.TryGetValue(key, out var parsed))
                    {
                        throw new ArgumentOutOfRangeException(nameof(key), key, "Debugging section is unavailable");
                    }

                    Debug.Assert(into.Count == 0);
                    into.Capacity = parsed.Count;
                    foreach (var (id, information) in parsed)
                    {
                        Debug.Assert(into.Count == id);
                        var entry = (T?)Activator.CreateInstance(typeof(T), this);
                        Debug.Assert(entry != null);
                        entry.Parse(information);
                        into.Add(entry);
                    }
                    this.VerifyInformationCount(key, into.Count);
                }

                #endregion

                #region Parser driver

                public async Task ParseAsync(string? path)
                {
                    if (this.Parsed)
                    {
                        throw new InvalidOperationException("A file has already been parsed.");
                    }

                    if (string.IsNullOrEmpty(path))
                    {
                        return;
                    }

                    using var reader = new StreamReader(path);
                    await this.ParseAsync(reader);
                }

                private async Task ParseAsync(StreamReader reader)
                {
                    await this.ParseLinesAsync(reader);

                    this.ExtractFiles();
                    this.ExtractLines();
                    this.ExtractModules();
                    this.ExtractSegments();
                    this.ExtractSpans();
                    this.ExtractScopes();
                    this.ExtractSymbols();
                    this.ExtractTypes();

                    // Parsed dictionary is no longer needed
                    this._parsed.Clear();

                    // We are now mostly parsed
                    this.Parsed = true;

                    // Oh, except for marking addressable scopes
                    this.BuildAddressableScopes();
                }

                private async Task ParseLinesAsync(StreamReader reader)
                {
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line == null)
                        {
                            break;
                        }

                        this.ParseLine(line.Split(' ', '\t'));
                    }
                }

                private void BuildAddressableScopes()
                {
                    if (!this.Parsed)
                    {
                        throw new InvalidOperationException("Fully parsed scopes are unavailable");
                    }
                    var scopes = from scope in this.Scopes where scope.Symbol != null select scope;
                    this.AddressableScopes.AddRange(scopes);
                }

                private void ParseLine(string[] elements)
                {
                    ArgumentNullException.ThrowIfNull(elements, nameof(elements));
                    ArgumentOutOfRangeException.ThrowIfNotEqual(elements.Length, 2, nameof(elements));

                    var key = elements[0];
                    var parts = elements[1].Split(',');

                    if (key is "version")
                    {
                        this._version = new(this);
                        this._version.Parse(BuildDictionary(parts));
                    }
                    else if (key is "info")
                    {
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
                    if (!this._parsed.TryGetValue(key, out var section))
                    {
                        this._parsed[key] = [];
                        section = this._parsed[key];
                    }

                    var dictionary = BuildDictionary(parts);
                    if (!dictionary.TryGetValue("id", out var id))
                    {
                        throw new InvalidOperationException("Invalid symbol file format (definition does not have id)");
                    }

                    var identifier = int.Parse(id);

                    if (this._information == null)
                    {
                        throw new InvalidOperationException("Invalid symbol file format (info section has not been parsed)");
                    }

                    var count = this._information.Count(key);
                    if ((identifier + 1) > count)
                    {
                        throw new InvalidOperationException($"Invalid symbol file format (No count information available for {section})");
                    }

                    if (!section.TryAdd(identifier, dictionary))
                    {
                        throw new InvalidOperationException($"Invalid symbol file format (definition id ({identifier}) has clashed)");
                    }
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
                        dictionary[key] = value;
                    }

                    return dictionary;
                }
#endregion
            }
        }
    }
}