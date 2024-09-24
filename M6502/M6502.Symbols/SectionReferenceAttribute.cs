namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            [AttributeUsage(AttributeTargets.Property)]
            internal sealed class SectionReferenceAttribute(string key, bool optional = false) : SectionPropertyAttribute(key, type: typeof(int), optional: optional)
            {
            }
        }
    }
}
