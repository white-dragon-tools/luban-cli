using Luban.IntegrationTests.Models;
using Luban;
using Luban.Pipeline;

namespace Luban.IntegrationTests.Infrastructure;

/// <summary>
/// Helper class for programmatically invoking Luban
/// </summary>
public class LubanTestHelper : IDisposable
{
    private readonly TempDirectoryManager _tempDirManager;
    private readonly string _configPath;
    private bool _disposed;
    private bool _initialized;

    public LubanTestHelper(string configPath)
    {
        _configPath = configPath;
        _tempDirManager = new TempDirectoryManager();
    }

    /// <summary>
    /// Output directory for generated files
    /// </summary>
    public string OutputDirectory => _tempDirManager.BaseDirectory;

    /// <summary>
    /// Initialize Luban plugin system
    /// </summary>
    private void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        try
        {
            // Initialize SimpleLauncher to load plugins
            var launcher = new SimpleLauncher();
            var options = new Dictionary<string, string>();
            launcher.Start(options);

            _initialized = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initialize Luban: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Run the full Luban pipeline (code generation and data export)
    /// </summary>
    public async Task<GenerationResult> RunFullPipelineAsync(
        string targetName,
        List<string> codeTargets = null,
        List<string> dataTargets = null)
    {
        try
        {
            // Set output directories
            var outputCodeDir = Path.Combine(OutputDirectory, "code");
            var outputDataDir = Path.Combine(OutputDirectory, "data");
            Directory.CreateDirectory(outputCodeDir);
            Directory.CreateDirectory(outputDataDir);

            // Initialize Luban with output directory options
            var options = new Dictionary<string, string>
            {
                ["outputCodeDir"] = outputCodeDir,
                ["outputDataDir"] = outputDataDir
            };

            var launcher = new SimpleLauncher();
            launcher.Start(options);

            // Load configuration
            var configLoader = new GlobalConfigLoader();
            var config = configLoader.Load(_configPath);
            GenerationContext.GlobalConf = config;

            // Create pipeline arguments
            var pipelineArgs = new PipelineArguments
            {
                Target = targetName,
                Config = config,
                CodeTargets = codeTargets ?? new List<string>(),
                DataTargets = dataTargets ?? new List<string>(),
                OutputTables = new List<string>(),
                IncludeTags = new List<string>(),
                ExcludeTags = new List<string>(),
                Variants = new Dictionary<string, string>(),
                SchemaCollector = "default"
            };

            // Run pipeline
            var pipeline = PipelineManager.Ins.CreatePipeline("default");
            await Task.Run(() => pipeline.Run(pipelineArgs));

            // Collect generated files
            var generatedFiles = GetGeneratedFiles();

            return GenerationResult.Successful(OutputDirectory, generatedFiles);
        }
        catch (Exception ex)
        {
            return GenerationResult.Failed($"Generation failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get all generated files in the output directory
    /// </summary>
    public List<string> GetGeneratedFiles(string pattern = "*.*")
    {
        var files = new List<string>();

        if (!Directory.Exists(OutputDirectory))
        {
            return files;
        }

        files.AddRange(Directory.GetFiles(OutputDirectory, pattern, SearchOption.AllDirectories));
        return files;
    }

    /// <summary>
    /// Get generated files matching a specific pattern
    /// </summary>
    public List<string> GetGeneratedFilesByExtension(string extension)
    {
        return GetGeneratedFiles($"*.{extension.TrimStart('.')}");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _tempDirManager?.Dispose();
        _disposed = true;
    }
}
