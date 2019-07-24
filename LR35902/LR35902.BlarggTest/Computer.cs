// <copyright file="Computer.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace LR35902.BlarggTest
{
    public class Computer
    {
        private readonly Configuration configuration;
        private readonly Board board;

        public Computer(Configuration configuration)
        {
            this.configuration = configuration;
            this.board = new Board(configuration);
        }

        public void Run()
        {
            var cycles = 0;
            var cpu = this.board.CPU;
            while (cpu.Powered)
            {
                cycles += EightBit.GameBoy.Bus.CyclesPerFrame;
                cycles -= this.board.RunRasterLines();
                cycles -= this.board.RunVerticalBlankLines();
            }
        }

        public void Plug(string path) => this.board.Plug(path);

        public void RaisePOWER()
        {
            this.board.RaisePOWER();
            this.board.Initialize();
        }

        public void LowerPOWER() => this.board.LowerPOWER();
    }
}
