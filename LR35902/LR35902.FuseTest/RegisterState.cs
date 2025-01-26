// <copyright file="RegisterState.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace LR35902.FuseTest
{
    using Fuse;
    using System.Globalization;

    public class RegisterState : AbstractRegisterState, IRegisterState
    {
        protected override void ParseInternalState(string[] tokens)
        {
            this.Halted = Convert.ToInt32(tokens[0], CultureInfo.InvariantCulture) == 1;
            this.TStates = Convert.ToInt32(tokens[1], CultureInfo.InvariantCulture);
        }
    }
}
