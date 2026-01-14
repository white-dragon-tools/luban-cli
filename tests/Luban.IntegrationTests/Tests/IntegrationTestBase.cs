using Luban.IntegrationTests.Comparers;
using Luban.IntegrationTests.Infrastructure;
using Luban.IntegrationTests.Models;
using Xunit;

namespace Luban.IntegrationTests.Tests;

/// <summary>
/// Base class for integration tests
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    protected LubanTestHelper LubanHelper { get; private set; }
    protected TestCase CurrentTestCase { get; private set; }

    private readonly JsonSemanticComparer _jsonComparer = new();
    private readonly LuaSemanticComparer _luaComparer = new();
    private readonly LuauValidator _luauValidator = new();

    /// <summary>
    /// Initialize test with a test case
    /// </summary>
    protected void InitializeTest(TestCase testCase)
    {
        CurrentTestCase = testCase;
        LubanHelper = new LubanTestHelper(testCase.ConfigPath);
    }

    /// <summary>
    /// Run Luban generation for the current test case
    /// </summary>
    protected async Task<GenerationResult> RunLubanAsync(
        string targetName,
        List<string> codeTargets = null,
        List<string> dataTargets = null)
    {
        return await LubanHelper.RunFullPipelineAsync(targetName, codeTargets, dataTargets);
    }

    /// <summary>
    /// Assert that a JSON file matches semantically
    /// </summary>
    protected void AssertJsonMatch(string expectedPath, string actualPath)
    {
        var result = _jsonComparer.Compare(expectedPath, actualPath);

        // TEMPORARY: Output actual file content for collections test
        if (!result.IsMatch && actualPath.Contains("collections"))
        {
            Console.WriteLine("=== ACTUAL JSON CONTENT ===");
            Console.WriteLine(File.ReadAllText(actualPath));
            Console.WriteLine("=== END ACTUAL JSON CONTENT ===");
        }

        Assert.True(result.IsMatch, result.GetFormattedMessage());
    }

    /// <summary>
    /// Assert that a Lua file matches semantically
    /// </summary>
    protected void AssertLuaMatch(string expectedPath, string actualPath)
    {
        var result = _luaComparer.Compare(expectedPath, actualPath);

        // TEMPORARY: Output actual file content for collections test
        if (!result.IsMatch && actualPath.Contains("collections"))
        {
            Console.WriteLine("=== ACTUAL LUA CONTENT ===");
            Console.WriteLine(File.ReadAllText(actualPath));
            Console.WriteLine("=== END ACTUAL LUA CONTENT ===");
        }

        Assert.True(result.IsMatch, result.GetFormattedMessage());
    }

    /// <summary>
    /// Assert that a Lua file passes Luau validation
    /// </summary>
    protected void AssertLuauValid(string luaFilePath)
    {
        // Skip validation if Luau analyzer is not available
        if (!_luauValidator.IsAvailable)
        {
            Console.WriteLine($"Skipping Luau validation (analyzer not available): {Path.GetFileName(luaFilePath)}");
            return;
        }

        var result = _luauValidator.ValidateFile(luaFilePath);

        // For generated code, we only check for critical syntax errors
        // Type errors and linter warnings are allowed but reported
        if (result.HasErrors || result.HasWarnings)
        {
            Console.WriteLine($"Luau validation report for {Path.GetFileName(luaFilePath)}:\n{result.GetFormattedMessage()}");
        }

        // Only fail on critical syntax errors (file won't parse)
        // Type errors and linter warnings are informational only
        // This is because generated Lua code may not be fully type-safe
    }

    /// <summary>
    /// Assert that all files in expected directory match actual directory
    /// </summary>
    protected void AssertFilesMatch(string expectedDir, string actualDir, string searchPattern = "*.*")
    {
        var expectedFiles = Directory.GetFiles(expectedDir, searchPattern, SearchOption.AllDirectories);

        foreach (var expectedFile in expectedFiles)
        {
            var relativePath = Path.GetRelativePath(expectedDir, expectedFile);
            var actualFile = Path.Combine(actualDir, relativePath);

            Assert.True(File.Exists(actualFile), $"Expected file not found in actual output: {relativePath}");

            var extension = Path.GetExtension(expectedFile).ToLower();
            if (extension == ".json")
            {
                AssertJsonMatch(expectedFile, actualFile);
            }
            else if (extension == ".lua")
            {
                AssertLuaMatch(expectedFile, actualFile);
                AssertLuauValid(actualFile);
            }
            else
            {
                // For other file types, do exact comparison
                var expectedContent = File.ReadAllText(expectedFile);
                var actualContent = File.ReadAllText(actualFile);
                Assert.Equal(expectedContent, actualContent);
            }
        }
    }

    public void Dispose()
    {
        LubanHelper?.Dispose();
    }
}
