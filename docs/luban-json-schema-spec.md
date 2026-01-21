# Luban JSON Schema 生成规范

本文档定义了 Luban 配置系统生成 JSON Schema 的标准，确保 VSCode 能正确提供智能提示和验证。

## 概述

Luban 的 `json-schema` 代码目标生成符合 JSON Schema Draft-07 规范的 schema 文件，支持：

- 多态类型的智能提示（`$type` 下拉选项）
- 属性名自动补全
- 枚举值自动补全
- 错误值实时检测

## 生成的文件

### 主 Schema 文件 (`schema.json`)

包含所有类型定义的主 schema 文件：

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "luban-schema",
  "definitions": {
    "EnumName": { ... },
    "BeanName": { ... },
    "BeanNameDataFile": { ... }
  },
  "tables": { ... }
}
```

### Wrapper Schema 文件 (`xxx.schema.json`)

为每个表生成独立的 wrapper schema 文件，直接引用 `XxxDataFile` 变体：

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$ref": "schema.json#/definitions/BeanNameDataFile"
}
```

## 多态类型定义规范

### if/then 结构（推荐）

使用 `if/then` 结构替代 `oneOf`，确保 VSCode 能正确提示 `$type` 选项：

```json
{
  "SkillEffect": {
    "type": "object",
    "properties": {
      "$type": {
        "type": "string",
        "enum": ["DamageEffect", "HealEffect", "BuffEffect"],
        "description": "Type discriminator for SkillEffect polymorphic type"
      }
    },
    "required": ["$type"],
    "allOf": [
      {
        "if": {
          "properties": { "$type": { "const": "DamageEffect" } },
          "required": ["$type"]
        },
        "then": { "$ref": "#/definitions/DamageEffect" }
      },
      {
        "if": {
          "properties": { "$type": { "const": "HealEffect" } },
          "required": ["$type"]
        },
        "then": { "$ref": "#/definitions/HealEffect" }
      }
    ],
    "discriminator": { "propertyName": "$type" }
  }
}
```

### 关键点

1. **`$type` 属性有显式 `enum`**：列出所有可选的具体类型名称
2. **使用 `allOf` + `if/then`**：根据 `$type` 值选择正确的子类型定义
3. **保留 `discriminator`**：兼容支持 OpenAPI 3.0 discriminator 的工具

## 独立文件 vs 嵌套对象

### 嵌套多态对象

当多态类型作为其他对象的字段时，需要 `$type` 来区分具体类型：

```json
{
  "effect": {
    "$type": "DamageEffect",
    "damage": 100,
    "damageType": "fire"
  }
}
```

### 独立文件（XxxDataFile 变体）

当数据类型作为独立文件存储时，类型已知，不需要 `$type`。支持 `$schema` 属性：

```json
{
  "$schema": "../schema/damage-effect.schema.json",
  "damage": 100,
  "damageType": "fire"
}
```

Luban 为每个多态子类型生成 `XxxDataFile` 变体定义，不包含 `$type` 字段，但支持 `$schema` 属性。

## VSCode 配置

### json.schemas 配置

在 `.vscode/settings.json` 中配置（注意 fileMatch 路径不带 `./` 前缀）：

```json
{
  "json.schemas": [
    {
      "fileMatch": ["configs/datas/buffs/**/*.json"],
      "url": "./configs/schema/i-buff-data.schema.json"
    },
    {
      "fileMatch": ["configs/datas/skills/**/*.json"],
      "url": "./configs/schema/skill-config.schema.json"
    }
  ]
}
```

### 使用 $schema 属性

也可以在 JSON 文件中直接指定 schema：

```json
{
  "$schema": "../schema/i-buff-data.schema.json",
  "id": 1,
  "name": "Burn"
}
```

## 命令行选项

```bash
# 生成 JSON Schema
luban -t all -c json-schema --conf luban.conf -x outputCodeDir=configs/schema

# 配置 fileMatch 路径前缀（生成 configs/datas/xxx/**/*.json 格式）
luban -t all -c json-schema --conf luban.conf -x outputCodeDir=configs/schema -x json-schema.dataDir=configs

# 禁用 wrapper schema 生成
luban -t all -c json-schema --conf luban.conf -x json-schema.generateWrappers=false

# 禁用 VSCode 配置生成
luban -t all -c json-schema --conf luban.conf -x json-schema.generateVscodeSettings=false

# 自定义主 schema 文件名
luban -t all -c json-schema --conf luban.conf -x json-schema.outputFile=my-schema.json

# 自定义 VSCode 配置中的 schema 路径
luban -t all -c json-schema --conf luban.conf -x json-schema.schemaDir=./configs/schema
```

### 选项说明

| 选项 | 默认值 | 说明 |
|------|--------|------|
| `json-schema.outputFile` | `schema.json` | 主 schema 文件名 |
| `json-schema.generateWrappers` | `true` | 是否生成 wrapper schema 文件 |
| `json-schema.generateVscodeSettings` | `true` | 是否生成 VSCode 配置文件 |
| `json-schema.schemaDir` | `./configs/schema` | VSCode 配置中 schema 文件的路径 |
| `json-schema.dataDir` | `""` | fileMatch 路径前缀，如 `configs` 会生成 `configs/datas/xxx/**/*.json` |

## 生成的 VSCode 配置

生成的 `vscode-json-schemas.json` 文件包含 `json.schemas` 配置，可以合并到 `.vscode/settings.json`：

```json
{
  "json.schemas": [
    {
      "fileMatch": ["configs/datas/buff/**/*.json"],
      "url": "./configs/schema/i-buff-data.schema.json"
    },
    {
      "fileMatch": ["configs/datas/skill/**/*.json"],
      "url": "./configs/schema/skill-data-config.schema.json"
    }
  ]
}
```

注意：`fileMatch` 路径不带 `./` 或 `**/` 前缀，直接使用相对路径如 `configs/datas/xxx/**/*.json`。

## 验收标准检查清单

- [x] 多态类型使用 `if/then` 结构
- [x] `$type` 属性有显式 `enum` 列出所有选项
- [x] VSCode 中输入 `$type` 能看到所有可选值
- [x] 每个多态子类型有 `XxxDataFile` 变体支持独立文件
- [x] `XxxDataFile` 变体支持 `$schema` 属性
- [x] 为每个表生成 wrapper schema 文件
- [x] fileMatch 路径格式为 `configs/datas/xxx/**/*.json`（不带 `./` 或 `**/` 前缀）

## 常见问题 FAQ

### Q: 为什么不使用 `oneOf` + `discriminator`？

A: VSCode 的 JSON Language Service 无法正确处理 `oneOf` + `discriminator` 结构来提示 `$type` 的可选值。`if/then` 结构是经过实际测试验证的解决方案。

### Q: XxxDataFile 变体有什么用？

A: 当你有一个独立的 JSON 文件只包含某个具体类型的数据时，使用 `XxxDataFile` 变体可以获得正确的智能提示，而不需要添加 `$type` 字段。该变体还支持 `$schema` 属性，方便在文件中直接指定 schema。

### Q: 如何为特定文件夹配置 schema？

A: 在 `.vscode/settings.json` 中使用 `json.schemas` 配置，通过 `fileMatch` 指定文件路径模式。

## 相关文件

- `src/Luban.JsonSchema/CodeTarget/JsonSchemaCodeTarget.cs` - JSON Schema 生成器实现
- `tests/Luban.IntegrationTests/TestData/json_schema_polymorphism/` - 测试用例
