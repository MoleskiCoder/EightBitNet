// <copyright file="Results.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Fuse
{
    using System.Collections.Generic;

    public class Results<T>
        where T : Fuse.IRegisterState, new()
    {
        private readonly Lines lines;

        public Results(string path) => this.lines = new Lines(path);

        public Dictionary<string, Result<T>> Container { get; } = new Dictionary<string, Result<T>>();

        public void Read() => this.lines.Read();

        public void Parse()
        {
            while (!this.lines.EndOfFile)
            {
                var result = new Result<T>();
                if (result.TryParse(this.lines))
                {
                    this.Container.Add(result.Description, result);
                }
            }
        }
    }
}
