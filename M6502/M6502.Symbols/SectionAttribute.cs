﻿namespace M6502.Symbols
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class SectionAttribute(string key, string? referencing = null) : Attribute
    {
        public string Key { get; private set; } = key;

        public string? Referencing { get; private set; } = referencing;
    }
}
