namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            using System.Diagnostics;

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
                public List<Symbol> Labels { get; } = [];
                public List<Symbol> Equates { get; } = [];

                #endregion

                #region Lookups

                #region Label lookup

                public List<Symbol> LookupLabels(int address)
                {
                    var returned = new List<Symbol>();
                    foreach (var label in this.Labels)
                    {
                        if (label.Value == address)
                        {
                            returned.Add(label);
                        }
                    }
                    return returned;
                }

                public Symbol? LookupLabel(int address)
                {
                    var labels = this.LookupLabels(address);
                    return labels.Count > 0 ? labels[0] : null;
                }

                public List<Symbol> LookupLabels(string name)
                {
                    var returned = new List<Symbol>();
                    foreach (var label in this.Labels)
                    {
                        if (label.Name == name)
                        {
                            returned.Add(label);
                        }
                    }
                    return returned;
                }

                public Symbol? LookupLabel(string name)
                {
                    var labels = this.LookupLabels(name);
                    return labels.Count > 0 ? labels[0] : null;
                }

                #endregion

                #region Constant lookup

                public List<Symbol> LookupEquates(int constant)
                {
                    var returned = new List<Symbol>();
                    foreach (var equate in this.Equates)
                    {
                        if (equate.Value == constant)
                        {
                            returned.Add(equate);
                        }
                    }
                    return returned;
                }

                public Symbol? LookupEquate(int constant)
                {
                    var equates = this.LookupEquates(constant);
                    return equates.Count > 0 ? equates[0] : null;
                }

                #endregion

                #region Scope lookup

                public Scope? LookupScope(string name)
                {
                    foreach (var scope in this.Scopes)
                    {
                        if (scope.Name == name)
                        {
                            return scope;
                        }
                    }
                    return null;
                }

                public Scope? LookupScope(int address)
                {
                    foreach (var scope in this.Scopes)
                    {
                        var symbol = scope.Symbol;
                        if (symbol != null)
                        {
                            var symbolAddress = symbol.Value;
                            var size = scope.Size;
                            if ((address >= symbolAddress) && (address < symbolAddress + size))
                            {
                                return scope;
                            }
                        }
                    }
                    return null;
                }

                #endregion

                #region Scope evaluation

                public static List<Scope> EvaluateScope(Scope start)
                {
                    var returned = new List<Scope>();
                    for (var current = start; current.Parent != null; current = current.Parent)
                    {
                        returned.Add(current);
                    }
                    return returned;
                }

                public static List<Scope> EvaluateScope(Symbol symbol) => EvaluateScope(symbol.Scope);

                #endregion

                #region Namespace evaluation from scope

                public static string BuildNamespace(Symbol symbol)
                {
                    var returned = string.Empty;
                    var scopes = EvaluateScope(symbol);
                    for (var i = scopes.Count - 1; i >= 0; i--)
                    {
                        var scope = scopes[i];
                        var name = scope.Name;
                        Debug.Assert(name.Length > 0);
                        returned += name;
                        var last = i == 0;
                        if (!last)
                        {
                            returned += '.';
                        }
                    }
                    return returned;
                }

                public static string PrefixNamespace(Symbol symbol)
                {
                    var prefix = BuildNamespace(symbol);
                    var name = symbol.Name;
                    return string.IsNullOrEmpty(prefix) ? name : $"{prefix}.{name}";
                }

                #endregion

                #region Qualified symbol lookup

                public bool TryGetQualifiedLabel(ushort absolute, out string label)
                {
                    var symbol = this.LookupLabel(absolute);
                    var available = symbol != null;
                    label = available ? PrefixNamespace(symbol) : string.Empty;
                    return available;
                }

                public string MaybeGetQualifiedLabel(ushort absolute) => this.TryGetQualifiedLabel(absolute, out var label) ? label : string.Empty;

                public bool TryGetQualifiedEquate(ushort value, out string name)
                {
                    var symbol = this.LookupEquate(value);
                    var available = symbol != null;
                    name = available ? PrefixNamespace(symbol) : string.Empty;
                    return available;
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
                    if (!this._parsed.TryGetValue(key, out var parsed))
                    {
                        throw new InvalidOperationException($"Debugging section: '{key}' is unavailable");
                    }
                    foreach (var element in parsed)
                    {
                        var id = element.Key;
                        Debug.Assert(into.Count == id);
                        var information = element.Value;
                        var entry = new T();
                        entry.Parse(this, information);
                        into.Add(entry);
                    }
                    this.VerifyInformationCount(key, into.Count);
                }

                #endregion

                #region Parser driver

                public void Parse(string path)
                {
                    if (this.Parsed)
                    {
                        throw new InvalidOperationException("A file has already been parsed.");
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

                    this.ExtractFiles();
                    this.ExtractLines();
                    this.ExtractModules();
                    this.ExtractSegments();
                    this.ExtractSpans();
                    this.ExtractScopes();
                    this.ExtractSymbols();
                    this.ExtractTypes();

                    this.Parsed = true;
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

                private static Dictionary<string, string> BuildDictionary(string[] parts)
                {
                    var dictionary = new Dictionary<string, string>();
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