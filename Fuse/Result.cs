// <copyright file="Result.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Fuse
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class Result<T>
        where T : Fuse.IRegisterState, new()
    {
        private readonly TestEvents events = new TestEvents();
        private readonly List<MemoryDatum> memoryData = new List<MemoryDatum>();

        public string Description { get; private set; }

        public T RegisterState { get; } = new T();

        public TestEvents Events => this.events;

        public ReadOnlyCollection<MemoryDatum> MemoryData => this.memoryData.AsReadOnly();

        public bool TryParse(Lines lines)
        {
            ArgumentNullException.ThrowIfNull(lines);

            while (!lines.EndOfFile && string.IsNullOrWhiteSpace(this.Description))
            {
                this.Description = lines.ReadLine();
            }

            if (string.IsNullOrWhiteSpace(this.Description))
            {
                return false;
            }

            this.events.Parse(lines);
            this.RegisterState.Parse(lines);

            bool finished;
            do
            {
                var line = lines.ReadLine();
                finished = string.IsNullOrWhiteSpace(line);
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
