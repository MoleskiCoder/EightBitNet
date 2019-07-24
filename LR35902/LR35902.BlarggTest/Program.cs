// <copyright file="Program.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace LR35902.BlarggTest
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new Configuration();

#if DEBUG
            configuration.DebugMode = true;
#endif

            var computer = new Computer(configuration);

            computer.Plug("blargg/cpu_instrs.gb");                  // Passed
            ////computer.plug("blargg/01-special.gb");				// Passed
            ////computer.plug("blargg/02-interrupts.gb");			// Passed
            ////computer.plug("blargg/03-op sp,hl.gb");				// Passed
            ////computer.plug("blargg/04-op r,imm.gb");				// Passed
            ////computer.plug("blargg/05-op rp.gb");					// Passed
            ////computer.plug("blargg/06-ld r,r.gb");				// Passed
            ////computer.plug("blargg/07-jr,jp,call,ret,rst.gb");	// Passed
            ////computer.plug("blargg/08-misc instrs.gb");			// Passed
            ////computer.plug("blargg/09-op r,r.gb");				// Passed
            ////computer.plug("blargg/10-bit ops.gb");				// Passed
            ////computer.plug("blargg/11-op a,(hl).gb");				// Passed

            ////computer.plug("blargg/instr_timing.gb");				// Failed #255
            ////computer.plug("blargg/interrupt_time.gb");			// Failed

            computer.RaisePOWER();
            computer.Run();
        }
    }
}
