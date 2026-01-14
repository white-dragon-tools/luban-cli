using System.Text.Json;
using Luban.IntegrationTests.Models;

namespace Luban.IntegrationTests.Comparers;

/// <summary>
/// Semantic comparer for JSON files
/// </summary>
public class JsonSemanticComparer : ISemanticComparer
{
    public ComparisonResult Compare(string expectedPath, string actualPath)
    {
        try
        {
            // Read both files
            var expectedJson = File.ReadAllText(expectedPath);
            var actualJson = File.ReadAllText(actualPath);

            // Parse JSON
            var expectedDoc = JsonDocument.Parse(expectedJson);
            var actualDoc = JsonDocument.Parse(actualJson);

            // Compare semantically
            var differences = new List<string>();
            CompareJsonElements(expectedDoc.RootElement, actualDoc.RootElement, "$", differences);

            if (differences.Count == 0)
            {
                return ComparisonResult.Match(expectedPath, actualPath);
            }
            else
            {
                return ComparisonResult.Mismatch(expectedPath, actualPath, differences);
            }
        }
        catch (Exception ex)
        {
            return ComparisonResult.Error(expectedPath, actualPath, $"Comparison error: {ex.Message}");
        }
    }

    private void CompareJsonElements(JsonElement expected, JsonElement actual, string path, List<string> differences)
    {
        // Check value type
        if (expected.ValueKind != actual.ValueKind)
        {
            differences.Add($"Path: {path} - Type mismatch: expected {expected.ValueKind}, actual {actual.ValueKind}");
            return;
        }

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                CompareJsonObjects(expected, actual, path, differences);
                break;
            case JsonValueKind.Array:
                CompareJsonArrays(expected, actual, path, differences);
                break;
            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                CompareJsonValues(expected, actual, path, differences);
                break;
        }
    }

    private void CompareJsonObjects(JsonElement expected, JsonElement actual, string path, List<string> differences)
    {
        var expectedProps = expected.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
        var actualProps = actual.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

        // Check for missing properties
        foreach (var key in expectedProps.Keys)
        {
            if (!actualProps.ContainsKey(key))
            {
                differences.Add($"Path: {path}.{key} - Property missing in actual");
            }
        }

        // Check for extra properties
        foreach (var key in actualProps.Keys)
        {
            if (!expectedProps.ContainsKey(key))
            {
                differences.Add($"Path: {path}.{key} - Extra property in actual");
            }
        }

        // Compare common properties
        foreach (var key in expectedProps.Keys.Intersect(actualProps.Keys))
        {
            CompareJsonElements(expectedProps[key], actualProps[key], $"{path}.{key}", differences);
        }
    }

    private void CompareJsonArrays(JsonElement expected, JsonElement actual, string path, List<string> differences)
    {
        var expectedArray = expected.EnumerateArray().ToList();
        var actualArray = actual.EnumerateArray().ToList();

        if (expectedArray.Count != actualArray.Count)
        {
            differences.Add($"Path: {path} - Array length mismatch: expected {expectedArray.Count}, actual {actualArray.Count}");
            return;
        }

        for (int i = 0; i < expectedArray.Count; i++)
        {
            CompareJsonElements(expectedArray[i], actualArray[i], $"{path}[{i}]", differences);
        }
    }

    private void CompareJsonValues(JsonElement expected, JsonElement actual, string path, List<string> differences)
    {
        var expectedStr = expected.GetRawText();
        var actualStr = actual.GetRawText();

        if (expectedStr != actualStr)
        {
            differences.Add($"Path: {path} - Value mismatch: expected {expectedStr}, actual {actualStr}");
        }
    }
}
