using Luban.IntegrationTests.Infrastructure;
using Luban.IntegrationTests.Models;
using Xunit;

namespace Luban.IntegrationTests.Tests;

/// <summary>
/// Dynamically discovered integration tests
/// </summary>
public class DiscoveredIntegrationTests : IntegrationTestBase
{
    /// <summary>
    /// Get all test cases for xUnit Theory
    /// </summary>
    public static TheoryData<TestCase> GetTestCases()
    {
        var testDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
        var discovery = new TestCaseDiscovery(testDataDir);
        var testCases = discovery.DiscoverTestCases();

        var theoryData = new TheoryData<TestCase>();
        foreach (var testCase in testCases)
        {
            theoryData.Add(testCase);
        }

        return theoryData;
    }

    /// <summary>
    /// Run a discovered integration test
    /// </summary>
    [Theory]
    [MemberData(nameof(GetTestCases))]
    public async Task RunDiscoveredTest(TestCase testCase)
    {
        // Initialize test
        InitializeTest(testCase);

        // Determine targets from expected output
        var codeTargets = new List<string>();
        var dataTargets = new List<string>();

        if (testCase.HasExpectedLua)
        {
            codeTargets.Add("lua-bin");
        }

        if (testCase.HasExpectedJson)
        {
            dataTargets.Add("json");
        }

        // Run Luban generation
        var result = await RunLubanAsync("test", codeTargets, dataTargets);

        // Assert generation succeeded
        if (!result.Success)
        {
            var errorDetails = $"Generation failed: {result.ErrorMessage}";
            if (result.Exception != null)
            {
                errorDetails += $"\n\nException: {result.Exception.GetType().Name}";
                errorDetails += $"\nMessage: {result.Exception.Message}";
                errorDetails += $"\nStack Trace:\n{result.Exception.StackTrace}";
            }
            Assert.Fail(errorDetails);
        }

        // Verify Lua output if expected
        if (testCase.HasExpectedLua)
        {
            var expectedLuaDir = Path.Combine(testCase.ExpectedDirectory, "lua");
            var actualLuaDir = Path.Combine(LubanHelper.OutputDirectory, "code");
            AssertFilesMatch(expectedLuaDir, actualLuaDir, "*.lua");
        }

        // Verify JSON output if expected
        if (testCase.HasExpectedJson)
        {
            var expectedJsonDir = Path.Combine(testCase.ExpectedDirectory, "json");
            var actualJsonDir = Path.Combine(LubanHelper.OutputDirectory, "data");
            AssertFilesMatch(expectedJsonDir, actualJsonDir, "*.json");
        }
    }
}
