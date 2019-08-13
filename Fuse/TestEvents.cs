// <copyright file="TestEvents.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Fuse
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class TestEvents
    {
        private readonly List<TestEvent> container = new List<TestEvent>();

        public ReadOnlyCollection<TestEvent> Container => this.container.AsReadOnly();

        public void Parse(Lines lines)
        {
            bool success;
            do
            {
                var e = new TestEvent();
                success = e.TryParse(lines);
                if (success)
                {
                    this.container.Add(e);
                }
            }
            while (success);
        }
    }
}
