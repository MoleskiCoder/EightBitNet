namespace M6502.Symbols
{
    using System.Diagnostics;
    using System.Reflection;

    public sealed class ReflectedSectionProperties
    {
        private readonly Dictionary<string, string> _entriesToProperties = [];

        public HashSet<string> StringKeys { get; } = [];
        public HashSet<string> EnumerationKeys { get; } = [];
        public HashSet<string> IntegerKeys { get; } = [];
        public HashSet<string> HexIntegerKeys { get; } = [];
        public HashSet<string> LongKeys { get; } = [];
        public HashSet<string> HexLongKeys { get; } = [];
        public HashSet<string> DateTimeKeys { get; } = [];
        public HashSet<string> MultipleKeys { get; } = [];

        public Dictionary<string, PropertyInfo> Properties { get; } = [];

        internal SectionAttribute? ClassAttribute { get; private set; }

        internal Dictionary<string, Tuple<string, System.Type>> ReferenceAttributes { get; } = [];

        internal Dictionary<string, Tuple<string, System.Type>> ReferencesAttributes { get; } = [];

        public void Build(System.Type type)
        {
            var sectionAttributes = type.GetCustomAttributes(typeof(SectionAttribute), true);
            Debug.Assert(sectionAttributes != null, "No section attributes available");
            Debug.Assert(sectionAttributes.Length == 1, "Must be a single section attribute available");
            var sectionAttribute = sectionAttributes[0];
            Debug.Assert(sectionAttribute != null, "Section attribute cannot be null");
            this.ClassAttribute = (SectionAttribute)sectionAttribute;

            foreach (var property in type.GetProperties())
            {
                this.ProcessPropertyAttributes(property);
            }
        }

        public string GetPropertyNameFromEntryName(string name)
        {
            var found = this._entriesToProperties.TryGetValue(name, out var propertyName);
            if (!found)
            {
                throw new ArgumentOutOfRangeException(nameof(name), name, "Missing property mapping");
            }
            Debug.Assert(propertyName != null);
            return propertyName;
        }

        private PropertyInfo GetPropertyFromEntryName(string key)
        {
            var propertyName = this.GetPropertyNameFromEntryName(key);
            var propertyAvailable = this.Properties.TryGetValue(propertyName, out var property);
            Debug.Assert(propertyAvailable);
            Debug.Assert(property != null);
            return property;
        }

        public object? GetValue(object? obj, string key)
        {
            var property = this.GetPropertyFromEntryName(key);
            Debug.Assert(property.CanRead);
            return property.GetValue(obj);
        }

        public void SetValue(object? obj, string key, object? value)
        {
            var property = this.GetPropertyFromEntryName(key);
            Debug.Assert(property.CanWrite);
            property.SetValue(obj, value); ;
        }

        public T? MaybeGetValue<T>(object? obj, string key) => (T?)this.GetValue(obj, key);

        public T GetValueT<T>(object? obj, string key)
        {
            var possible = this.MaybeGetValue<T>(obj, key);
            return possible != null ? possible : throw new ArgumentOutOfRangeException(nameof(key), key, "Property read issue");
        }

        private void ProcessPropertyAttributes(PropertyInfo property)
        {
            var attributes = property.GetCustomAttributes(typeof(SectionPropertyAttribute), true);
            if (attributes.Length > 0)
            {
                Debug.Assert(attributes.Length == 1, "Too many section property attributes");
                this.Properties.Add(property.Name, property);
                this.ProcessSectionPropertyAttribute(property.PropertyType, property.Name, attributes[0]);
            }
        }

        private void ProcessSectionPropertyAttribute(System.Type? type, string name, object attribute)
        {
            ArgumentNullException.ThrowIfNull(type, nameof(type));
            this.ProcessSectionPropertyAttribute(type, name, (SectionPropertyAttribute)attribute);
        }

        public void AddStringKey(string key)
        {
            var added = this.StringKeys.Add(key);
            Debug.Assert(added, $"<{key}> already has an entry");
        }

        public void AddEnumerationKey(string key)
        {
            var added = this.EnumerationKeys.Add(key);
            Debug.Assert(added, $"<{key}> already has an entry");
        }

        public void AddMultiplesKey(string key)
        {
            var added = this.MultipleKeys.Add(key);
            Debug.Assert(added, $"<{key}> already has an entry");
        }

        public void AddHexIntegerKey(string key)
        {
            var added = this.HexIntegerKeys.Add(key);
            Debug.Assert(added, $"<{key}> already has an entry");
        }

        public void AddIntegerKey(string key)
        {
            var added = this.IntegerKeys.Add(key);
            Debug.Assert(added, $"<{key}> already has an entry");
        }

        public void AddHexLongKey(string key)
        {
            var added = this.HexLongKeys.Add(key);
            Debug.Assert(added, $"<{key}> already has an entry");
        }

        public void AddLongKey(string key)
        {
            var added = this.LongKeys.Add(key);
            Debug.Assert(added, $"<{key}> already has an entry");
        }

        public void AddDateTimeKey(string key)
        {
            var added = this.DateTimeKeys.Add(key);
            Debug.Assert(added, $"<{key}> already has an entry");
        }

        private void ProcessSectionPropertyAttribute(System.Type originalType, string name, SectionPropertyAttribute attribute)
        {
            var key = attribute.Key;
            this._entriesToProperties.Add(key, name);

            if (attribute is SectionReferenceAttribute)
            {
                this.ReferenceAttributes.Add(key, new Tuple<string, System.Type>(name, originalType));
            } else if (attribute is SectionReferencesAttribute)
            {
                this.ReferencesAttributes.Add(key, new Tuple<string, System.Type>(name, originalType));
            }

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
                Debug.Assert(type == typeof(string), "Enumeration must be of type string");
                this.AddEnumerationKey(key);
                return;
            }

            var hex = attribute.Hexadecimal;

            if (type == typeof(string))
            {
                Debug.Assert(!enumeration, "Enumeration case should already have been handled");
                Debug.Assert(!hex, "Cannot have a hexadecimal string type");
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
            else if (type == typeof(DateTime) || type == typeof(Nullable<DateTime>))
            {
                this.AddDateTimeKey(key);
            }
            else
            {
                throw new NotImplementedException($"Property type <{type}> has not been implemented");
            }
        }
    }
}
