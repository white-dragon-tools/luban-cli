# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Luban is a powerful game configuration solution written in C# (.NET 8.0). This fork is specifically designed for Roblox-TS projects, extending the original Luban with features like constructor validators, string enums, and JSON Schema output.

For user-facing documentation, see [README.md](./README.md).

## Build and Development Commands

### Building
```bash
cd src
dotnet build Luban.sln              # Debug build
dotnet build Luban.sln -c Release   # Release build
```

### Running
```bash
cd src/Luban
dotnet run -- --conf <config_file> -t <target> [options]
```

### Code Formatting
```bash
cd src && dotnet format --severity error -v n
```

### Testing
```bash
npm test                                    # Run all tests
npm run test:verbose                        # Detailed output
npm run test:filter "DisplayName~basic_types"  # Specific test
```

For detailed testing documentation, see [tests/README.md](./tests/README.md).

For Luau validation setup, see [docs/LUAU_INTEGRATION.md](./docs/LUAU_INTEGRATION.md).

## Architecture Overview

### Core Pipeline Flow

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

**Core:**
- `Luban.Core/` - Core framework, pipeline, type system, plugin infrastructure
- `Luban/` - CLI entry point

**Code Generators:**
- `Luban.Lua/`, `Luban.CSharp/`, `Luban.Java/`, `Luban.Typescript/`, etc.
- `Luban.JsonSchema/` - JSON Schema output for luban-editor

**Data Processing:**
- `Luban.DataLoader.Builtin/` - Loads from Excel, JSON, XML, YAML, Lua
- `Luban.DataValidator.Builtin/` - Validates data (ref, range, path, constructor)
- `Luban.DataTarget.Builtin/` - Exports to binary, JSON, XML, YAML

### Key Architectural Concepts

#### Plugin System (CustomBehaviourManager)

All extensibility points use attribute-based registration:

```csharp
[CodeTarget("csharp-bin")]
public class CsharpBinCodeTarget : CodeTargetBase { }

[DataLoader("excel")]
public class ExcelDataLoader : DataLoaderBase { }

[Validator("ref")]
public class RefValidator : DataValidatorBase { }
```

#### Type System

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

#### Template-Based Code Generation

Most code generators use Scriban templates (`.sbn` files). Templates have access to:
- `ctx` - GenerationContext
- `table` / `bean` / `enum` - Current definition
- Helper functions for naming conventions, type mapping

Template search order:
1. Custom template directories (via `--customTemplateDir`)
2. Project `Templates/` directory
3. Embedded resources in assemblies

### Important Files

- `src/Luban/Program.cs` - CLI entry point
- `src/Luban.Core/Pipeline/DefaultPipeline.cs` - Main generation pipeline
- `src/Luban.Core/CustomBehaviour/CustomBehaviourManager.cs` - Plugin registration
- `src/Luban.Core/Defs/DefAssembly.cs` - Type system and schema compilation
- `src/Luban.Core/GenerationContext.cs` - Central state during generation
- `src/Luban.Core/CodeTarget/TemplateCodeTargetBase.cs` - Base for template generators

## Common Development Patterns

### Adding a New Code Generator

1. Create new project: `Luban.YourLanguage/`
2. Add reference to `Luban.Core`
3. Create code target class with `[CodeTarget("yourlang")]` attribute
4. Inherit from `TemplateCodeTargetBase` or `CodeTargetBase`
5. Create Scriban templates in `Templates/` directory
6. Add project reference in `Luban/Luban.csproj`

### Adding a New Validator

1. Create class in `Luban.DataValidator.Builtin/`
2. Add `[Validator("yourvalidator")]` attribute
3. Inherit from appropriate validator base class
4. Implement validation logic in visitor pattern methods

For validator usage documentation, see [docs/VALIDATORS.md](./docs/VALIDATORS.md).

### Adding a New Data Loader

1. Create class in `Luban.DataLoader.Builtin/`
2. Add `[DataLoader("yourformat")]` attribute
3. Inherit from `DataLoaderBase`
4. Implement `LoadAsync()` method

## Command-Line Options

Key options:
- `--conf` - Configuration file (REQUIRED)
- `-t, --target` - Target name (REQUIRED)
- `-c, --codeTarget` - Code generation targets (e.g., "csharp-bin", "lua-bin")
- `-d, --dataTarget` - Data export targets (e.g., "bin", "json")
- `--customTemplateDir` - Custom template directories
- `-x, --xargs` - Custom arguments in key=value format

Environment options follow pattern: `{family}.{target}.{optionName}`

## Development Notes

- Uses .NET 8.0 with implicit usings enabled
- Nullable reference types are disabled project-wide
- Run `dotnet format` before committing
- Pipeline processes targets in parallel - be mindful of thread safety

## External Resources

- Official Documentation: https://www.datable.cn/
- Example Projects: https://github.com/focus-creative-games/luban_examples
- Discord: https://discord.gg/dGY4zzGMJ4

## 版本发布
- 提交所有变更
- patch 版本+1
- 提交 pr