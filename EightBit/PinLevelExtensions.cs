// <copyright file="PinLevelExtensions.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    public static class PinLevelExtensions
    {
        public static bool Raised(this PinLevel line) => line == PinLevel.High;

        public static bool Lowered(this PinLevel line) => line == PinLevel.Low;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public static void Raise(this ref PinLevel line) => line = PinLevel.High;

        public static void Lower(this ref PinLevel line) => line = PinLevel.Low;

        public static void Match(this ref PinLevel line, int condition) => Match(ref line, condition != 0);

        public static void Match(this ref PinLevel line, bool condition)
        {
            if (condition)
            {
                line.Raise();
            }
            else
            {
                line.Lower();
            }
        }
    }
}