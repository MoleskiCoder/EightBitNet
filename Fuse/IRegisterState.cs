// <copyright file="IRegisterState.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Fuse
{
    using System.Collections.ObjectModel;
    using EightBit;

    public interface IRegisterState
    {
        ReadOnlyCollection<Register16> Registers
        {
            get;
        }

        bool Halted { get; }

        int TStates { get; }

        void Parse(Lines lines);
    }
}
