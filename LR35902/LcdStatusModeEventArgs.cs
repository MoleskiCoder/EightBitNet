// <copyright file="LcdStatusModeEventArgs.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace EightBit.GameBoy
{
    using System;

    public class LcdStatusModeEventArgs : EventArgs
    {
        public LcdStatusModeEventArgs(LcdStatusMode value) => this.LcdStatusMode = value;

        public LcdStatusMode LcdStatusMode { get; }
    }
}
