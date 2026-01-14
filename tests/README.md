# Luban 集成测试框架

## 概述

本测试框架为 Luban 项目提供自动化集成测试，验证 Lua 代码生成和 JSON 数据导出的正确性。

## 特性

- **自动测试发现**：自动扫描 `TestData/` 目录，识别所有测试用例
- **语义比较**：
  - JSON：解析后比较对象，忽略属性顺序和格式化
  - Lua：忽略注释和空白字符
- **Luau 验证**：使用 Luau 静态分析工具检查生成的 Lua 代码
- **测试隔离**：每个测试使用独立的临时目录
- **xUnit 集成**：使用 xUnit Theory 动态生成测试

## 测试数据结构

每个测试用例遵循以下目录结构：

```
TestData/{test_name}/
├── schema/              # Schema 定义文件
│   ├── luban.conf      # Luban 配置（必需）
│   └── defines.xml     # Schema 定义文件
├── input/              # 输入数据文件（仅 JSON）
│   └── *.json          # 数据文件
└── expected/           # 期望输出文件
    ├── lua/            # 期望的 Lua 代码生成输出
    │   └── *.lua
    └── json/           # 期望的 JSON 数据导出输出
        └── *.json
```

## 运行测试

### 运行所有测试

```bash
cd tests/Luban.IntegrationTests
dotnet test
```

### 运行特定测试

```bash
dotnet test --filter "DisplayName~basic_types"
```

### 详细输出

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Luau 代码检查

生成的 Lua 代码会自动通过 Luau 静态分析工具进行验证。

#### 安装 Luau

**方式 1：使用 rokit（推荐）**
```bash
rokit init
rokit add luau-lang/luau
```

**方式 2：手动下载**
```bash
# Windows
npm run luau:install

# Linux/Mac
./scripts/install-luau.sh
```

#### 运行 Luau 检查

```bash
# 检查所有 Lua 文件
npm run luau:check

# 使用严格模式检查
npm run luau:check:strict
```

#### 集成测试中的 Luau 验证

在集成测试运行时，会自动对生成的 Lua 文件执行 Luau 验证：
- 如果检测到**错误**，测试将失败
- 如果检测到**警告**，测试会通过，但会在控制台输出警告信息
- 如果 Luau 分析器不可用，测试会跳过验证并在控制台输出提示

**注意**：生成的 Lua 代码应该通过 Luau 的基本检查，避免类型错误和语法错误。

## 添加新测试用例

1. 在 `TestData/` 下创建新目录（例如：`my_test`）

2. 创建 `schema/luban.conf`：
```json
{
  "groups": [
    {
      "names": ["default"],
      "default": true
    }
  ],
  "schemaFiles": [
    {
      "fileName": "defines.xml",
      "type": ""
    }
  ],
  "dataDir": "../input",
  "targets": [
    {
      "name": "test",
      "manager": "",
      "groups": ["default"],
      "topModule": "cfg"
    }
  ]
}
```

3. 创建 `schema/defines.xml`：
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <bean name="MyBean">
    <var name="id" type="int"/>
    <var name="name" type="string"/>
  </bean>
  <table name="TbMyBean" value="MyBean" input="*@mydata.json"/>
</root>
```

**注意**：对于包含多条记录的 JSON 数组，使用 `*@filename.json` 格式。

4. 创建 `input/mydata.json`：
```json
[
  {"id": 1, "name": "Item1"},
  {"id": 2, "name": "Item2"}
]
```

5. 运行测试生成实际输出，然后将生成的文件复制到 `expected/` 目录：
   - Lua 代码：`expected/lua/`
   - JSON 数据：`expected/json/`

6. 再次运行测试验证

## 测试用例示例

### basic_types

测试基本数据类型（int, string, float, bool）的代码生成和数据导出。

**Schema**：
- Bean: `Item` (id, name, price, enabled)
- Table: `TbItem`

**输入**：2 条记录的 JSON 数组

**输出**：
- Lua 代码：包含 Item 类定义和反序列化逻辑
- JSON 数据：导出的表数据

## 架构说明

### 核心组件

- **LubanTestHelper**：编程式调用 Luban，模拟 CLI 行为
- **TestCaseDiscovery**：自动发现和验证测试用例
- **JsonSemanticComparer**：JSON 语义比较
- **LuaSemanticComparer**：Lua 语义比较
- **IntegrationTestBase**：测试基类，提供断言辅助方法
- **DiscoveredIntegrationTests**：动态生成的集成测试类

### 测试流程

1. 测试发现：扫描 `TestData/` 目录
2. 初始化：创建临时输出目录
3. 运行 Luban：生成代码和导出数据
4. 验证输出：语义比较生成的文件与期望文件
5. 清理：删除临时目录

## 故障排查

### 测试失败：文件不匹配

检查差异输出，确认：
- JSON：属性值是否正确（忽略顺序和格式）
- Lua：逻辑是否正确（忽略注释和空白）

### 测试失败：文件未找到

确认：
- 期望文件名与 Luban 生成的文件名匹配
- 表名决定输出文件名（例如：`TbItem` → `tbitem.json`）

### 数据加载失败

确认：
- JSON 文件格式正确
- 多记录文件使用 `*@filename.json` 格式
- `dataDir` 路径正确（相对于 schema 目录）

## 最佳实践

1. **测试命名**：使用描述性名称（例如：`basic_types`, `inheritance`, `collections`）
2. **最小化测试**：每个测试专注于一个特性
3. **期望输出**：从实际生成的正确输出复制，而不是手写
4. **版本控制**：提交所有测试数据和期望输出到 Git

## 技术细节

### JSON 输入格式

Luban 支持两种 JSON 输入格式：

1. **单记录**：`filename.json` - 文件包含单个对象
2. **多记录**：`*@filename.json` - 文件包含对象数组

### 输出文件命名

- Lua 代码：`schema.lua`（固定名称）
- JSON 数据：`{tablename}.json`（小写表名）

### 临时目录

测试使用 `%TEMP%/LubanTests/{guid}/` 作为临时输出目录，测试完成后自动清理。

## 贡献

添加新测试用例时，请确保：
1. 测试用例结构完整
2. 期望输出正确
3. 测试通过
4. 更新本文档（如有新特性）
