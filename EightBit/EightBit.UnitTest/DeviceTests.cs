namespace EightBit.UnitTest
{
    using EightBit;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DeviceTests
    {
        [TestMethod]
        public void Device_InitialState_IsNotPowered()
        {
            var device = new Device();
            Assert.IsFalse(device.Powered);
            Assert.AreEqual(PinLevel.Low, device.POWER);
        }

        [TestMethod]
        public void Device_RaisePOWER_SetsPoweredHigh_AndFiresEvents()
        {
            var device = new Device();
            bool raising = false, raised = false;

            device.RaisingPOWER += (s, e) => raising = true;
            device.RaisedPOWER += (s, e) => raised = true;

            device.RaisePOWER();

            Assert.IsTrue(device.Powered);
            Assert.AreEqual(PinLevel.High, device.POWER);
            Assert.IsTrue(raising);
            Assert.IsTrue(raised);
        }

        [TestMethod]
        public void Device_LowerPOWER_SetsPoweredLow_AndFiresEvents()
        {
            var device = new Device();
            device.RaisePOWER(); // Set to High first

            bool lowering = false, lowered = false;
            device.LoweringPOWER += (s, e) => lowering = true;
            device.LoweredPOWER += (s, e) => lowered = true;

            device.LowerPOWER();

            Assert.IsFalse(device.Powered);
            Assert.AreEqual(PinLevel.Low, device.POWER);
            Assert.IsTrue(lowering);
            Assert.IsTrue(lowered);
        }

        [TestMethod]
        public void Device_RaisePOWER_DoesNothing_IfAlreadyHigh()
        {
            var device = new Device();
            device.RaisePOWER();

            bool raising = false, raised = false;
            device.RaisingPOWER += (s, e) => raising = true;
            device.RaisedPOWER += (s, e) => raised = true;

            device.RaisePOWER();

            Assert.IsTrue(device.Powered);
            Assert.IsFalse(raising);
            Assert.IsFalse(raised);
        }

        [TestMethod]
        public void Device_LowerPOWER_DoesNothing_IfAlreadyLow()
        {
            var device = new Device();

            bool lowering = false, lowered = false;
            device.LoweringPOWER += (s, e) => lowering = true;
            device.LoweredPOWER += (s, e) => lowered = true;

            device.LowerPOWER();

            Assert.IsFalse(device.Powered);
            Assert.IsFalse(lowering);
            Assert.IsFalse(lowered);
        }
    }
}
