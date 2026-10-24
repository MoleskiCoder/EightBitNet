// <copyright file="Program.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

using Zilog.FuseTest;

var suite = new TestSuite("fuse-tests\\tests");
suite.Read();
suite.Parse();
suite.Run();
