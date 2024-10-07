namespace EightBit.Files.Symbols
{
    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class SectionReferencesAttribute(string key) : SectionPropertyAttribute(key, many: true)
    {
    }
}
