namespace Luban.IntegrationTests.Models;

/// <summary>
/// Represents the result of comparing two files
/// </summary>
public class ComparisonResult
{
    /// <summary>
    /// Whether the files match semantically
    /// </summary>
    public bool IsMatch { get; set; }

    /// <summary>
    /// Path to the expected file
    /// </summary>
    public string ExpectedFile { get; set; }

    /// <summary>
    /// Path to the actual file
    /// </summary>
    public string ActualFile { get; set; }

    /// <summary>
    /// List of differences found
    /// </summary>
    public List<string> Differences { get; set; } = new();

    /// <summary>
    /// Error message if comparison failed
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Create a successful match result
    /// </summary>
    public static ComparisonResult Match(string expectedFile, string actualFile)
    {
        return new ComparisonResult
        {
            IsMatch = true,
            ExpectedFile = expectedFile,
            ActualFile = actualFile
        };
    }

    /// <summary>
    /// Create a mismatch result with differences
    /// </summary>
    public static ComparisonResult Mismatch(string expectedFile, string actualFile, List<string> differences)
    {
        return new ComparisonResult
        {
            IsMatch = false,
            ExpectedFile = expectedFile,
            ActualFile = actualFile,
            Differences = differences
        };
    }

    /// <summary>
    /// Create an error result
    /// </summary>
    public static ComparisonResult Error(string expectedFile, string actualFile, string errorMessage)
    {
        return new ComparisonResult
        {
            IsMatch = false,
            ExpectedFile = expectedFile,
            ActualFile = actualFile,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// Get a formatted error message for assertion
    /// </summary>
    public string GetFormattedMessage()
    {
        if (IsMatch)
        {
            return $"Files match: {ExpectedFile}";
        }

        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            return $"Comparison error: {ErrorMessage}";
        }

        var message = $"Files do not match:\nExpected: {ExpectedFile}\nActual: {ActualFile}\n\nDifferences:\n";
        message += string.Join("\n", Differences);
        return message;
    }
}
