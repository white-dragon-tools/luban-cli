namespace Luban.IntegrationTests.Infrastructure;

/// <summary>
/// Validator for Luau scripts using luau-analyze command
/// </summary>
public class LuauValidator
{
    private readonly string _luauAnalyzePath;

    /// <summary>
    /// Create a new Luau validator
    /// </summary>
    /// <param name="luauAnalyzePath">Path to luau-analyze executable (optional, will search PATH if not provided)</param>
    public LuauValidator(string? luauAnalyzePath = null)
    {
        _luauAnalyzePath = luauAnalyzePath ?? FindLuauAnalyze();
    }

    /// <summary>
    /// Find luau-analyze executable in PATH or common locations
    /// </summary>
    private static string? FindLuauAnalyze()
    {
        var candidates = new List<string>
        {
            "luau-analyze",
            "luau-analyze.exe",
        };

        // Try to find .luau directory by searching upward from current directory
        var currentDir = Directory.GetCurrentDirectory();
        var checkedDirs = new HashSet<string>();

        for (var dir = currentDir; dir != null && !checkedDirs.Contains(dir); dir = Path.GetDirectoryName(dir))
        {
            checkedDirs.Add(dir);
            candidates.AddRange(new[]
            {
                Path.Combine(dir, ".luau", "luau-analyze.exe"),
                Path.Combine(dir, ".luau", "luau-analyze"),
            });
        }

        // Check in user profile for rokit
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userProfile))
        {
            candidates.AddRange(new[]
            {
                Path.Combine(userProfile, ".rokit", "tools", "luau", "luau-analyze"),
                Path.Combine(userProfile, ".rokit", "tools", "luau", "luau-analyze.exe"),
            });
        }

        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = candidate,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    return candidate;
                }
            }
            catch
            {
                // Continue to next candidate
            }
        }

        return null;
    }

    /// <summary>
    /// Validate a single Luau file
    /// </summary>
    /// <param name="filePath">Path to the Luau file</param>
    /// <param name="strictMode">Use strict type checking mode</param>
    /// <returns>Validation result</returns>
    public LuauValidationResult ValidateFile(string filePath, bool strictMode = false)
    {
        if (_luauAnalyzePath == null)
        {
            return LuauValidationResult.NotAvailable();
        }

        if (!File.Exists(filePath))
        {
            return LuauValidationResult.Error($"File not found: {filePath}");
        }

        try
        {
            var args = new List<string>();
            args.Add($"\"{filePath}\"");

            // Add type checking mode
            if (strictMode)
            {
                args.Add("--typechecking-mode=strict");
            }

            var output = RunLuauAnalyze(string.Join(" ", args));

            if (string.IsNullOrWhiteSpace(output.Output))
            {
                return LuauValidationResult.Success();
            }

            // Parse output to determine if it's warnings or errors
            var hasErrors = output.Output.Contains("Error:");
            var hasWarnings = output.Output.Contains("Warning:");

            return hasErrors
                ? LuauValidationResult.Error(output.Output)
                : LuauValidationResult.Warning(output.Output);
        }
        catch (Exception ex)
        {
            return LuauValidationResult.Error($"Validation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate multiple Luau files
    /// </summary>
    /// <param name="filePaths">Paths to the Luau files</param>
    /// <param name="strictMode">Use strict type checking mode</param>
    /// <returns>Validation result for all files</returns>
    public LuauValidationResult ValidateFiles(IEnumerable<string> filePaths, bool strictMode = false)
    {
        var allIssues = new List<string>();
        var hasErrors = false;
        var hasWarnings = false;

        foreach (var filePath in filePaths)
        {
            var result = ValidateFile(filePath, strictMode);

            if (!result.IsValid)
            {
                allIssues.Add($"File: {Path.GetFileName(filePath)}");
                allIssues.AddRange(result.Issues);

                if (result.HasErrors)
                {
                    hasErrors = true;
                }
                if (result.HasWarnings)
                {
                    hasWarnings = true;
                }
            }
        }

        if (allIssues.Count == 0)
        {
            return LuauValidationResult.Success();
        }

        if (hasErrors)
        {
            return LuauValidationResult.Error(string.Join("\n", allIssues));
        }

        if (hasWarnings)
        {
            return LuauValidationResult.Warning(string.Join("\n", allIssues));
        }

        return LuauValidationResult.Success();
    }

    /// <summary>
    /// Run luau-analyze command
    /// </summary>
    private (string Output, int ExitCode) RunLuauAnalyze(string arguments)
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = _luauAnalyzePath!,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var output = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) => output.AppendLine(e.Data);
        process.ErrorDataReceived += (sender, e) => output.AppendLine(e.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return (output.ToString(), process.ExitCode);
    }

    /// <summary>
    /// Check if Luau analyzer is available
    /// </summary>
    public bool IsAvailable => _luauAnalyzePath != null;
}
