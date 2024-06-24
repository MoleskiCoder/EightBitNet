namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            [AttributeUsage(AttributeTargets.Property)]
            internal class SectionReferenceAttribute(string key) : SectionPropertyAttribute(key, type: typeof(int))
            {
            }
        }
    }
}
