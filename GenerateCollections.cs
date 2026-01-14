using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Luban;
using Luban.Pipeline;

class Program
{
    static async Task Main(string[] args)
    {
        var configPath = Path.GetFullPath("tests/Luban.IntegrationTests/TestData/collections/schema/luban.conf");
        var outputCodeDir = Path.GetFullPath("temp_collections_output/code");
        var outputDataDir = Path.GetFullPath("temp_collections_output/data");

        Directory.CreateDirectory(outputCodeDir);
        Directory.CreateDirectory(outputDataDir);

        var options = new Dictionary<string, string>
        {
            ["outputCodeDir"] = outputCodeDir,
            ["outputDataDir"] = outputDataDir
        };

        var launcher = new SimpleLauncher();
        launcher.Start(options);

        var configLoader = new GlobalConfigLoader();
        var config = configLoader.Load(configPath);
        GenerationContext.GlobalConf = config;

        var pipelineArgs = new PipelineArguments
        {
            Target = "test",
            Config = config,
            CodeTargets = new List<string> { "lua-bin" },
            DataTargets = new List<string> { "json" },
            OutputTables = new List<string>(),
            IncludeTags = new List<string>(),
            ExcludeTags = new List<string>(),
            Variants = new Dictionary<string, string>(),
            SchemaCollector = "default"
        };

        var pipeline = PipelineManager.Ins.CreatePipeline("default");
        await Task.Run(() => pipeline.Run(pipelineArgs));

        Console.WriteLine("Generated successfully!");
        Console.WriteLine($"Code: {outputCodeDir}");
        Console.WriteLine($"Data: {outputDataDir}");
    }
}
