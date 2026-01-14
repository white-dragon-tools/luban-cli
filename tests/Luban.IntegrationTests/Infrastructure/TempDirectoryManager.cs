namespace Luban.IntegrationTests.Infrastructure;

/// <summary>
/// Manages temporary directories for test isolation
/// </summary>
public class TempDirectoryManager : IDisposable
{
    private readonly string _baseDirectory;
    private readonly List<string> _createdDirectories = new();
    private bool _disposed;

    public TempDirectoryManager()
    {
        _baseDirectory = Path.Combine(Path.GetTempPath(), "LubanTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_baseDirectory);
    }

    /// <summary>
    /// Create a new temporary directory
    /// </summary>
    public string CreateTempDirectory(string name = null)
    {
        var dirName = name ?? Guid.NewGuid().ToString();
        var fullPath = Path.Combine(_baseDirectory, dirName);
        Directory.CreateDirectory(fullPath);
        _createdDirectories.Add(fullPath);
        return fullPath;
    }

    /// <summary>
    /// Get the base temporary directory
    /// </summary>
    public string BaseDirectory => _baseDirectory;

    /// <summary>
    /// Clean up all temporary directories
    /// </summary>
    public void Cleanup()
    {
        if (_disposed)
            return;

        try
        {
            if (Directory.Exists(_baseDirectory))
            {
                Directory.Delete(_baseDirectory, recursive: true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to cleanup temp directory {_baseDirectory}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Cleanup();
        _disposed = true;
    }
}
