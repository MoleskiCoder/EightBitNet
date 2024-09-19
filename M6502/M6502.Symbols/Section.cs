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
            using System.Reflection;

            public class Section
            {
                protected static readonly Dictionary<System.Type, ReflectedSectionProperties> _sectionPropertiesCache = [];
                protected static readonly DateTime _unixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                protected readonly Parser _container;

                protected readonly Dictionary<string, int> _integers = [];
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

                public object? GetValue(string key) => this.SectionProperties.GetValue(this, key);

                public T? MaybeGetValue<T>(string key) => this.SectionProperties.MaybeGetValue<T>(this, key);

                public T GetValueT<T>(string key) => this.SectionProperties.GetValueT<T>(this, key);

                public virtual void Parse(IDictionary<string, string> entries)
                {
                    this.ProcessAttributesOfProperties();
                    foreach (var entry in entries)
                    {
                        this.Parse(entry);
                    }
                }

                private void Parse(KeyValuePair<string, string> entry) => this.Parse(entry.Key, entry.Value);

                private void Parse(string key, string value)
                {
                    var sectionProperties = this.SectionProperties;
                    var propertyName = sectionProperties.GetPropertyNameFromEntryName(key);
                    var propertyAvailable = sectionProperties.Properties.TryGetValue(propertyName, out var property);
                    if (!propertyAvailable)
                    {
                        throw new InvalidOperationException($"Unable to locate property name {propertyName} using reflection");
                    }
                    Debug.Assert(property != null);
                    this.Parse(property, key, value);
                }

                private void Parse(PropertyInfo property, string key, string value)
                {
                    if (property.CanWrite)
                    {
                        this.ParseValueProperty(property, key, value);                    }
                    else
                    {
                        this.ParseReferenceProperty(key, value);
                    }
                }

                private void ParseReferenceProperty(string key, string value)
                {
                    var sectionProperties = this.SectionProperties;
                    if (sectionProperties.IntegerKeys.Contains(key))
                    {
                        this._integers.Add(key, ExtractInteger(value));
                    }
                    else if (sectionProperties.MultipleKeys.Contains(key))
                    {
                        this._multiples.Add(key, ExtractCompoundInteger(value));
                    }
                    else
                    {
                        throw new InvalidOperationException($"Doesn't appear to be a valid reference type: {key}");
                    }
                }

                private void ParseValueProperty(PropertyInfo property, string key, string value)
                {
                    var sectionProperties = this.SectionProperties;
                    if (sectionProperties.StringKeys.Contains(key))
                    {
                        property?.SetValue(this, ExtractString(value));
                    }
                    else if (sectionProperties.EnumerationKeys.Contains(key))
                    {
                        property?.SetValue(this, ExtractEnumeration(value));
                    }
                    else if (sectionProperties.IntegerKeys.Contains(key))
                    {
                        property?.SetValue(this, ExtractInteger(value));
                    }
                    else if (sectionProperties.HexIntegerKeys.Contains(key))
                    {
                        property?.SetValue(this, ExtractHexInteger(value));
                    }
                    else if (sectionProperties.LongKeys.Contains(key))
                    {
                        property?.SetValue(this, ExtractLong(value));
                    }
                    else if (sectionProperties.HexLongKeys.Contains(key))
                    {
                        property?.SetValue(this, ExtractHexLong(value));
                    }
                    else if (sectionProperties.DateTimeKeys.Contains(key))
                    {
                        property?.SetValue(this, ExtractDateTime(value));
                    }
                    else if (sectionProperties.MultipleKeys.Contains(key))
                    {
                        property?.SetValue(this, ExtractCompoundInteger(value));
                    }
                    else
                    {
                        throw new InvalidOperationException($"Section: {key} has not been categorised");
                    }
                }

                protected int? MaybeTakeInteger(string key) => this._integers.TryGetValue(key, out var value) ? value : null;
                protected List<int>? MaybeTakeMultiple(string key) => this._multiples.TryGetValue(key, out var value) ? value : null;

                protected int TakeInteger(string key) => this.MaybeTakeInteger(key) ?? throw new ArgumentOutOfRangeException(nameof(key), key, "Missing integer entry in section");

                protected static string ExtractString(string value) => value.Trim('"');
                protected static string ExtractEnumeration(string value) => value;

                private static T ExtractHexValue<T>(string value) where T : INumberBase<T> => T.Parse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                private static T ExtractNumericValue<T>(string value) where T : IParsable<T> => T.Parse(value, CultureInfo.InvariantCulture);

                protected static int ExtractHexInteger(string value) => ExtractHexValue<int>(value);
                protected static long ExtractHexLong(string value) => ExtractHexValue<long>(value);
                protected static int ExtractInteger(string value) => ExtractNumericValue<int>(value);
                protected static long ExtractLong(string value) => ExtractNumericValue<long>(value);
                protected static DateTime ExtractDateTime(string value) => _unixEpoch.AddSeconds(ExtractHexLong(value));
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