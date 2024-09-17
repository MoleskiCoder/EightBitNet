namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            using System;
            using System.Diagnostics;
            using System.Globalization;
            using System.Numerics;

            public class Section
            {
                protected static readonly Dictionary<System.Type, ReflectedSectionProperties> _sectionPropertiesCache = [];

                protected readonly Parser _container;

                protected readonly Dictionary<string, string> _strings = [];
                protected readonly Dictionary<string, int> _integers = [];
                protected readonly Dictionary<string, long> _longs = [];
                protected readonly Dictionary<string, List<int>> _multiples = [];

                protected ReflectedSectionProperties SectionProperties
                {
                    get
                    {
                        var type = this.GetType();
                        var obtained = _sectionPropertiesCache.TryGetValue(type, out var properties);
                        Debug.Assert(obtained, $"Section properties for {type.Name} have not been built");
                        Debug.Assert(properties != null);
                        return properties;
                    }
                }
                protected Section(Parser container) => this._container = container;

                protected void ProcessAttributesOfProperties()
                {
                    var type = this.GetType();
                    Debug.Assert(_sectionPropertiesCache != null);
                    if (!_sectionPropertiesCache.ContainsKey(type))
                    {
                        _sectionPropertiesCache.Add(type, new ReflectedSectionProperties(type));
                    }
                }

                public virtual void Parse(IDictionary<string, string> entries)
                {
                    this.ProcessAttributesOfProperties();
                    foreach (var entry in entries)
                    {
                        this.Parse(entry);
                    }
                }

                private void Parse(KeyValuePair<string, string> entry)
                {
                    var key = entry.Key;
                    var value = entry.Value;
                    if (this.SectionProperties.StringKeys.Contains(key))
                    {
                        this._strings.Add(key, ExtractString(value));
                    }
                    else if (this.SectionProperties.EnumerationKeys.Contains(key))
                    {
                        this._strings.Add(key, ExtractEnumeration(value));
                    }
                    else if (this.SectionProperties.IntegerKeys.Contains(key))
                    {
                        this._integers.Add(key, ExtractInteger(value));
                    }
                    else if (this.SectionProperties.HexIntegerKeys.Contains(key))
                    {
                        this._integers.Add(key, ExtractHexInteger(value));
                    }
                    else if (this.SectionProperties.LongKeys.Contains(key))
                    {
                        this._longs.Add(key, ExtractLong(value));
                    }
                    else if (this.SectionProperties.HexLongKeys.Contains(key))
                    {
                        this._longs.Add(key, ExtractHexLong(value));
                    }
                    else if (this.SectionProperties.MultipleKeys.Contains(key))
                    {
                        this._multiples.Add(key, ExtractCompoundInteger(value));
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

                protected int TakeInteger(string key) => this.MaybeTakeInteger(key) ?? throw new ArgumentOutOfRangeException(nameof(key), key, "Missing integer entry in section");
                protected long TakeLong(string key) => this.MaybeTakeLong(key) ?? throw new ArgumentOutOfRangeException(nameof(key), key, "Missing long integer entry in section");
                protected string TakeString(string key) => this.MaybeTakeString(key) ?? throw new ArgumentOutOfRangeException(nameof(key), key, "Missing string entry in section");
                protected List<int> TakeMultiple(string key) => this.MaybeTakeMultiple(key) ?? throw new ArgumentOutOfRangeException(nameof(key), key, "Missing multiple entry in section");

                protected static string ExtractString(string value) => value.Trim('"');
                protected static string ExtractEnumeration(string value) => value;

                private static T ExtractHexValue<T>(string value) where T : INumberBase<T> => T.Parse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                private static T ExtractNumericValue<T>(string value) where T : IParsable<T> => T.Parse(value, CultureInfo.InvariantCulture);

                protected static int ExtractHexInteger(string value) => ExtractHexValue<int>(value);
                protected static long ExtractHexLong(string value) => ExtractHexValue<long>(value);
                protected static int ExtractInteger(string value) => ExtractNumericValue<int>(value);
                protected static long ExtractLong(string value) => ExtractNumericValue<long>(value);
                protected static string[] ExtractCompoundString(string value) => value.Split('+');

                protected static List<int> ExtractCompoundInteger(string value)
                {
                    var elements = ExtractCompoundString(value);
                    var returned = new List<int>(elements.Length);
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