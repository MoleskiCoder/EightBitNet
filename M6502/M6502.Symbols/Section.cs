namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            using System.Globalization;
            using System.Reflection;

            public class Section
            {
                protected readonly Parser _container;

                protected readonly Dictionary<string, string> _strings = [];
                private readonly HashSet<string> _string_keys = [];
                private readonly HashSet<string> _enumeration_keys = [];

                protected readonly Dictionary<string, int> _integers = [];
                private readonly HashSet<string> _integer_keys = [];
                private readonly HashSet<string> _hex_integer_keys = [];

                protected readonly Dictionary<string, long> _longs = [];
                private readonly HashSet<string> _long_keys = [];
                private readonly HashSet<string> _hex_long_keys = [];

                protected readonly Dictionary<string, List<int>> _multiples = [];
                private readonly HashSet<string> _multiple_keys = [];

                protected Section(Parser container)
                {
                    this.ProcessAttributesOfProperties();
                    this._container = container;
                }

                public virtual void Parse(IDictionary<string, string> entries)
                {
                    foreach (var entry in entries)
                    {
                        this.Parse(entry);
                    }
                }

                private void ProcessAttributesOfProperties()
                {
                    var type = this.GetType();
                    foreach (var property in type.GetProperties())
                    {
                        this.ProcessPropertyAttributes(property);
                    }
                }

                private void ProcessPropertyAttributes(PropertyInfo property)
                {
                    var attributes = property.GetCustomAttributes(typeof(SectionPropertyAttribute), true);
                    if (attributes.Length > 0)
                    {
                        this.ProcessSectionPropertyAttribute(property.PropertyType, attributes[0]);
                    }
                }

                private void ProcessSectionPropertyAttribute(System.Type? type, object attribute)
                {
                    ArgumentNullException.ThrowIfNull(type, nameof(type));
                    this.ProcessSectionPropertyAttribute(type, (SectionPropertyAttribute)attribute);
                }

                protected void AddStringKey(string key)
                {
                    if (!this._string_keys.Add(key))
                    {
                        throw new InvalidOperationException($"<{key}> already has an entry");
                    }
                }

                protected void AddEnumerationKey(string key)
                {
                    if (!this._enumeration_keys.Add(key))
                    {
                        throw new InvalidOperationException($"<{key}> already has an entry");
                    }
                }

                protected void AddMultiplesKey(string key)
                {
                    if (!this._multiple_keys.Add(key))
                    {
                        throw new InvalidOperationException($"<{key}> already has an entry");
                    }
                }

                protected void AddHexIntegerKey(string key)
                {
                    if (!this._hex_integer_keys.Add(key))
                    {
                        throw new InvalidOperationException($"<{key}> already has an entry");
                    }
                }

                protected void AddIntegerKey(string key)
                {
                    if (!this._integer_keys.Add(key))
                    {
                        throw new InvalidOperationException($"<{key}> already has an entry");
                    }
                }

                protected void AddHexLongKey(string key)
                {
                    if (!this._hex_long_keys.Add(key))
                    {
                        throw new InvalidOperationException($"<{key}> already has an entry");
                    }
                }

                protected void AddLongKey(string key)
                {
                    if (!this._long_keys.Add(key))
                    {
                        throw new InvalidOperationException($"<{key}> already has an entry");
                    }
                }

                private void ProcessSectionPropertyAttribute(System.Type originalType, SectionPropertyAttribute attribute)
                {
                    var key = attribute.Key;

                    var multiples = attribute.Many;
                    if (multiples)
                    {
                        // Type is irrelevant
                        this.AddMultiplesKey(key);
                        return;
                    }

                    var type = attribute.Type ?? originalType;

                    var enumeration = attribute.Enumeration;
                    if (enumeration)
                    {
                        System.Diagnostics.Debug.Assert(type == typeof(string), "Enumeration must be of type string");
                        this.AddEnumerationKey(key);
                        return;
                    }

                    var hex = attribute.Hexadecimal;

                    if (type == typeof(string))
                    {
                        System.Diagnostics.Debug.Assert(!enumeration, "Enumeration case should already have been handled");
                        System.Diagnostics.Debug.Assert(!hex, "Cannot have a hexadecimal string type");
                        this.AddStringKey(key);
                    }
                    else if (type == typeof(int) || type == typeof(Nullable<int>))
                    {
                        if (hex)
                        {
                            this.AddHexIntegerKey(key);
                        }
                        else
                        {
                            this.AddIntegerKey(key);
                        }
                    }
                    else if (type == typeof(long) || type == typeof(Nullable<long>))
                    {
                        if (hex)
                        {
                            this.AddHexLongKey(key);
                        }
                        else
                        {
                            this.AddLongKey(key);
                        }
                    }
                    else
                    {
                        throw new NotImplementedException($"Property type <{type}> has not been implemented");
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