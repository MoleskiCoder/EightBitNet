// <copyright file="Computer.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace LR35902.BlarggTest
{
    internal class Computer(Configuration configuration)
    {
        private readonly Board board = new(configuration);

        public void Run()
        {
            var cpu = this.board.CPU;
            while (cpu.Powered)
            {
                this.board.RunRasterLines();
                this.board.RunVerticalBlankLines();
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
