﻿namespace EightBit.Files.Symbols
{
    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class SectionEnumerationAttribute(string key) : SectionPropertyAttribute(key, enumeration: true)
    {
    }
}
