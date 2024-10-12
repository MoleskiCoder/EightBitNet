// <copyright file="Program.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

using Z80.FuseTest;

var suite = new TestSuite("fuse-tests\\tests");
suite.Read();
suite.Parse();
suite.Run();
