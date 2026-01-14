using System.Text.RegularExpressions;
using Luban.IntegrationTests.Models;

namespace Luban.IntegrationTests.Comparers;

/// <summary>
/// Semantic comparer for Lua files
/// </summary>
public class LuaSemanticComparer : ISemanticComparer
{
    public ComparisonResult Compare(string expectedPath, string actualPath)
    {
        try
        {
            // Read both files
            var expectedLua = File.ReadAllText(expectedPath);
            var actualLua = File.ReadAllText(actualPath);

            // Normalize both files
            var normalizedExpected = NormalizeLua(expectedLua);
            var normalizedActual = NormalizeLua(actualLua);

            // Compare normalized content
            if (normalizedExpected == normalizedActual)
            {
                return ComparisonResult.Match(expectedPath, actualPath);
            }
            else
            {
                var differences = GetLineDifferences(normalizedExpected, normalizedActual);
                return ComparisonResult.Mismatch(expectedPath, actualPath, differences);
            }
        }
        catch (Exception ex)
        {
            return ComparisonResult.Error(expectedPath, actualPath, $"Comparison error: {ex.Message}");
        }
    }

    /// <summary>
    /// Normalize Lua code by removing comments and normalizing whitespace
    /// </summary>
    private string NormalizeLua(string lua)
    {
        // Remove multi-line comments --[[ ... ]]
        lua = Regex.Replace(lua, @"--\[\[.*?\]\]", "", RegexOptions.Singleline);

        // Remove single-line comments
        lua = Regex.Replace(lua, @"--[^\n]*", "");

        // Normalize whitespace: multiple spaces/tabs to single space
        lua = Regex.Replace(lua, @"[ \t]+", " ");

        // Normalize newlines: multiple newlines to single newline
        lua = Regex.Replace(lua, @"\n\s*\n+", "\n");

        // Trim each line
        var lines = lua.Split('\n');
        lua = string.Join("\n", lines.Select(line => line.Trim()));

        // Trim overall
        lua = lua.Trim();

        return lua;
    }

    /// <summary>
    /// Get line-by-line differences between two strings
    /// </summary>
    private List<string> GetLineDifferences(string expected, string actual)
    {
        var differences = new List<string>();
        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');

        int maxLines = Math.Max(expectedLines.Length, actualLines.Length);

        for (int i = 0; i < maxLines; i++)
        {
            var expectedLine = i < expectedLines.Length ? expectedLines[i] : "<missing>";
            var actualLine = i < actualLines.Length ? actualLines[i] : "<missing>";

            if (expectedLine != actualLine)
            {
                differences.Add($"Line {i + 1}:");
                differences.Add($"  Expected: {expectedLine}");
                differences.Add($"  Actual:   {actualLine}");
            }
        }

        return differences;
    }
}
