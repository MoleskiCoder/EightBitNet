namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            [AttributeUsage(AttributeTargets.Property)]
            internal class SectionEnumerationAttribute(string key) : SectionPropertyAttribute(key, enumeration: true)
            {
            }
        }
    }
}
