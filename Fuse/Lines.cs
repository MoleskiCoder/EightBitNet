// <copyright file="Lines.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Fuse
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    public class Lines(string path)
    {
        private readonly string path = path;
        private readonly List<string> lines = [];
        private int position = -1;

        public bool EndOfFile => this.position == this.lines.Count;

        public void Read()
        {
            using (var reader = File.OpenText(this.path))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    Debug.Assert(line != null);
                    var ignored = line.StartsWith(";", StringComparison.OrdinalIgnoreCase);
                    if (!ignored)
                    {
                        this.lines.Add(line);
                    }
                }
            }

            // Users should check EndOfFile before using a bad position...
            this.position = 0;
        }

        public string ReadLine()
        {
            try
            {
                return this.PeekLine();
            }
            finally
            {
                this.Increment();
            }
        }

        public void UnreadLine() => this.Decrement();

        public string PeekLine() => this.lines[this.position];

        private void Increment() => ++this.position;

        private void Decrement() => --this.position;
    }
}
