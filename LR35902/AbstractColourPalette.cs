// <copyright file="AbstractColourPalette.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace LR35902
{
    public class AbstractColourPalette<T>
    {
        protected AbstractColourPalette()
        {
        }

        protected T[] Colours { get; } = new T[4];

        public T Colour(int index) => this.Colours[index];
    }
}
