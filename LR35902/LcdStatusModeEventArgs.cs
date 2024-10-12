// <copyright file="LcdStatusModeEventArgs.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace LR35902
{
    public class LcdStatusModeEventArgs(LcdStatusMode value) : EventArgs
    {
        public LcdStatusMode Mode { get; } = value;
    }
}
