namespace EightBit.UnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using EightBit;

    [TestClass]
    public class ChipTests
    {
        [TestMethod]
        public void Bit_ReturnsCorrectBit()
        {
            Assert.AreEqual(0x01, Chip.Bit(0));
            Assert.AreEqual(0x02, Chip.Bit(1));
            Assert.AreEqual(0x80, Chip.Bit(7));
            Assert.AreEqual(0x08, Chip.Bit((byte)3));
        }

        [TestMethod]
        public void SetBit_SetsBitCorrectly()
        {
            Assert.AreEqual(0b00001101, Chip.SetBit(0b00001001, 0b00000100));
            Assert.AreEqual(0b00001101, Chip.SetBit(0b00001101, 0b00000100));
            Assert.AreEqual(0b00001101, Chip.SetBit(0b00001101, 0b00000100, true));
            Assert.AreEqual(0b00001001, Chip.SetBit(0b00001101, 0b00000100, false));
        }

        [TestMethod]
        public void ClearBit_ClearsBitCorrectly()
        {
            Assert.AreEqual(0b00001001, Chip.ClearBit(0b00001101, 0b00000100));
            Assert.AreEqual(0b00001101, Chip.ClearBit(0b00001101, 0b00000100, false));
            Assert.AreEqual(0b00001001, Chip.ClearBit(0b00001101, 0b00000100, true));
        }

        [TestMethod]
        public void HighByte_LowByte_WorkCorrectly()
        {
            ushort value = 0xABCD;
            Assert.AreEqual(0xAB, Chip.HighByte(value));
            Assert.AreEqual(0xCD, Chip.LowByte(value));
            int intValue = 0x1234;
            Assert.AreEqual(0x12, Chip.HighByte(intValue));
            Assert.AreEqual(0x34, Chip.LowByte(intValue));
        }

        [TestMethod]
        public void PromoteByte_DemoteByte_WorkCorrectly()
        {
            Assert.AreEqual(0x3400, Chip.PromoteByte(0x34));
            Assert.AreEqual(0x12, Chip.DemoteByte(0x1234));
        }

        [TestMethod]
        public void HigherPart_LowerPart_WorkCorrectly()
        {
            ushort value = 0xABCD;
            Assert.AreEqual(0xAB00, Chip.HigherPart(value));
            Assert.AreEqual(0xCD, Chip.LowerPart(value));
        }

        [TestMethod]
        public void MakeWord_CreatesCorrectWord()
        {
            Assert.AreEqual(0x1234, Chip.MakeWord(0x34, 0x12));
        }

        [TestMethod]
        public void NibbleMethods_WorkCorrectly()
        {
            byte value = 0xAB;
            Assert.AreEqual(0xA, Chip.HighNibble(value));
            Assert.AreEqual(0xB, Chip.LowNibble(value));
            Assert.AreEqual(0xA0, Chip.HigherNibble(value));
            Assert.AreEqual(0xB, Chip.LowerNibble(value));
            Assert.AreEqual(0xB0, Chip.PromoteNibble(value));
            Assert.AreEqual(0xA, Chip.DemoteNibble(value));
        }

        [TestMethod]
        public void CountBits_ReturnsCorrectCount()
        {
            Assert.AreEqual(0, Chip.CountBits(0));
            Assert.AreEqual(1, Chip.CountBits(1));
            Assert.AreEqual(8, Chip.CountBits(0xFF));
        }

        [TestMethod]
        public void EvenParity_ReturnsCorrectParity()
        {
            Assert.IsTrue(Chip.EvenParity(0)); // 0 bits set
            Assert.IsFalse(Chip.EvenParity(1)); // 1 bit set
            Assert.IsTrue(Chip.EvenParity(3)); // 2 bits set
        }

        [TestMethod]
        public void FindFirstSet_ReturnsCorrectIndex()
        {
            Assert.AreEqual(0, Chip.FindFirstSet(0));
            Assert.AreEqual(1, Chip.FindFirstSet(1));
            Assert.AreEqual(2, Chip.FindFirstSet(2));
            Assert.AreEqual(3, Chip.FindFirstSet(4));
            Assert.AreEqual(5, Chip.FindFirstSet(0b10000));
        }
    }
}
