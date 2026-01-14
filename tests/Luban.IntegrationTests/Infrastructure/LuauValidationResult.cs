namespace Luban.IntegrationTests.Infrastructure;

/// <summary>
/// Result of Luau validation
/// </summary>
public class LuauValidationResult
{
    /// <summary>
    /// Whether the validation passed (no issues)
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Whether there are errors
    /// </summary>
    public bool HasErrors { get; }

    /// <summary>
    /// Whether there are warnings
    /// </summary>
    public bool HasWarnings { get; }

    /// <summary>
    /// List of issues (errors and warnings)
    /// </summary>
    public List<string> Issues { get; }

    /// <summary>
    /// Error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    private LuauValidationResult(bool isValid, bool hasErrors, bool hasWarnings, List<string> issues, string? errorMessage)
    {
        IsValid = isValid;
        HasErrors = hasErrors;
        HasWarnings = hasWarnings;
        Issues = issues;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Create a successful validation result
    /// </summary>
    public static LuauValidationResult Success()
    {
        return new LuauValidationResult(true, false, false, new List<string>(), null);
    }

    /// <summary>
    /// Create a result with errors
    /// </summary>
    public static LuauValidationResult Error(string message)
    {
        var issues = new List<string>
        {
            message
        };
        return new LuauValidationResult(false, true, false, issues, message);
    }

    /// <summary>
    /// Create a result with warnings
    /// </summary>
    public static LuauValidationResult Warning(string message)
    {
        var issues = new List<string>
        {
            message
        };
        return new LuauValidationResult(false, false, true, issues, null);
    }

    /// <summary>
    /// Create a result when Luau analyzer is not available
    /// </summary>
    public static LuauValidationResult NotAvailable()
    {
        return new LuauValidationResult(true, false, false, new List<string>(), null)
        {
            ErrorMessage = "Luau analyzer not available. Install via: rokit add luau-lang/luau"
        };
    }

    /// <summary>
    /// Get formatted message for display
    /// </summary>
    public string GetFormattedMessage()
    {
        if (IsValid)
        {
            return "Luau validation passed";
        }

        var sb = new System.Text.StringBuilder();

        if (HasErrors)
        {
            sb.AppendLine("Luau validation FAILED with errors:");
        }
        else if (HasWarnings)
        {
            sb.AppendLine("Luau validation completed with warnings:");
        }

        foreach (var issue in Issues)
        {
            sb.AppendLine($"  {issue}");
        }

        return sb.ToString().Trim();
    }
}
