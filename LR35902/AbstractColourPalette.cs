// <copyright file="AbstractColourPalette.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace LR35902
{
    using System.Collections.ObjectModel;

    public class AbstractColourPalette<T>
    {
        protected AbstractColourPalette()
        {
            this.Colours = [.. _colours];
        }

        private readonly List<T> _colours = new(4);

        protected Collection<T> Colours { get; }

        public T Colour(int index) => this.Colours[index];
    }
}
