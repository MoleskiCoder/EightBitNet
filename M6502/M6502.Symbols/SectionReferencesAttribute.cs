namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            [AttributeUsage(AttributeTargets.Property)]
            internal class SectionReferencesAttribute(string key) : SectionPropertyAttribute(key, many: true)
            {
            }
        }
    }
}
