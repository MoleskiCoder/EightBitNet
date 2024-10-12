// <copyright file="Tests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Fuse
{
    using System.Collections.Generic;
    using System.Diagnostics;

    public class Tests<T>(string path)
        where T : Fuse.IRegisterState, new()
    {
        private readonly Lines lines = new(path);

        public Dictionary<string, Test<T>> Container { get; } = [];

        public void Read() => this.lines.Read();

        public void Parse()
        {
            while (!this.lines.EndOfFile)
            {
                var test = new Test<T>();
                if (test.TryParse(this.lines))
                {
                    Debug.Assert(test.Description is not null);
                    this.Container.Add(test.Description, test);
                }
            }
        }
    }
}
