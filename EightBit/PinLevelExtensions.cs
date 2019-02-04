// <copyright file="PinLevelExtensions.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public static class PinLevelExtensions
    {
        public static bool Raised(this PinLevel line) => line == PinLevel.High;

        public static bool Lowered(this PinLevel line) => line == PinLevel.Low;

        public static void Raise(this ref PinLevel line) => line = PinLevel.High;

        public static void Lower(this ref PinLevel line) => line = PinLevel.Low;
    }
}
