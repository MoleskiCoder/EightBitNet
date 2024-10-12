// <copyright file="Results.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Fuse
{
    using System.Collections.Generic;
    using System.Diagnostics;

    public class Results<T>(string path)
        where T : Fuse.IRegisterState, new()
    {
        private readonly Lines lines = new(path);

        public Dictionary<string, Result<T>> Container { get; } = [];

        public void Read() => this.lines.Read();

        public void Parse()
        {
            while (!this.lines.EndOfFile)
            {
                var result = new Result<T>();
                if (result.TryParse(this.lines))
                {
                    Debug.Assert(result.Description is not null);
                    this.Container.Add(result.Description, result);
                }
            }
        }
    }
}
