// <copyright file="SingleStepTests.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>
namespace SM83.HarteTest
{
    using System.Reflection;

    [TestClass]
    public class SingleStepTests
    {
        private const string TestDataDirectory = @"C:\github\spectrum\libraries\EightBit\modules\sm83\v1";

        public static IEnumerable<object[]> OpcodeFiles
        {
            get
            {
                if (!Directory.Exists(TestDataDirectory))
                {
                    yield break;
                }

                foreach (var file in Directory.EnumerateFiles(TestDataDirectory, "*.json").OrderBy(f => f))
                {
                    if (new FileInfo(file).Length > 0)
                    {
                        yield return [file];
                    }
                }
            }
        }

        public static string GetDisplayName(MethodInfo _, object[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            return System.IO.Path.GetFileNameWithoutExtension((string)data[0]);
        }

        [TestMethod]
        [DynamicData(nameof(OpcodeFiles), DynamicDataSourceType.Property, DynamicDataDisplayName = nameof(GetDisplayName))]
        public async Task Opcode_SingleStep(string filePath)
        {
            var runner = new TestRunner();
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
                    Assert.Fail($"Opcode not implemented: {test.Name}");
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
}
