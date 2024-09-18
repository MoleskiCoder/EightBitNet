namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            [AttributeUsage(AttributeTargets.Property)]
            internal sealed class SectionReferencesAttribute(string key) : SectionPropertyAttribute(key, many: true)
            {
            }
        }
    }
}
