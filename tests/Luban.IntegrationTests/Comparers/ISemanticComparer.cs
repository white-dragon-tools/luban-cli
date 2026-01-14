using Luban.IntegrationTests.Models;

namespace Luban.IntegrationTests.Comparers;

/// <summary>
/// Interface for semantic file comparison
/// </summary>
public interface ISemanticComparer
{
    /// <summary>
    /// Compare two files semantically
    /// </summary>
    /// <param name="expectedPath">Path to expected file</param>
    /// <param name="actualPath">Path to actual file</param>
    /// <returns>Comparison result</returns>
    ComparisonResult Compare(string expectedPath, string actualPath);
}
