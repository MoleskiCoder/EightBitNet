// <copyright file="BusTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace EightBit.UnitTest
{
    using EightBit;

    [TestClass]
    public class BusTests
    {
        // Minimal Bus whose entire address space maps to a single read-only RAM region.
        // Seed()/RawPeek() reach the RAM directly, bypassing the access-level check,
        // so they can be used to set up and verify the underlying memory independently
        // of whatever the Bus write path does.
        private sealed class ReadOnlyBus : Bus
        {
            private readonly Ram _ram = new(0x10000);
            private readonly MemoryMapping _mapping;

            public ReadOnlyBus()
                => this._mapping = new(this._ram, 0x0000, (ushort)Mask.Sixteen, AccessLevel.ReadOnly);

            /// <summary>Writes directly to the backing RAM, bypassing the access-level guard.</summary>
            public void Seed(ushort address, byte value) => this._ram.Poke(address, value);

            /// <summary>Reads directly from the backing RAM, bypassing the access-level guard.</summary>
            public byte RawPeek(ushort address) => this._ram.Peek(address);

            public override MemoryMapping Mapping(ushort _) => this._mapping;

            public override void Initialize() { }
        }

        private readonly ReadOnlyBus _bus = new();

        [TestMethod]
        public void Bus_Read_From_ReadOnly_Returns_Memory_Value()
        {
            _bus.Seed(0x1000, 0xAB);
            _bus.Address.Joined = 0x1000;
            _bus.Read();
            Assert.AreEqual(0xAB, _bus.Data, "Read from read-only mapping must return the underlying memory value");
        }

        [TestMethod]
        public void Bus_Write_To_ReadOnly_Does_Not_Modify_Memory()
        {
            _bus.Seed(0x2000, 0x55);
            _bus.Address.Joined = 0x2000;
            _bus.Data = 0xFF;
            _bus.Write();
            Assert.AreEqual(0x55, _bus.RawPeek(0x2000), "Write to read-only mapping must not modify the underlying memory");
            Assert.AreEqual(0xFF, _bus.Data, "Write to read-only mapping must leave the written value on the bus, not the memory value");
        }

        [TestMethod]
        public void Bus_Read_After_Write_To_ReadOnly_Returns_Original_Value()
        {
            _bus.Seed(0x3000, 0x42);
            _bus.Address.Joined = 0x3000;
            _bus.Data = 0xFF;
            _bus.Write();  // silently discarded
            _bus.Read();   // must see the original value, not 0xFF
            Assert.AreEqual(0x42, _bus.Data, "Read after a discarded write must return the original memory value");
        }
    }
}
