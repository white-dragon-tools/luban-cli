# Luban for Roblox-TS

![icon](docs/images/logo.png)

[![license](http://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT) ![star](https://img.shields.io/github/stars/focus-creative-games/luban?style=flat-square)

[中文](./README.md) | English

This project is based on [Luban](https://github.com/focus-creative-games/luban) v4.5, specifically designed to provide configuration compilation support for Roblox-TS projects.

## Core Features

### Original Luban Features

- **Rich Source Data Formats** - Supports Excel (csv, xls, xlsx, xlsm), JSON, XML, YAML, Lua
- **Rich Export Formats** - Supports binary, JSON, BSON, XML, Lua, YAML
- **Complete Type System** - Supports OOP type inheritance for complex data like behavior trees, skills, quests
- **Multi-language Code Generation** - C#, Java, Go, C++, Lua, Python, JavaScript, TypeScript, Rust, etc.
- **Powerful Data Validation** - ref reference check, path resource path, range check, etc.
- **Cross-platform Support** - Runs well on Win, Linux, Mac platforms

### Roblox-TS Extended Features

| Feature | Status | Description |
|---------|--------|-------------|
| Constructor Validator | ✅ Done | Validates type inheritance relationships |
| String Enum Types | ✅ Done | Supports string-valued enums |
| JSON Schema Output | ✅ Done | Provides schema for luban-editor |
| Object Factory | 🔴 Planned | Creates independent object instances from config |
| Flamework Reflect ID | 🔴 Planned | Converts config data to class instances |
| TypeScript Reference Positioning | 🔴 Planned | Generates .d.ts referencing existing types |

## Quick Start

### Build the Project

```bash
cd src
dotnet build Luban.sln
```

### Run Luban

```bash
cd src/Luban
dotnet run -- --conf <config_file> -t <target> [options]
```

### Run Tests

```bash
# Run all tests
npm test

# Run with verbose output
npm run test:verbose

# Run specific test
npm run test:filter "DisplayName~basic_types"
```

## Documentation

### User Documentation

- [Official Documentation](https://www.datable.cn/) - Complete Luban usage guide
- [Quick Start](https://www.datable.cn/docs/beginner/quickstart) - Getting started tutorial
- [Example Projects](https://github.com/focus-creative-games/luban_examples) - Examples for various languages

### Project Documentation

- [Data Validators](./docs/VALIDATORS.md) - Usage guide for constructor, ref, path, range validators
- [JSON Schema Output](./docs/JSON_SCHEMA_OUTPUT.md) - JSON Schema generation feature details
- [Luau Integration](./docs/LUAU_INTEGRATION.md) - Luau static analysis integration guide
- [Integration Tests](./tests/README.md) - Test framework usage guide

### Development Documentation

- [CLAUDE.md](./CLAUDE.md) - Project architecture and development guide (for AI assistants and developers)

## Project Structure

```
luban/
├── src/
│   ├── Luban/                      # CLI entry point
│   ├── Luban.Core/                 # Core framework
│   ├── Luban.Lua/                  # Lua code generator
│   ├── Luban.JsonSchema/           # JSON Schema generator
│   ├── Luban.DataLoader.Builtin/   # Data loaders
│   ├── Luban.DataValidator.Builtin/# Data validators
│   └── Luban.DataTarget.Builtin/   # Data exporters
├── tests/
│   └── Luban.IntegrationTests/     # Integration tests
├── docs/                           # Detailed documentation
└── scripts/                        # Build scripts
```

## Support and Contact

- QQ Group: 692890842 (Luban Development Exchange Group)
- Discord: https://discord.gg/dGY4zzGMJ4
- Email: luban@code-philosophy.com

## License

Licensed under [MIT](https://github.com/focus-creative-games/luban/blob/main/LICENSE)
