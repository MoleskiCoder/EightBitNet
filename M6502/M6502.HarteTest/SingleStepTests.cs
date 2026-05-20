// <copyright file="SingleStepTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace M6502.HarteTest
{
    using System.Reflection;

    internal static class HarteTestHelper
    {
        internal static IEnumerable<object[]> GetOpcodeFiles(string directory)
        {
            if (!Directory.Exists(directory))
            {
                yield break;
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*.json").OrderBy(f => f))
            {
                if (new FileInfo(file).Length > 0)
                {
                    yield return [file];
                }
            }
        }

        internal static string GetDisplayName(MethodInfo _, object[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            return System.IO.Path.GetFileNameWithoutExtension((string)data[0]);
        }

        internal static async Task RunOpcodeAsync(string filePath, TestRunner runner)
        {
            runner.Initialize();
            var checker = new Checker(runner);
            checker.Initialise();

            using var suite = new OpcodeTestSuite(filePath);
            await foreach (var test in suite.TestsAsync)
            {
                if (test is null)
                {
                    throw new InvalidOperationException("Test cannot be null");
                }

                checker.Check(test);

                if (checker.Unimplemented)
                {
                    Assert.Inconclusive($"Opcode not implemented: {test.Name}");
                    return;
                }

                if (checker.Invalid)
                {
                    Assert.Fail($"Test '{test.Name}' failed:{System.Environment.NewLine}{string.Join(System.Environment.NewLine, checker.Messages)}");
                    return;
                }
            }
        }
    }

    [TestClass]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Test class needs to be externally visible")]
    public class Mos6502SingleStepTests
    {
        private const string TestDataDirectory = @"C:\github\spectrum\libraries\EightBit\modules\65x02\6502\v1";

        public static IEnumerable<object[]> OpcodeFiles => HarteTestHelper.GetOpcodeFiles(TestDataDirectory);

        public static string GetDisplayName(MethodInfo m, object[] data) => HarteTestHelper.GetDisplayName(m, data);

        [TestMethod]
        [DynamicData(nameof(OpcodeFiles), DynamicDataSourceType.Property, DynamicDataDisplayName = nameof(GetDisplayName))]
        public async Task OpcodeSingleStep(string filePath)
            => await HarteTestHelper.RunOpcodeAsync(filePath, new TestRunner(r => new MOS6502(r))).ConfigureAwait(false);
    }

    [TestClass]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Test class needs to be externally visible")]
    public class Wdc65c02SingleStepTests
    {
        private const string TestDataDirectory = @"C:\github\spectrum\libraries\EightBit\modules\65x02\wdc65c02\v1";

        public static IEnumerable<object[]> OpcodeFiles => HarteTestHelper.GetOpcodeFiles(TestDataDirectory);

        public static string GetDisplayName(MethodInfo m, object[] data) => HarteTestHelper.GetDisplayName(m, data);

        [TestMethod]
        [DynamicData(nameof(OpcodeFiles), DynamicDataSourceType.Property, DynamicDataDisplayName = nameof(GetDisplayName))]
        public async Task OpcodeSingleStep(string filePath)
            => await HarteTestHelper.RunOpcodeAsync(filePath, new TestRunner(r => new WDC65C02(r))).ConfigureAwait(false);
    }
}
