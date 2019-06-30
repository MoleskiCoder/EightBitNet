// <copyright file="MC6850.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace EightBit
{
    using System;

    // +--------+----------------------------------------------------------------------------------+
    // |        |                               Buffer address                                     |
    // |        +------------------+------------------+--------------------+-----------------------+
    // |        |             _    |            _     |               _    |               _       |
    // |  Data  |      RS * R/W    |     RS * R/W     |        RS * R/W    |        RS * R/W       |
    // |  Bus   |   (high)(low)    |   (high)(high)   |       (low)(low)   |       (low)(low)      |
    // |  Line  |     Transmit     |     Receive      |                    |                       |
    // | Number |       Data       |      Data        |         Control    |         Status        |
    // |        |     Register     |     Register     |         register   |        register       |
    // |        +------------------+------------------+--------------------+-----------------------+
    // |        |  (Write only)    +   (Read only)    +       (Write only) |      (Read only)      |
    // +--------+------------------+------------------+--------------------+-----------------------+
    // |    0   |    Data bit 0*   |    Data bit 0    |   Counter divide   | Receive data register |
    // |        |                  |                  |    select 1 (CR0)  |      full (RDRF)      |
    // +--------+------------------+------------------+--------------------+-----------------------+
    // |    1   |    Data bit 1    |    Data bit 1    |   Counter divide   | Transmit data register|
    // |        |                  |                  |   select 2 (CR1)   |     empty (TDRE)      |
    // +--------+------------------+------------------+--------------------+-----------------------+
    // |    2   |   Data bit 2     |    Data bit 2    |   Word select 1    |  Data carrier detect  |
    // |        |                  |                  |       (CR2)        |      (DCD active)     |
    // +--------+------------------+------------------+--------------------+-----------------------+
    // |    3   |   Data bit 3     |    Data bit 3    |   Word select 1    |     Clear to send     |
    // |        |                  |                  |       (CR3)        |      (CTS active)     |
    // +--------+------------------+------------------+--------------------+-----------------------+
    // |    4   |   Data bit 4     |    Data bit 4    |   Word select 1    |     Framing error     |
    // |        |                  |                  |       (CR4)        |          (FE)         |
    // +--------+------------------+------------------+--------------------+-----------------------+
    // |    5   |   Data bit 5     |    Data bit 5    | Transmit control 1 |    Receiver overrun   |
    // |        |                  |                  |       (CR5)        |        (OVRN)         |
    // +--------+------------------+------------------+--------------------+-----------------------+
    // |    6   |   Data bit 6     |    Data bit 6    | Transmit control 2 |    Parity error (PE)  |
    // |        |                  |                  |       (CR6)        |                       |
    // +--------+------------------+------------------+--------------------+-----------------------+
    // |    7   |   Data bit 7***  |    Data bit 7**  | Receive interrupt  |    Interrupt request  |
    // |        |                  |                  |   enable (CR7)     |      (IRQ active)     |
    // +--------+------------------+------------------+--------------------+-----------------------+
    //      * Leading bit = LSB = Bit 0
    //     ** Data bit will be zero in 7-bit plus parity modes
    //    *** Data bit is "don't care" in 7-bit plus parity modes
    public sealed class MC6850 : ClockedChip
    {
        private PinLevel rxdataLine = PinLevel.Low;
        private PinLevel txdataLine = PinLevel.Low;

        private PinLevel rtsLine = PinLevel.Low;
        private PinLevel ctsLine = PinLevel.Low;
        private PinLevel dcdLine = PinLevel.Low;
        private PinLevel oldDcdLine = PinLevel.Low;  // So we can detect low -> high transition

        private PinLevel rxClkLine = PinLevel.Low;
        private PinLevel txClkLine = PinLevel.Low;

        private PinLevel cs0Line = PinLevel.Low;
        private PinLevel cs1Line = PinLevel.Low;
        private PinLevel cs2Line = PinLevel.Low;

        private PinLevel rsLine = PinLevel.Low;
        private PinLevel rwLine = PinLevel.Low;

        private PinLevel eLine = PinLevel.Low;
        private PinLevel irqLine = PinLevel.Low;

        private byte data = 0;

        private bool statusRead = false;

        // Control registers
        private CounterDivideSelect counterDivide = CounterDivideSelect.One;
        private WordSelect wordSelect = WordSelect.SevenEvenTwo;
        private TransmitterControl transmitControl = TransmitterControl.ReadyLowInterruptDisabled;
        private ReceiveControl receiveControl = ReceiveControl.ReceiveInterruptDisable;

        // Status registers
        private bool statusRDRF = false;
        private bool statusTDRE = true;
        private bool statusOVRN = false;

        // Data registers
        private byte tdr = 0;
        private byte rdr = 0;

        private StartupCondition startup = StartupCondition.WarmStart;

        public event EventHandler<EventArgs> Accessing;

        public event EventHandler<EventArgs> Accessed;

        public event EventHandler<EventArgs> Transmitting;

        public event EventHandler<EventArgs> Transmitted;

        public event EventHandler<EventArgs> Receiving;

        public event EventHandler<EventArgs> Received;

        [Flags]
        public enum ControlRegister
        {
            None = 0,
            CR0 = 0b1,          // Counter divide
            CR1 = 0b10,         //      "
            CR2 = 0b100,        // Word select
            CR3 = 0b1000,       //      "
            CR4 = 0b10000,      //      "
            CR5 = 0b100000,     // Transmit control
            CR6 = 0b1000000,    //      "
            CR7 = 0b10000000,   // Receive control
        }

        // CR0 and CR1
        public enum CounterDivideSelect
        {
            One = 0b00,
            Sixteen = 0b01,
            SixtyFour = 0b10,
            MasterReset = 0b11,
        }

        // CR2, CR3 and CR4
        public enum WordSelect
        {
            SevenEvenTwo = 0b000,
            SevenOddTwo = 0b001,
            SevenEvenOne = 0b010,
            SevenOddOne = 0b011,
            EightTwo = 0b100,
            EightOne = 0b101,
            EightEvenOne = 0b110,
            EightOddOne = 0b111,
        }

        // CR5 and CR6
        public enum TransmitterControl
        {
            ReadyLowInterruptDisabled = 0b00,
            ReadyLowInterruptEnabled = 0b01,
            ReadyHighInterruptDisabled = 0b10,
            ReadyLowInterruptDisabledTransmitBreak = 0b11,
        }

        // CR7
        public enum ReceiveControl
        {
            ReceiveInterruptDisable = 0b0,
            ReceiveInterruptEnable = 0b1,   // Triggers on: RDR full, overrun, DCD low -> high
        }

        // STATUS REGISTER Information on the status of the ACIA is
        // available to the MPU by reading the ACIA Status Register.
        // This read-only register is selected when RS is low and R/W is high.
        // Information stored in this register indicates the status of the
        // Transmit Data Register, the Receive Data Register and error logic,
        // and the peripheral/modem status inputs of the ACIA
        [Flags]
        public enum StatusRegister
        {
            None = 0,

            // Receive Data Register Full (RDRF), Bit 0 - Receive Data
            // Register Full indicates that received data has been
            // transferred to the Receive Data Register. RDRF is cleared
            // after an MPU read of the Receive Data Register or by a
            // master reset. The cleared or empty state indicates that the
            // contents of the Receive Data Register are not current.
            // Data Carrier Detect being high also causes RDRF to indicate
            // empty.
            STATUS_RDRF = 0b1,

            // Transmit Data Register Empty (TDRE), Bit 1 - The Transmit
            // Data Register Empty bit being set high indicates that the
            // Transmit Data Register contents have been transferred and
            // that new data may be entered. The low state indicates that
            // the register is full and that transmission of a new
            // character has not begun since the last write data command.
            STATUS_TDRE = 0b10,

            // .                    ___
            // Data Carrier Detect (DCD), Bit 2 - The Data Carrier Detect
            // bit will be high when the DCD (low) input from a modem has gone
            // high to indicate that a carrier is not present. This bit
            // going high causes an Interrupt Request to be generated when
            // the Receive Interrupt Enable is set. It remains high after
            // the DCD (low) input is returned low until cleared by first reading
            // the Status Register and then the Data Register or until a
            // master reset occurs. If the DCD (low) input remains high after
            // read status and read data or master reset has occurred, the
            // interrupt is cleared, the DCD (low) status bit remains high and
            // will follow the DCD (low) input.
            STATUS_DCD = 0b100,

            // .              ___
            // Clear-to-Send (CTS), Bit 3 - The Clear-to-Send bit indicates
            // the state of the Clear-to-Send input from a modem. A low CTS (low)
            // indicates that there is a Clear-to-Send from the modem. In
            // the high state, the Transmit Data Register Empty bit is
            // inhibited and the Clear-to-Send status bit will be high.
            // Master reset does not affect the Clear-to-Send status bit.
            STATUS_CTS = 0b1000,

            // Framing Error (FE), Bit 4 - Framing error indicates that the
            // received character is improperly framed by a start and a
            // stop bit and is detected by the absence of the first stop
            // bit. This error indicates a synchronization error, faulty
            // transmission, or a break condition. The framing error flag
            // is set or reset during the receive data transfer time.
            // Therefore, this error indicator is present throughout the
            // time that the associated character is available.
            STATUS_FE = 0b10000,

            // Receiver Overrun (OVRN), Bit 5- Overrun is an error flag
            // that indicates that one or more characters in the data
            // stream were lost. That is, a character or a number of
            // characters were received but not read from the Receive
            // Data Register (RDR) prior to subsequent characters being
            // received. The overrun condition begins at the midpoint of
            // the last bit of the second character received in succession
            // without a read of the RDR having occurred. The Overrun does
            // not occur in the Status Register until the valid character
            // prior to Overrun has been read. The RDRF bit remains set
            // until the Overrun is reset. Character synchronization is
            // maintained during the Overrun condition. The Overrun
            // indication is reset after the reading of data from the
            // Receive Data Register or by a Master Reset.
            STATUS_OVRN = 0b100000,

            // Parity Error (PE), Bit 6 - The parity error flag indicates
            // that the number of highs {ones) in the character does not
            // agree with the preselected odd or even parity. Odd parity
            // is defined to be when the total number of ones is odd. The
            // parity error indication will be present as long as the data
            // character is in the RDR. If no parity is selected, then both
            // the transmitter parity generator output and the receiver
            // parity check results are inhibited
            STATUS_PE = 0b1000000,

            // .                  ___
            // Interrupt Request (IRQ), Bit 7- The IRQ (low) bit indicates the
            // state of the IRQ (low) output. Any interrupt condition with its
            // applicable enable will be indicated in this status bit.
            // Anytime the IRQ (low) output is low the IRQ bit will be high to
            // indicate the interrupt or service request status. IRQ (low) is
            // cleared by a read operation to the Receive Data Register or
            // a write operation to the Transmit Data Register.
            STATUS_IRQ = 0b10000000,
        }

        private enum StartupCondition
        {
            ColdStart,
            WarmStart,
            Unknown,
        }

        // Receive data, (I) Active high
        public ref PinLevel RXDATA => ref this.rxdataLine;

        // Transmit data, (O) Active high
        public ref PinLevel TXDATA => ref this.txdataLine;

        // Request to send, (O) Active low
        public ref PinLevel RTS => ref this.rtsLine;

        // Clear to send, (I) Active low
        public ref PinLevel CTS => ref this.ctsLine;

        // Data carrier detect, (I) Active low
        public ref PinLevel DCD => ref this.dcdLine;

        // Transmit clock, (I) Active high
        public ref PinLevel RXCLK => ref this.rxClkLine;

        // Receive clock, (I) Active high
        public ref PinLevel TXCLK => ref this.txClkLine;

        // Chip select, bit 0, (I) Active high
        public ref PinLevel CS0 => ref this.cs0Line;

        // Chip select, bit 1, (I) Active high
        public ref PinLevel CS1 => ref this.cs1Line;

        // Chip select, bit 2, (I) Active low
        public ref PinLevel CS2 => ref this.cs2Line;

        // Register select, (I) Active high
        public ref PinLevel RS => ref this.rsLine;

        // Read/Write, (I) Read high, write low
        public ref PinLevel RW => ref this.rwLine;

        // ACIA Enable, (I) Active high
        public ref PinLevel E => ref this.eLine;

        // Interrupt request, (O) Active low
        public ref PinLevel IRQ => ref this.irqLine;

        // Data, (I/O)
        public ref byte DATA => ref this.data;

        // Expose these internal registers, so we can update internal state

        // Transmit data register;
        public ref byte TDR => ref this.tdr;

        // Receive data register;
        public ref byte RDR => ref this.rdr;

        public bool Activated => this.Powered && this.E.Raised() && this.Selected;

        public bool Selected => this.CS0.Raised() && this.CS1.Raised() && this.CS2.Lowered();

        private bool TransmitInterruptEnabled => this.transmitControl == TransmitterControl.ReadyLowInterruptEnabled;

        private bool ReceiveInterruptEnabled => this.receiveControl == ReceiveControl.ReceiveInterruptEnable;

        private bool TransmitReadyHigh => this.transmitControl == TransmitterControl.ReadyHighInterruptDisabled;

        private byte Status
        {
            get
            {
                byte status = 0;
                status = SetBit(status, StatusRegister.STATUS_RDRF, this.statusRDRF);
                status = SetBit(status, StatusRegister.STATUS_TDRE, this.statusTDRE);
                status = SetBit(status, StatusRegister.STATUS_DCD, this.DCD.Raised());
                status = SetBit(status, StatusRegister.STATUS_CTS, this.CTS.Raised());
                status = ClearBit(status, StatusRegister.STATUS_FE);
                status = SetBit(status, StatusRegister.STATUS_OVRN, this.statusOVRN);
                status = ClearBit(status, StatusRegister.STATUS_PE);
                return SetBit(status, StatusRegister.STATUS_IRQ, this.IRQ.Lowered());
            }
        }

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            this.startup = StartupCondition.ColdStart;
        }

        public void MarkTransmitComplete()
        {
            this.statusTDRE = this.CTS.Lowered();
            if (this.statusTDRE && this.TransmitInterruptEnabled)
            {
                this.IRQ.Lower();
            }

            this.OnTransmitted();
        }

        public void MarkReceiveStarting()
        {
            this.statusOVRN = this.statusRDRF;    // If the RDR was already full, this means we're losing data
            this.statusRDRF = true;
            if (this.ReceiveInterruptEnabled)
            {
                this.IRQ.Lower();
            }

            this.OnReceiving();
        }

        public string DumpStatus()
        {
            var value = this.Status;
            var returned = string.Empty;
            returned += "(";
            returned += (value & (byte)StatusRegister.STATUS_IRQ) != 0 ? "IRQ " : "- ";
            returned += (value & (byte)StatusRegister.STATUS_PE) != 0 ? "PE " : "- ";
            returned += (value & (byte)StatusRegister.STATUS_OVRN) != 0 ? "OVRN " : "- ";
            returned += (value & (byte)StatusRegister.STATUS_FE) != 0 ? "FE " : "- ";
            returned += (value & (byte)StatusRegister.STATUS_CTS) != 0 ? "CTS " : "- ";
            returned += (value & (byte)StatusRegister.STATUS_DCD) != 0 ? "DCD " : "- ";
            returned += (value & (byte)StatusRegister.STATUS_TDRE) != 0 ? "TDRE " : "- ";
            returned += (value & (byte)StatusRegister.STATUS_RDRF) != 0 ? "RDRF " : "- ";
            returned += ") ";
            return returned;
        }

        protected override void OnTicked()
        {
            base.OnTicked();
            this.Step();
        }

        private static byte SetBit(byte f, StatusRegister flag, bool condition) => SetBit(f, (byte)flag, condition);

        private static byte ClearBit(byte f, StatusRegister flag) => ClearBit(f, (byte)flag);

        private void Step()
        {
            this.ResetCycles();

            if (!this.Activated)
            {
                return;
            }

            this.OnAccessing();

            var writing = this.RW.Lowered();
            var registers = this.RS.Lowered();

            if (registers)
            {
                if (writing)
                {
                    this.counterDivide = (CounterDivideSelect)(this.DATA & (byte)(ControlRegister.CR0 | ControlRegister.CR1));
                    if (this.counterDivide == CounterDivideSelect.MasterReset)
                    {
                        this.Reset();
                    }
                    else
                    {
                        this.wordSelect = (WordSelect)((this.DATA & (byte)(ControlRegister.CR2 | ControlRegister.CR3 | ControlRegister.CR4)) >> 2);
                        this.transmitControl = (TransmitterControl)((this.DATA & (byte)(ControlRegister.CR5 | ControlRegister.CR6)) >> 5);
                        this.receiveControl = (ReceiveControl)((this.DATA & (byte)ControlRegister.CR7) >> 7);
                        this.RTS.Match(this.TransmitReadyHigh);
                    }
                }
                else
                {
                    this.DATA = this.Status;
                }
            }
            else
            {
                this.IRQ.Raise();
                if (writing)
                {
                    this.StartTransmit();
                }
                else
                {
                    this.CompleteReceive();
                }
            }

            // Catch the transition to lost carrier
            if (this.oldDcdLine.Lowered() && this.DCD.Raised())
            {
                this.IRQ.Raise();
                this.statusRead = false;
            }

            this.oldDcdLine = this.dcdLine;

            this.OnAccessed();
        }

        private void OnAccessing() => this.Accessing?.Invoke(this, EventArgs.Empty);

        private void OnAccessed() => this.Accessed?.Invoke(this, EventArgs.Empty);

        private void OnTransmitting() => this.Transmitting?.Invoke(this, EventArgs.Empty);

        private void OnTransmitted() => this.Transmitted?.Invoke(this, EventArgs.Empty);

        private void OnReceiving() => this.Receiving?.Invoke(this, EventArgs.Empty);

        private void OnReceived() => this.Received?.Invoke(this, EventArgs.Empty);

        private void Reset()
        {
            if (this.startup == StartupCondition.ColdStart)
            {
                this.IRQ.Raise();
                this.RTS.Raise();
            }

            this.statusRDRF = false;
            this.statusTDRE = true;
            this.statusOVRN = false;
            this.DCD.Lower();
            this.startup = StartupCondition.WarmStart;
            this.statusRead = false;
        }

        private void StartTransmit()
        {
            this.TDR = this.DATA;
            this.statusTDRE = false;
            this.OnTransmitting();
        }

        private void CompleteReceive()
        {
            this.DATA = this.RDR;
            this.statusOVRN = this.statusRDRF = false;    // Any existing data overrun is now moot.
            this.OnReceived();
        }
    }
}