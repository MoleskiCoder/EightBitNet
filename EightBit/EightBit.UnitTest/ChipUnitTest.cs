// <copyright file="ChipUnitTest.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace UnitTestEightBit
{
    using EightBit;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ChipUnitTest
    {
        [TestMethod]
        public void TestLowByte()
        {
            const ushort input = 0xf00f;
            byte low = Chip.LowByte(input);
            Assert.AreEqual(0xf, low);
        }

        [TestMethod]
        public void TestHighByte()
        {
            const ushort input = 0xf00f;
            byte high = Chip.HighByte(input);
            Assert.AreEqual(0xf0, high);
        }

        [TestMethod]
        public void TestClearFlag()
        {
            byte flags = 0xff;
            flags = Chip.ClearFlag(flags, 0x80);
            Assert.AreEqual(0x7f, flags);
        }

        [TestMethod]
        public void TestClearFlagNonZero()
        {
            byte flags = 0xff;
            flags = Chip.ClearFlag(flags, 0x80, 1);
            Assert.AreEqual(0x7f, flags);
        }

        [TestMethod]
        public void TestClearFlagZero()
        {
            byte flags = 0xff;
            flags = Chip.ClearFlag(flags, 0x80, 0);
            Assert.AreEqual(0xff, flags);
        }

        [TestMethod]
        public void TestClearFlagFalse()
        {
            byte flags = 0xff;
            flags = Chip.ClearFlag(flags, 0x80, false);
            Assert.AreEqual(0xff, flags);
        }

        [TestMethod]
        public void TestClearFlagTrue()
        {
            byte flags = 0xff;
            flags = Chip.ClearFlag(flags, 0x80, true);
            Assert.AreEqual(0x7f, flags);
        }

        [TestMethod]
        public void TestSetFlag()
        {
            byte flags = 0x7f;
            flags = Chip.SetFlag(flags, 0x80);
            Assert.AreEqual(0xff, flags);
        }

        [TestMethod]
        public void TestSetFlagNonZero()
        {
            byte flags = 0x7f;
            flags = Chip.SetFlag(flags, 0x80, 1);
            Assert.AreEqual(0xff, flags);
        }

        [TestMethod]
        public void TestSetFlagZero()
        {
            byte flags = 0x7f;
            flags = Chip.SetFlag(flags, 0x80, 0);
            Assert.AreEqual(0x7f, flags);
        }

        [TestMethod]
        public void TestSetFlagFalse()
        {
            byte flags = 0x7f;
            flags = Chip.SetFlag(flags, 0x80, false);
            Assert.AreEqual(0x7f, flags);
        }

        [TestMethod]
        public void TestSetFlagTrue()
        {
            byte flags = 0x7f;
            flags = Chip.SetFlag(flags, 0x80, true);
            Assert.AreEqual(0xff, flags);
        }

        [TestMethod]
        public void TestLowerPart()
        {
            const ushort input = 0xf00f;
            ushort lower = Chip.LowerPart(input);
            Assert.AreEqual(0xf, lower);
        }

        [TestMethod]
        public void TestHigherPart()
        {
            const ushort input = 0xf00f;
            ushort higher = Chip.HigherPart(input);
            Assert.AreEqual(0xf000, higher);
        }

        [TestMethod]
        public void TestDemoteByte()
        {
            const ushort input = 0xf00f;
            byte demoted = Chip.DemoteByte(input);
            Assert.AreEqual(0xf0, demoted);
        }

        [TestMethod]
        public void TestPromoteByte()
        {
            const byte input = 0xf0;
            ushort promoted = Chip.PromoteByte(input);
            Assert.AreEqual(0xf000, promoted);
        }

        [TestMethod]
        public void TestLowNibble()
        {
            const byte input = 0xab;
            int nibble = Chip.LowNibble(input);
            Assert.AreEqual(0xb, nibble);
        }

        [TestMethod]
        public void TestHighNibble()
        {
            const byte input = 0xab;
            int nibble = Chip.HighNibble(input);
            Assert.AreEqual(0xa, nibble);
        }

        [TestMethod]
        public void TestDemoteNibble()
        {
            const byte input = 0xab;
            int nibble = Chip.DemoteNibble(input);
            Assert.AreEqual(0xa, nibble);
        }

        [TestMethod]
        public void TestPromoteNibble()
        {
            const byte input = 0xab;
            int nibble = Chip.PromoteNibble(input);
            Assert.AreEqual(0xb0, nibble);
        }

        [TestMethod]
        public void TestHigherNibble()
        {
            const byte input = 0xab;
            int nibble = Chip.HigherNibble(input);
            Assert.AreEqual(0xa0, nibble);
        }

        [TestMethod]
        public void TestLowerNibble()
        {
            const byte input = 0xab;
            int nibble = Chip.LowerNibble(input);
            Assert.AreEqual(0xb, nibble);
        }

        [TestMethod]
        public void TestMakeWord()
        {
            ushort word = Chip.MakeWord(0xcd, 0xab);
            Assert.AreEqual(0xabcd, word);
        }
    }
}
