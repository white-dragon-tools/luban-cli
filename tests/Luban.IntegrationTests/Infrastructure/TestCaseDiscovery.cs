using Luban.IntegrationTests.Models;

namespace Luban.IntegrationTests.Infrastructure;

/// <summary>
/// Discovers and validates integration test cases
/// </summary>
public class TestCaseDiscovery
{
    private readonly string _testDataDirectory;

    public TestCaseDiscovery(string testDataDirectory)
    {
        _testDataDirectory = testDataDirectory;
    }

    /// <summary>
    /// Discover all valid test cases in the TestData directory
    /// </summary>
    public List<TestCase> DiscoverTestCases()
    {
        var testCases = new List<TestCase>();

        if (!Directory.Exists(_testDataDirectory))
        {
            Console.WriteLine($"Warning: TestData directory not found: {_testDataDirectory}");
            return testCases;
        }

        var subdirectories = Directory.GetDirectories(_testDataDirectory);

        foreach (var dir in subdirectories)
        {
            var testCase = TryCreateTestCase(dir);
            if (testCase != null)
            {
                testCases.Add(testCase);
            }
        }

        return testCases;
    }

    /// <summary>
    /// Try to create a test case from a directory
    /// </summary>
    private TestCase TryCreateTestCase(string directory)
    {
        var testCase = new TestCase
        {
            Name = Path.GetFileName(directory),
            TestDirectory = directory
        };

        // Validate test case structure
        if (!ValidateTestCase(testCase, out var errorMessage))
        {
            Console.WriteLine($"Skipping invalid test case '{testCase.Name}': {errorMessage}");
            return null;
        }

        return testCase;
    }

    /// <summary>
    /// Validate that a test case has the required structure
    /// </summary>
    public bool ValidateTestCase(TestCase testCase, out string errorMessage)
    {
        errorMessage = null;

        // Check schema directory exists
        if (!Directory.Exists(testCase.SchemaDirectory))
        {
            errorMessage = "schema/ directory not found";
            return false;
        }

        // Check luban.conf exists
        if (!File.Exists(testCase.ConfigPath))
        {
            errorMessage = "schema/luban.conf not found";
            return false;
        }

        // Check input directory exists
        if (!Directory.Exists(testCase.InputDirectory))
        {
            errorMessage = "input/ directory not found";
            return false;
        }

        // Check expected directory exists
        if (!Directory.Exists(testCase.ExpectedDirectory))
        {
            errorMessage = "expected/ directory not found";
            return false;
        }

        // Check that at least one expected output type exists
        if (!testCase.HasExpectedLua && !testCase.HasExpectedJson && !testCase.HasExpectedJsonSchema)
        {
            errorMessage = "No expected output found (expected/lua/, expected/json/, or expected/json-schema/)";
            return false;
        }

        return true;
    }
}
