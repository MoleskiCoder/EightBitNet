namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            using System.Diagnostics;
            using System.Reflection;

            public sealed class ReflectedSectionProperties
            {
                public HashSet<string> StringKeys { get; } = [];
                public HashSet<string> EnumerationKeys { get; } = [];
                public HashSet<string> IntegerKeys { get; } = [];
                public HashSet<string> HexIntegerKeys { get; } = [];
                public HashSet<string> LongKeys { get; } = [];
                public HashSet<string> HexLongKeys { get; } = [];
                public HashSet<string> DateTimeKeys { get; } = [];
                public HashSet<string> MultipleKeys { get; } = [];

                public ReflectedSectionProperties(System.Type type)
                {
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
                        Debug.Assert(attributes.Length == 1, "Too many section property attributes");
                        this.ProcessSectionPropertyAttribute(property.PropertyType, attributes[0]);
                    }
                }

                private void ProcessSectionPropertyAttribute(System.Type? type, object attribute)
                {
                    ArgumentNullException.ThrowIfNull(type, nameof(type));
                    this.ProcessSectionPropertyAttribute(type, (SectionPropertyAttribute)attribute);
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
    }
}
