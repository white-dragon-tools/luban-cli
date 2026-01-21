namespace Luban.IntegrationTests.Models;

/// <summary>
/// Represents a single integration test case
/// </summary>
public class TestCase
{
    /// <summary>
    /// Name of the test case (directory name)
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Full path to the test case directory
    /// </summary>
    public string TestDirectory { get; set; }

    /// <summary>
    /// Path to the schema directory
    /// </summary>
    public string SchemaDirectory => Path.Combine(TestDirectory, "schema");

    /// <summary>
    /// Path to the input data directory
    /// </summary>
    public string InputDirectory => Path.Combine(TestDirectory, "input");

    /// <summary>
    /// Path to the expected output directory
    /// </summary>
    public string ExpectedDirectory => Path.Combine(TestDirectory, "expected");

    /// <summary>
    /// Path to luban.conf file
    /// </summary>
    public string ConfigPath => Path.Combine(SchemaDirectory, "luban.conf");

    /// <summary>
    /// Whether this test case has expected Lua output
    /// </summary>
    public bool HasExpectedLua => Directory.Exists(Path.Combine(ExpectedDirectory, "lua"));

    /// <summary>
    /// Whether this test case has expected JSON output
    /// </summary>
    public bool HasExpectedJson => Directory.Exists(Path.Combine(ExpectedDirectory, "json"));

    /// <summary>
    /// Whether this test case has expected JSON Schema output
    /// </summary>
    public bool HasExpectedJsonSchema => Directory.Exists(Path.Combine(ExpectedDirectory, "json-schema"));

    public override string ToString() => Name;
}
