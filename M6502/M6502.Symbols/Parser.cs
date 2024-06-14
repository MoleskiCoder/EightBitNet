namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            using System;
            using System.Collections.Frozen;
            using System.Diagnostics;

            public sealed class Parser
            {
                #region Variables, properties etc.

                public bool Parsed { get; private set; }

                // Section -> Unique ID list of dictionary entries
                // Being sorted allows us to verify IDs as they arrive
                private readonly Dictionary<string, SortedDictionary<int, FrozenDictionary<string, string>>> _parsed_intermediate = [];
                private FrozenDictionary<string, FrozenDictionary<int, FrozenDictionary<string, string>>>? _parsed;

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
                public List<Symbol> Labels { get; } = [];
                public List<Symbol> Equates { get; } = [];

                // Value lookup structures
                public Dictionary<int, List<Symbol>> Addresses { get; } = [];
                public Dictionary<int, List<Symbol>> Constants { get; } = [];

                // Scope clarification
                public List<Scope> AddressableScopes { get; } = [];

                // Scope cache for precomputed ranges
                private readonly int?[] _scopeAddressCache = new int?[0x10000];

                #endregion

                #region Lookups

                private static IEnumerable<T> SelectNameMatching<T>(string name, IEnumerable<T> items) where T : NamedSection => from item in items where item.Name == name select item;

                #region Label lookup

                public void AddLabel(Symbol symbol)
                {
                    if (symbol.Type != "lab")
                    {
                        throw new ArgumentOutOfRangeException(nameof(symbol), "Not a label");
                    }
                    this.Labels.Add(symbol);
                    this.AddAddress(symbol);
                }

                private void AddAddress(Symbol symbol)
                {
                    var value = symbol.Value;
                    if (this.Addresses.TryGetValue(value, out var symbols))
                    {
                        symbols.Add(symbol);
                    }
                    else
                    {
                        this.Addresses.Add(value, [symbol]);
                    }
                }

                private List<Symbol> LookupLabels(int address) => this.Addresses.TryGetValue(address, out var symbols) ? symbols : [];

                public Symbol? LookupLabel(int address)
                {
                    var labels = this.LookupLabels(address);
                    return labels.Count > 0 ? labels[0] : null;
                }

                private IEnumerable<Symbol> LookupLabels(string name) => SelectNameMatching(name, this.Labels);

                public Symbol? LookupLabel(string name)
                {
                    var labels = this.LookupLabels(name).ToList();
                    return labels.Count > 0 ? labels[0] : null;
                }

                #endregion

                #region Constant lookup

                public void AddEquate(Symbol symbol)
                {
                    if (symbol.Type != "equ")
                    {
                        throw new ArgumentOutOfRangeException(nameof(symbol), "Not an equate");
                    }
                    this.Equates.Add(symbol);
                    this.AddConstant(symbol);
                }

                private void AddConstant(Symbol symbol)
                {
                    var value = symbol.Value;
                    if (this.Constants.TryGetValue(value, out var symbols))
                    {
                        symbols.Add(symbol);
                    }
                    else
                    {
                        this.Constants.Add(value, [symbol]);
                    }
                }

                private List<Symbol> LookupEquates(int constant) => this.Constants.TryGetValue(constant, out var symbols) ? symbols : [];

                public Symbol? LookupEquate(int constant)
                {
                    var equates = this.LookupEquates(constant);
                    return equates.Count > 0 ? equates[0] : null;
                }

                #endregion

                #region Scope lookup

                private int LocateScope(int address)
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

                public Scope? LookupScope(int address)
                {
                    var index = _scopeAddressCache[address] ?? (_scopeAddressCache[address] = this.LocateScope(address));
                    return index == -1 ? null : this.AddressableScopes[index.Value];
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

                public bool TryGetQualifiedLabel(ushort absolute, out string label)
                {
                    var symbol = this.LookupLabel(absolute);
                    label = PrefixNamespace(symbol);
                    return symbol != null;
                }

                public string MaybeGetQualifiedLabel(ushort absolute) => this.TryGetQualifiedLabel(absolute, out var label) ? label : string.Empty;

                public bool TryGetQualifiedEquate(ushort value, out string name)
                {
                    var symbol = this.LookupEquate(value);
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

                private void Extract<T>(string key, List<T> into) where T : IdentifiableSection, new()
                {
                    if (this._parsed == null)
                    {
                        throw new InvalidOperationException("Parsed dictionary has not been frozen");
                    }

                    if (!this._parsed.TryGetValue(key, out var parsed))
                    {
                        throw new InvalidOperationException($"Debugging section: '{key}' is unavailable");
                    }

                    Debug.Assert(into.Count == 0);
                    into.Capacity = parsed.Count;
                    foreach (var (id, information) in parsed)
                    {
                        Debug.Assert(into.Count == id);
                        var entry = new T();
                        entry.Parse(this, information);
                        into.Add(entry);
                    }
                    this.VerifyInformationCount(key, into.Count);
                }

                #endregion

                #region Parser driver

                public void Parse(string? path)
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
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (line == null)
                        {
                            break;
                        }

                        this.ParseLine(line.Split(' ', '\t'));
                    }

                    this.FreezeParsedData();

                    // Intermediate data no longer needed
                    // Only "frozen" parsed data is needed.
#if DEBUG
                    this._parsed_intermediate.Clear();
#endif
                    this.ExtractFiles();
                    this.ExtractLines();
                    this.ExtractModules();
                    this.ExtractSegments();
                    this.ExtractSpans();
                    this.ExtractScopes();
                    this.ExtractSymbols();
                    this.ExtractTypes();

                    // Frozen parsed data is no longer needed
#if DEBUG
                    this._parsed = null;
#endif

                    // We are now mostly parsed
                    this.Parsed = true;

                    // Oh, except for marking addressable scopes
                    this.BuildAddressableScopes();
                }

                private void FreezeParsedData()
                {
                    var intermediateSections = new Dictionary<string, FrozenDictionary<int, FrozenDictionary<string, string>>>(this._parsed_intermediate.Count);
                    foreach (var (name, entries) in this._parsed_intermediate)
                    {
                        intermediateSections.Add(name, FrozenDictionary.ToFrozenDictionary(entries));
                    }
                    this._parsed = FrozenDictionary.ToFrozenDictionary(intermediateSections);
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
                    Debug.Assert(elements != null);
                    if (elements.Length != 2)
                    {
                        throw new InvalidOperationException("Invalid symbol file format (definition does not have section/values format)");
                    }

                    var key = elements[0];
                    var parts = elements[1].Split(',');

                    if (key is "version")
                    {
                        this._version = new Version();
                        this._version.Parse(this, BuildDictionary(parts));
                    }
                    else if (key is "info")
                    {
                        this._information = new Information();
                        this._information.Parse(this, BuildDictionary(parts));
                    }
                    else
                    {
                        this.Parse(key, parts);
                    }
                }

                private void Parse(string key, string[] parts)
                {
                    if (!this._parsed_intermediate.TryGetValue(key, out var section))
                    {
                        this._parsed_intermediate[key] = [];
                        section = this._parsed_intermediate[key];
                    }

                    var dictionary = BuildDictionary(parts);
                    if (!dictionary.TryGetValue("id", out var id))
                    {
                        throw new InvalidOperationException("Invalid symbol file format (definition does not have id)");
                    }

                    var identifier = int.Parse(id);
                    if (section.ContainsKey(identifier))
                    {
                        throw new InvalidOperationException("Invalid symbol file format (definition id has clashed)");
                    }

                    if (this._information == null)
                    {
                        throw new InvalidOperationException("Invalid symbol file format (info section has not been parsed)");
                    }

                    var count = this._information.Count(key);
                    if ((identifier + 1) > count)
                    {
                        throw new InvalidOperationException($"Invalid symbol file format (No count information available for {section})");
                    }

                    section.Add(identifier, dictionary);
                }

                private static FrozenDictionary<string, string> BuildDictionary(string[] parts)
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

                    return FrozenDictionary.ToFrozenDictionary(dictionary);
                }

#endregion
            }
        }
    }
}