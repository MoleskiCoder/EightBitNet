// <copyright file="IRegisterState.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace Fuse
{
    using EightBit;
    using System.Collections.ObjectModel;

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
