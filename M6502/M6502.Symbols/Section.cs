namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            using System.Globalization;

            public class Section
            {
                protected Parser? _parent;

                protected readonly Dictionary<string, string> _strings = [];
                protected readonly Dictionary<string, int> _integers = [];
                protected readonly Dictionary<string, long> _longs = [];
                protected readonly Dictionary<string, List<int>> _multiples = [];

                protected readonly HashSet<string> _string_keys = [];
                protected readonly HashSet<string> _enumeration_keys = [];
                protected readonly HashSet<string> _integer_keys = [];
                protected readonly HashSet<string> _long_keys = [];
                protected readonly HashSet<string> _hex_integer_keys = [];
                protected readonly HashSet<string> _hex_long_keys = [];
                protected readonly HashSet<string> _multiple_keys = [];

                public virtual void Parse(Parser parent, Dictionary<string, string> entries)
                {
                    this._parent = parent;
                    foreach (var entry in entries)
                    {
                        this.Parse(entry);
                    }
                }

                private void Parse(KeyValuePair<string, string> entry)
                {
                    var key = entry.Key;
                    var value = entry.Value;
                    if (_string_keys.Contains(key))
                    {
                        this._strings[key] = ExtractString(value);
                    }
                    else if (_enumeration_keys.Contains(key))
                    {
                        this._strings[key] = ExtractEnumeration(value);
                    }
                    else if (_integer_keys.Contains(key))
                    {
                        this._integers[key] = ExtractInteger(value);
                    }
                    else if (_hex_integer_keys.Contains(key))
                    {
                        this._integers[key] = ExtractHexInteger(value);
                    }
                    else if (_long_keys.Contains(key))
                    {
                        this._longs[key] = ExtractLong(value);
                    }
                    else if (_hex_long_keys.Contains(key))
                    {
                        this._longs[key] = ExtractHexLong(value);
                    }
                    else if (_multiple_keys.Contains(key))
                    {
                        this._multiples[key] = ExtractCompoundInteger(value);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Section: {key} has not been categorised");
                    }
                }

                protected int? MaybeTakeInteger(string key) => this._integers.TryGetValue(key, out var value) ? value : null;
                protected long? MaybeTakeLong(string key) => this._longs.TryGetValue(key, out var value) ? value : null;
                protected string? MaybeTakeString(string key) => this._strings.TryGetValue(key, out var value) ? value : null;
                protected List<int>? MaybeTakeMultiple(string key) => this._multiples.TryGetValue(key, out var value) ? value : null;

                protected int TakeInteger(string key) => this.MaybeTakeInteger(key) ?? throw new InvalidOperationException($"Section is missing an integer entry named {key}");
                protected long TakeLong(string key) => this.MaybeTakeLong(key) ?? throw new InvalidOperationException($"Section is missing an long integer entry named {key}");
                protected string TakeString(string key) => this.MaybeTakeString(key) ?? throw new InvalidOperationException($"Section is missing a string entry named {key}");
                protected List<int> TakeMultiple(string key) => this.MaybeTakeMultiple(key) ?? throw new InvalidOperationException($"Section is missing a multiple entry named {key}");

                protected static string ExtractString(string value) => value.Trim('"');
                protected static string ExtractEnumeration(string value) => value;
                protected static int ExtractHexInteger(string value) => int.Parse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                protected static long ExtractHexLong(string value) => long.Parse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                protected static int ExtractInteger(string value) => int.Parse(value);
                protected static long ExtractLong(string value) => long.Parse(value);
                protected static List<string> ExtractCompoundString(string value) => new(value.Split('+'));
                protected static List<int> ExtractCompoundInteger(string value)
                {
                    var elements = ExtractCompoundString(value);
                    var returned = new List<int>(elements.Count);
                    foreach (var element in elements)
                    {
                        returned.Add(ExtractInteger(element));
                    }
                    return returned;
                }
            }
        }
    }
}