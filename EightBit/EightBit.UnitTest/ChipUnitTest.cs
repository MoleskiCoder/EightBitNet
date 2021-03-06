﻿// <copyright file="ChipUnitTest.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ChipUnitTest
    {
        [TestMethod]
        public void TestLowByte()
        {
            const ushort input = 0xf00f;
            var low = Chip.LowByte(input);
            Assert.AreEqual(0xf, low);
        }

        [TestMethod]
        public void TestHighByte()
        {
            const ushort input = 0xf00f;
            var high = Chip.HighByte(input);
            Assert.AreEqual(0xf0, high);
        }

        [TestMethod]
        public void TestClearBit()
        {
            byte flags = 0xff;
            flags = Chip.ClearBit(flags, 0x80);
            Assert.AreEqual(0x7f, flags);
        }

        [TestMethod]
        public void TestClearBitNonZero()
        {
            byte flags = 0xff;
            flags = Chip.ClearBit(flags, 0x80, 1);
            Assert.AreEqual(0x7f, flags);
        }

        [TestMethod]
        public void TestClearBitZero()
        {
            byte flags = 0xff;
            flags = Chip.ClearBit(flags, 0x80, 0);
            Assert.AreEqual(0xff, flags);
        }

        [TestMethod]
        public void TestClearBitFalse()
        {
            byte flags = 0xff;
            flags = Chip.ClearBit(flags, 0x80, false);
            Assert.AreEqual(0xff, flags);
        }

        [TestMethod]
        public void TestClearBitTrue()
        {
            byte flags = 0xff;
            flags = Chip.ClearBit(flags, 0x80, true);
            Assert.AreEqual(0x7f, flags);
        }

        [TestMethod]
        public void TestSetBit()
        {
            byte flags = 0x7f;
            flags = Chip.SetBit(flags, 0x80);
            Assert.AreEqual(0xff, flags);
        }

        [TestMethod]
        public void TestSetBitNonZero()
        {
            byte flags = 0x7f;
            flags = Chip.SetBit(flags, 0x80, 1);
            Assert.AreEqual(0xff, flags);
        }

        [TestMethod]
        public void TestSetBitZero()
        {
            byte flags = 0x7f;
            flags = Chip.SetBit(flags, 0x80, 0);
            Assert.AreEqual(0x7f, flags);
        }

        [TestMethod]
        public void TestSetBitFalse()
        {
            byte flags = 0x7f;
            flags = Chip.SetBit(flags, 0x80, false);
            Assert.AreEqual(0x7f, flags);
        }

        [TestMethod]
        public void TestSetBitTrue()
        {
            byte flags = 0x7f;
            flags = Chip.SetBit(flags, 0x80, true);
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
            var higher = Chip.HigherPart(input);
            Assert.AreEqual(0xf000, higher);
        }

        [TestMethod]
        public void TestDemoteByte()
        {
            const ushort input = 0xf00f;
            var demoted = Chip.DemoteByte(input);
            Assert.AreEqual(0xf0, demoted);
        }

        [TestMethod]
        public void TestPromoteByte()
        {
            const byte input = 0xf0;
            var promoted = Chip.PromoteByte(input);
            Assert.AreEqual(0xf000, promoted);
        }

        [TestMethod]
        public void TestLowNibble()
        {
            const byte input = 0xab;
            var nibble = Chip.LowNibble(input);
            Assert.AreEqual(0xb, nibble);
        }

        [TestMethod]
        public void TestHighNibble()
        {
            const byte input = 0xab;
            var nibble = Chip.HighNibble(input);
            Assert.AreEqual(0xa, nibble);
        }

        [TestMethod]
        public void TestDemoteNibble()
        {
            const byte input = 0xab;
            var nibble = Chip.DemoteNibble(input);
            Assert.AreEqual(0xa, nibble);
        }

        [TestMethod]
        public void TestPromoteNibble()
        {
            const byte input = 0xab;
            var nibble = Chip.PromoteNibble(input);
            Assert.AreEqual(0xb0, nibble);
        }

        [TestMethod]
        public void TestHigherNibble()
        {
            const byte input = 0xab;
            var nibble = Chip.HigherNibble(input);
            Assert.AreEqual(0xa0, nibble);
        }

        [TestMethod]
        public void TestLowerNibble()
        {
            const byte input = 0xab;
            var nibble = Chip.LowerNibble(input);
            Assert.AreEqual(0xb, nibble);
        }

        [TestMethod]
        public void TestMakeWord()
        {
            var word = Chip.MakeWord(0xcd, 0xab);
            Assert.AreEqual(0xabcd, word);
        }

        [TestMethod]
        public void TestFindFirstSet_1()
        {
            var position = Chip.FindFirstSet(12);
            Assert.AreEqual(3, position);
        }

        [TestMethod]
        public void TestFindFirstSet_2()
        {
            var position = Chip.FindFirstSet(1);
            Assert.AreEqual(1, position);
        }

        [TestMethod]
        public void TestFindFirstSet_3()
        {
            var position = Chip.FindFirstSet(128);
            Assert.AreEqual(8, position);
        }

        [TestMethod]
        public void TestFindFirstSet_4()
        {
            var position = Chip.FindFirstSet(0);
            Assert.AreEqual(0, position);
        }
    }
}
