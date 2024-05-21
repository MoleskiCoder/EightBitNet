﻿// <copyright file="Program.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

using System.IO;

namespace M6502.HarteTest
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var directory = @"C:\github\spectrum\libraries\EightBit\modules\65x02\6502\v1";

            await ProcessTestSuiteAsync(directory);
        }

        private static async Task ProcessTestSuiteAsync(string directory)
        {
            var startTime = DateTime.Now;

            var unimplemented_opcode_count = 0;
            var invalid_opcode_count = 0;

            var runner = new TestRunner();
            runner.Initialize();

            var checker = new Checker(runner);
            checker.Initialise();

            var testSuite = new ProcessorTestSuite(directory);
            foreach (var opcode in testSuite.OpcodeTests())
            {
                Console.WriteLine($"Processing: {Path.GetFileName(opcode.Path)}");

                List<string?> testNames = [];
                var tests = opcode.TestsAsync ?? throw new InvalidOperationException("No tests are available");
                await foreach (var test in tests)
                {
                    if (test == null)
                    {
                        throw new InvalidOperationException("Test cannot be null");
                    }

                    checker.Check(test);
                    if (checker.Invalid)
                    {
                        ++invalid_opcode_count;

                        // Was it just unimplemented?
                        if (checker.Unimplemented)
                        {
                            ++unimplemented_opcode_count;
                        }

                        // Let's see if we had any successes!
                        if (testNames.Count > 0)
                        {
                            Console.WriteLine("**** The follow test variations succeeeded");
                            foreach (var testName in testNames)
                            {
                                Console.WriteLine($"****** {testName}");
                            }
                        }

                        // OK, we've attempted an implementation, how did it fail?
                        foreach (var message in checker.Messages)
                        {
                            Console.WriteLine($"**** {message}");
                        }

                        // I'm not really interested in the remaining tests for this opcode
                        break;
                    }

                    testNames.Add(test.Name);
                }
            }

            var finishTime = DateTime.Now;
            var elapsedTime = finishTime - startTime;

            Console.Write($"Elapsed time: {elapsedTime.TotalSeconds} seconds");
            Console.Write($", unimplemented opcode count: {unimplemented_opcode_count}");
            Console.Write($", invalid opcode count: {invalid_opcode_count - unimplemented_opcode_count}");
            Console.WriteLine();
        }
    }
}