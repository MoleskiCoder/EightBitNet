﻿namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            using System.Collections.Generic;

            // sym id = 16, name = "solve", addrsize = absolute, size = 274, scope = 0, def = 94,ref=144+17+351,val=0xF314,seg=6,type=lab
            public sealed class Symbol(Parser container) : NamedSection(container)
            {
                [SectionEnumeration("addrsize")]
                public string? AddressSize { get; private set; }

                [SectionProperty("size")]
                public int? Size { get; private set; }

                [SectionReference("scope")]
                public Symbols.Scope Scope => this.TakeScopeReference();

                [SectionReferences("def")]
                public List<Line> Definitions => this.TakeLineReferences("def");  // Guess

                [SectionReferences("ref")]
                public List<Line> References => this.TakeLineReferences("ref"); // Guess

                [SectionProperty("val", hexadecimal: true)]
                public int Value { get; private set; }

                [SectionReference("seg")]
                public Symbols.Segment Segment => this.TakeSegmentReference();

                [SectionEnumeration("type")]
                public string? Type { get; private set; }
            }
        }
    }
}