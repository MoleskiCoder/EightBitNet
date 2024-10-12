// <copyright file="RegisterState.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Z80.FuseTest
{
    using Fuse;
    using System.Globalization;

    internal class RegisterState : AbstractRegisterState, IRegisterState
    {
        public int I { get; private set; } = -1;

        public int R { get; private set; } = -1;

        public bool IFF1 { get; private set; }

        public bool IFF2 { get; private set; }

        public int IM { get; private set; } = -1;

        protected override void ParseInternalState(string[] tokens)
        {
            ArgumentNullException.ThrowIfNull(tokens);
            this.I = Convert.ToInt32(tokens[0], 16);
            this.R = Convert.ToInt32(tokens[1], 16);
            this.IFF1 = Convert.ToInt32(tokens[2], CultureInfo.InvariantCulture) == 1;
            this.IFF2 = Convert.ToInt32(tokens[3], CultureInfo.InvariantCulture) == 1;
            this.IM = Convert.ToInt32(tokens[4], CultureInfo.InvariantCulture);
            this.Halted = Convert.ToInt32(tokens[5], CultureInfo.InvariantCulture) == 1;
            this.TStates = Convert.ToInt32(tokens[6], CultureInfo.InvariantCulture);
        }
    }
}
