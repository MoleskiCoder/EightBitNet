// <copyright file="LcdStatusMode.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace LR35902
{
    public enum LcdStatusMode
    {
        HBlank = 0b00,
        VBlank = 0b01,
        SearchingOamRam = 0b10,
        TransferringDataToLcd = 0b11,
    }
}
