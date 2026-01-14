# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Luban is a powerful game configuration solution written in C# (.NET 8.0). It processes configuration data from various formats (Excel, JSON, XML, YAML, Lua) and generates type-safe code for multiple programming languages (C#, Java, Go, C++, Lua, Python, JavaScript, TypeScript, Rust, PHP, Erlang, Godot, Dart). The tool supports complex type systems including OOP inheritance, making it suitable for expressing complex gameplay data like behavior trees, skills, and quest systems.

## Build and Development Commands

### Building the Project
```bash
# Build the entire solution
cd src
dotnet build Luban.sln

# Build in Release mode
dotnet build Luban.sln -c Release

# Build specific project
dotnet build Luban/Luban.csproj
```

### Running Luban
```bash
# Run from source
cd src/Luban
dotnet run -- --conf <config_file> -t <target> [options]

# After building, run the executable
./src/Luban/bin/Debug/net8.0/Luban --conf <config_file> -t <target>
```

### Code Formatting
```bash
# Format code (from scripts directory)
cd scripts
./format.sh    # Linux/Mac
format.bat     # Windows

# Or directly from src directory
cd src
dotnet format --severity error -v n
```

### Testing

The project includes an integration testing framework in `tests/Luban.IntegrationTests/` that validates Lua code generation and JSON data export.

**Quick Start:**
```bash
# Run all tests (using npm scripts)
npm test

# Run tests with detailed output
npm run test:verbose

# Run specific test by name
npm run test:filter "DisplayName~basic_types"

# Watch mode (auto-run on file changes)
npm run test:watch

# Build and test together
npm run build:test
```

**Direct dotnet commands:**
```bash
# Run all tests
cd tests/Luban.IntegrationTests
dotnet test

# Verbose output
dotnet test --logger "console;verbosity=detailed"

# Filter tests
dotnet test --filter "DisplayName~basic_types"
```

**Test Framework Architecture:**

The integration testing framework provides:
- **Automatic test discovery**: Scans `TestData/` directory for test cases
- **Programmatic Luban invocation**: `LubanTestHelper` simulates CLI behavior
- **Semantic comparison**:
  - JSON: Parses and compares objects (ignores formatting/order)
  - Lua: Ignores comments and whitespace
- **Test isolation**: Each test uses independent temporary directories
- **xUnit integration**: Uses Theory and MemberData for dynamic test generation

**Adding New Test Cases:**

1. Create directory under `tests/Luban.IntegrationTests/TestData/{test_name}/`
2. Add required subdirectories:
   ```
   {test_name}/
   ├── schema/
   │   ├── luban.conf      # Luban configuration
   │   └── defines.xml     # Schema definitions
   ├── input/
   │   └── *.json          # Input data files
   └── expected/
       ├── lua/            # Expected Lua code output
       └── json/           # Expected JSON data output
   ```
3. Run tests - new test cases are automatically discovered
4. On first run, copy generated output to `expected/` directories
5. Subsequent runs validate against expected output

**Important Notes:**
- Multi-record JSON files must use `*@filename.json` format in table definitions
- Output filenames are based on table names (e.g., `TbItem` → `tbitem.json`)
- See `tests/README.md` for detailed documentation and troubleshooting

**Test Data Format Example:**
```xml
<!-- schema/defines.xml -->
<table name="TbItem" value="Item" input="*@items.json"/>
```

The `*@` prefix indicates the JSON file contains an array of records.

## Architecture Overview

### Core Pipeline Flow

Luban uses a plugin-based architecture with a clear generation pipeline:

```
1. Load Schema (DefAssembly creation)
   ├── Parse configuration files
   ├── Create type definitions (beans, enums, tables)
   └── Build RawAssembly → DefAssembly

2. Prepare Generation Context
   ├── Calculate export types based on groups/variants
   ├── Sort bean types for language dependencies
   └── Initialize GenerationContext

3. Process Targets (parallel execution)
   ├── Code Targets: Generate language-specific code
   ├── Load Data: Parse input files (Excel, JSON, etc.)
   ├── Validate Data: Run validators (ref, range, path checks)
   └── Data Targets: Export to binary, JSON, XML, YAML
```

### Project Structure

**Core Projects:**
- `Luban.Core/` - Core framework, pipeline, type system, and plugin infrastructure
- `Luban/` - CLI entry point (Program.cs)

**Language Code Generators:**
- `Luban.CSharp/`, `Luban.Java/`, `Luban.Lua/`, `Luban.Python/`, `Luban.Golang/`, `Luban.Rust/`, `Luban.Typescript/`, `Luban.Cpp/`, `Luban.Javascript/`, `Luban.PHP/`, `Luban.Dart/`, `Luban.Gdscript/`

**Data Processing:**
- `Luban.DataLoader.Builtin/` - Loads data from Excel, JSON, XML, YAML, Lua
- `Luban.DataValidator.Builtin/` - Validates data (ref checks, range checks, path validation)
- `Luban.DataTarget.Builtin/` - Exports data to binary, JSON, XML, YAML

**Serialization Formats:**
- `Luban.Protobuf/`, `Luban.FlatBuffers/`, `Luban.MsgPack/`, `Luban.Bson/`

**Other:**
- `Luban.L10N/` - Localization support
- `Luban.Schema.Builtin/` - Schema loading

### Key Architectural Concepts

#### 1. Plugin System (CustomBehaviourManager)

All extensibility points use attribute-based registration:

```csharp
[CodeTarget("csharp-bin")]
public class CsharpBinCodeTarget : CodeTargetBase { }

[DataLoader("excel")]
public class ExcelDataLoader : DataLoaderBase { }

[Validator("ref")]
public class RefValidator : DataValidatorBase { }
```

Plugins are automatically discovered via reflection at startup.

#### 2. Type System

**Core Types (Luban.Core/Types/):**
- Primitives: `TBool`, `TByte`, `TShort`, `TInt`, `TLong`, `TFloat`, `TDouble`, `TString`, `TDateTime`
- Containers: `TArray`, `TList`, `TSet`, `TMap`
- Custom: `TBean` (structures with OOP inheritance), `TEnum`

**Definition Classes (Luban.Core/Defs/):**
- `DefAssembly` - Collection of all types and tables
- `DefTable` - Table definition with input files and value type
- `DefBean` - Structure definition with fields (supports inheritance)
- `DefEnum` - Enumeration definition
- `DefField` - Field definition with type and attributes
- `Record` - Single data record instance

#### 3. Template-Based Code Generation

Most code generators use Scriban templates (`.sbn` files):

```csharp
public abstract class TemplateCodeTargetBase : CodeTargetBase
{
    // Generates code using templates for tables, beans, enums
    // Templates are searched in: custom dirs → project Templates/ → embedded resources
}
```

#### 4. Data Loading and Validation

**Data Loading Flow:**
```
DataLoaderManager.LoadDatas()
├── For each table:
│   ├── Determine loader by file extension (.xlsx, .json, .xml, .yaml, .lua)
│   ├── Parse records from file
│   └── Create DType instances (DBean, DInt, DString, etc.)
```

**Validation Flow:**
```
DataValidatorContext.ValidateTables()
├── For each table (parallel):
│   ├── Create DataValidatorVisitor
│   └── Validate each record using registered validators
```

#### 5. Generation Context

`GenerationContext` is the central state holder during generation:
- Contains `DefAssembly` (all type definitions)
- Holds all loaded table data
- Manages export groups and variants
- Provides access to tables, types, and records

### Important Files and Their Roles

- `src/Luban/Program.cs` - CLI entry point, argument parsing
- `src/Luban.Core/Pipeline/DefaultPipeline.cs` - Main generation pipeline
- `src/Luban.Core/CustomBehaviour/CustomBehaviourManager.cs` - Plugin registration system
- `src/Luban.Core/Defs/DefAssembly.cs` - Type system and schema compilation
- `src/Luban.Core/GenerationContext.cs` - Central state during generation
- `src/Luban.Core/CodeTarget/TemplateCodeTargetBase.cs` - Base for template-based generators
- `src/Luban.Core/DataLoader/DataLoaderManager.cs` - Data loading orchestration
- `src/Luban.Core/DataValidator/DataValidatorContext.cs` - Data validation orchestration

## Common Development Patterns

### Adding a New Language Code Generator

1. Create new project: `Luban.YourLanguage/`
2. Add reference to `Luban.Core`
3. Create code target class with `[CodeTarget("yourlang")]` attribute
4. Inherit from `TemplateCodeTargetBase` or `CodeTargetBase`
5. Create Scriban templates in `Templates/` directory
6. Add project reference in `Luban/Luban.csproj`

### Adding a New Data Loader

1. Create class in `Luban.DataLoader.Builtin/` (or new project)
2. Add `[DataLoader("yourformat")]` attribute
3. Inherit from `DataLoaderBase`
4. Implement `LoadAsync()` method to parse your format
5. Return `List<Record>` with parsed data

### Adding a New Validator

1. Create class in `Luban.DataValidator.Builtin/`
2. Add `[Validator("yourvalidator")]` attribute
3. Inherit from appropriate validator base class
4. Implement validation logic in visitor pattern methods

### Working with Templates

Templates use Scriban syntax and have access to:
- `ctx` - GenerationContext
- `table` - Current DefTable
- `bean` - Current DefBean
- `enum` - Current DefEnum
- Various helper functions for naming conventions, type mapping, etc.

## Configuration and Options

### Command-Line Options

Key options when running Luban:
- `--conf` - Configuration file (REQUIRED)
- `-t, --target` - Target name (REQUIRED)
- `-c, --codeTarget` - Code generation targets (e.g., "csharp-bin", "lua-bin")
- `-d, --dataTarget` - Data export targets (e.g., "bin", "json")
- `--customTemplateDir` - Custom template directories
- `-x, --xargs` - Custom arguments in key=value format
- `-w, --watchDir` - Watch directories for auto-regeneration

### Environment Options (EnvManager)

Options follow naming pattern: `{family}.{target}.{optionName}`

Common options:
- `codeStyle` - Code formatting style
- `namingConvention` - Naming convention (PascalCase, camelCase, snake_case)
- `outputCodeDir` - Output directory for generated code
- `outputDataDir` - Output directory for exported data

## Notes for Development

### Namespace Validation
The codebase includes validation to prevent reserved keywords or special characters in namespaces. See recent commit: "fix: 当namespace中包含保留字或者关键字时抛出错误"

### Code Style
- Uses .NET 8.0 with implicit usings enabled
- Nullable reference types are disabled project-wide
- Follow existing formatting patterns (use `dotnet format` before committing)

### Parallel Processing
The pipeline processes code targets and data targets in parallel for performance. Be mindful of thread safety when modifying core generation logic.

### Template Search Order
1. Custom template directories (via `--customTemplateDir`)
2. Project `Templates/` directory
3. Embedded resources in assemblies

## External Resources

- Official Documentation: https://www.datable.cn/
- Quick Start Guide: https://www.datable.cn/docs/beginner/quickstart
- Example Projects: https://github.com/focus-creative-games/luban_examples
- Discord: https://discord.gg/dGY4zzGMJ4
