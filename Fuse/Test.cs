// <copyright file="Test.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Fuse
{
    using System;
    using System.Collections.Generic;

    public class Test<T>
        where T : Fuse.IRegisterState, new()
    {
        private readonly List<MemoryDatum> memoryData = [];

        public string? Description { get; private set; }

        public T RegisterState { get; } = new T();

        public IReadOnlyCollection<MemoryDatum> MemoryData => this.memoryData.AsReadOnly();

        public bool TryParse(Lines lines)
        {
            ArgumentNullException.ThrowIfNull(lines);

            while (!lines.EndOfFile && string.IsNullOrEmpty(this.Description))
            {
                this.Description = lines.ReadLine();
            }

            if (string.IsNullOrEmpty(this.Description))
            {
                return false;
            }

            this.RegisterState.Parse(lines);

            bool finished;
            do
            {
                var line = lines.ReadLine();
                finished = line == "-1";
                if (!finished)
                {
                    var datum = new MemoryDatum();
                    datum.Parse(line);
                    this.memoryData.Add(datum);
                }
            }
            while (!finished);

            return true;
        }
    }
}
