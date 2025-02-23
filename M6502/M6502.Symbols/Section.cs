namespace M6502.Symbols
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Numerics;
    using System.Reflection;

    public class Section
    {
        protected static readonly Dictionary<System.Type, ReflectedSectionProperties> SectionPropertiesCache = [];
        protected static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly Parser _container;

        // Needed to evaluate references on a second pass
        private readonly Dictionary<string, int> _references = [];
        private readonly Dictionary<string, List<int>> _multipleReferences = [];

        protected Parser Container => this._container;

        protected Dictionary<string, string>? Parsed { get; private set; }

        protected ReflectedSectionProperties SectionProperties => GetSectionProperties(this.GetType());

        protected static ReflectedSectionProperties GetSectionProperties(System.Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            var obtained = SectionPropertiesCache.TryGetValue(type, out var properties);
            Debug.Assert(obtained, $"Section properties for {type.Name} have not been built");
            Debug.Assert(properties is not null);
            return properties;
        }

        protected Section(Parser container) => this._container = container;

        protected void ProcessAttributesOfProperties()
        {
            var type = this.GetType();
            var entry = new ReflectedSectionProperties();
            Debug.Assert(SectionPropertiesCache is not null);
            if (SectionPropertiesCache.TryAdd(type, entry))
            {
                entry.Build(type);
            }
        }

        public T GetValueT<T>(string key) => this.SectionProperties.GetValueT<T>(this, key);

        public virtual void Parse(Dictionary<string, string>? entries)
        {
            ArgumentNullException.ThrowIfNull(entries);
            this.Parsed = entries;
            this._container.SectionEntries.Add(this);
            this.ProcessAttributesOfProperties();
            foreach (var entry in entries)
            {
                var (key, value) = entry;
                this.Parse(key, value);
            }
        }

        private void Parse(string key, string value)
        {
            var propertyName = this.SectionProperties.GetPropertyNameFromEntryName(key);
            var propertyAvailable = this.SectionProperties.Properties.TryGetValue(propertyName, out var property);
            if (!propertyAvailable)
            {
                throw new InvalidOperationException($"Unable to locate property name {propertyName} using reflection");
            }
            Debug.Assert(property is not null);
            this.Parse(property, key, value);
        }

        private void Parse(PropertyInfo property, string key, string value)
        {
            var reference = this.SectionProperties.ReferenceAttributes.ContainsKey(key);
            var references = this.SectionProperties.ReferencesAttributes.ContainsKey(key);
            var lazy = reference || references;

            if (lazy)
            {
                if (reference)
                {
                    this._references.Add(key, ExtractInteger(value));
                }
                else if (references)
                {
                    var multiple = ExtractCompoundInteger(value);
                    this._multipleReferences.Add(key, [.. multiple]);
                }
                else
                {
                    throw new InvalidOperationException($"Getting here should be impossible!  Key {key} is lazy, but not a reference");
                }
                return;
            }

            if (this.SectionProperties.StringKeys.Contains(key))
            {
                property?.SetValue(this, ExtractString(value));
            }
            else if (this.SectionProperties.EnumerationKeys.Contains(key))
            {
                property?.SetValue(this, ExtractEnumeration(value));
            }
            else if (this.SectionProperties.IntegerKeys.Contains(key))
            {
                property?.SetValue(this, ExtractInteger(value));
            }
            else if (this.SectionProperties.HexIntegerKeys.Contains(key))
            {
                property?.SetValue(this, ExtractHexInteger(value));
            }
            else if (this.SectionProperties.LongKeys.Contains(key))
            {
                property?.SetValue(this, ExtractLong(value));
            }
            else if (this.SectionProperties.HexLongKeys.Contains(key))
            {
                property?.SetValue(this, ExtractHexLong(value));
            }
            else if (this.SectionProperties.DateTimeKeys.Contains(key))
            {
                property?.SetValue(this, ExtractDateTime(value));
            }
            else if (this.SectionProperties.MultipleKeys.Contains(key))
            {
                property?.SetValue(this, ExtractCompoundInteger(value));
            }
            else
            {
                throw new InvalidOperationException($"Section: {key} has not been categorised");
            }
        }

        protected static string ExtractString(string? value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return value.Trim('"');
        }

        protected static string ExtractEnumeration(string value) => value;

        private static T ExtractHexValue<T>(string value) where T : INumberBase<T> => T.Parse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        private static T ExtractNumericValue<T>(string value) where T : IParsable<T> => T.Parse(value, CultureInfo.InvariantCulture);

        protected static int ExtractHexInteger(string value) => ExtractHexValue<int>(value);
        protected static long ExtractHexLong(string value) => ExtractHexValue<long>(value);
        protected static int ExtractInteger(string value) => ExtractNumericValue<int>(value);
        protected static long ExtractLong(string value) => ExtractNumericValue<long>(value);
        protected static DateTime ExtractDateTime(string value) => UnixEpoch.AddSeconds(ExtractHexLong(value));

        protected static string[] ExtractCompoundString(string? value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return value.Split('+');
        }

        protected static ReadOnlyCollection<int> ExtractCompoundInteger(string value)
        {
            var elements = ExtractCompoundString(value);
            var returned = new List<int>(elements.Length);
            foreach (var element in elements)
            {
                returned.Add(ExtractInteger(element));
            }
            return returned.AsReadOnly();
        }
    }
}