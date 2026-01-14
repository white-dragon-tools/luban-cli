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
        Assert.True(result.IsMatch, result.GetFormattedMessage());
    }

    /// <summary>
    /// Assert that a Lua file matches semantically
    /// </summary>
    protected void AssertLuaMatch(string expectedPath, string actualPath)
    {
        var result = _luaComparer.Compare(expectedPath, actualPath);
        Assert.True(result.IsMatch, result.GetFormattedMessage());
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
