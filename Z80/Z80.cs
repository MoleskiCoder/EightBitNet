// <copyright file="Z80.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Z80
{
    using EightBit;

    public class Z80 : IntelProcessor
    {
        public Z80(Bus bus, InputOutput ports)
        : base(bus)
        {
            this._ports = ports;
            this.RaisedPOWER += this.Z80_RaisedPOWER;
        }

        private readonly InputOutput _ports;

        public InputOutput Ports => this._ports;

        private readonly Register16[] _accumulatorFlags = [new Register16(), new Register16()];
        private readonly Register16[][] _registers =
        [
            [new Register16(), new Register16(), new Register16()],
            [new Register16(), new Register16(), new Register16()]
        ];

        private RefreshRegister _refresh = new(0x7f);

        private bool _prefixCB;
        private bool _prefixDD;
        private bool _prefixED;
        private bool _prefixFD;

        private int _accumulatorFlagsSet;

        private int _registerSet;
        private sbyte _displacement;
        private bool _displaced;

        public byte IV { get; set; } = 0xff;

        public int IM { get; set; }

        public bool IFF1 { get; set; }

        public bool IFF2 { get; set; }

        public byte Q { get; set; }     // Previously modified instruction status register

        private byte _modifiedF;        // In-flight status register.  Used to build "Q"

        public override Register16 AF => this._accumulatorFlags[this._accumulatorFlagsSet];

        private Register16[] CurrentRegisterSet => this._registers[this._registerSet];

        public override Register16 BC => this.CurrentRegisterSet[(int)RegisterIndex.IndexBC];

        public override Register16 DE => this.CurrentRegisterSet[(int)RegisterIndex.IndexDE];

        public override Register16 HL => this.CurrentRegisterSet[(int)RegisterIndex.IndexHL];

        public Register16 IX { get; } = new(0xffff);

        public Register16 IY { get; } = new(0xffff);

        // ** From the Z80 CPU User Manual
        // Memory Refresh(R) Register.The Z80 CPU contains a memory _refresh counter,
        // enabling dynamic memories to be used with the same ease as static memories.Seven bits
        // of this 8-bit register are automatically incremented after each instruction fetch.The eighth
        // bit remains as programmed, resulting from an LD R, A instruction. The data in the _refresh
        // counter is sent out on the lower portion of the address bus along with a _refresh control
        // signal while the CPU is decoding and executing the fetched instruction. This mode of _refresh
        // is transparent to the programmer and does not slow the CPU operation.The programmer
        // can load the R register for testing purposes, but this register is normally not used by the
        // programmer. During _refresh, the contents of the I Register are placed on the upper eight
        // bits of the address bus.
        public ref RefreshRegister REFRESH => ref this._refresh;

        private void DisplaceAddress()
        {
            var displacement = (this._prefixDD ? this.IX : this.IY).Word + this._displacement;
            this.MEMPTR.Word = (ushort)displacement;
        }

        public void Exx() => this._registerSet ^= 1;

        public void ExxAF() => this._accumulatorFlagsSet ^= 1;

        public override void Execute()
        {
            var decoded = this.GetDecodedOpCode(this.OpCode);

            var x = decoded.X;
            var y = decoded.Y;
            var z = decoded.Z;

            var p = decoded.P;
            var q = decoded.Q;

            if (this._prefixCB)
            {
                this.ExecuteCB(x, y, z);
            }
            else if (this._prefixED)
            {
                this.ExecuteED(x, y, z, p, q);
            }
            else
            {
                this.ExecuteOther(x, y, z, p, q);
            }
        }

        public override void PoweredStep()
        {
            this._modifiedF = 0;
            this._displaced = this._prefixCB = this._prefixDD = this._prefixED = this._prefixFD = false;

            if (this.RESET.Lowered())
            {
                this.HandleRESET();
                return;
            }
            else if (this.NMI.Lowered())
            {
                this.HandleNMI();
                return;
            }
            else if (this.INT.Lowered())
            {
                this.RaiseINT();
                this.RaiseHALT();
                if (this.IFF1)
                {
                    this.HandleINT();
                    return;
                }
            }

            // ** From the Z80 CPU User Manual
            // When a software HALT instruction is executed, the CPU executes NOPs until an interrupt
            // is received(either a nonmaskable or a maskable interrupt while the interrupt flip-flop is
            // enabled). The two interrupt lines are sampled with the rising clock edge during each T4
            // state as depicted in Figure 11.If a nonmaskable interrupt is received or a maskable interrupt
            // is received and the interrupt enable flip-flop is set, then the HALT state is exited on
            // the next rising clock edge.The following cycle is an interrupt acknowledge cycle corresponding
            // to the type of interrupt that was received.If both are received at this time, then
            // the nonmaskable interrupt is acknowledged because it is the highest priority.The purpose
            // of executing NOP instructions while in the HALT state is to keep the memory _refresh signals
            // active.Each cycle in the HALT state is a normal M1(fetch) cycle except that the data
            // received from the memory is ignored and an NOP instruction is forced internally to the
            // CPU.The HALT acknowledge signal is active during this time indicating that the processor
            // is in the HALT state.
            this.Execute(this.FetchInstruction());

            this.Q = this._modifiedF;
        }

        private void Z80_RaisedPOWER(object? sender, EventArgs e)
        {
            this.RaiseM1();
            this.RaiseRFSH();
            this.RaiseIORQ();
            this.RaiseMREQ();
            this.RaiseRD();
            this.RaiseWR();

            this.DisableInterrupts();
            this.IM = 0;

            this.REFRESH = 0;
            this.IV = (byte)Mask.Eight;

            this.IX.Word = this.IY.Word = (ushort)Mask.Sixteen;

            // One of the register sets has been initialised
            // (by the IntelProcessor base class)
            // so now lets initialise the other register set.
            this.Exx();
            this.ExxAF();
            this.ResetRegisterSet();
        }

        #region Z80 specific pins

        #region NMI pin

        public event EventHandler<EventArgs>? RaisingNMI;

        public event EventHandler<EventArgs>? RaisedNMI;

        public event EventHandler<EventArgs>? LoweringNMI;

        public event EventHandler<EventArgs>? LoweredNMI;

        private PinLevel _nmiLine = PinLevel.Low;

        public ref PinLevel NMI => ref this._nmiLine;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaiseNMI()
        {
            if (this.NMI.Lowered())
            {
                RaisingNMI?.Invoke(this, EventArgs.Empty);
                try
                {
                    this.NMI.Raise();
                }
                finally
                {
                    RaisedNMI?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public virtual void LowerNMI()
        {
            if (this.NMI.Raised())
            {
                LoweringNMI?.Invoke(this, EventArgs.Empty);
                try
                {
                    this.NMI.Lower();
                }
                finally
                {
                    LoweredNMI?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        protected sealed class AutoNMI : IDisposable
        {
            private readonly Z80 _cpu;
            private bool _disposed;

            public AutoNMI(Z80 cpu)
            {
                _cpu = cpu;
                _cpu.LowerNMI();
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _cpu.RaiseNMI();
                    _disposed = true;
                }
                GC.SuppressFinalize(this);
            }
        }

        #endregion

        #region M1 pin

        public event EventHandler<EventArgs>? RaisingM1;

        public event EventHandler<EventArgs>? RaisedM1;

        public event EventHandler<EventArgs>? LoweringM1;

        public event EventHandler<EventArgs>? LoweredM1;

        private PinLevel _m1Line = PinLevel.Low;

        public ref PinLevel M1 => ref this._m1Line;

        protected virtual void OnLoweringM1() => LoweringM1?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredM1() => LoweredM1?.Invoke(this, EventArgs.Empty);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaiseM1()
        {
            if (this.M1.Lowered())
            {
                RaisingM1?.Invoke(this, EventArgs.Empty);
                try
                {
                    this.M1.Raise();
                }
                finally
                {
                    RaisedM1?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public virtual void LowerM1()
        {
            if (this.M1.Raised())
            {
                LoweringM1?.Invoke(this, EventArgs.Empty);
                try
                {
                    this.M1.Lower();
                }
                finally
                {
                    LoweredM1?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        protected sealed class AutoM1 : IDisposable
        {
            private readonly Z80 _cpu;
            private bool _disposed;

            public AutoM1(Z80 cpu)
            {
                _cpu = cpu;
                _cpu.LowerM1();
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _cpu.RaiseM1();
                    _disposed = true;
                }
                GC.SuppressFinalize(this);
            }
        }

        #endregion

        #region RFSH pin

        public event EventHandler<EventArgs>? RaisingRFSH;

        public event EventHandler<EventArgs>? RaisedRFSH;

        public event EventHandler<EventArgs>? LoweringRFSH;

        public event EventHandler<EventArgs>? LoweredRFSH;

        // ** From the Z80 CPU User Manual
        // RFSH.Refresh(output, active Low). RFSH, together with MREQ, indicates that the lower
        // seven bits of the system’s address bus can be used as a _refresh address to the system’s
        // dynamic memories.

        private PinLevel _refreshLine = PinLevel.Low;

        public ref PinLevel RFSH => ref this._refreshLine;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaiseRFSH()
        {
            if (this.RFSH.Lowered())
            {
                RaisingRFSH?.Invoke(this, EventArgs.Empty);
                try
                {
                    this.RFSH.Raise();
                    ++this.REFRESH;
                }
                finally
                {
                    RaisedRFSH?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public virtual void LowerRFSH()
        {
            if (this.RFSH.Raised())
            {
                LoweringRFSH?.Invoke(this, EventArgs.Empty);
                try
                {
                    this.RFSH.Lower();
                }
                finally
                {
                    LoweredRFSH?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        protected sealed class AutoRFSH : IDisposable
        {
            private readonly Z80 _cpu;
            private bool _disposed;

            public AutoRFSH(Z80 cpu)
            {
                _cpu = cpu;
                _cpu.LowerRFSH();
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _cpu.RaiseRFSH();
                    _disposed = true;
                }
                GC.SuppressFinalize(this);
            }
        }

        #endregion

        #region MREQ pin

        public event EventHandler<EventArgs>? RaisingMREQ;

        public event EventHandler<EventArgs>? RaisedMREQ;

        public event EventHandler<EventArgs>? LoweringMREQ;

        public event EventHandler<EventArgs>? LoweredMREQ;

        private PinLevel _memoryRequestLine = PinLevel.Low;

        public ref PinLevel MREQ => ref this._memoryRequestLine;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaiseMREQ()
        {
            if (this.MREQ.Lowered())
            {
                RaisingMREQ?.Invoke(this, EventArgs.Empty);
                try
                {
                    this.MREQ.Raise();
                }
                finally
                {
                    RaisedMREQ?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public virtual void LowerMREQ()
        {
            if (this.MREQ.Raised())
            {
                LoweringMREQ?.Invoke(this, EventArgs.Empty);
                try
                {
                    this.MREQ.Lower();
                }
                finally
                {
                    LoweredMREQ?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        protected sealed class AutoMREQ : IDisposable
        {
            private readonly Z80 _cpu;
            private bool _disposed;

            public AutoMREQ(Z80 cpu)
            {
                _cpu = cpu;
                _cpu.LowerMREQ();
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _cpu.RaiseMREQ();
                    _disposed = true;
                }
                GC.SuppressFinalize(this);
            }
        }

        #endregion

        #region IORQ pin

        public event EventHandler<EventArgs>? RaisingIORQ;

        public event EventHandler<EventArgs>? RaisedIORQ;

        public event EventHandler<EventArgs>? LoweringIORQ;

        public event EventHandler<EventArgs>? LoweredIORQ;

        private PinLevel _ioRequestLine = PinLevel.Low;

        public ref PinLevel IORQ => ref this._ioRequestLine;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaiseIORQ()
        {
            if (this.IORQ.Lowered())
            {
                RaisingIORQ?.Invoke(this, EventArgs.Empty);
                try
                {
                    this.IORQ.Raise();
                }
                finally
                {
                    RaisedIORQ?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public virtual void LowerIORQ()
        {
            if (this.IORQ.Raised())
            {
                LoweringIORQ?.Invoke(this, EventArgs.Empty);
                this.IORQ.Lower();
                LoweredIORQ?.Invoke(this, EventArgs.Empty);
            }
        }

        protected sealed class AutoIORQ : IDisposable
        {
            private readonly Z80 _cpu;
            private bool _disposed;

            public AutoIORQ(Z80 cpu)
            {
                _cpu = cpu;
                _cpu.LowerIORQ();
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _cpu.RaiseIORQ();
                    _disposed = true;
                }
                GC.SuppressFinalize(this);
            }
        }

        #endregion

        #region RD pin

        public event EventHandler<EventArgs>? RaisingRD;

        public event EventHandler<EventArgs>? RaisedRD;

        public event EventHandler<EventArgs>? LoweringRD;

        public event EventHandler<EventArgs>? LoweredRD;

        private PinLevel _rdLine = PinLevel.Low;

        public ref PinLevel RD => ref this._rdLine;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaiseRD()
        {
            if (this.RD.Lowered())
            {
                RaisingRD?.Invoke(this, EventArgs.Empty);
                try
                {
                    this.RD.Raise();
                }
                finally
                {
                    RaisedRD?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public virtual void LowerRD()
        {
            if (this.RD.Raised())
            {
                LoweringRD?.Invoke(this, EventArgs.Empty);
                try
                {
                    this.RD.Lower();
                }
                finally
                {
                    LoweredRD?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        protected sealed class AutoRD : IDisposable
        {
            private readonly Z80 _cpu;
            private bool _disposed;

            public AutoRD(Z80 cpu)
            {
                _cpu = cpu;
                _cpu.LowerRD();
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _cpu.RaiseRD();
                    _disposed = true;
                }
                GC.SuppressFinalize(this);
            }
        }

        #endregion

        #region WR pin

        public event EventHandler<EventArgs>? RaisingWR;

        public event EventHandler<EventArgs>? RaisedWR;

        public event EventHandler<EventArgs>? LoweringWR;

        public event EventHandler<EventArgs>? LoweredWR;

        private PinLevel _wrLine = PinLevel.Low;

        public ref PinLevel WR => ref this._wrLine;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "The word 'raise' is used in an electrical sense")]
        public virtual void RaiseWR()
        {
            if (this.WR.Lowered())
            {
                RaisingWR?.Invoke(this, EventArgs.Empty);
                try
                {
                    this.WR.Raise();
                }
                finally
                {
                    RaisedWR?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public virtual void LowerWR()
        {
            if (this.WR.Raised())
            {
                LoweringWR?.Invoke(this, EventArgs.Empty);
                try
                {
                    this.WR.Lower();
                }
                finally
                {
                    LoweredWR?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        protected sealed class AutoWR : IDisposable
        {
            private readonly Z80 _cpu;
            private bool _disposed;

            public AutoWR(Z80 cpu)
            {
                _cpu = cpu;
                _cpu.LowerWR();
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _cpu.RaiseWR();
                    _disposed = true;
                }
                GC.SuppressFinalize(this);
            }
        }

        #endregion

        #endregion

        protected void MemoryUpdate(int ticks)
        {
            this.OnWritingMemory();
            try
            {
                using var _ = new AutoMREQ(this);
                using var __ = new AutoWR(this);
                this.Tick(ticks);
                base.MemoryWrite();
            }
            finally
            {
                this.OnWroteMemory();
            }
        }

        protected override void MemoryWrite()
        {
            this.MemoryUpdate(3);
        }

        protected override byte MemoryRead()
        {
            this.OnReadingMemory();
            try
            {
                this.Tick();
                try
                {
                    using var __ = new AutoMREQ(this);
                    using var _ = new AutoRD(this);
                    this.Tick();
                    return base.MemoryRead();
                }
                finally
                {
                    if (this.M1.Lowered())
                    {
                        this.Bus.Address.Assign(this.REFRESH, this.IV);
                        using var _ = new AutoRFSH(this);
                        this.Tick();
                        using var __ = new AutoMREQ(this);
                    }
                    this.Tick();
                }
            }
            finally
            {
                this.OnReadMemory();
            }
        }

        // From Zilog Z80 manual
        // https://www.zilog.com/docs/z80/um0080.pdf
        //
        // RESET. Reset (input, active Low).
        // _____
        // RESET initializes the CPU as follows: it resets the
        // interrupt enable flip-flop, clears the Program Counter and registers I and R, and sets the
        // interrupt status to Mode 0. During reset time, the address and data bus enter a high-impedance
        // state, and all control output signals enter an inactive state.RESET must be active for
        // a minimum of three full clock cycles before a reset operation is complete.
        protected override void HandleRESET()
        {
            base.HandleRESET();
            this.IV = this.REFRESH = 0;
            this.IM = 0;
            this.SP.Word = this.AF.Word = (ushort)Mask.Sixteen;
            this.Tick(3);
        }

        private byte ReadDataUnderInterrupt()
        {
            using var _ = new AutoM1(this);
            using var __ = new AutoIORQ(this);
            return this.Bus.Data;
        }

        protected override void HandleINT()
        {
            base.HandleINT();

            var data = this.ReadDataUnderInterrupt();

            this.DisableInterrupts();
            this.Tick(5);
            switch (this.IM)
            {
                case 0: // i8080 equivalent
                    this.Execute(data);
                    break;
                case 1:
                    this.Tick();
                    this.Restart(7 << 3);   // 7 cycles
                    break;
                case 2:
                    this.Tick(7);
                    this.MEMPTR.Assign(data, this.IV);
                    this.Call(this.MEMPTR);
                    break;
                default:
                    throw new NotSupportedException("Invalid interrupt mode");
            }
        }

        protected override void Call(Register16 destination)
        {
            this.Tick();
            base.Call(destination);
        }

        protected override void JumpRelative(sbyte offset)
        {
            base.JumpRelative(offset);
            this.Tick(5);
        }

        private int Zero() => ZeroTest(this.F);

        private int Carry() => CarryTest(this.F);

        private int Parity() => ParityTest(this.F);

        private int Sign() => SignTest(this.F);

        private int HalfCarry() => HalfCarryTest(this.F);

        private int Subtracting() => SubtractingTest(this.F);

        private static int ZeroTest(byte data) => data & (byte)StatusBits.ZF;

        private static int CarryTest(byte data) => data & (byte)StatusBits.CF;

        private static int ParityTest(byte data) => data & (byte)StatusBits.PF;

        private static int SignTest(byte data) => data & (byte)StatusBits.SF;

        private static int HalfCarryTest(byte data) => data & (byte)StatusBits.HC;

        private static int SubtractingTest(byte data) => data & (byte)StatusBits.NF;

        private static int XTest(byte data) => data & (byte)StatusBits.XF;

        private static int YTest(byte data) => data & (byte)StatusBits.YF;

        private void AdjustStatusFlags(byte value) => this._modifiedF = this.F = value;

        private void SetBit(StatusBits flag) => this.AdjustStatusFlags(SetBit(this.F, flag));

        private static byte SetBit(byte f, StatusBits flag) => SetBit(f, (byte)flag);

        private void SetBit(StatusBits flag, int condition) => this.AdjustStatusFlags(SetBit(this.F, flag, condition));

        private static byte SetBit(byte f, StatusBits flag, int condition) => SetBit(f, (byte)flag, condition);

        private void SetBit(StatusBits flag, bool condition) => this.AdjustStatusFlags(SetBit(this.F, flag, condition));

        private static byte SetBit(byte f, StatusBits flag, bool condition) => SetBit(f, (byte)flag, condition);

        private void ClearBit(StatusBits flag) => this.AdjustStatusFlags(ClearBit(this.F, flag));

        private static byte ClearBit(byte f, StatusBits flag) => ClearBit(f, (byte)flag);

        private void ClearBit(StatusBits flag, int condition) => this.AdjustStatusFlags(ClearBit(this.F, flag, condition));

        private static byte ClearBit(byte f, StatusBits flag, int condition) => ClearBit(f, (byte)flag, condition);

        private void AdjustSign(byte value) => this.AdjustStatusFlags(AdjustSign(this.F, value));

        private static byte AdjustSign(byte input, byte value) => SetBit(input, StatusBits.SF, SignTest(value));

        private void AdjustZero(byte value) => this.AdjustStatusFlags(AdjustZero(this.F, value));

        private static byte AdjustZero(byte input, byte value) => ClearBit(input, StatusBits.ZF, value);

        private void AdjustParity(byte value) => this.AdjustStatusFlags(AdjustParity(this.F, value));

        private static byte AdjustParity(byte input, byte value) => SetBit(input, StatusBits.PF, EvenParity(value));

        private void AdjustSZ(byte value) => this.AdjustStatusFlags(AdjustSZ(this.F, value));

        private static byte AdjustSZ(byte input, byte value)
        {
            input = AdjustSign(input, value);
            return AdjustZero(input, value);
        }

        private void AdjustSZP(byte value) => this.AdjustStatusFlags(AdjustSZP(this.F, value));

        private static byte AdjustSZP(byte input, byte value)
        {
            input = AdjustSZ(input, value);
            return AdjustParity(input, value);
        }

        private void AdjustXY(byte value) => this.AdjustStatusFlags(AdjustXY(this.F, value));

        private static byte AdjustXY(byte input, byte value)
        {
            input = SetBit(input, StatusBits.XF, XTest(value));
            return SetBit(input, StatusBits.YF, YTest(value));
        }

        private void AdjustSZPXY(byte value) => this.AdjustStatusFlags(AdjustSZPXY(this.F, value));

        private static byte AdjustSZPXY(byte input, byte value)
        {
            input = AdjustSZP(input, value);
            return AdjustXY(input, value);
        }

        private void AdjustSZXY(byte value) => this.AdjustStatusFlags(AdjustSZXY(this.F, value));

        private static byte AdjustSZXY(byte input, byte value)
        {
            input = AdjustSZ(input, value);
            return AdjustXY(input, value);
        }

        private void AdjustHalfCarryAdd(byte before, byte value, int calculation) => this.AdjustStatusFlags(AdjustHalfCarryAdd(this.F, before, value, calculation));

        private static byte AdjustHalfCarryAdd(byte input, byte before, byte value, int calculation) => SetBit(input, StatusBits.HC, CalculateHalfCarryAdd(before, value, calculation));

        private void AdjustHalfCarrySub(byte before, byte value, int calculation) => this.AdjustStatusFlags(AdjustHalfCarrySub(this.F, before, value, calculation));

        private static byte AdjustHalfCarrySub(byte input, byte before, byte value, int calculation) => SetBit(input, StatusBits.HC, CalculateHalfCarrySub(before, value, calculation));

        private void AdjustOverflowAdd(int beforeNegative, int valueNegative, int afterNegative) => this.AdjustStatusFlags(AdjustOverflowAdd(this.F, beforeNegative, valueNegative, afterNegative));

        private static byte AdjustOverflowAdd(byte input, int beforeNegative, int valueNegative, int afterNegative)
        {
            var overflow = beforeNegative == valueNegative && beforeNegative != afterNegative;
            return SetBit(input, StatusBits.VF, overflow);
        }

        private void AdjustOverflowAdd(byte before, byte value, byte calculation) => this.AdjustStatusFlags(AdjustOverflowAdd(this.F, before, value, calculation));

        private static byte AdjustOverflowAdd(byte input, byte before, byte value, byte calculation) => AdjustOverflowAdd(input, SignTest(before), SignTest(value), SignTest(calculation));

        private void AdjustOverflowSub(int beforeNegative, int valueNegative, int afterNegative) => this.AdjustStatusFlags(AdjustOverflowSub(this.F, beforeNegative, valueNegative, afterNegative));

        private static byte AdjustOverflowSub(byte input, int beforeNegative, int valueNegative, int afterNegative)
        {
            var overflow = beforeNegative != valueNegative && beforeNegative != afterNegative;
            return SetBit(input, StatusBits.VF, overflow);
        }

        private void AdjustOverflowSub(byte before, byte value, byte calculation) => this.AdjustStatusFlags(AdjustOverflowSub(this.F, before, value, calculation));

        private static byte AdjustOverflowSub(byte input, byte before, byte value, byte calculation) => AdjustOverflowSub(input, SignTest(before), SignTest(value), SignTest(calculation));

        private static byte RES(int n, byte operand) => ClearBit(operand, Bit(n));

        private static byte SET(int n, byte operand) => SetBit(operand, Bit(n));

        protected override void DisableInterrupts() => this.IFF1 = this.IFF2 = false;

        protected override void EnableInterrupts() => this.IFF1 = this.IFF2 = true;

        private Register16 HL2() => this._prefixDD ? this.IX : this._prefixFD ? this.IY : this.HL;

        private Register16 RP(int rp) => rp switch
        {
            0 => this.BC,
            1 => this.DE,
            2 => this.HL2(),
            3 => this.SP,
            _ => throw new ArgumentOutOfRangeException(nameof(rp)),
        };

        private Register16 RP2(int rp) => rp switch
        {
            0 => this.BC,
            1 => this.DE,
            2 => this.HL2(),
            3 => this.AF,
            _ => throw new ArgumentOutOfRangeException(nameof(rp)),
        };

        private ref byte R(int r, AccessLevel access = AccessLevel.ReadOnly)
        {
            switch (r)
            {
                case 0:
                    return ref this.B;
                case 1:
                    return ref this.C;
                case 2:
                    return ref this.D;
                case 3:
                    return ref this.E;
                case 4:
                    return ref this.HL2().High;
                case 5:
                    return ref this.HL2().Low;
                case 6:
                    if (this._displaced)
                    {
                        this.DisplaceAddress();
                        this.Bus.Address.Assign(this.MEMPTR);
                    }
                    else
                    {
                        this.Bus.Address.Assign(this.HL);
                    }

                    switch (access)
                    {
                        case AccessLevel.ReadOnly:
                            this.MemoryRead();
                            break;
                        case AccessLevel.WriteOnly:
                            this.Tick();
                            break;
                        default:
                            throw new NotSupportedException("Invalid access level");
                    }

                    // Will need a post-MemoryWrite
                    return ref this.Bus.Data;
                case 7:
                    return ref this.A;
                default:
                    throw new ArgumentOutOfRangeException(nameof(r));
            }
        }

        private void R(int r, byte value, int ticks = 0)
        {
            this.R(r, AccessLevel.WriteOnly) = value;
            if (r == 6)
            {
                this.Tick(ticks);
                this.MemoryUpdate(1);
                this.Tick();
            }
        }

        private ref byte R2(int r)
        {
            switch (r)
            {
                case 0:
                    return ref this.B;
                case 1:
                    return ref this.C;
                case 2:
                    return ref this.D;
                case 3:
                    return ref this.E;
                case 4:
                    return ref this.H;
                case 5:
                    return ref this.L;
                case 6:
                    // N.B. Write not possible, when r == 6
                    _ = this.MemoryRead(this.HL);
                    return ref this.Bus.Data;
                case 7:
                    return ref this.A;
                default:
                    throw new ArgumentOutOfRangeException(nameof(r));
            }
        }

        private void ExecuteCB(int x, int y, int z)
        {
            var memoryZ = z == 6;
            var indirect = (!this._displaced && memoryZ) || this._displaced;
            var direct = !indirect;

            byte operand;
            if (this._displaced)
            {
                this.Tick(2);
                this.DisplaceAddress();
                operand = this.MemoryRead(this.MEMPTR);
            }
            else
            {
                operand = this.R(z);
            }

            var update = x != 1; // BIT does not update
            switch (x)
            {
                case 0: // rot[y] r[z]
                    operand = y switch
                    {
                        0 => this.RLC(operand),
                        1 => this.RRC(operand),
                        2 => this.RL(operand),
                        3 => this.RR(operand),
                        4 => this.SLA(operand),
                        5 => this.SRA(operand),
                        6 => this.SLL(operand),
                        7 => this.SRL(operand),
                        _ => throw new NotSupportedException("Invalid operation mode"),
                    };
                    this.AdjustSZP(operand);
                    break;
                case 1: // BIT y, r[z]
                    this.BIT(y, operand);
                    this.AdjustXY(direct ? operand : this.MEMPTR.High);
                    if (indirect)
                        this.Tick();
                    break;
                case 2: // RES y, r[z]
                    operand = RES(y, operand);
                    break;
                case 3: // SET y, r[z]
                    operand = SET(y, operand);
                    break;
                default:
                    throw new NotSupportedException("Invalid operation mode");
            }

            if (update)
            {
                if (this._displaced)
                {
                    this.Tick();
                    this.MemoryWrite(operand);
                    if (!memoryZ)
                    {
                        this.R2(z) = operand;
                    }
                }
                else
                {
                    if (memoryZ)
                        this.Tick();
                    this.R(z, operand);
                }
            }
        }

        private void ExecuteED(int x, int y, int z, int p, int q)
        {
            switch (x)
            {
                case 0:
                case 3: // Invalid instruction, equivalent to NONI followed by NOP
                    break;
                case 1:
                    switch (z)
                    {
                        case 0: // Input from port with 16-bit address
                            this.Bus.Address.Assign(this.BC);
                            this.ReadPort();
                            this.MEMPTR.Increment();
                            if (y != 6)
                            {
                                this.R(y, AccessLevel.WriteOnly) = this.Bus.Data; // IN r[y],(C)
                            }

                            this.AdjustSZPXY(this.Bus.Data);
                            this.ClearBit(StatusBits.NF | StatusBits.HC);
                            break;
                        case 1: // Output to port with 16-bit address
                            this.WritePort(this.BC, y == 6 ? (byte)0 : this.R(y));
                            this.MEMPTR.Increment();
                            break;
                        case 2: // 16-bit add/subtract with carry
                            this.HL2().Assign(q switch
                            {
                                0 => this.SBC(this.HL2(), this.RP(p)), // SBC HL, rp[p]
                                1 => this.ADC(this.HL2(), this.RP(p)), // ADC HL, rp[p]
                                _ => throw new NotSupportedException("Invalid operation mode"),
                            });
                            this.Tick(7);
                            break;
                        case 3: // Retrieve/store register pair from/to immediate address
                            this.FetchWordAddress();
                            switch (q)
                            {
                                case 0: // LD (nn), rp[p]
                                    this.SetWord(this.RP(p));
                                    break;
                                case 1: // LD rp[p], (nn)
                                    this.RP(p).Assign(this.GetWord());
                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            break;
                        case 4: // Negate accumulator
                            this.NEG();
                            break;
                        case 5: // Return from interrupt
                            switch (y)
                            {
                                case 1:
                                    this.RetI(); // RETI
                                    break;
                                default:
                                    this.RetN(); // RETN
                                    break;
                            }

                            break;
                        case 6: // Set interrupt mode
                            this.IM = y switch
                            {
                                0 or 1 or 4 or 5 => 0,
                                2 or 6 => 1,
                                3 or 7 => 2,
                                _ => throw new NotSupportedException("Invalid operation mode"),
                            };
                            break;
                        case 7: // Assorted ops
                            switch (y)
                            {
                                case 0: // LD I,A
                                    this.IV = this.A;
                                    this.Tick();
                                    break;
                                case 1: // LD R,A
                                    this.REFRESH = this.A;
                                    this.Tick();
                                    break;
                                case 2: // LD A,I
                                    this.AdjustSZXY(this.A = this.IV);
                                    this.ClearBit(StatusBits.NF | StatusBits.HC);
                                    this.SetBit(StatusBits.PF, this.IFF2);
                                    this.Tick();
                                    break;
                                case 3: // LD A,R
                                    this.AdjustSZXY(this.A = this.REFRESH);
                                    this.ClearBit(StatusBits.NF | StatusBits.HC);
                                    this.SetBit(StatusBits.PF, this.IFF2);
                                    this.Tick();
                                    break;
                                case 4: // RRD
                                    this.RRD();
                                    break;
                                case 5: // RLD
                                    this.RLD();
                                    break;
                                case 6: // NOP
                                case 7: // NOP
                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            break;
                        default:
                            throw new NotSupportedException("Invalid operation mode");
                    }

                    break;
                case 2:
                    switch (z)
                    {
                        case 0: // LD
                            switch (y)
                            {
                                case 4: // LDI
                                    this.LDI();
                                    break;
                                case 5: // LDD
                                    this.LDD();
                                    break;
                                case 6: // LDIR
                                    this.LDIR();
                                    break;
                                case 7: // LDDR
                                    this.LDDR();
                                    break;
                                default:
                                    break;
                            }

                            break;
                        case 1: // CP
                            switch (y)
                            {
                                case 4: // CPI
                                    this.CPI();
                                    break;
                                case 5: // CPD
                                    this.CPD();
                                    break;
                                case 6: // CPIR
                                    this.CPIR();
                                    break;
                                case 7: // CPDR
                                    this.CPDR();
                                    break;
                                default:
                                    break;
                            }

                            break;
                        case 2: // IN
                            switch (y)
                            {
                                case 4: // INI
                                    this.INI();
                                    break;
                                case 5: // IND
                                    this.IND();
                                    break;
                                case 6: // INIR
                                    this.INIR();
                                    break;
                                case 7: // INDR
                                    this.INDR();
                                    break;
                                default:
                                    break;
                            }

                            break;
                        case 3: // OUT
                            switch (y)
                            {
                                case 4: // OUTI
                                    this.OUTI();
                                    break;
                                case 5: // OUTD
                                    this.OUTD();
                                    break;
                                case 6: // OTIR
                                    this.OTIR();
                                    break;
                                case 7: // OTDR
                                    this.OTDR();
                                    break;
                                default:
                                    break;
                            }

                            break;
                        default:
                            break;
                    }

                    break;
                default:
                    break;
            }
        }

        private void ExecuteOther(int x, int y, int z, int p, int q)
        {
            var memoryY = y == 6;
            var memoryZ = z == 6;
            switch (x)
            {
                case 0:
                    switch (z)
                    {
                        case 0: // Relative jumps and assorted ops
                            switch (y)
                            {
                                case 0: // NOP
                                    break;
                                case 1: // EX AF AF'
                                    this.ExxAF();
                                    break;
                                case 2: // DJNZ d
                                    this.Tick();
                                    _ = this.JumpRelativeConditional(--this.B != 0);
                                    break;
                                case 3: // JR d
                                    this.JumpRelative(this.FetchByte());
                                    break;
                                case 4: // JR cc,d
                                case 5:
                                case 6:
                                case 7:
                                    _ = this.JumpRelativeConditionalFlag(y - 4);
                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            break;
                        case 1: // 16-bit load immediate/add
                            switch (q)
                            {
                                case 0: // LD rp,nn
                                    this.RP(p).Assign(this.FetchWord());
                                    break;
                                case 1: // ADD HL,rp
                                    this.HL2().Assign(this.Add(this.HL2(), this.RP(p)));
                                    this.Tick(7);
                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            break;
                        case 2: // Indirect loading
                            switch (q)
                            {
                                case 0:
                                    switch (p)
                                    {
                                        case 0: // LD (BC),A
                                            WriteMemoryIndirect(this.BC, this.A);
                                            break;
                                        case 1: // LD (DE),A
                                            WriteMemoryIndirect(this.DE, this.A);
                                            break;
                                        case 2: // LD (nn),HL
                                            this.FetchWordAddress();
                                            this.SetWord(this.HL2());
                                            break;
                                        case 3: // LD (nn),A
                                            this.FetchWordMEMPTR();
                                            WriteMemoryIndirect(this.A);
                                            break;
                                        default:
                                            throw new NotSupportedException("Invalid operation mode");
                                    }

                                    break;
                                case 1:
                                    switch (p)
                                    {
                                        case 0: // LD A,(BC)
                                            this.A = this.ReadMemoryIndirect(this.BC);
                                            break;
                                        case 1: // LD A,(DE)
                                            this.A = this.ReadMemoryIndirect(this.DE);
                                            break;
                                        case 2: // LD HL,(nn)
                                            this.FetchWordAddress();
                                            this.HL2().Assign(this.GetWord());
                                            break;
                                        case 3: // LD A,(nn)
                                            this.FetchWordMEMPTR();
                                            this.A = this.ReadMemoryIndirect();
                                            break;
                                        default:
                                            throw new NotSupportedException("Invalid operation mode");
                                    }

                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            break;
                        case 3: // 16-bit INC/DEC
                            switch (q)
                            {
                                case 0: // INC rp
                                    this.RP(p).Increment();
                                    break;
                                case 1: // DEC rp
                                    this.RP(p).Decrement();
                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }
                            this.Tick(2);
                            break;
                        case 4: // 8-bit INC
                            if (memoryY && this._displaced)
                            {
                                this.FetchDisplacement();
                                this.Tick(5);
                            }
                            this.R(y, this.Increment(this.R(y)), 1);
                            break;

                        case 5: // 8-bit DEC
                            if (memoryY && this._displaced)
                            {
                                this.FetchDisplacement();
                                this.Tick(5);
                            }
                            this.R(y, this.Decrement(this.R(y)), 1);
                            break;

                        case 6: // 8-bit load immediate
                            {
                                var displacing = memoryY && this._displaced;
                                if (displacing)
                                {
                                    this.FetchDisplacement();
                                }

                                var value = this.FetchByte();

                                if (displacing)
                                {
                                    this.Tick(2);
                                }

                                this.R(y, value);  // LD r,n
                                break;
                            }

                        case 7: // Assorted operations on accumulator/flags
                            switch (y)
                            {
                                case 0:
                                    this.A = this.RLC(this.A);
                                    break;
                                case 1:
                                    this.A = this.RRC(this.A);
                                    break;
                                case 2:
                                    this.A = this.RL(this.A);
                                    break;
                                case 3:
                                    this.A = this.RR(this.A);
                                    break;
                                case 4:
                                    this.DAA();
                                    break;
                                case 5:
                                    this.CPL();
                                    break;
                                case 6:
                                    this.SCF();
                                    break;
                                case 7:
                                    this.CCF();
                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            break;
                        default:
                            throw new NotSupportedException("Invalid operation mode");
                    }

                    break;
                case 1: // 8-bit loading
                    if (!(memoryZ && memoryY))
                    {
                        var normal = true;
                        if (this._displaced)
                        {
                            if (memoryZ || memoryY)
                            {
                                this.FetchDisplacement();
                                this.Tick(5);
                            }

                            if (memoryZ)
                            {
                                switch (y)
                                {
                                    case 4:
                                        this.H = this.R(z);
                                        normal = false;
                                        break;
                                    case 5:
                                        this.L = this.R(z);
                                        normal = false;
                                        break;
                                    default:
                                        break;
                                }
                            }

                            if (memoryY)
                            {
                                switch (z)
                                {
                                    case 4:
                                        this.R(y, this.H);
                                        normal = false;
                                        break;
                                    case 5:
                                        this.R(y, this.L);
                                        normal = false;
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }

                        if (normal)
                        {
                            var value = this.R(z);
                            this.R(y, value);
                        }
                    }
                    else
                    {
                        this.LowerHALT(); // Exception (replaces LD (HL), (HL))
                    }

                    break;
                case 2:
                    { // Operate on accumulator and register/memory location
                        if (memoryZ && this._displaced)
                        {
                            this.FetchDisplacement();
                            this.Tick(5);
                        }

                        var value = this.R(z);
                        switch (y)
                        {
                            case 0: // ADD A,r
                                this.A = this.Add(this.A, value);
                                break;
                            case 1: // ADC A,r
                                this.A = this.ADC(this.A, value);
                                break;
                            case 2: // SUB r
                                this.A = this.SUB(this.A, value);
                                break;
                            case 3: // SBC A,r
                                this.A = this.SBC(this.A, value);
                                break;
                            case 4: // AND r
                                this.AndR(value);
                                break;
                            case 5: // XOR r
                                this.XorR(value);
                                break;
                            case 6: // OR r
                                this.OrR(value);
                                break;
                            case 7: // CP r
                                this.Compare(value);
                                break;
                            default:
                                throw new NotSupportedException("Invalid operation mode");
                        }

                        break;
                    }

                case 3:
                    switch (z)
                    {
                        case 0: // Conditional return
                            _ = this.ReturnConditionalFlag(y);
                            break;
                        case 1: // POP & various ops
                            switch (q)
                            {
                                case 0: // POP rp2[p]
                                    this.RP2(p).Assign(this.PopWord());
                                    break;
                                case 1:
                                    switch (p)
                                    {
                                        case 0: // RET
                                            this.Return();
                                            break;
                                        case 1: // EXX
                                            this.Exx();
                                            break;
                                        case 2: // JP HL
                                            this.Jump(this.HL2());
                                            break;
                                        case 3: // LD SP,HL
                                            this.SP.Assign(this.HL2());
                                            this.Tick(2);
                                            break;
                                        default:
                                            throw new NotSupportedException("Invalid operation mode");
                                    }

                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            break;
                        case 2: // Conditional jump
                            _ = this.JumpConditionalFlag(y);
                            break;
                        case 3: // Assorted operations
                            switch (y)
                            {
                                case 0: // JP nn
                                    this.JumpIndirect();
                                    break;
                                case 1: // CB prefix
                                    this._prefixCB = true;
                                    if (this._displaced)
                                    {
                                        this.FetchDisplacement();
                                        this.Execute(this.FetchByte());
                                    }
                                    else
                                    {
                                        this.Execute(this.FetchInstruction());
                                    }

                                    break;
                                case 2: // OUT (n),A
                                    this.WritePort(this.FetchByte());
                                    break;
                                case 3: // IN A,(n)
                                    this.ReadPort(this.FetchByte());
                                    this.A = this.Bus.Data;
                                    break;
                                case 4: // EX (SP),HL
                                    this.XHTL(this.HL2());
                                    break;
                                case 5: // EX DE,HL
                                    {
                                        this.Intermediate.Assign(this.DE);
                                        this.DE.Assign(this.HL);
                                        this.HL.Assign(this.Intermediate);
                                    }
                                    break;
                                case 6: // DI
                                    this.DisableInterrupts();
                                    break;
                                case 7: // EI
                                    this.EnableInterrupts();
                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            break;
                        case 4: // Conditional call: CALL cc[y], nn
                            _ = this.CallConditionalFlag(y);
                            break;
                        case 5: // PUSH & various ops
                            switch (q)
                            {
                                case 0: // PUSH rp2[p]
                                    this.Tick();
                                    this.PushWord(this.RP2(p));
                                    break;
                                case 1:
                                    switch (p)
                                    {
                                        case 0: // CALL nn
                                            this.CallIndirect();
                                            break;
                                        case 1: // DD prefix
                                            this._displaced = this._prefixDD = true;
                                            this.Execute(this.FetchInstruction());
                                            break;
                                        case 2: // ED prefix
                                            this._prefixED = true;
                                            this.Execute(this.FetchInstruction());
                                            break;
                                        case 3: // FD prefix
                                            this._displaced = this._prefixFD = true;
                                            this.Execute(this.FetchInstruction());
                                            break;
                                        default:
                                            throw new NotSupportedException("Invalid operation mode");
                                    }

                                    break;
                                default:
                                    throw new NotSupportedException("Invalid operation mode");
                            }

                            break;
                        case 6:
                            { // Operate on accumulator and immediate operand: alu[y] n
                                var operand = this.FetchByte();
                                switch (y)
                                {
                                    case 0: // ADD A,n
                                        this.A = this.Add(this.A, operand);
                                        break;
                                    case 1: // ADC A,n
                                        this.A = this.ADC(this.A, operand);
                                        break;
                                    case 2: // SUB n
                                        this.A = this.SUB(this.A, operand);
                                        break;
                                    case 3: // SBC A,n
                                        this.A = this.SBC(this.A, operand);
                                        break;
                                    case 4: // AND n
                                        this.AndR(operand);
                                        break;
                                    case 5: // XOR n
                                        this.XorR(operand);
                                        break;
                                    case 6: // OR n
                                        this.OrR(operand);
                                        break;
                                    case 7: // CP n
                                        this.Compare(operand);
                                        break;
                                    default:
                                        throw new NotSupportedException("Invalid operation mode");
                                }

                                break;
                            }

                        case 7: // Restart: RST y * 8
                            this.Restart((byte)(y << 3));
                            break;
                        default:
                            throw new NotSupportedException("Invalid operation mode");
                    }

                    break;
                default:
                    break;
            }
        }

        private void HandleNMI()
        {
            this.RaiseNMI();
            this.RaiseHALT();
            this.IFF2 = this.IFF1;
            this.IFF1 = false;
            using var __ = new AutoM1(this);
            _ = this.Bus.Data;
            this.Restart(0x66);
        }

        private void FetchDisplacement() => this._displacement = (sbyte)this.FetchByte();

        // ** From the Z80 CPU User Manual
        // Figure 5 depicts the timing during an M1 (op code fetch) cycle. The Program Counter is
        // placed on the address bus at the beginning of the M1 cycle. One half clock cycle later, the
        // MREQ signal goes active. At this time, the address to memory has had time to stabilize so
        // that the falling edge of MREQ can be used directly as a chip enable clock to dynamic
        // memories. The RD line also goes active to indicate that the memory read data should be
        // enabled onto the CPU data bus. The CPU samples the data from the memory space on the
        // data bus with the rising edge of the clock of state T3, and this same edge is used by the
        // CPU to turn off the RD and MREQ signals. As a result, the data is sampled by the CPU
        // before the RD signal becomes inactive. Clock states T3 and T4 of a fetch cycle are used to
        // _refresh dynamic memories. The CPU uses this time to decode and execute the fetched
        // instruction so that no other concurrent operation can be performed.
        protected override byte FetchInstruction()
        {
            using var _ = new AutoM1(this);
            return base.FetchInstruction();
        }

        private byte Subtract(byte operand, byte value, int carry = 0)
        {
            this.Intermediate.Word = (ushort)(operand - value - carry);
            var result = this.Intermediate.Low;

            this.AdjustHalfCarrySub(operand, value, result);
            this.AdjustOverflowSub(operand, value, result);

            this.SetBit(StatusBits.NF);
            this.SetBit(StatusBits.CF, CarryTest(this.Intermediate.High));
            this.AdjustSZ(result);

            return result;
        }

        private byte Increment(byte operand)
        {
            this.ClearBit(StatusBits.NF);
            this.AdjustSZXY(++operand);
            this.SetBit(StatusBits.VF, operand == (byte)Bits.Bit7);
            this.ClearBit(StatusBits.HC, LowNibble(operand));
            return operand;
        }

        private byte Decrement(byte operand)
        {
            this.SetBit(StatusBits.NF);
            this.ClearBit(StatusBits.HC, LowNibble(operand));
            this.AdjustSZXY(--operand);
            this.SetBit(StatusBits.VF, operand == (byte)Mask.Seven);
            return operand;
        }

        private void RetN()
        {
            this.Return();
            this.IFF1 = this.IFF2;
        }

        private void RetI() => this.RetN();

        protected sealed override bool ConvertCondition(int flag) => flag switch
        {
            0 => this.Zero() == 0,
            1 => this.Zero() != 0,
            2 => this.Carry() == 0,
            3 => this.Carry() != 0,
            4 => this.Parity() == 0,
            5 => this.Parity() != 0,
            6 => this.Sign() == 0,
            7 => this.Sign() != 0,
            _ => throw new ArgumentOutOfRangeException(nameof(flag)),
        };

        protected sealed override bool ReturnConditionalFlag(int flag)
        {
            var condition = this.ConvertCondition(flag);
            this.Tick();
            if (condition)
            {
                this.Return();
            }
            return condition;
        }

        private Register16 SBC(Register16 operand, Register16 value)
        {
            var subtraction = operand.Word - value.Word - this.Carry();
            this.Intermediate.Word = (ushort)subtraction;

            this.SetBit(StatusBits.NF);
            this.ClearBit(StatusBits.ZF, this.Intermediate.Word);
            this.SetBit(StatusBits.CF, subtraction & (int)Bits.Bit16);
            this.AdjustHalfCarrySub(operand.High, value.High, this.Intermediate.High);
            this.AdjustXY(this.Intermediate.High);

            var beforeNegative = SignTest(operand.High);
            var valueNegative = SignTest(value.High);
            var afterNegative = SignTest(this.Intermediate.High);

            this.SetBit(StatusBits.SF, afterNegative);
            this.AdjustOverflowSub(beforeNegative, valueNegative, afterNegative);

            this.MEMPTR.Word = (ushort)(operand.Word + 1);

            return this.Intermediate;
        }

        private Register16 ADC(Register16 operand, Register16 value)
        {
            _ = this.Add(operand, value, this.Carry());
            this.ClearBit(StatusBits.ZF, this.Intermediate.Word);

            var beforeNegative = SignTest(operand.High);
            var valueNegative = SignTest(value.High);
            var afterNegative = SignTest(this.Intermediate.High);

            this.SetBit(StatusBits.SF, afterNegative);
            this.AdjustOverflowAdd(beforeNegative, valueNegative, afterNegative);

            return this.Intermediate;
        }

        private Register16 Add(Register16 operand, Register16 value, int carry = 0)
        {
            var addition = operand.Word + value.Word + carry;
            this.Intermediate.Word = (ushort)addition;

            this.ClearBit(StatusBits.NF);
            this.SetBit(StatusBits.CF, addition & (int)Bits.Bit16);
            this.AdjustHalfCarryAdd(operand.High, value.High, this.Intermediate.High);
            this.AdjustXY(this.Intermediate.High);

            this.MEMPTR.Word = (ushort)(operand.Word + 1);

            return this.Intermediate;
        }

        private byte Add(byte operand, byte value, int carry = 0)
        {
            this.Intermediate.Word = (ushort)(operand + value + carry);
            var result = this.Intermediate.Low;

            this.AdjustHalfCarryAdd(operand, value, result);
            this.AdjustOverflowAdd(operand, value, result);

            this.ClearBit(StatusBits.NF);
            this.SetBit(StatusBits.CF, CarryTest(this.Intermediate.High));
            this.AdjustSZXY(result);

            return result;
        }

        private byte ADC(byte operand, byte value) => this.Add(operand, value, this.Carry());

        private byte SUB(byte operand, byte value, int carry = 0)
        {
            var subtraction = this.Subtract(operand, value, carry);
            this.AdjustXY(subtraction);
            return subtraction;
        }

        private byte SBC(byte operand, byte value) => this.SUB(operand, value, this.Carry());

        private void AndR(byte value)
        {
            this.SetBit(StatusBits.HC);
            this.ClearBit(StatusBits.CF | StatusBits.NF);
            this.AdjustSZPXY(this.A &= value);
        }

        private void XorR(byte value)
        {
            this.ClearBit(StatusBits.HC | StatusBits.CF | StatusBits.NF);
            this.AdjustSZPXY(this.A ^= value);
        }

        private void OrR(byte value)
        {
            this.ClearBit(StatusBits.HC | StatusBits.CF | StatusBits.NF);
            this.AdjustSZPXY(this.A |= value);
        }

        private void Compare(byte value)
        {
            _ = this.Subtract(this.A, value);
            this.AdjustXY(value);
        }

        private byte RLC(byte operand)
        {
            this.ClearBit(StatusBits.NF | StatusBits.HC);
            var carry = operand & (byte)Bits.Bit7;
            this.SetBit(StatusBits.CF, carry);
            var result = (byte)((operand << 1) | (carry >> 7));
            this.AdjustXY(result);
            return result;
        }

        private byte RRC(byte operand)
        {
            this.ClearBit(StatusBits.NF | StatusBits.HC);
            var carry = operand & (byte)Bits.Bit0;
            this.SetBit(StatusBits.CF, carry);
            var result = (byte)((operand >> 1) | (carry << 7));
            this.AdjustXY(result);
            return result;
        }

        private byte RL(byte operand)
        {
            this.ClearBit(StatusBits.NF | StatusBits.HC);
            var carry = this.Carry();
            this.SetBit(StatusBits.CF, operand & (byte)Bits.Bit7);
            var result = (byte)((operand << 1) | carry);
            this.AdjustXY(result);
            return result;
        }

        private byte RR(byte operand)
        {
            this.ClearBit(StatusBits.NF | StatusBits.HC);
            var carry = this.Carry();
            this.SetBit(StatusBits.CF, operand & (byte)Bits.Bit0);
            var result = (byte)((operand >> 1) | (carry << 7));
            this.AdjustXY(result);
            return result;
        }

        private byte SLA(byte operand)
        {
            this.ClearBit(StatusBits.NF | StatusBits.HC);
            this.SetBit(StatusBits.CF, operand & (byte)Bits.Bit7);
            var result = (byte)(operand << 1);
            this.AdjustXY(result);
            return result;
        }

        private byte SRA(byte operand)
        {
            this.ClearBit(StatusBits.NF | StatusBits.HC);
            this.SetBit(StatusBits.CF, operand & (byte)Bits.Bit0);
            var result = (byte)((operand >> 1) | (operand & (byte)Bits.Bit7));
            this.AdjustXY(result);
            return result;
        }

        private byte SLL(byte operand)
        {
            this.ClearBit(StatusBits.NF | StatusBits.HC);
            this.SetBit(StatusBits.CF, operand & (byte)Bits.Bit7);
            var result = (byte)((operand << 1) | (byte)Bits.Bit0);
            this.AdjustXY(result);
            return result;
        }

        private byte SRL(byte operand)
        {
            this.ClearBit(StatusBits.NF | StatusBits.HC);
            this.SetBit(StatusBits.CF, operand & (byte)Bits.Bit0);
            var result = (byte)((operand >> 1) & ~(byte)Bits.Bit7);
            this.AdjustXY(result);
            this.SetBit(StatusBits.ZF, result);
            return result;
        }

        private void BIT(int n, byte operand)
        {
            this.SetBit(StatusBits.HC);
            this.ClearBit(StatusBits.NF);
            var discarded = (byte)(operand & Bit(n));
            this.AdjustSZ(discarded);
            this.ClearBit(StatusBits.PF, discarded);
        }

        private void DAA()
        {
            var updated = this.A;

            var lowAdjust = this.HalfCarry() != 0 || LowNibble(this.A) > 9;
            var highAdjust = this.Carry() != 0 || this.A > 0x99;

            if (this.Subtracting() != 0)
            {
                if (lowAdjust)
                {
                    updated -= 6;
                }

                if (highAdjust)
                {
                    updated -= 0x60;
                }
            }
            else
            {
                if (lowAdjust)
                {
                    updated += 6;
                }

                if (highAdjust)
                {
                    updated += 0x60;
                }
            }

            this.AdjustStatusFlags((byte)((this.F & (byte)(StatusBits.CF | StatusBits.NF)) | (this.A > 0x99 ? (byte)StatusBits.CF : 0) | HalfCarryTest((byte)(this.A ^ updated))));
            this.AdjustSZPXY(this.A = updated);
        }

        // https://worldofspectrum.org/forums/discussion/comment/666508/#Comment_666508

        private void SCF()
        {
            this.SetBit(StatusBits.CF);
            this.ClearBit(StatusBits.HC | StatusBits.NF);
            this.AdjustXY((byte)((this.Q ^ this.F) | this.A));
        }

        private void CCF()
        {
            this.ClearBit(StatusBits.NF);
            var carry = this.Carry();
            this.SetBit(StatusBits.HC, carry);
            this.ClearBit(StatusBits.CF, carry);
            this.AdjustXY((byte)((this.Q ^ this.F) | this.A));
        }

        private void CPL()
        {
            this.SetBit(StatusBits.HC | StatusBits.NF);
            this.AdjustXY(this.A = (byte)~this.A);
        }

        private void XHTL(Register16 exchange)
        {
            this.MEMPTR.Low = this.MemoryRead(this.SP);
            this.Bus.Address.Increment();
            this.MEMPTR.High = this.MemoryRead();
            this.Tick();
            _ = this.Bus.Address.Decrement();
            this.Tick();
            this.Bus.Data = exchange.Low;
            exchange.Low = this.MEMPTR.Low;
            this.MemoryUpdate(1);
            this.Tick();
            _ = this.Bus.Address.Increment();
            this.Tick();
            this.Bus.Data = exchange.High;
            exchange.High = this.MEMPTR.High;
            this.MemoryUpdate(1);
            this.Tick(3);
        }

        #region Block instructions

        private void RepeatBlockInstruction()
        {
            this.MEMPTR.Assign(this.DecrementPC());
            _ = this.DecrementPC();
            this.AdjustXY(this.PC.High);
        }

        #region Block compare

        private void BlockCompare()
        {
            _ = this.MemoryRead(this.HL);
            var result = (byte)(this.A - this.Bus.Data);

            this.SetBit(StatusBits.PF, --this.BC.Word);

            this.AdjustSZ(result);
            this.AdjustHalfCarrySub(this.A, this.Bus.Data, result);
            this.SetBit(StatusBits.NF);

            result -= (byte)(this.HalfCarry() >> 4);

            this.SetBit(StatusBits.YF, result & (byte)Bits.Bit1);
            this.SetBit(StatusBits.XF, result & (byte)Bits.Bit3);

            this.Tick(5);
        }

        #region Block compare single

        private void CPI()
        {
            this.BlockCompare();
            this.HL.Increment();
            this.MEMPTR.Increment();
        }

        private void CPD()
        {
            this.BlockCompare();
            this.HL.Decrement();
            this.MEMPTR.Decrement();
        }

        #endregion

        #region Block compare repeated

        private void CPIR()
        {
            this.CPI();
            if (this.Parity() != 0 && this.Zero() == 0)
            {
                this.RepeatBlockInstruction();
                this.Tick(5);
            }
        }

        private void CPDR()
        {
            this.CPD();
            if (this.Parity() != 0 && this.Zero() == 0)
            {
                this.RepeatBlockInstruction();
                this.Tick(5);
            }
            else
            {
                this.MEMPTR.Word = (ushort)(this.PC.Word - 2);
                this.Tick(2);
            }
        }

        #endregion

        #endregion

        #region Block load

        private void BlockLoad()
        {
            this.MemoryRead(this.HL);
            this.Bus.Address.Assign(this.DE);
            this.Tick();
            this.MemoryUpdate(1);
            this.Tick(3);
            var xy = this.A + this.Bus.Data;
            this.SetBit(StatusBits.XF, xy & (int)Bits.Bit3);
            this.SetBit(StatusBits.YF, xy & (int)Bits.Bit1);
            this.ClearBit(StatusBits.NF | StatusBits.HC);
            this.SetBit(StatusBits.PF, --this.BC.Word);
        }

        #region Block load single

        private void LDI()
        {
            this.BlockLoad();
            this.HL.Increment();
            this.DE.Increment();
        }

        private void LDD()
        {
            this.BlockLoad();
            this.HL.Decrement();
            this.DE.Decrement();
        }

        #endregion

        #region Block load repeated

        private void LDIR()
        {
            this.LDI();
            if (this.Parity() != 0)
            {
                this.RepeatBlockInstruction();
            }
            this.Tick(5);
        }

        private void LDDR()
        {
            this.LDD();
            if (this.Parity() != 0)
            {
                this.RepeatBlockInstruction();
            }
            this.Tick(5);
        }

        #endregion

        #endregion

        #region Block input/output

        private void AdjustBlockRepeatFlagsIO()
        {
            if (this.Carry() != 0)
            {
                var negative = SignTest(this.Bus.Data) != 0;
                var direction = (byte)(this.B + (negative ? -1 : +1));
                var parity = (this.Parity() >> 2) ^ (EvenParity((byte)(direction & (byte)Mask.Three)) ? 1 : 0) ^ 1;
                this.SetBit(StatusBits.PF, parity != 0);
                this.SetBit(StatusBits.HC, (this.B & (byte)Mask.Four) == (negative ? 0 : (int)Mask.Four));
            }
            else
            {
                this.AdjustParity((byte)(this.B & (byte)Mask.Three));
            }
        }

        private void AdjustBlockInputOutputFlags(int basis)
        {
            this.SetBit(StatusBits.HC | StatusBits.CF, basis > 0xff);
            this.AdjustParity((byte)((basis & (int)Mask.Three) ^ this.B));
        }

        private void AdjustBlockInFlagsIncrement()
        {
            AdjustBlockInputOutputFlags((byte)(this.C + 1) + this.Bus.Data);
        }

        private void AdjustBlockInFlagsDecrement()
        {
            AdjustBlockInputOutputFlags((byte)(this.C - 1) + this.Bus.Data);
        }

        private void AdjustBlockOutFlags()
        {
            // HL needs to have been incremented or decremented prior to this call
            this.SetBit(StatusBits.NF, SignTest(this.Bus.Data));
            AdjustBlockInputOutputFlags(this.L + this.Bus.Data);
        }

        #region Block input

        private void BlockIn()
        {
            this.Tick();
            this.Bus.Address.Assign(this.BC);
            this.ReadPort();
            this.Bus.Address.Assign(this.HL);
            this.Tick();
            this.MemoryUpdate(1);
            this.Tick();
            this.AdjustSZXY(--this.B);
            this.SetBit(StatusBits.NF, this.Bus.Data & (byte)StatusBits.SF);
        }

        #region Block input single

        private void INI()
        {
            this.BlockIn();
            this.AdjustBlockInFlagsIncrement();
            this.HL.Increment();
            this.MEMPTR.Increment();
        }

        private void IND()
        {
            this.BlockIn();
            this.AdjustBlockInFlagsDecrement();
            this.HL.Decrement();
            this.MEMPTR.Decrement();
        }

        #endregion

        #region Block input repeated

        private void INIR()
        {
            this.INI();
            if (this.Zero() == 0)
            {
                this.RepeatBlockInstruction();
                this.AdjustBlockRepeatFlagsIO();
                this.Tick(5);
            }
        }

        private void INDR()
        {
            this.IND();
            if (this.Zero() == 0)
            {
                this.RepeatBlockInstruction();
                this.AdjustBlockRepeatFlagsIO();
                this.Tick(5);
            }
        }

        #endregion

        #endregion

        #region Block output

        private void BlockOut()
        {
            this.Tick();
            _ = this.MemoryRead(this.HL);
            this.B = this.Decrement(this.B);
            this.WritePort(this.BC);
        }

        #region Block output single

        private void OUTI()
        {
            this.BlockOut();
            this.HL.Increment();
            this.AdjustBlockOutFlags();
            this.MEMPTR.Increment();
        }

        private void OUTD()
        {
            this.BlockOut();
            this.HL.Decrement();
            this.AdjustBlockOutFlags();
            this.MEMPTR.Decrement();
        }

        #endregion

        #region Block output repeated

        private void OTIR()
        {
            this.OUTI();
            if (this.Zero() == 0)
            {
                this.RepeatBlockInstruction();
                this.AdjustBlockRepeatFlagsIO();
                this.Tick(5);
            }
        }

        private void OTDR()
        {
            this.OUTD();
            if (this.Zero() == 0)
            {
                this.RepeatBlockInstruction();
                this.AdjustBlockRepeatFlagsIO();
                this.Tick(5);
            }
        }

        #endregion

        #endregion

        #endregion

        #endregion

        private void NEG()
        {
            this.SetBit(StatusBits.PF, this.A == (byte)Bits.Bit7);
            this.SetBit(StatusBits.CF, this.A);
            this.SetBit(StatusBits.NF);

            var original = this.A;

            this.A = (byte)(~this.A + 1);   // two's complement

            this.AdjustHalfCarrySub(0, original, this.A);
            this.AdjustOverflowSub((byte)0, original, this.A);

            this.AdjustSZXY(this.A);
        }

        private byte ReadMemoryIndirect(Register16 via)
        {
            this.MEMPTR.Assign(via);
            return this.ReadMemoryIndirect();
        }

        private byte ReadMemoryIndirect()
        {
            this.Bus.Address.Assign(this.MEMPTR);
            this.MEMPTR.Increment();
            return this.MemoryRead();
        }

        private void WriteMemoryIndirect(Register16 via, byte data)
        {
            this.MEMPTR.Assign(via);
            this.WriteMemoryIndirect(data);
        }

        private void WriteMemoryIndirect(byte data)
        {
            this.Bus.Address.Assign(this.MEMPTR);
            this.MEMPTR.Increment();
            this.MEMPTR.High = this.Bus.Data = data;
            this.MemoryWrite();
        }

        private void RRD()
        {
            var memory = ReadMemoryIndirect(this.HL);
            this.Tick(2);
            this.Bus.Data = (byte)(PromoteNibble(this.A) | HighNibble(memory));
            this.MemoryUpdate(1);
            this.Tick(4);
            this.A = (byte)(HigherNibble(this.A) | LowerNibble(memory));
            this.AdjustSZPXY(this.A);
            this.ClearBit(StatusBits.NF | StatusBits.HC);
        }

        private void RLD()
        {
            var memory = ReadMemoryIndirect(this.HL);
            this.Tick(2);
            this.Bus.Data = (byte)(PromoteNibble(memory) | LowNibble(this.A));
            this.MemoryUpdate(1);
            this.Tick(4);
            this.A = (byte)(HigherNibble(this.A) | HighNibble(memory));
            this.AdjustSZPXY(this.A);
            this.ClearBit(StatusBits.NF | StatusBits.HC);
        }

        #region Input/output port control

        private void WritePort(byte port)
        {
            this.Bus.Address.Assign(port, this.Bus.Data = this.A);
            this.WritePort();
            ++this.MEMPTR.Low;
        }

        private void WritePort(Register16 port, byte data)
        {
            this.Bus.Data = data;
            this.WritePort(port);
        }

        private void WritePort(Register16 port)
        {
            this.Bus.Address.Assign(port);
            this.WritePort();
        }

        private void WritePort()
        {
            this.MEMPTR.Assign(this.Bus.Address);
            this.Tick(2);
            {
                using var _ = new AutoIORQ(this);
                using var __ = new AutoWR(this);
                this.Ports.Write(this.Bus.Address, this.Bus.Data);
                this.Tick();
            }
            this.Tick();
        }

        private void ReadPort(byte port)
        {
            this.Bus.Address.Assign(port, this.Bus.Data = this.A);
            this.ReadPort();
            this.MEMPTR.Increment();
        }

        private void ReadPort()
        {
            this.MEMPTR.Assign(this.Bus.Address);
            this.Tick(2);
            {
                using var _ = new AutoIORQ(this);
                using var __ = new AutoRD(this);
                this.Bus.Data = this.Ports.Read(this.Bus.Address);
                this.Tick();
            }
            this.Tick();
        }

        #endregion
    }
}
