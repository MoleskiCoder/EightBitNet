// <copyright file="Tests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Fuse
{
    using System.Collections.Generic;

    public class Tests
    {
        private readonly Lines lines;

        public Tests(string path) => this.lines = new Lines(path);

        public Dictionary<string, Test> Container { get; } = new Dictionary<string, Test>();

        public void Read() => this.lines.Read();

        public void Parse()
        {
            while (!this.lines.EndOfFile)
            {
                var test = new Test();
                if (test.TryParse(this.lines))
                {
                    this.Container.Add(test.Description, test);
                }
            }
        }
    }
}
