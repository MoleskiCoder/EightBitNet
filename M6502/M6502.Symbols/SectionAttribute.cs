namespace EightBit.Files.Symbols
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class SectionAttribute(string key, string? referencing = null) : Attribute
    {
        public string Key { get; private set; } = key;

        public string? Referencing { get; private set; } = referencing;
    }
}
