# Luban for Roblox-TS

![icon](docs/images/logo.png)

[![license](http://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT) ![star](https://img.shields.io/github/stars/focus-creative-games/luban?style=flat-square)

[English](./README_EN.md) | 中文

本项目基于 [Luban](https://github.com/focus-creative-games/luban) v4.5 开发，专门为 Roblox-TS 项目提供配置编译支持。

## 核心特性

### 原版 Luban 特性

- **丰富的源数据格式** - 支持 Excel (csv, xls, xlsx, xlsm)、JSON、XML、YAML、Lua
- **丰富的导出格式** - 支持 binary、JSON、BSON、XML、Lua、YAML
- **完备的类型系统** - 支持 OOP 类型继承，可表达行为树、技能、剧情等复杂数据
- **多语言代码生成** - C#、Java、Go、C++、Lua、Python、JavaScript、TypeScript、Rust 等
- **强大的数据校验** - ref 引用检查、path 资源路径、range 范围检查等
- **跨平台支持** - Win、Linux、Mac 平台良好运行

### Roblox-TS 扩展特性

| 特性 | 状态 | 说明 |
|------|------|------|
| Constructor 验证器 | ✅ 已完成 | 验证类型继承关系 |
| 字符串枚举类型 | ✅ 已完成 | 支持字符串值的枚举 |
| JSON Schema 输出 | ✅ 已完成 | 为 luban-editor 提供 schema |
| 工厂函数 (ObjectFactory) | 🔴 计划中 | 从配置创建独立对象实例 |
| Flamework Reflect ID | 🔴 计划中 | 配置数据转换为类实例 |
| TypeScript 引用定位 | 🔴 计划中 | 生成 .d.ts 引用已有类型 |

## 快速开始

### 构建项目

```bash
cd src
dotnet build Luban.sln
```

### 运行 Luban

```bash
cd src/Luban
dotnet run -- --conf <config_file> -t <target> [options]
```

### 运行测试

```bash
# 运行所有测试
npm test

# 运行详细输出
npm run test:verbose

# 运行特定测试
npm run test:filter "DisplayName~basic_types"
```

## 文档

### 用户文档

- [官方文档](https://www.datable.cn/) - Luban 完整使用指南
- [快速上手](https://www.datable.cn/docs/beginner/quickstart) - 入门教程
- [示例项目](https://github.com/focus-creative-games/luban_examples) - 各语言示例

### 项目文档

- [数据验证器](./docs/VALIDATORS.md) - constructor、ref、path、range 验证器使用说明
- [JSON Schema 输出](./docs/JSON_SCHEMA_OUTPUT.md) - JSON Schema 生成功能详解
- [Luau 集成](./docs/LUAU_INTEGRATION.md) - Luau 静态分析集成说明
- [集成测试](./tests/README.md) - 测试框架使用说明

### 开发文档

- [CLAUDE.md](./CLAUDE.md) - 项目架构和开发指南（供 AI 助手和开发者参考）

## 项目结构

```
luban/
├── src/
│   ├── Luban/                      # CLI 入口
│   ├── Luban.Core/                 # 核心框架
│   ├── Luban.Lua/                  # Lua 代码生成器
│   ├── Luban.JsonSchema/           # JSON Schema 生成器
│   ├── Luban.DataLoader.Builtin/   # 数据加载器
│   ├── Luban.DataValidator.Builtin/# 数据验证器
│   └── Luban.DataTarget.Builtin/   # 数据导出器
├── tests/
│   └── Luban.IntegrationTests/     # 集成测试
├── docs/                           # 详细文档
└── scripts/                        # 构建脚本
```

## 支持与联系

- QQ群: 692890842 (Luban开发交流群)
- Discord: https://discord.gg/dGY4zzGMJ4
- 邮箱: luban@code-philosophy.com

## License

基于 [MIT](https://github.com/focus-creative-games/luban/blob/main/LICENSE) 许可证
