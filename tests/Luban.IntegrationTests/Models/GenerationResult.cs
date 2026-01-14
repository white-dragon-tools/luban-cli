namespace Luban.IntegrationTests.Models;

/// <summary>
/// Represents the result of running Luban generation
/// </summary>
public class GenerationResult
{
    /// <summary>
    /// Whether the generation succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Output directory where files were generated
    /// </summary>
    public string OutputDirectory { get; set; }

    /// <summary>
    /// Error message if generation failed
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Exception if generation failed
    /// </summary>
    public Exception Exception { get; set; }

    /// <summary>
    /// List of generated files
    /// </summary>
    public List<string> GeneratedFiles { get; set; } = new();

    /// <summary>
    /// Create a successful result
    /// </summary>
    public static GenerationResult Successful(string outputDirectory, List<string> generatedFiles)
    {
        return new GenerationResult
        {
            Success = true,
            OutputDirectory = outputDirectory,
            GeneratedFiles = generatedFiles
        };
    }

    /// <summary>
    /// Create a failed result
    /// </summary>
    public static GenerationResult Failed(string errorMessage, Exception exception = null)
    {
        return new GenerationResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Exception = exception
        };
    }
}
