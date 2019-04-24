namespace EightBit
{
    using System;

    // Uses some information from:
    // http://www.cpu-world.com/Arch/6809.html

    //  |---------------|-----------------------------------|
    //  |   MPU State   |                                   |
    //  |_______________|   MPU State Definition            |
    //  |   BA  |   BS  |                                   |
    //  |_______|_______|___________________________________|
    //  |   0   |   0   |   Normal (running)                |
    //  |   0   |   1   |   Interrupt or RESET Acknowledge  |
    //  |   1   |   0   |   SYNC Acknowledge                |
    //  |   1   |   1   |   HALT Acknowledge                |
    //  |-------|-------|-----------------------------------|

    public sealed class MC6809 : BigEndianProcessor
    {
        private const byte RESETvector = 0xfe;      // RESET vector
        private const byte NMIvector = 0xfc;        // NMI vector
        private const byte SWIvector = 0xfa;        // SWI vector
        private const byte IRQvector = 0xf8;        // IRQ vector
        private const byte FIRQvector = 0xf6;       // FIRQ vector
        private const byte SWI2vector = 0xf4;       // SWI2 vector
        private const byte SWI3vector = 0xf2;       // SWI3 vector
        private const byte RESERVEDvector = 0xf0;   // RESERVED vector

        private byte cc = 0;
        private byte dp = 0;

        private PinLevel nmiLine = PinLevel.Low;
        private PinLevel firqLine = PinLevel.Low;
        private PinLevel haltLine = PinLevel.Low;
        private PinLevel baLine = PinLevel.Low;
        private PinLevel bsLine = PinLevel.Low;

        private bool prefix10 = false;
        private bool prefix11 = false;

        public MC6809(Bus bus)
        : base(bus)
        {
        }

        public event EventHandler<EventArgs> ExecutingInstruction;

        public event EventHandler<EventArgs> ExecutedInstruction;

        public event EventHandler<EventArgs> RaisingNMI;

        public event EventHandler<EventArgs> RaisedNMI;

        public event EventHandler<EventArgs> LoweringNMI;

        public event EventHandler<EventArgs> LoweredNMI;

        public event EventHandler<EventArgs> RaisingFIRQ;

        public event EventHandler<EventArgs> RaisedFIRQ;

        public event EventHandler<EventArgs> LoweringFIRQ;

        public event EventHandler<EventArgs> LoweredFIRQ;

        public event EventHandler<EventArgs> RaisingHALT;

        public event EventHandler<EventArgs> RaisedHALT;

        public event EventHandler<EventArgs> LoweringHALT;

        public event EventHandler<EventArgs> LoweredHALT;

        public event EventHandler<EventArgs> RaisingBA;

        public event EventHandler<EventArgs> RaisedBA;

        public event EventHandler<EventArgs> LoweringBA;

        public event EventHandler<EventArgs> LoweredBA;

        public event EventHandler<EventArgs> RaisingBS;

        public event EventHandler<EventArgs> RaisedBS;

        public event EventHandler<EventArgs> LoweringBS;

        public event EventHandler<EventArgs> LoweredBS;

        public Register16 D { get; } = new Register16();

        public ref byte A => ref this.D.High;

        public ref byte B => ref this.D.Low;

        public Register16 X { get; } = new Register16();

        public Register16 Y { get; } = new Register16();

        public Register16 U { get; } = new Register16();

        public Register16 S { get; } = new Register16();

        public ref byte DP => ref this.dp;

        public ref byte CC => ref this.cc;

        public bool Halted => this.HALT.Lowered();

        public int EntireRegisterSet => this.CC & (byte)StatusBits.EF;

        public int FastInterruptMasked => this.CC & (byte)StatusBits.FF;

        public int HalfCarry => this.CC & (byte)StatusBits.HF;

        public int InterruptMasked => this.CC & (byte)StatusBits.IF;

        public int Negative => this.CC & (byte)StatusBits.NF;

        public int Zero => this.CC & (byte)StatusBits.ZF;

        public int Overflow => this.CC & (byte)StatusBits.VF;

        public int Carry => this.CC & (byte)StatusBits.CF;

        private bool LS => (this.Carry != 0) || (this.Zero != 0);               // (C OR Z)

        private bool HI => !this.LS;                                            // !(C OR Z)

        private bool LT => ((this.Negative >> 3) ^ (this.Overflow >> 1)) != 0;  // (N XOR V)

        private bool GE => !this.LT;                                            // !(N XOR V)

        private bool LE => (this.Zero != 0) || this.LT;                         // (Z OR (N XOR V))

        private bool GT => !this.LE;                                            // !(Z OR (N XOR V))

        public ref PinLevel NMI => ref this.nmiLine;

        public ref PinLevel FIRQ => ref this.firqLine;

        public ref PinLevel HALT => ref this.haltLine;

        public ref PinLevel BA => ref this.baLine;

        public ref PinLevel BS => ref this.bsLine;

        public void Halt()
        {
            --this.PC.Word;
            this.LowerHALT();
        }

        public void Proceed()
        {
            ++this.PC.Word;
            this.RaiseHALT();
        }

        public override int Step()
        {
            this.ResetCycles();
            this.OnExecutingInstruction();
            if (this.Powered)
            {
                this.prefix10 = this.prefix11 = false;
                if (this.Halted)
                {
                    this.HandleHALT();
                }
                else if (this.RESET.Lowered())
                {
                    this.HandleRESET();
                }
                else if (this.NMI.Lowered())
                {
                    this.HandleNMI();
                }
                else if (this.FIRQ.Lowered() && (this.FastInterruptMasked == 0))
                {
                    this.HandleFIRQ();
                }
                else if (this.INT.Lowered() && (this.InterruptMasked == 0))
                {
                    this.HandleINT();
                }
                else
                {
                    this.Execute(this.FetchByte());
                }
            }

            this.OnExecutedInstruction();
            return this.Cycles;
        }

        public override int Execute()
        {
            this.LowerBA();
            this.LowerBS();
            var prefixed = this.prefix10 || this.prefix11;
            var unprefixed = !prefixed;
            if (unprefixed)
            {
                this.ExecuteUnprefixed();
            }
            else
            {
                if (this.prefix10)
                {
                    this.Execute10();
                }
                else
                {
                    this.Execute11();
                }
            }

            return this.Cycles;
        }

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            this.LowerBA();
            this.LowerBS();
        }

        public void RaiseNMI()
        {
            this.OnRaisingNMI();
            this.NMI.Raise();
            this.OnRaisedNMI();
        }

        public void LowerNMI()
        {
            this.OnLoweringNMI();
            this.NMI.Lower();
            this.OnLoweredNMI();
        }

        public void RaiseFIRQ()
        {
            this.OnRaisingFIRQ();
            this.FIRQ.Raise();
            this.OnRaisedFIRQ();
        }

        public void LowerFIRQ()
        {
            this.OnLoweringFIRQ();
            this.FIRQ.Lower();
            this.OnLoweredFIRQ();
        }

        public void RaiseHALT()
        {
            this.OnRaisingHALT();
            this.HALT.Raise();
            this.OnRaisedHALT();
        }

        public void LowerHALT()
        {
            this.OnLoweringHALT();
            this.HALT.Lower();
            this.OnLoweredHALT();
        }

        public void RaiseBA()
        {
            this.OnRaisingBA();
            this.BA.Raise();
            this.OnRaisedBA();
        }

        public void LowerBA()
        {
            this.OnLoweringBA();
            this.BA.Lower();
            this.OnLoweredBA();
        }

        public void RaiseBS()
        {
            this.OnRaisingBS();
            this.BS.Raise();
            this.OnRaisedBS();
        }

        public void LowerBS()
        {
            this.OnLoweringBS();
            this.BS.Lower();
            this.OnLoweredBS();
        }

        protected override void HandleRESET()
        {
            base.HandleRESET();
            this.RaiseNMI();
            this.LowerBA();
            this.RaiseBS();
            this.DP = 0;
            SetFlag(this.CC, StatusBits.IF);  // Disable IRQ
            SetFlag(this.CC, StatusBits.FF);  // Disable FIRQ
            this.Jump(this.GetWordPaged(0xff, RESETvector));
            this.Tick(10);
        }

        protected override void HandleINT()
        {
            base.HandleINT();
            this.LowerBA();
            this.RaiseBS();
            this.SaveEntireRegisterState();
            SetFlag(this.CC, StatusBits.IF);  // Disable IRQ
            this.Jump(this.GetWordPaged(0xff, IRQvector));
            this.Tick(12);
        }

        protected override byte Pop() => this.PopS();

        protected override void Push(byte value) => this.PushS(value);

        private void HandleHALT()
        {
            this.RaiseBA();
            this.RaiseBS();
        }

        private void HandleNMI()
        {
            this.RaiseNMI();
            this.LowerBA();
            this.RaiseBS();
            this.SaveEntireRegisterState();
            SetFlag(this.CC, StatusBits.IF);  // Disable IRQ
            SetFlag(this.CC, StatusBits.FF);  // Disable FIRQ
            this.Jump(this.GetWordPaged(0xff, NMIvector));
            this.Tick(12);
        }

        private void HandleFIRQ()
        {
            this.RaiseFIRQ();
            this.LowerBA();
            this.RaiseBS();
            this.SavePartialRegisterState();
            SetFlag(this.CC, StatusBits.IF);  // Disable IRQ
            SetFlag(this.CC, StatusBits.FF);  // Disable FIRQ
            this.Jump(this.GetWordPaged(0xff, FIRQvector));
            this.Tick(12);
        }

        private void OnRaisingNMI() => this.RaisingNMI?.Invoke(this, EventArgs.Empty);

        private void OnRaisedNMI() => this.RaisedNMI?.Invoke(this, EventArgs.Empty);

        private void OnLoweringNMI() => this.LoweringNMI?.Invoke(this, EventArgs.Empty);

        private void OnLoweredNMI() => this.LoweredNMI?.Invoke(this, EventArgs.Empty);

        private void OnRaisingFIRQ() => this.RaisingFIRQ?.Invoke(this, EventArgs.Empty);

        private void OnRaisedFIRQ() => this.RaisedFIRQ?.Invoke(this, EventArgs.Empty);

        private void OnLoweringFIRQ() => this.LoweringFIRQ?.Invoke(this, EventArgs.Empty);

        private void OnLoweredFIRQ() => this.LoweredFIRQ?.Invoke(this, EventArgs.Empty);

        private void OnRaisingHALT() => this.RaisingHALT?.Invoke(this, EventArgs.Empty);

        private void OnRaisedHALT() => this.RaisedHALT?.Invoke(this, EventArgs.Empty);

        private void OnLoweringHALT() => this.LoweringHALT?.Invoke(this, EventArgs.Empty);

        private void OnLoweredHALT() => this.LoweredHALT?.Invoke(this, EventArgs.Empty);

        private void OnRaisingBA() => this.RaisingBA?.Invoke(this, EventArgs.Empty);

        private void OnRaisedBA() => this.RaisedBA?.Invoke(this, EventArgs.Empty);

        private void OnLoweringBA() => this.LoweringBA?.Invoke(this, EventArgs.Empty);

        private void OnLoweredBA() => this.LoweredBA?.Invoke(this, EventArgs.Empty);

        private void OnRaisingBS() => this.RaisingBS?.Invoke(this, EventArgs.Empty);

        private void OnRaisedBS() => this.RaisedBS?.Invoke(this, EventArgs.Empty);

        private void OnLoweringBS() => this.LoweringBS?.Invoke(this, EventArgs.Empty);

        private void OnLoweredBS() => this.LoweredBS?.Invoke(this, EventArgs.Empty);

        private void OnExecutingInstruction() => this.ExecutingInstruction?.Invoke(this, EventArgs.Empty);

        private void OnExecutedInstruction() => this.ExecutedInstruction?.Invoke(this, EventArgs.Empty);

        private static byte SetFlag(byte f, StatusBits flag) => SetFlag(f, (byte)flag);

        private static byte SetFlag(byte f, StatusBits flag, int condition) => SetFlag(f, (byte)flag, condition);

        private static byte SetFlag(byte f, StatusBits flag, bool condition) => SetFlag(f, (byte)flag, condition);

        private static byte ClearFlag(byte f, StatusBits flag) => ClearFlag(f, (byte)flag);

        private static byte ClearFlag(byte f, StatusBits flag, int condition) => ClearFlag(f, (byte)flag, condition);

        private void Push(Register16 stack, byte value) => this.BusWrite(--stack, value);

        private void PushS(byte value) => this.Push(this.S, value);

        private void PushWord(Register16 stack, Register16 value)
        {
            this.Push(stack, value.Low);
            this.Push(stack, value.High);
        }

        private byte Pop(Register16 stack) => this.BusRead(stack++);

        private byte PopS() => this.Pop(this.S);

        private Register16 PopWord(Register16 stack)
        {
            var high = this.Pop(stack);
            var low = this.Pop(stack);
            return new Register16(low, high);
        }

        private Register16 RR(int which)
        {
            switch (which) {
                case 0b00:
                    return this.X;
                case 0b01:
                    return this.Y;
                case 0b10:
                    return this.U;
                case 0b11:
                    return this.S;
                default:
                    throw new ArgumentOutOfRangeException(nameof(which), which, "Which does not specify a valid register");
            }
        }

        private Register16 Address_relative_byte() => new Register16(this.PC.Word + (sbyte)this.FetchByte());

        private Register16 Address_relative_word() => new Register16(this.PC.Word + (short)this.FetchWord().Word);

        private Register16 Address_direct() => new Register16(this.FetchByte(), this.DP);

        private Register16 Address_extended() => this.FetchWord();

        private Register16 Address_indexed()
        {
            var type = this.FetchByte();
            var r = this.RR((type & (byte)(Bits.Bit6 | Bits.Bit5)) >> 5);

            var address = new Register16();
            if ((type & (byte)Bits.Bit7) != 0)
            {
                switch (type & (byte)Mask.Mask4)
                {
                    case 0b0000:    // ,R+
                        this.Tick(2);
                        address.Word = r.Word++;
                        break;
                    case 0b0001:    // ,R++
                        this.Tick(3);
                        address.Word = r.Word;
                        r.Word += 2;
                        break;
                    case 0b0010:    // ,-R
                        this.Tick(2);
                        address.Word = --r.Word;
                        break;
                    case 0b0011:    // ,--R
                        this.Tick(3);
                        r.Word -= 2;
                        address.Word = r.Word;
                        break;
                    case 0b0100:    // ,R
                        address.Word = r.Word;
                        break;
                    case 0b0101:    // B,R
                        this.Tick();
                        address.Word = (ushort)(r.Word + (sbyte)this.B);
                        break;
                    case 0b0110:    // A,R
                        this.Tick();
                        address.Word = (ushort)(r.Word + (sbyte)this.A);
                        break;
                    case 0b1000:    // n,R (eight-bit)
                        this.Tick();
                        address.Word = (ushort)(r.Word + (sbyte)this.FetchByte());
                        break;
                    case 0b1001:    // n,R (sixteen-bit)
                        this.Tick(4);
                        address.Word = (ushort)(r.Word + (short)this.FetchWord().Word);
                        break;
                    case 0b1011:    // D,R
                        this.Tick(4);
                        address.Word = (ushort)(r.Word + this.D.Word);
                        break;
                    case 0b1100:    // n,PCR (eight-bit)
                        this.Tick();
                        address.Word = this.Address_relative_byte().Word;
                        break;
                    case 0b1101:    // n,PCR (sixteen-bit)
                        this.Tick(2);
                        address.Word = this.Address_relative_word().Word;
                        break;
                    case 0b1111:    // [n]
                        this.Tick(2);
                        address.Word = this.Address_extended().Word;
                        break;
                    default:
                        throw new InvalidOperationException("Invalid index type");
                }

                var indirect = type & (byte)Bits.Bit4;
                if (indirect != 0)
                {
                    this.Tick(3);
                    address.Word = this.GetWord(address).Word;
                }
            }
            else
            {
                // EA = ,R + 5-bit offset
                this.Tick();
                address = new Register16(r.Word + SignExtend(5, (byte)(type & (byte)Mask.Mask5)));
            }
            return address;
        }

        private byte AM_immediate_byte() => this.FetchByte();

        private byte AM_direct_byte() => this.BusRead(this.Address_direct());

        private byte AM_indexed_byte() => this.BusRead(this.Address_indexed());

        private byte AM_extended_byte() => this.BusRead(this.Address_extended());

        private Register16 AM_immediate_word() => this.FetchWord();

        private Register16 AM_direct_word() => this.GetWord(this.Address_direct());

        private Register16 AM_indexed_word() => this.GetWord(this.Address_indexed());

        private Register16 AM_extended_word() => this.GetWord(this.Address_extended());

        private byte AdjustZero(byte datum) => ClearFlag(this.CC, StatusBits.ZF, datum);

        private byte AdjustZero(ushort datum) => ClearFlag(this.CC, StatusBits.ZF, datum);

        private byte AdjustZero(Register16 datum) => this.AdjustZero(datum.Word);

        private byte AdjustNegative(byte datum) => SetFlag(this.CC, StatusBits.NF, datum & (byte)Bits.Bit7);

        private byte AdjustNegative(ushort datum) => SetFlag(this.CC, StatusBits.NF, datum & (ushort)Bits.Bit15);

        private byte AdjustNZ(byte datum)
        {
            this.CC = this.AdjustZero(datum);
            return this.AdjustNegative(datum);
        }

        private byte AdjustNZ(ushort datum)
        {
            this.CC = this.AdjustZero(datum);
            return this.AdjustNegative(datum);
        }

        private byte AdjustNZ(Register16 datum) => this.AdjustNZ(datum.Word);

        private byte AdjustCarry(ushort datum) => SetFlag(this.CC, StatusBits.CF, datum & (ushort)Bits.Bit8);           // 8-bit addition

        private byte AdjustCarry(uint datum) => SetFlag(this.CC, StatusBits.CF, (int)(datum & (uint)Bits.Bit16));       // 16-bit addition

        private byte AdjustCarry(Register16 datum) => this.AdjustCarry(datum.Word);

        private byte AdjustOverflow(byte before, byte data, Register16 after)
        {
            var lowAfter = after.Low;
            var highAfter = after.High;
            return SetFlag(this.CC, StatusBits.VF, (before ^ data ^ lowAfter ^ (highAfter << 7)) & (int)Bits.Bit7);
        }

        private byte AdjustOverflow(ushort before, ushort data, uint after)
        {
            var lowAfter = (ushort)(after & (uint)Mask.Mask16);
            var highAfter = (ushort)(after >> 16);
            return SetFlag(this.CC, StatusBits.VF, (before ^ data ^ lowAfter ^ (highAfter << 15)) & (int)Bits.Bit15);
        }

        private byte AdjustHalfCarry(byte before, byte data, byte after) => SetFlag(this.CC, StatusBits.HF, (before ^ data ^ after) & (int)Bits.Bit4);

        private byte AdjustAddition(byte before, byte data, Register16 after)
        {
            var result = after.Low;
            this.CC = this.AdjustNZ(result);
            this.CC = this.AdjustCarry(after);
            this.CC = this.AdjustOverflow(before, data, after);
            return this.AdjustHalfCarry(before, data, result);
        }

        private byte AdjustAddition(ushort before, ushort data, uint after)
        {
            var result = new Register16(after & (uint)Mask.Mask16);
            this.CC = this.AdjustNZ(result);
            this.CC = this.AdjustCarry(after);
            return this.AdjustOverflow(before, data, after);
        }

        private byte AdjustAddition(Register16 before, Register16 data, uint after) => this.AdjustAddition(before.Word, data.Word, after);

        private byte AdjustSubtraction(byte before, byte data, Register16 after)
        {
            var result = after.Low;
            this.CC = this.AdjustNZ(result);
            this.CC = this.AdjustCarry(after);
            return this.AdjustOverflow(before, data, after);
        }

        private byte AdjustSubtraction(ushort before, ushort data, uint after)
        {
            var result = new Register16(after & (uint)Mask.Mask16);
            this.CC = this.AdjustNZ(result);
            this.CC = this.AdjustCarry(after);
            return this.AdjustOverflow(before, data, after);
        }

        private byte AdjustSubtraction(Register16 before, Register16 data, uint after) => this.AdjustSubtraction(before.Word, data.Word, after);

        private byte Through(byte data)
        {
            this.CC = ClearFlag(this.CC, StatusBits.VF);
            this.CC = this.AdjustNZ(data);
            return data;
        }

        private Register16 Through(Register16 data)
        {
            this.CC = ClearFlag(this.CC, StatusBits.VF);
            this.CC = this.AdjustNZ(data);
            return data;
        }

        private byte LD(byte data) => this.Through(data);

        private Register16 LD(Register16 data) => this.Through(data);

        private byte ST(byte data) => this.Through(data);

        private Register16 ST(Register16 data) => this.Through(data);

        private void Jump(Register16 destination) => this.Jump(destination.Word);

        private bool Branch(ushort destination, bool condition)
        {
            if (condition)
            {
                this.Jump(destination);
            }

            return condition;
        }

        private bool Branch(Register16 destination, bool condition) => this.Branch(destination.Word, condition);

        private void BranchShort(bool condition) => this.Branch(this.Address_relative_byte(), condition);

        private void BranchLong(bool condition)
        {
            if (this.Branch(this.Address_relative_word(), condition))
            {
                this.Tick();
            }
        }

        private void SaveEntireRegisterState()
        {
            this.CC = SetFlag(this.CC, StatusBits.EF);
            this.SaveRegisterState();
        }

        private void SavePartialRegisterState()
        {
            this.CC = ClearFlag(this.CC, StatusBits.EF);
            this.SaveRegisterState();
        }

        private void SaveRegisterState() => this.PSH(this.S, this.EntireRegisterSet != 0 ? (byte)Mask.Mask8 : (byte)0b10000001);

        private void RestoreRegisterState() => this.PUL(this.S, this.EntireRegisterSet != 0 ? (byte)Mask.Mask8 : (byte)0b10000001);

        private ref byte ReferenceTransfer8(int specifier)
        {
            switch (specifier)
            {
                case 0b1000:
                    return ref this.A;
                case 0b1001:
                    return ref this.B;
                case 0b1010:
                    return ref this.CC;
                case 0b1011:
                    return ref this.DP;
                default:
                    throw new ArgumentOutOfRangeException(nameof(specifier), specifier, "Invalid specifier");
            }
        }

        private Register16 ReferenceTransfer16(int specifier)
        {
            switch (specifier) {
                case 0b0000:
                    return this.D;
                case 0b0001:
                    return this.X;
                case 0b0010:
                    return this.Y;
                case 0b0011:
                    return this.U;
                case 0b0100:
                    return this.S;
                case 0b0101:
                    return this.PC;
                default:
                    throw new ArgumentOutOfRangeException(nameof(specifier), specifier, "Invalid specifier");
            }
        }

        private void ExecuteUnprefixed()
        {
            switch (this.OpCode)
            {
                case 0x10: this.prefix10 = true; this.Execute(this.FetchByte()); break;
                case 0x11: this.prefix11 = true; this.Execute(this.FetchByte()); break;

                // ABX
                case 0x3a: this.Tick(3); this.X.Word += this.B; break;                                          // ABX (inherent)

                // ADC
                case 0x89: this.Tick(2); this.A = this.ADC(this.A, this.AM_immediate_byte()); break;            // ADC (ADCA immediate)
                case 0x99: this.Tick(4); this.A = this.ADC(this.A, this.AM_direct_byte()); break;               // ADC (ADCA direct)
                case 0xa9: this.Tick(4); this.A = this.ADC(this.A, this.AM_indexed_byte()); break;              // ADC (ADCA indexed)
                case 0xb9: this.Tick(4); this.A = this.ADC(this.A, this.AM_extended_byte()); break;             // ADC (ADCA extended)

                case 0xc9: this.Tick(2); this.B = this.ADC(this.B, this.AM_immediate_byte()); break;            // ADC (ADCB immediate)
                case 0xd9: this.Tick(4); this.B = this.ADC(this.B, this.AM_direct_byte()); break;               // ADC (ADCB direct)
                case 0xe9: this.Tick(4); this.B = this.ADC(this.B, this.AM_indexed_byte()); break;              // ADC (ADCB indexed)
                case 0xf9: this.Tick(4); this.B = this.ADC(this.B, this.AM_extended_byte()); break;             // ADC (ADCB extended)

                // ADD
                case 0x8b: this.Tick(2); this.A = this.ADD(this.A, this.AM_immediate_byte()); break;            // ADD (ADDA immediate)
                case 0x9b: this.Tick(4); this.A = this.ADD(this.A, this.AM_direct_byte()); break;               // ADD (ADDA direct)
                case 0xab: this.Tick(4); this.A = this.ADD(this.A, this.AM_indexed_byte()); break;              // ADD (ADDA indexed)
                case 0xbb: this.Tick(5); this.A = this.ADD(this.A, this.AM_extended_byte()); break;             // ADD (ADDA extended)

                case 0xcb: this.Tick(2); this.B = this.ADD(this.B, this.AM_immediate_byte()); break;            // ADD (ADDB immediate)
                case 0xdb: this.Tick(4); this.B = this.ADD(this.B, this.AM_direct_byte()); break;               // ADD (ADDB direct)
                case 0xeb: this.Tick(4); this.B = this.ADD(this.B, this.AM_indexed_byte()); break;              // ADD (ADDB indexed)
                case 0xfb: this.Tick(5); this.B = this.ADD(this.B, this.AM_extended_byte()); break;             // ADD (ADDB extended)

                case 0xc3: this.Tick(4); this.D.Word = this.ADD(this.D, this.AM_immediate_word()).Word; break;  // ADD (ADDD immediate)
                case 0xd3: this.Tick(6); this.D.Word = this.ADD(this.D, this.AM_direct_word()).Word; break;     // ADD (ADDD direct)
                case 0xe3: this.Tick(6); this.D.Word = this.ADD(this.D, this.AM_indexed_word()).Word; break;    // ADD (ADDD indexed)
                case 0xf3: this.Tick(7); this.D.Word = this.ADD(this.D, this.AM_extended_word()).Word; break;   // ADD (ADDD extended)

                // AND
                case 0x84: this.Tick(2); this.A = this.AndR(this.A, this.AM_immediate_byte()); break;           // AND (ANDA immediate)
                case 0x94: this.Tick(4); this.A = this.AndR(this.A, this.AM_direct_byte()); break;              // AND (ANDA direct)
                case 0xa4: this.Tick(4); this.A = this.AndR(this.A, this.AM_indexed_byte()); break;             // AND (ANDA indexed)
                case 0xb4: this.Tick(5); this.A = this.AndR(this.A, this.AM_extended_byte()); break;            // AND (ANDA extended)

                case 0xc4: this.Tick(2); this.B = this.AndR(this.B, this.AM_immediate_byte()); break;           // AND (ANDB immediate)
                case 0xd4: this.Tick(4); this.B = this.AndR(this.B, this.AM_direct_byte()); break;              // AND (ANDB direct)
                case 0xe4: this.Tick(4); this.B = this.AndR(this.B, this.AM_indexed_byte()); break;             // AND (ANDB indexed)
                case 0xf4: this.Tick(5); this.B = this.AndR(this.B, this.AM_extended_byte()); break;            // AND (ANDB extended)

                case 0x1c: this.Tick(3); this.CC &= this.AM_immediate_byte(); break;                            // AND (ANDCC immediate)

                // ASL/LSL
                case 0x08: this.Tick(6); this.BusWrite(this.ASL(this.AM_direct_byte())); break;                 // ASL (direct)
                case 0x48: this.Tick(2); this.A = this.ASL(this.A); break;                                      // ASL (ASLA inherent)
                case 0x58: this.Tick(2); this.B = this.ASL(this.B); break;                                      // ASL (ASLB inherent)
                case 0x68: this.Tick(6); this.BusWrite(this.ASL(this.AM_indexed_byte())); break;                // ASL (indexed)
                case 0x78: this.Tick(7); this.BusWrite(this.ASL(this.AM_extended_byte())); break;               // ASL (extended)

                // ASR
                case 0x07: this.Tick(6); this.BusWrite(this.ASR(this.AM_direct_byte())); break;                 // ASR (direct)
                case 0x47: this.Tick(2); this.A = this.ASR(this.A); break;                                      // ASR (ASRA inherent)
                case 0x57: this.Tick(2); this.B = this.ASR(this.B); break;                                      // ASR (ASRB inherent)
                case 0x67: this.Tick(6); this.BusWrite(this.ASR(this.AM_indexed_byte())); break;                // ASR (indexed)
                case 0x77: this.Tick(7); this.BusWrite(this.ASR(this.AM_extended_byte())); break;               // ASR (extended)

                // BIT
                case 0x85: this.Tick(2); this.BIT(this.A, this.AM_immediate_byte()); break;                     // BIT (BITA immediate)
                case 0x95: this.Tick(4); this.BIT(this.A, this.AM_direct_byte()); break;                        // BIT (BITA direct)
                case 0xa5: this.Tick(4); this.BIT(this.A, this.AM_indexed_byte()); break;                       // BIT (BITA indexed)
                case 0xb5: this.Tick(5); this.BIT(this.A, this.AM_extended_byte()); break;                      // BIT (BITA extended)

                case 0xc5: this.Tick(2); this.BIT(this.B, this.AM_immediate_byte()); break;                     // BIT (BITB immediate)
                case 0xd5: this.Tick(4); this.BIT(this.B, this.AM_direct_byte()); break;                        // BIT (BITB direct)
                case 0xe5: this.Tick(4); this.BIT(this.B, this.AM_indexed_byte()); break;                       // BIT (BITB indexed)
                case 0xf5: this.Tick(5); this.BIT(this.B, this.AM_extended_byte()); break;                      // BIT (BITB extended)

                // CLR
                case 0x0f: this.Tick(6); this.BusWrite(this.Address_direct(), this.CLR()); break;               // CLR (direct)
                case 0x4f: this.Tick(2); this.A = this.CLR(); break;                                            // CLR (CLRA implied)
                case 0x5f: this.Tick(2); this.B = this.CLR(); break;                                            // CLR (CLRB implied)
                case 0x6f: this.Tick(6); this.BusWrite(this.Address_indexed(), this.CLR()); break;              // CLR (indexed)
                case 0x7f: this.Tick(7); this.BusWrite(this.Address_extended(), this.CLR()); break;             // CLR (extended)

                // CMP

                // CMPA
                case 0x81: this.Tick(2); this.CMP(this.A, this.AM_immediate_byte()); break;                     // CMP (CMPA, immediate)
                case 0x91: this.Tick(4); this.CMP(this.A, this.AM_direct_byte()); break;                        // CMP (CMPA, direct)
                case 0xa1: this.Tick(4); this.CMP(this.A, this.AM_indexed_byte()); break;                       // CMP (CMPA, indexed)
                case 0xb1: this.Tick(5); this.CMP(this.A, this.AM_extended_byte()); break;                      // CMP (CMPA, extended)

                // CMPB
                case 0xc1: this.Tick(2); this.CMP(this.B, this.AM_immediate_byte()); break;                     // CMP (CMPB, immediate)
                case 0xd1: this.Tick(4); this.CMP(this.B, this.AM_direct_byte()); break;                        // CMP (CMPB, direct)
                case 0xe1: this.Tick(4); this.CMP(this.B, this.AM_indexed_byte()); break;                       // CMP (CMPB, indexed)
                case 0xf1: this.Tick(5); this.CMP(this.B, this.AM_extended_byte()); break;                      // CMP (CMPB, extended)

                // CMPX
                case 0x8c: this.Tick(4); this.CMP(this.X, this.AM_immediate_word()); break;                     // CMP (CMPX, immediate)
                case 0x9c: this.Tick(6); this.CMP(this.X, this.AM_direct_word()); break;                        // CMP (CMPX, direct)
                case 0xac: this.Tick(6); this.CMP(this.X, this.AM_indexed_word()); break;                       // CMP (CMPX, indexed)
                case 0xbc: this.Tick(7); this.CMP(this.X, this.AM_extended_word()); break;                      // CMP (CMPX, extended)

                // COM
                case 0x03: this.Tick(6); this.BusWrite(this.COM(this.AM_direct_byte())); break;                 // COM (direct)
                case 0x43: this.Tick(2); this.A = this.COM(this.A); break;                                      // COM (COMA inherent)
                case 0x53: this.Tick(2); this.B = this.COM(this.B); break;                                      // COM (COMB inherent)
                case 0x63: this.Tick(6); this.BusWrite(this.COM(this.AM_indexed_byte())); break;                // COM (indexed)
                case 0x73: this.Tick(7); this.BusWrite(this.COM(this.AM_extended_byte())); break;               // COM (extended)

                // CWAI
                case 0x3c: this.Tick(11); this.CWAI(this.AM_direct_byte()); break;                              // CWAI (direct)

                // DAA
                case 0x19: this.Tick(2); this.A = this.DA(this.A); break;                                       // DAA (inherent)

                // DEC
                case 0x0a: this.Tick(6); this.BusWrite(this.DEC(this.AM_direct_byte())); break;                 // DEC (direct)
                case 0x4a: this.Tick(2); this.A = this.DEC(this.A); break;                                      // DEC (DECA inherent)
                case 0x5a: this.Tick(2); this.B = this.DEC(this.B); break;                                      // DEC (DECB inherent)
                case 0x6a: this.Tick(6); this.BusWrite(this.DEC(this.AM_indexed_byte())); break;                // DEC (indexed)
                case 0x7a: this.Tick(7); this.BusWrite(this.DEC(this.AM_extended_byte())); break;               // DEC (extended)

                // EOR

                // EORA
                case 0x88: this.Tick(2); this.A = this.EorR(this.A, this.AM_immediate_byte()); break;           // EOR (EORA immediate)
                case 0x98: this.Tick(4); this.A = this.EorR(this.A, this.AM_direct_byte()); break;              // EOR (EORA direct)
                case 0xa8: this.Tick(4); this.A = this.EorR(this.A, this.AM_indexed_byte()); break;             // EOR (EORA indexed)
                case 0xb8: this.Tick(5); this.A = this.EorR(this.A, this.AM_extended_byte()); break;            // EOR (EORA extended)

                // EORB
                case 0xc8: this.Tick(2); this.B = this.EorR(this.B, this.AM_immediate_byte()); break;           // EOR (EORB immediate)
                case 0xd8: this.Tick(4); this.B = this.EorR(this.B, this.AM_direct_byte()); break;              // EOR (EORB direct)
                case 0xe8: this.Tick(4); this.B = this.EorR(this.B, this.AM_indexed_byte()); break;             // EOR (EORB indexed)
                case 0xf8: this.Tick(5); this.B = this.EorR(this.B, this.AM_extended_byte()); break;            // EOR (EORB extended)

                // EXG
                case 0x1e: this.Tick(8); this.EXG(this.AM_immediate_byte()); break;                             // EXG (R1,R2 immediate)

                // INC
                case 0x0c: this.Tick(6); this.BusWrite(this.INC(this.AM_direct_byte())); break;                 // INC (direct)
                case 0x4c: this.Tick(2); this.A = this.INC(this.A); break;                                      // INC (INCA inherent)
                case 0x5c: this.Tick(2); this.B = this.INC(this.B); break;                                      // INC (INCB inherent)
                case 0x6c: this.Tick(6); this.BusWrite(this.INC(this.AM_indexed_byte())); break;                // INC (indexed)
                case 0x7c: this.Tick(7); this.BusWrite(this.INC(this.AM_extended_byte())); break;               // INC (extended)

                // JMP
                case 0x0e: this.Tick(6); this.Jump(this.Address_direct()); break;                               // JMP (direct)
                case 0x6e: this.Tick(6); this.Jump(this.Address_indexed()); break;                              // JMP (indexed)
                case 0x7e: this.Tick(7); this.Jump(this.Address_extended()); break;                             // JMP (extended)

                // JSR
                case 0x9d: this.Tick(6); this.JSR(this.Address_direct()); break;                                // JSR (direct)
                case 0xad: this.Tick(6); this.JSR(this.Address_indexed()); break;                               // JSR (indexed)
                case 0xbd: this.Tick(7); this.JSR(this.Address_extended()); break;                              // JSR (extended)

                // LD

                // LDA
                case 0x86: this.Tick(2); this.A = this.LD(this.AM_immediate_byte()); break;                     // LD (LDA immediate)
                case 0x96: this.Tick(4); this.A = this.LD(this.AM_direct_byte()); break;                        // LD (LDA direct)
                case 0xa6: this.Tick(4); this.A = this.LD(this.AM_indexed_byte()); break;                       // LD (LDA indexed)
                case 0xb6: this.Tick(5); this.A = this.LD(this.AM_extended_byte()); break;                      // LD (LDA extended)

                // LDB
                case 0xc6: this.Tick(2); this.B = this.LD(this.AM_immediate_byte()); break;                     // LD (LDB immediate)
                case 0xd6: this.Tick(4); this.B = this.LD(this.AM_direct_byte()); break;                        // LD (LDB direct)
                case 0xe6: this.Tick(4); this.B = this.LD(this.AM_indexed_byte()); break;                       // LD (LDB indexed)
                case 0xf6: this.Tick(5); this.B = this.LD(this.AM_extended_byte()); break;                      // LD (LDB extended)

                // LDD
                case 0xcc: this.Tick(3); this.D.Word = this.LD(this.AM_immediate_word()).Word; break;           // LD (LDD immediate)
                case 0xdc: this.Tick(5); this.D.Word = this.LD(this.AM_direct_word()).Word; break;              // LD (LDD direct)
                case 0xec: this.Tick(5); this.D.Word = this.LD(this.AM_indexed_word()).Word; break;             // LD (LDD indexed)
                case 0xfc: this.Tick(6); this.D.Word = this.LD(this.AM_extended_word()).Word; break;            // LD (LDD extended)

                // LDU
                case 0xce: this.Tick(3); this.U.Word = this.LD(this.AM_immediate_word()).Word; break;           // LD (LDU immediate)
                case 0xde: this.Tick(5); this.U.Word = this.LD(this.AM_direct_word()).Word; break;              // LD (LDU direct)
                case 0xee: this.Tick(5); this.U.Word = this.LD(this.AM_indexed_word()).Word; break;             // LD (LDU indexed)
                case 0xfe: this.Tick(6); this.U.Word = this.LD(this.AM_extended_word()).Word; break;            // LD (LDU extended)

                // LDX
                case 0x8e: this.Tick(3); this.X.Word = this.LD(this.AM_immediate_word()).Word; break;           // LD (LDX immediate)
                case 0x9e: this.Tick(5); this.X.Word = this.LD(this.AM_direct_word()).Word; break;              // LD (LDX direct)
                case 0xae: this.Tick(5); this.X.Word = this.LD(this.AM_indexed_word()).Word; break;             // LD (LDX indexed)
                case 0xbe: this.Tick(6); this.X.Word = this.LD(this.AM_extended_word()).Word; break;            // LD (LDX extended)

                // LEA
                case 0x30: this.Tick(4); this.CC = this.AdjustZero(this.X.Word = this.Address_indexed().Word); break;     // LEA (LEAX indexed)
                case 0x31: this.Tick(4); this.CC = this.AdjustZero(this.Y.Word = this.Address_indexed().Word); break;     // LEA (LEAY indexed)
                case 0x32: this.Tick(4); this.S.Word = this.Address_indexed().Word; break;                      // LEA (LEAS indexed)
                case 0x33: this.Tick(4); this.U.Word = this.Address_indexed().Word; break;                      // LEA (LEAU indexed)

                // LSR
                case 0x04: this.Tick(6); this.BusWrite(this.LSR(this.AM_direct_byte())); break;                 // LSR (direct)
                case 0x44: this.Tick(2); this.A = this.LSR(this.A); break;                                      // LSR (LSRA inherent)
                case 0x54: this.Tick(2); this.B = this.LSR(this.B); break;                                      // LSR (LSRB inherent)
                case 0x64: this.Tick(6); this.BusWrite(this.LSR(this.AM_indexed_byte())); break;                // LSR (indexed)
                case 0x74: this.Tick(7); this.BusWrite(this.LSR(this.AM_extended_byte())); break;               // LSR (extended)

                // MUL
                case 0x3d: this.Tick(11); this.D.Word = this.MUL(this.A, this.B).Word; break;                   // MUL (inherent)

                // NEG
                case 0x00: this.Tick(6); this.BusWrite(this.NEG(this.AM_direct_byte())); break;                 // NEG (direct)
                case 0x40: this.Tick(2); this.A = this.NEG(this.A); break;                                      // NEG (NEGA, inherent)
                case 0x50: this.Tick(2); this.B = this.NEG(this.B); break;                                      // NEG (NEGB, inherent)
                case 0x60: this.Tick(6); this.BusWrite(this.NEG(this.AM_indexed_byte())); break;                // NEG (indexed)
                case 0x70: this.Tick(7); this.BusWrite(this.NEG(this.AM_extended_byte())); break;               // NEG (extended)

                // NOP
                case 0x12: this.Tick(2); break;                                                                 // NOP (inherent)

                // OR

                // ORA
                case 0x8a: this.Tick(2); this.A = this.OrR(this.A, this.AM_immediate_byte()); break;            // OR (ORA immediate)
                case 0x9a: this.Tick(4); this.A = this.OrR(this.A, this.AM_direct_byte()); break;               // OR (ORA direct)
                case 0xaa: this.Tick(4); this.A = this.OrR(this.A, this.AM_indexed_byte()); break;              // OR (ORA indexed)
                case 0xba: this.Tick(5); this.A = this.OrR(this.A, this.AM_extended_byte()); break;             // OR (ORA extended)

                // ORB
                case 0xca: this.Tick(2); this.B = this.OrR(this.B, this.AM_immediate_byte()); break;            // OR (ORB immediate)
                case 0xda: this.Tick(4); this.B = this.OrR(this.B, this.AM_direct_byte()); break;               // OR (ORB direct)
                case 0xea: this.Tick(4); this.B = this.OrR(this.B, this.AM_indexed_byte()); break;              // OR (ORB indexed)
                case 0xfa: this.Tick(5); this.B = this.OrR(this.B, this.AM_extended_byte()); break;             // OR (ORB extended)

                // ORCC
                case 0x1a: this.Tick(3); this.CC |= this.AM_immediate_byte(); break;                            // OR (ORCC immediate)

                // PSH
                case 0x34: this.Tick(5); this.PSH(this.S, this.AM_immediate_byte()); break;                     // PSH (PSHS immediate)
                case 0x36: this.Tick(5); this.PSH(this.U, this.AM_immediate_byte()); break;                     // PSH (PSHU immediate)

                // PUL
                case 0x35: this.Tick(5); this.PUL(this.S, this.AM_immediate_byte()); break;                     // PUL (PULS immediate)
                case 0x37: this.Tick(5); this.PUL(this.U, this.AM_immediate_byte()); break;                     // PUL (PULU immediate)

                // ROL
                case 0x09: this.Tick(6); this.BusWrite(this.ROL(this.AM_direct_byte())); break;                 // ROL (direct)
                case 0x49: this.Tick(2); this.A = this.ROL(this.A); break;                                      // ROL (ROLA inherent)
                case 0x59: this.Tick(2); this.B = this.ROL(this.B); break;                                      // ROL (ROLB inherent)
                case 0x69: this.Tick(6); this.BusWrite(this.ROL(this.AM_indexed_byte())); break;                // ROL (indexed)
                case 0x79: this.Tick(7); this.BusWrite(this.ROL(this.AM_extended_byte())); break;               // ROL (extended)

                // ROR
                case 0x06: this.Tick(6); this.BusWrite(this.ROR(this.AM_direct_byte())); break;                 // ROR (direct)
                case 0x46: this.Tick(2); this.A = this.ROR(this.A); break;                                      // ROR (RORA inherent)
                case 0x56: this.Tick(2); this.B = this.ROR(this.B); break;                                      // ROR (RORB inherent)
                case 0x66: this.Tick(6); this.BusWrite(this.ROR(this.AM_indexed_byte())); break;                // ROR (indexed)
                case 0x76: this.Tick(7); this.BusWrite(this.ROR(this.AM_extended_byte())); break;               // ROR (extended)

                // RTI
                case 0x3B: this.Tick(6); this.RTI(); break;                                                     // RTI (inherent)

                // RTS
                case 0x39: this.Tick(5); this.RTS(); break;                                                     // RTS (inherent)

                // SBC

                // SBCA
                case 0x82: this.Tick(4); this.A = this.SBC(this.A, this.AM_immediate_byte()); break;            // SBC (SBCA immediate)
                case 0x92: this.Tick(4); this.A = this.SBC(this.A, this.AM_direct_byte()); break;               // SBC (SBCA direct)
                case 0xa2: this.Tick(4); this.A = this.SBC(this.A, this.AM_indexed_byte()); break;              // SBC (SBCA indexed)
                case 0xb2: this.Tick(5); this.A = this.SBC(this.A, this.AM_extended_byte()); break;             // SBC (SBCB extended)

                // SBCB
                case 0xc2: this.Tick(4); this.B = this.SBC(this.B, this.AM_immediate_byte()); break;            // SBC (SBCB immediate)
                case 0xd2: this.Tick(4); this.B = this.SBC(this.B, this.AM_direct_byte()); break;               // SBC (SBCB direct)
                case 0xe2: this.Tick(4); this.B = this.SBC(this.B, this.AM_indexed_byte()); break;              // SBC (SBCB indexed)
                case 0xf2: this.Tick(5); this.B = this.SBC(this.B, this.AM_extended_byte()); break;             // SBC (SBCB extended)

                // SEX
                case 0x1d: this.Tick(2); this.A = this.SEX(this.B); break;                                      // SEX (inherent)

                // ST

                // STA
                case 0x97: this.Tick(4); this.BusWrite(this.Address_direct(), this.ST(this.A)); break;          // ST (STA direct)
                case 0xa7: this.Tick(4); this.BusWrite(this.Address_indexed(), this.ST(this.A)); break;         // ST (STA indexed)
                case 0xb7: this.Tick(5); this.BusWrite(this.Address_extended(), this.ST(this.A)); break;        // ST (STA extended)

                // STB
                case 0xd7: this.Tick(4); this.BusWrite(this.Address_direct(), this.ST(this.B)); break;          // ST (STB direct)
                case 0xe7: this.Tick(4); this.BusWrite(this.Address_indexed(), this.ST(this.B)); break;         // ST (STB indexed)
                case 0xf7: this.Tick(5); this.BusWrite(this.Address_extended(), this.ST(this.B)); break;        // ST (STB extended)

                // STD
                case 0xdd: this.Tick(5); this.SetWord(this.Address_direct(), this.ST(this.D)); break;           // ST (STD direct)
                case 0xed: this.Tick(5); this.SetWord(this.Address_indexed(), this.ST(this.D)); break;          // ST (STD indexed)
                case 0xfd: this.Tick(6); this.SetWord(this.Address_extended(), this.ST(this.D)); break;         // ST (STD extended)

                // STU
                case 0xdf: this.Tick(5); this.SetWord(this.Address_direct(), this.ST(this.U)); break;           // ST (STU direct)
                case 0xef: this.Tick(5); this.SetWord(this.Address_indexed(), this.ST(this.U)); break;          // ST (STU indexed)
                case 0xff: this.Tick(6); this.SetWord(this.Address_extended(), this.ST(this.U)); break;         // ST (STU extended)

                // STX
                case 0x9f: this.Tick(5); this.SetWord(this.Address_direct(), this.ST(this.X)); break;           // ST (STX direct)
                case 0xaf: this.Tick(5); this.SetWord(this.Address_indexed(), this.ST(this.X)); break;          // ST (STX indexed)
                case 0xbf: this.Tick(6); this.SetWord(this.Address_extended(), this.ST(this.X)); break;         // ST (STX extended)

                // SUB

                // SUBA
                case 0x80: this.Tick(2); this.A = this.SUB(this.A, this.AM_immediate_byte()); break;            // SUB (SUBA immediate)
                case 0x90: this.Tick(4); this.A = this.SUB(this.A, this.AM_direct_byte()); break;               // SUB (SUBA direct)
                case 0xa0: this.Tick(4); this.A = this.SUB(this.A, this.AM_indexed_byte()); break;              // SUB (SUBA indexed)
                case 0xb0: this.Tick(5); this.A = this.SUB(this.A, this.AM_extended_byte()); break;             // SUB (SUBA extended)

                // SUBB
                case 0xc0: this.Tick(2); this.B = this.SUB(this.B, this.AM_immediate_byte()); break;            // SUB (SUBB immediate)
                case 0xd0: this.Tick(4); this.B = this.SUB(this.B, this.AM_direct_byte()); break;               // SUB (SUBB direct)
                case 0xe0: this.Tick(4); this.B = this.SUB(this.B, this.AM_indexed_byte()); break;              // SUB (SUBB indexed)
                case 0xf0: this.Tick(5); this.B = this.SUB(this.B, this.AM_extended_byte()); break;             // SUB (SUBB extended)

                // SUBD
                case 0x83: this.Tick(4); this.D.Word = this.SUB(this.D, this.AM_immediate_word()).Word; break;  // SUB (SUBD immediate)
                case 0x93: this.Tick(6); this.D.Word = this.SUB(this.D, this.AM_direct_word()).Word; break;     // SUB (SUBD direct)
                case 0xa3: this.Tick(6); this.D.Word = this.SUB(this.D, this.AM_indexed_word()).Word; break;    // SUB (SUBD indexed)
                case 0xb3: this.Tick(7); this.D.Word = this.SUB(this.D, this.AM_extended_word()).Word; break;   // SUB (SUBD extended)

                // SWI
                case 0x3f: this.Tick(10); this.SWI(); break;                                                    // SWI (inherent)

                // SYNC
                case 0x13: this.Tick(4); this.Halt(); break;                                                    // SYNC (inherent)

                // TFR
                case 0x1f: this.Tick(6); this.TFR(this.AM_immediate_byte()); break;                             // TFR (immediate)

                // TST
                case 0x0d: this.Tick(6); this.TST(this.AM_direct_byte()); break;                                // TST (direct)
                case 0x4d: this.Tick(2); this.TST(this.A); break;                                               // TST (TSTA inherent)
                case 0x5d: this.Tick(2); this.TST(this.B); break;                                               // TST (TSTB inherent)
                case 0x6d: this.Tick(6); this.TST(this.AM_indexed_byte()); break;                               // TST (indexed)
                case 0x7d: this.Tick(7); this.TST(this.AM_extended_byte()); break;                              // TST (extended)

                // Branching

                case 0x16: this.Tick(5); this.Jump(this.Address_relative_word()); break;                        // BRA (LBRA relative)
                case 0x17: this.Tick(9); this.JSR(this.Address_relative_word()); break;                         // BSR (LBSR relative)
                case 0x20: this.Tick(3); this.Jump(this.Address_relative_byte()); break;                        // BRA (relative)
                case 0x21: this.Tick(3); this.Address_relative_byte(); break;                                   // BRN (relative)
                case 0x22: this.Tick(3); this.BranchShort(this.HI); break;                                      // BHI (relative)
                case 0x23: this.Tick(3); this.BranchShort(this.LS); break;                                      // BLS (relative)
                case 0x24: this.Tick(3); this.BranchShort(this.Carry == 0); break;                              // BCC (relative)
                case 0x25: this.Tick(3); this.BranchShort(this.Carry != 0); break;                              // BCS (relative)
                case 0x26: this.Tick(3); this.BranchShort(this.Zero == 0); break;                               // BNE (relative)
                case 0x27: this.Tick(3); this.BranchShort(this.Zero != 0); break;                               // BEQ (relative)
                case 0x28: this.Tick(3); this.BranchShort(this.Overflow == 0); break;                           // BVC (relative)
                case 0x29: this.Tick(3); this.BranchShort(this.Overflow != 0); break;                           // BVS (relative)
                case 0x2a: this.Tick(3); this.BranchShort(this.Negative == 0); break;                           // BPL (relative)
                case 0x2b: this.Tick(3); this.BranchShort(this.Negative != 0); break;                           // BMI (relative)
                case 0x2c: this.Tick(3); this.BranchShort(this.GE); break;                                      // BGE (relative)
                case 0x2d: this.Tick(3); this.BranchShort(this.LT); break;                                      // BLT (relative)
                case 0x2e: this.Tick(3); this.BranchShort(this.GT); break;                                      // BGT (relative)
                case 0x2f: this.Tick(3); this.BranchShort(this.LE); break;                                      // BLE (relative)

                case 0x8d: this.Tick(7); this.JSR(this.Address_relative_byte()); break;                         // BSR (relative)

                default:
                    throw new InvalidOperationException("Unknown op-code");
            }
        }

        private void Execute10()
        {
            switch (this.OpCode)
            {
                // CMP

                // CMPD
                case 0x83: this.Tick(5); this.CMP(this.D, this.AM_immediate_word()); break;                     // CMP (CMPD, immediate)
                case 0x93: this.Tick(7); this.CMP(this.D, this.AM_direct_word()); break;                        // CMP (CMPD, direct)
                case 0xa3: this.Tick(7); this.CMP(this.D, this.AM_indexed_word()); break;                       // CMP (CMPD, indexed)
                case 0xb3: this.Tick(8); this.CMP(this.D, this.AM_extended_word()); break;                      // CMP (CMPD, extended)

                // CMPY
                case 0x8c: this.Tick(5); this.CMP(this.Y, this.AM_immediate_word()); break;                     // CMP (CMPY, immediate)
                case 0x9c: this.Tick(7); this.CMP(this.Y, this.AM_direct_word()); break;                        // CMP (CMPY, direct)
                case 0xac: this.Tick(7); this.CMP(this.Y, this.AM_indexed_word()); break;                       // CMP (CMPY, indexed)
                case 0xbc: this.Tick(8); this.CMP(this.Y, this.AM_extended_word()); break;                      // CMP (CMPY, extended)

                // LD

                // LDS
                case 0xce: this.Tick(4); this.S.Word = this.LD(this.AM_immediate_word()).Word; break;           // LD (LDS immediate)
                case 0xde: this.Tick(6); this.S.Word = this.LD(this.AM_direct_word()).Word; break;              // LD (LDS direct)
                case 0xee: this.Tick(6); this.S.Word = this.LD(this.AM_indexed_word()).Word; break;             // LD (LDS indexed)
                case 0xfe: this.Tick(7); this.S.Word = this.LD(this.AM_extended_word()).Word; break;            // LD (LDS extended)

                // LDY
                case 0x8e: this.Tick(4); this.Y.Word = this.LD(this.AM_immediate_word()).Word; break;           // LD (LDY immediate)
                case 0x9e: this.Tick(6); this.Y.Word = this.LD(this.AM_direct_word()).Word; break;              // LD (LDY direct)
                case 0xae: this.Tick(6); this.Y.Word = this.LD(this.AM_indexed_word()).Word; break;             // LD (LDY indexed)
                case 0xbe: this.Tick(7); this.Y.Word = this.LD(this.AM_extended_word()).Word; break;            // LD (LDY extended)

                // Branching

                case 0x21: this.Tick(5); this.Address_relative_word(); break;                                   // BRN (LBRN relative)
                case 0x22: this.Tick(5); this.BranchLong(this.HI); break;                                       // BHI (LBHI relative)
                case 0x23: this.Tick(5); this.BranchLong(this.LS); break;                                       // BLS (LBLS relative)
                case 0x24: this.Tick(5); this.BranchLong(this.Carry == 0); break;                               // BCC (LBCC relative)
                case 0x25: this.Tick(5); this.BranchLong(this.Carry != 0); break;                               // BCS (LBCS relative)
                case 0x26: this.Tick(5); this.BranchLong(this.Zero == 0); break;                                // BNE (LBNE relative)
                case 0x27: this.Tick(5); this.BranchLong(this.Zero != 0); break;                                // BEQ (LBEQ relative)
                case 0x28: this.Tick(5); this.BranchLong(this.Overflow == 0); break;                            // BVC (LBVC relative)
                case 0x29: this.Tick(5); this.BranchLong(this.Overflow != 0); break;                            // BVS (LBVS relative)
                case 0x2a: this.Tick(5); this.BranchLong(this.Negative == 0); break;                            // BPL (LBPL relative)
                case 0x2b: this.Tick(5); this.BranchLong(this.Negative != 0); break;                            // BMI (LBMI relative)
                case 0x2c: this.Tick(5); this.BranchLong(this.GE); break;                                       // BGE (LBGE relative)
                case 0x2d: this.Tick(5); this.BranchLong(this.LT); break;                                       // BLT (LBLT relative)
                case 0x2e: this.Tick(5); this.BranchLong(this.GT); break;                                       // BGT (LBGT relative)
                case 0x2f: this.Tick(5); this.BranchLong(this.LE); break;                                       // BLE (LBLE relative)

                // STS
                case 0xdf: this.Tick(6); this.SetWord(this.Address_direct(), this.ST(this.S)); break;           // ST (STS direct)
                case 0xef: this.Tick(6); this.SetWord(this.Address_indexed(), this.ST(this.S)); break;          // ST (STS indexed)
                case 0xff: this.Tick(7); this.SetWord(this.Address_extended(), this.ST(this.S)); break;         // ST (STS extended)

                // STY
                case 0x9f: this.Tick(6); this.SetWord(this.Address_direct(), this.ST(this.Y)); break;           // ST (STY direct)
                case 0xaf: this.Tick(6); this.SetWord(this.Address_indexed(), this.ST(this.Y)); break;          // ST (STY indexed)
                case 0xbf: this.Tick(7); this.SetWord(this.Address_extended(), this.ST(this.Y)); break;         // ST (STY extended)

                // SWI
                case 0x3f: this.Tick(11); this.SWI2(); break;                                                   // SWI (SWI2 inherent)

                default:
                    throw new InvalidOperationException("Unknown 10 prefixed op-code");
            }
        }

        private void Execute11()
        {
            switch (this.OpCode)
            {
                // CMP

                // CMPU
                case 0x83: this.Tick(5); this.CMP(this.U, this.AM_immediate_word()); break;                     // CMP (CMPU, immediate)
                case 0x93: this.Tick(7); this.CMP(this.U, this.AM_direct_word()); break;                        // CMP (CMPU, direct)
                case 0xa3: this.Tick(7); this.CMP(this.U, this.AM_indexed_word()); break;                       // CMP (CMPU, indexed)
                case 0xb3: this.Tick(8); this.CMP(this.U, this.AM_extended_word()); break;                      // CMP (CMPU, extended)

                // CMPS
                case 0x8c: this.Tick(5); this.CMP(this.S, this.AM_immediate_word()); break;                     // CMP (CMPS, immediate)
                case 0x9c: this.Tick(7); this.CMP(this.S, this.AM_direct_word()); break;                        // CMP (CMPS, direct)
                case 0xac: this.Tick(7); this.CMP(this.S, this.AM_indexed_word()); break;                       // CMP (CMPS, indexed)
                case 0xbc: this.Tick(8); this.CMP(this.S, this.AM_extended_word()); break;                      // CMP (CMPS, extended)

                // SWI
                case 0x3f: this.Tick(11); this.SWI3(); break;                                                   // SWI (SWI3 inherent)

                default:
                    throw new InvalidOperationException("Unknown 11 prefixed op-code");
            }
        }

        private byte ADC(byte operand, byte data) => this.ADD(operand, data, (byte)this.Carry);

        private byte ADD(byte operand, byte data, byte carry = 0)
        {
            var addition = new Register16(operand + data + carry);
            this.CC = this.AdjustAddition(operand, data, addition);
            return addition.Low;
        }

        private Register16 ADD(Register16 operand, Register16 data)
        {
            var addition = (uint)(operand.Word + data.Word);
            this.CC = this.AdjustAddition(operand, data, addition);
            return new Register16(addition & (uint)Mask.Mask16);
        }

        private byte AndR(byte operand, byte data) => this.Through((byte)(operand & data));

        private byte ASL(byte operand)
        {
            this.CC = SetFlag(this.CC, StatusBits.CF, operand & (byte)Bits.Bit7);
            this.CC = this.AdjustNZ(operand <<= 1);
            var overflow = this.Carry ^ (this.Negative >> 3);
            this.CC = SetFlag(this.CC, StatusBits.VF, overflow);
            return operand;
        }

        private byte ASR(byte operand)
        {
            this.CC = SetFlag(this.CC, StatusBits.CF, operand & (byte)Bits.Bit0);
            var result = (byte)((operand >> 1) | (int)Bits.Bit7);
            this.CC = this.AdjustNZ(result);
            return result;
        }

        private void BIT(byte operand, byte data) => this.AndR(operand, data);

        private byte CLR()
        {
            this.CC = ClearFlag(this.CC, StatusBits.CF);
            return this.Through((byte)0U);
        }

        private void CMP(byte operand, byte data) => this.SUB(operand, data);

        private void CMP(Register16 operand, Register16 data) => this.SUB(operand, data);

        private byte COM(byte operand)
        {
            this.CC = SetFlag(this.CC, StatusBits.CF);
            return this.Through((byte)~operand);
        }

        private void CWAI(byte data)
        {
            this.CC &= data;
            this.SaveEntireRegisterState();
            this.Halt();
        }

        private byte DA(byte operand)
        {
            this.CC = SetFlag(this.CC, StatusBits.CF, operand > 0x99);

            var lowAdjust = (this.HalfCarry != 0) || (Chip.LowNibble(operand) > 9);
            var highAdjust = (this.Carry != 0) || (operand > 0x99);

            if (lowAdjust)
            {
                operand += 6;
            }

            if (highAdjust)
            {
                operand += 0x60;
            }

            return this.Through(operand);
        }

        private byte DEC(byte operand)
        {
            var subtraction = new Register16(operand - 1);
            var result = subtraction.Low;
            this.CC = this.AdjustNZ(result);
            this.CC = this.AdjustOverflow(operand, 1, subtraction);
            return result;
        }

        private byte EorR(byte operand, byte data) => this.Through((byte)(operand ^ data));

        private void EXG(byte data)
        {
            var specifier1 = Chip.HighNibble(data);
            var type1 = specifier1 & (int)Bits.Bit3;  // transfer type, part 1

            var specifier2 = Chip.LowNibble(data);
            var type2 = specifier2 & (int)Bits.Bit3;  // transfer type, part 2

            if (type1 != type2)
            {
                throw new ArgumentOutOfRangeException(nameof(data), data, "Type specifications do not match");
            }

            if (type1 == 0)
            {
                ref var register1 = ref this.ReferenceTransfer8(specifier1);
                ref var register2 = ref this.ReferenceTransfer8(specifier2);
                (register1, register2) = (register2, register1);
            }
            else
            {
                var register1 = this.ReferenceTransfer16(specifier1);
                var register2 = this.ReferenceTransfer16(specifier2);
                (register1.Word, register2.Word) = (register2.Word, register1.Word);
            }
        }

        private byte INC(byte operand)
        {
            var addition = new Register16(operand + 1);
            var result = addition.Low;
            this.CC = this.AdjustNZ(result);
            this.CC = this.AdjustOverflow(operand, 1, addition);
            this.CC = this.AdjustHalfCarry(operand, 1, result);
            return result;
        }

        private void JSR(ushort address) => this.Call(address);

        private void JSR(Register16 address) => this.JSR(address.Word);

        private byte LSR(byte operand)
        {
            this.CC = SetFlag(this.CC, StatusBits.CF, operand & (byte)Bits.Bit0);
            this.CC = this.AdjustNZ(operand >>= 1);
            return operand;
        }

        private Register16 MUL(byte first, byte second)
        {
            var result = new Register16(first * second);
            this.CC = this.AdjustZero(result);
            this.CC = SetFlag(this.CC, StatusBits.CF, result.Low & (byte)Bits.Bit7);
            return result;
        }

        private byte NEG(byte operand)
        {
            this.CC = SetFlag(this.CC, StatusBits.VF, operand == (byte)Bits.Bit7);
            var result = new Register16(0 - operand);
            operand = result.Low;
            this.CC = this.AdjustNZ(operand);
            this.CC = this.AdjustCarry(result);
            return operand;
        }

        private byte OrR(byte operand, byte data) => this.Through((byte)(operand | data));

        private void PSH(Register16 stack, byte data)
        {
            if ((data & (byte)Bits.Bit7) != 0)
            {
                this.Tick(2);
                this.PushWord(stack, this.PC);
            }

            if ((data & (byte)Bits.Bit6) != 0)
            {
                this.Tick(2);
                // Pushing to the S stack means we must be pushing U
                this.PushWord(stack, object.ReferenceEquals(stack, this.S) ? this.U : this.S);
            }

            if ((data & (byte)Bits.Bit5) != 0)
            {
                this.Tick(2);
                this.PushWord(stack, this.Y);
            }

            if ((data & (byte)Bits.Bit4) != 0)
            {
                this.Tick(2);
                this.PushWord(stack, this.X);
            }

            if ((data & (byte)Bits.Bit3) != 0)
            {
                this.Tick();
                this.Push(stack, this.DP);
            }

            if ((data & (byte)Bits.Bit2) != 0)
            {
                this.Tick();
                this.Push(stack, this.B);
            }

            if ((data & (byte)Bits.Bit1) != 0)
            {
                this.Tick();
                this.Push(stack, this.A);
            }

            if ((data & (byte)Bits.Bit0) != 0)
            {
                this.Tick();
                this.Push(stack, this.CC);
            }
        }

        private void PUL(Register16 stack, byte data)
        {
            if ((data & (byte)Bits.Bit0) != 0)
            {
                this.Tick();
                this.CC = this.Pop(stack);
            }

            if ((data & (byte)Bits.Bit1) != 0)
            {
                this.Tick();
                this.A = this.Pop(stack);
            }

            if ((data & (byte)Bits.Bit2) != 0)
            {
                this.Tick();
                this.B = this.Pop(stack);
            }

            if ((data & (byte)Bits.Bit3) != 0)
            {
                this.Tick();
                this.DP = this.Pop(stack);
            }

            if ((data & (byte)Bits.Bit4) != 0)
            {
                this.Tick(2);
                this.X.Word = this.PopWord(stack).Word;
            }

            if ((data & (byte)Bits.Bit5) != 0)
            {
                this.Tick(2);
                this.Y.Word = this.PopWord(stack).Word;
            }

            if ((data & (byte)Bits.Bit6) != 0)
            {
                this.Tick(2);
                // Pulling from the S stack means we must be pulling U
                (object.ReferenceEquals(stack, this.S) ? this.U : this.S).Word = this.PopWord(stack).Word;
            }

            if ((data & (byte)Bits.Bit7) != 0)
            {
                this.Tick(2);
                this.PC.Word = this.PopWord(stack).Word;
            }
        }

        private byte ROL(byte operand)
        {
            var carryIn = this.Carry;
            this.CC = SetFlag(this.CC, StatusBits.CF, operand & (byte)Bits.Bit7);
            this.CC = SetFlag(this.CC, StatusBits.VF, ((operand & (byte)Bits.Bit7) >> 7) ^ ((operand & (byte)Bits.Bit6) >> 6));
            var result = (byte)((operand << 1) | carryIn);
            this.CC = this.AdjustNZ(result);
            return result;
        }

        private byte ROR(byte operand)
        {
            var carryIn = this.Carry;
            this.CC = SetFlag(this.CC, StatusBits.CF, operand & (byte)Bits.Bit0);
            var result = (byte)((operand >> 1) | (carryIn << 7));
            this.CC = this.AdjustNZ(result);
            return result;
        }

        private void RTI() => this.RestoreRegisterState();

        private void RTS() => this.Return();

        private byte SBC(byte operand, byte data) => this.SUB(operand, data, (byte)this.Carry);

        private byte SUB(byte operand, byte data, byte carry = 0)
        {
            var subtraction = new Register16(operand - data - carry);
            this.CC = this.AdjustSubtraction(operand, data, subtraction);
            return subtraction.Low;
        }

        private Register16 SUB(Register16 operand, Register16 data)
        {
            var subtraction = (uint)(operand.Word - data.Word);
            this.CC = this.AdjustSubtraction(operand, data, subtraction);
            return new Register16(subtraction & (uint)Mask.Mask16);
        }

        private byte SEX(byte from)
        {
            this.CC = this.AdjustNZ(from);
            return (from & (byte)Bits.Bit7) != 0 ? (byte)Mask.Mask8 : (byte)0;
        }

        private void SWI()
        {
            this.SaveEntireRegisterState();
            this.CC = SetFlag(this.CC, StatusBits.IF);  // Disable IRQ
            this.CC = SetFlag(this.CC, StatusBits.FF);  // Disable FIRQ
            this.Jump(this.GetWordPaged(0xff, SWIvector));
        }

        private void SWI2()
        {
            this.SaveEntireRegisterState();
            this.Jump(this.GetWordPaged(0xff, SWI2vector));
        }

        private void SWI3()
        {
            this.SaveEntireRegisterState();
            this.Jump(this.GetWordPaged(0xff, SWI3vector));
        }

        private void TFR(byte data)
        {
            var specifier1 = Chip.HighNibble(data);
            var type1 = specifier1 & (int)Bits.Bit3;  // transfer type, part 1

            var specifier2 = Chip.LowNibble(data);
            var type2 = specifier2 & (int)Bits.Bit3;  // transfer type, part 2

            if (type1 != type2)
            {
                throw new ArgumentOutOfRangeException(nameof(data), data, "Type specifications do not match");
            }

            if (type1 == 0)
            {
                ref var register1 = ref this.ReferenceTransfer8(specifier1);
                ref var register2 = ref this.ReferenceTransfer8(specifier2);
                register2 = register1;
            }
            else
            {
                var register1 = this.ReferenceTransfer16(specifier1);
                var register2 = this.ReferenceTransfer16(specifier2);
                register2.Word = register1.Word;
            }
        }

        private void TST(byte data) => this.CMP(data, 0);
    }
}