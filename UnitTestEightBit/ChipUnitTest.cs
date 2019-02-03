namespace UnitTestEightBit
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using EightBit;

    [TestClass]
    public class ChipUnitTest
    {
        #region LowByte

        [TestMethod]
        public void TestLowByte()
        {
            ushort input = 0xf00f;
            byte low = Chip.LowByte(input);
            Assert.AreEqual(0xf, low);
        }

        #endregion

        #region HighByte

        [TestMethod]
        public void TestHighByte()
        {
            ushort input = 0xf00f;
            byte high = Chip.HighByte(input);
            Assert.AreEqual(0xf0, high);
        }

        #endregion

        #region ClearFlag

        [TestMethod]
        public void TestClearFlag()
        {
            byte flags = 0xff;
            EightBit.Chip.ClearFlag(ref flags, 0x80);
            Assert.AreEqual(0x7f, flags);
        }

        [TestMethod]
        public void TestClearFlagNonZero()
        {
            byte flags = 0xff;
            EightBit.Chip.ClearFlag(ref flags, 0x80, 1);
            Assert.AreEqual(0x7f, flags);
        }

        [TestMethod]
        public void TestClearFlagZero()
        {
            byte flags = 0xff;
            EightBit.Chip.ClearFlag(ref flags, 0x80, 0);
            Assert.AreEqual(0xff, flags);
        }

        [TestMethod]
        public void TestClearFlagFalse()
        {
            byte flags = 0xff;
            EightBit.Chip.ClearFlag(ref flags, 0x80, false);
            Assert.AreEqual(0xff, flags);
        }

        [TestMethod]
        public void TestClearFlagTrue()
        {
            byte flags = 0xff;
            EightBit.Chip.ClearFlag(ref flags, 0x80, true);
            Assert.AreEqual(0x7f, flags);
        }

        #endregion

        #region SetFlag

        [TestMethod]
        public void TestSetFlag()
        {
            byte flags = 0x7f;
            EightBit.Chip.SetFlag(ref flags, 0x80);
            Assert.AreEqual(0xff, flags);
        }

        [TestMethod]
        public void TestSetFlagNonZero()
        {
            byte flags = 0x7f;
            EightBit.Chip.SetFlag(ref flags, 0x80, 1);
            Assert.AreEqual(0xff, flags);
        }

        [TestMethod]
        public void TestSetFlagZero()
        {
            byte flags = 0x7f;
            EightBit.Chip.SetFlag(ref flags, 0x80, 0);
            Assert.AreEqual(0x7f, flags);
        }

        [TestMethod]
        public void TestSetFlagFalse()
        {
            byte flags = 0x7f;
            EightBit.Chip.SetFlag(ref flags, 0x80, false);
            Assert.AreEqual(0x7f, flags);
        }

        [TestMethod]
        public void TestSetFlagTrue()
        {
            byte flags = 0x7f;
            EightBit.Chip.SetFlag(ref flags, 0x80, true);
            Assert.AreEqual(0xff, flags);
        }

        #endregion

        #region LowerPart

        [TestMethod]
        public void TestLowerPart()
        {
            ushort input = 0xf00f;
            ushort lower = Chip.LowerPart(input);
            Assert.AreEqual(0xf, lower);
        }

        #endregion

        #region HigherPart

        [TestMethod]
        public void TestHigherPart()
        {
            ushort input = 0xf00f;
            ushort higher = Chip.HigherPart(input);
            Assert.AreEqual(0xf000, higher);
        }

        #endregion

        #region DemoteByte

        [TestMethod]
        public void TestDemoteByte()
        {
            ushort input = 0xf00f;
            byte demoted = Chip.DemoteByte(input);
            Assert.AreEqual(0xf0, demoted);
        }

        #endregion

        #region PromoteByte

        [TestMethod]
        public void TestPromoteByte()
        {
            byte input = 0xf0;
            ushort promoted = Chip.PromoteByte(input);
            Assert.AreEqual(0xf000, promoted);
        }

        #endregion

        #region LowNibble

        [TestMethod]
        public void TestLowNibble()
        {
            byte input = 0xab;
            int nibble = Chip.LowNibble(input);
            Assert.AreEqual(0xb, nibble);
        }

        #endregion

        #region HighNibble

        [TestMethod]
        public void TestHighNibble()
        {
            byte input = 0xab;
            int nibble = Chip.HighNibble(input);
            Assert.AreEqual(0xa, nibble);
        }

        #endregion

        #region DemoteNibble

        [TestMethod]
        public void TestDemoteNibble()
        {
            byte input = 0xab;
            int nibble = Chip.DemoteNibble(input);
            Assert.AreEqual(0xa, nibble);
        }

        #endregion

        #region PromoteNibble

        [TestMethod]
        public void TestPromoteNibble()
        {
            byte input = 0xab;
            int nibble = Chip.PromoteNibble(input);
            Assert.AreEqual(0xb0, nibble);
        }

        #endregion

        #region HigherNibble

        [TestMethod]
        public void TestHigherNibble()
        {
            byte input = 0xab;
            int nibble = Chip.HigherNibble(input);
            Assert.AreEqual(0xa0, nibble);
        }

        #endregion

        #region LowerNibble

        [TestMethod]
        public void TestLowerNibble()
        {
            byte input = 0xab;
            int nibble = Chip.LowerNibble(input);
            Assert.AreEqual(0xb, nibble);
        }

        #endregion

        #region MakeWord

        [TestMethod]
        public void TestMakeWord()
        {
            ushort word = Chip.MakeWord(0xcd, 0xab);
            Assert.AreEqual(0xabcd, word);
        }

        #endregion

    }
}
