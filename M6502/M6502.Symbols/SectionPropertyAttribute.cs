namespace EightBit.Files.Symbols
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class SectionPropertyAttribute(string key, System.Type? type = null, bool enumeration = false, bool hexadecimal = false, bool optional = false, bool many = false) : Attribute
    {
        public string Key { get; private set; } = key;

        public System.Type? Type { get; private set; } = type;

        public bool Enumeration { get; private set; } = enumeration;

        public bool Hexadecimal { get; private set; } = hexadecimal;

        public bool Optional { get; private set; } = optional;

        public bool Many { get; private set; } = many;
    }
}
