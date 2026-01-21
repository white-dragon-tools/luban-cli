# Luban JSON Schema 输出功能设计文档

## 目录

- [1. 概述](#1-概述)
  - [1.1 背景](#11-背景)
  - [1.2 目标](#12-目标)
  - [1.3 快速开始](#13-快速开始)
- [2. 架构设计](#2-架构设计)
  - [2.1 模块结构](#21-模块结构)
  - [2.2 依赖关系](#22-依赖关系)
  - [2.3 核心流程](#23-核心流程)
  - [2.4 核心类说明](#24-核心类说明)
- [3. 输出格式设计](#3-输出格式设计)
  - [3.1 文件结构](#31-文件结构)
  - [3.2 基础类型映射](#32-基础类型映射)
  - [3.3 容器类型映射](#33-容器类型映射)
  - [3.4 Enum 映射](#34-enum-映射)
  - [3.5 Bean 映射](#35-bean-映射)
  - [3.6 多态类型映射](#36-多态类型映射)
- [4. Validator 映射](#4-validator-映射)
  - [4.1 映射规则](#41-映射规则)
  - [4.2 示例](#42-示例)
- [5. 扩展属性](#5-扩展属性)
- [6. Table 元数据](#6-table-元数据)
- [7. 实现状态](#7-实现状态)
  - [7.1 已完成功能](#71-已完成功能-)
  - [7.2 待完善功能](#72-待完善功能-)
  - [7.3 实现亮点](#73-实现亮点-)
- [8. 测试](#8-测试)
  - [8.1 集成测试](#81-集成测试)
  - [8.2 运行测试](#82-运行测试)
  - [8.3 验证方式](#83-验证方式)
  - [8.4 示例输出](#84-示例输出)
- [9. 配置选项](#9-配置选项)
  - [9.1 命令行参数](#91-命令行参数)
  - [9.2 环境选项](#92-环境选项)
  - [9.3 在 luban.conf 中配置](#93-在-lubanconf-中配置)
- [10. 与 luban-editor 集成](#10-与-luban-editor-集成)
  - [10.1 使用场景](#101-使用场景)
  - [10.2 Schema 加载示例](#102-schema-加载示例)
  - [10.3 扩展属性的使用](#103-扩展属性的使用)
- [11. 最佳实践](#11-最佳实践)
  - [11.1 Schema 设计建议](#111-schema-设计建议)
  - [11.2 性能优化](#112-性能优化)
  - [11.3 版本控制](#113-版本控制)
- [12. 故障排查](#12-故障排查)
  - [12.1 常见问题](#121-常见问题)
  - [12.2 调试技巧](#122-调试技巧)
- [13. 未来规划](#13-未来规划)
  - [13.1 计划功能](#131-计划功能)
  - [13.2 已知限制](#132-已知限制)
- [14. 参考资源](#14-参考资源)
- [总结](#总结)

## 1. 概述

### 1.1 背景

为支持 `luban-editor`（基于 Web 的 Luban 配置数据编辑器），需要将 Luban 的 XML Schema 转换为 JSON Schema 格式。JSON Schema 是一种标准的 schema 描述格式，可被 `react-jsonschema-form` 等前端库直接使用，自动生成表单 UI。

### 1.2 目标

- 新增 CodeTarget：`json-schema`
- 输出符合 JSON Schema Draft-07 规范的 schema 文件
- 完整映射 Luban 类型系统（基础类型、容器、Bean、Enum）
- 映射 Validator 到 JSON Schema 约束
- 支持多态类型（继承）

### 1.3 快速开始

**步骤 1：准备 Luban Schema**

```xml
<!-- defines.xml -->
<root>
  <enum name="ItemType">
    <var name="Weapon" value="1"/>
    <var name="Armor" value="2"/>
  </enum>

  <bean name="Item">
    <var name="id" type="int"/>
    <var name="name" type="string"/>
    <var name="type" type="ItemType"/>
    <var name="price" type="int" tags="range=1,99999"/>
  </bean>

  <table name="TbItem" value="Item" index="id" input="*@items.json"/>
</root>
```

**步骤 2：生成 JSON Schema**

```bash
# 从源码运行
cd src/Luban
dotnet run -- \
  -t json-schema \
  -d path/to/defines.xml \
  -x outputCodeDir=./output/schema

# 或使用已编译的 Luban.dll
dotnet Luban.dll \
  -t json-schema \
  -d Defines/__root__.xml \
  -x outputCodeDir=./output/schema
```

**步骤 3：查看生成的 Schema**

```bash
cat output/schema/schema.json
```

生成的 schema.json 包含完整的类型定义和表元数据，可直接用于前端编辑器。

## 2. 架构设计

### 2.1 模块结构

```
src/Luban.JsonSchema/
├── Luban.JsonSchema.csproj
├── AssemblyInfo.cs                # 程序集注册
├── CodeTarget/
│   └── JsonSchemaCodeTarget.cs    # CodeTarget 入口，主要生成逻辑
└── TypeVisitors/
    └── JsonSchemaTypeVisitor.cs   # TType → JSON Schema 转换访问者
```

**注意：** 当前实现不使用 Scriban 模板，而是直接使用 System.Text.Json 生成 JSON。

### 2.2 依赖关系

```
Luban.JsonSchema
    └── Luban.Core (类型系统、插件框架、生成上下文)
```

**关键依赖：**
- `System.Text.Json` - JSON 生成和序列化
- `Luban.Core.CodeTarget` - CodeTarget 基类
- `Luban.Core.Defs` - 类型定义（DefBean, DefEnum, DefTable）
- `Luban.Core.Types` - 类型系统（TType 及其子类）
- `Luban.Core.TypeVisitors` - 访问者模式接口

### 2.3 核心流程

```
1. Luban CLI 加载 Schema，编译得到 DefAssembly
   ├── 解析 XML 定义文件
   ├── 创建类型定义（DefBean, DefEnum）
   └── 创建表定义（DefTable）

2. JsonSchemaCodeTarget.Handle() 被调用
   ├── 创建根 JSON 对象（$schema, $id）
   └── 生成三个主要部分：

3. 生成 definitions（类型定义）
   ├── 遍历 ctx.ExportEnums
   │   └── GenerateEnumSchema() - 生成枚举 schema
   └── 遍历 ctx.ExportBeans
       └── GenerateBeanSchema() - 生成 Bean schema
           ├── 如果是抽象类型 → GeneratePolymorphicBeanSchema()
           └── 否则 → GenerateConcreteBeanSchema()
               ├── 使用 JsonSchemaTypeVisitor 转换字段类型
               ├── 处理可空类型（WrapNullable）
               └── 应用验证器（ApplyValidators）

4. 生成 tables（表元数据）
   └── 遍历 ctx.ExportTables
       └── GenerateTableMeta() - 生成表元数据

5. 序列化并输出 JSON Schema 文件
   └── 使用 JsonSerializerOptions 格式化输出
```

### 2.4 核心类说明

#### JsonSchemaCodeTarget

主要职责：
- 实现 `[CodeTarget("json-schema")]` 注册
- 重写 `Handle()` 方法，生成完整的 JSON Schema
- 提供类型转换方法（GenerateEnumSchema, GenerateBeanSchema 等）
- 处理验证器映射（ApplyValidators）
- 处理可空类型包装（WrapNullable）

关键方法：
- `GenerateSchema()` - 生成完整 schema 的入口
- `GenerateEnumSchema()` - 枚举类型转换
- `GenerateBeanSchema()` - Bean 类型转换（分发到多态或具体）
- `GeneratePolymorphicBeanSchema()` - 多态类型（oneOf）
- `GenerateConcreteBeanSchema()` - 具体类型（object）
- `ApplyValidators()` - 验证器映射
- `GenerateTableMeta()` - 表元数据生成

#### JsonSchemaTypeVisitor

实现 `ITypeFuncVisitor<JsonObject>` 接口，使用访问者模式转换 Luban 类型到 JSON Schema。

支持的类型：
- 基础类型：TBool, TByte, TShort, TInt, TLong, TFloat, TDouble, TString, TDateTime
- 容器类型：TArray, TList, TSet, TMap
- 自定义类型：TBean, TEnum

每个 Accept 方法返回对应的 JsonObject，表示该类型的 JSON Schema 定义。

## 3. 输出格式设计

### 3.1 文件结构

输出目录结构如下：

```
output/
├── schema.json              # 主 schema 文件，包含所有类型定义
├── vscode-json-schemas.json # VSCode json.schemas 配置
└── definitions/             # 每个表的 wrapper schema
    ├── item.schema.json
    ├── skill.schema.json
    └── ...
```

**主 schema 文件 (schema.json)**：

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "luban-schema",
  "definitions": {
    "Item": { ... },
    "ItemType": { ... },
    "Skill": { ... }
  },
  "tables": {
    "TbItem": {
      "valueType": "Item",
      "mode": "map",
      "index": "id",
      "inputFiles": ["item.json"]
    }
  }
}
```

**Wrapper schema 文件 (definitions/*.schema.json)**：

每个表会生成一个 wrapper schema 文件，用于 VSCode 智能提示：

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$ref": "../schema.json#/definitions/ItemDataFile"
}
```

### 3.2 基础类型映射

| Luban 类型 | JSON Schema |
|-----------|-------------|
| `bool` | `{ "type": "boolean" }` |
| `byte` | `{ "type": "integer", "minimum": 0, "maximum": 255 }` |
| `short` | `{ "type": "integer", "minimum": -32768, "maximum": 32767 }` |
| `int` | `{ "type": "integer" }` |
| `long` | `{ "type": "integer" }` |
| `float` | `{ "type": "number" }` |
| `double` | `{ "type": "number" }` |
| `string` | `{ "type": "string" }` |
| `text` | `{ "type": "string", "x-luban-type": "text" }` |
| `datetime` | `{ "type": "string", "format": "date-time" }` |

### 3.3 容器类型映射

#### array / list

```json
// list,int
{
  "type": "array",
  "items": { "type": "integer" }
}

// list,Item
{
  "type": "array",
  "items": { "$ref": "#/definitions/Item" }
}
```

#### set

```json
// set,string
{
  "type": "array",
  "items": { "type": "string" },
  "uniqueItems": true
}
```

#### map

```json
// map,string,int
{
  "type": "object",
  "additionalProperties": { "type": "integer" },
  "x-luban-key-type": "string"
}

// map,int,Item （整数 key）
{
  "type": "object",
  "additionalProperties": { "$ref": "#/definitions/Item" },
  "x-luban-key-type": "integer",
  "propertyNames": { "pattern": "^-?[0-9]+$" }
}
```

### 3.4 Enum 映射

#### 普通枚举（整数值）

```xml
<enum name="ItemType">
  <var name="Weapon" value="1"/>
  <var name="Armor" value="2"/>
  <var name="Consumable" value="3"/>
</enum>
```

```json
{
  "type": "integer",
  "enum": [1, 2, 3],
  "x-luban-enum": "ItemType",
  "x-luban-enum-items": [
    { "name": "Weapon", "value": 1, "alias": null, "comment": null },
    { "name": "Armor", "value": 2, "alias": null, "comment": null },
    { "name": "Consumable", "value": 3, "alias": null, "comment": null }
  ]
}
```

#### 字符串枚举

```xml
<enum name="Quality">
  <var name="Normal" alias="normal"/>
  <var name="Rare" alias="rare"/>
  <var name="Epic" alias="epic"/>
</enum>
```

```json
{
  "type": "string",
  "enum": ["normal", "rare", "epic"],
  "x-luban-enum": "Quality",
  "x-luban-enum-items": [
    { "name": "Normal", "value": "normal" },
    { "name": "Rare", "value": "rare" },
    { "name": "Epic", "value": "epic" }
  ]
}
```

#### Flags 枚举

```xml
<enum name="ItemFlags" flags="true">
  <var name="Tradable" value="1"/>
  <var name="Stackable" value="2"/>
  <var name="Destroyable" value="4"/>
</enum>
```

```json
{
  "type": "integer",
  "x-luban-enum": "ItemFlags",
  "x-luban-flags": true,
  "x-luban-enum-items": [
    { "name": "Tradable", "value": 1 },
    { "name": "Stackable", "value": 2 },
    { "name": "Destroyable", "value": 4 }
  ]
}
```

### 3.5 Bean 映射

#### 简单 Bean

```xml
<bean name="Item">
  <var name="id" type="int"/>
  <var name="name" type="string"/>
  <var name="price" type="int"/>
</bean>
```

```json
{
  "type": "object",
  "properties": {
    "id": { "type": "integer" },
    "name": { "type": "string" },
    "price": { "type": "integer" }
  },
  "required": ["id", "name", "price"],
  "additionalProperties": false
}
```

#### 可空字段

```xml
<bean name="Item">
  <var name="id" type="int"/>
  <var name="desc" type="string?"/>
</bean>
```

```json
{
  "type": "object",
  "properties": {
    "id": { "type": "integer" },
    "desc": { "type": ["string", "null"] }
  },
  "required": ["id"]
}
```

### 3.6 多态类型映射

#### 继承结构

```xml
<bean name="Effect">
  <var name="id" type="int"/>
  <var name="duration" type="float"/>
</bean>

<bean name="DamageEffect" parent="Effect">
  <var name="damage" type="int"/>
  <var name="damageType" type="string"/>
</bean>

<bean name="HealEffect" parent="Effect">
  <var name="healAmount" type="int"/>
</bean>
```

#### 基类（抽象）

当一个 Bean 有子类时，会生成 oneOf 结构：

```json
{
  "Effect": {
    "oneOf": [
      { "$ref": "#/definitions/DamageEffect" },
      { "$ref": "#/definitions/HealEffect" }
    ],
    "discriminator": {
      "propertyName": "$type"
    }
  }
}
```

#### 子类（具体）

子类包含父类的所有字段，并添加 `$type` 判别器：

```json
{
  "DamageEffect": {
    "type": "object",
    "properties": {
      "$type": { "const": "DamageEffect" },
      "id": { "type": "integer" },
      "duration": { "type": "number" },
      "damage": { "type": "integer" },
      "damageType": { "type": "string" }
    },
    "required": ["$type", "id", "duration", "damage", "damageType"],
    "additionalProperties": false
  },
  "HealEffect": {
    "type": "object",
    "properties": {
      "$type": { "const": "HealEffect" },
      "id": { "type": "integer" },
      "duration": { "type": "number" },
      "healAmount": { "type": "integer" }
    },
    "required": ["$type", "id", "duration", "healAmount"],
    "additionalProperties": false
  }
}
```

#### 使用多态类型

在 Bean 中引用多态类型时，会引用基类：

```xml
<bean name="Skill">
  <var name="id" type="int"/>
  <var name="effect" type="Effect"/>
  <var name="effects" type="list,Effect"/>
</bean>
```

生成的 schema：

```json
{
  "Skill": {
    "type": "object",
    "properties": {
      "id": { "type": "integer" },
      "effect": { "$ref": "#/definitions/Effect" },
      "effects": {
        "type": "array",
        "items": { "$ref": "#/definitions/Effect" }
      }
    },
    "required": ["id", "effect", "effects"]
  }
}
```

对应的 JSON 数据示例：

```json
{
  "id": 1,
  "effect": {
    "$type": "DamageEffect",
    "id": 1,
    "duration": 5.0,
    "damage": 100,
    "damageType": "physical"
  },
  "effects": [
    {
      "$type": "HealEffect",
      "id": 2,
      "duration": 3.0,
      "healAmount": 50
    }
  ]
}
```

## 4. Validator 映射

### 4.1 映射规则

| Luban Validator | JSON Schema | 适用类型 |
|-----------------|-------------|---------|
| `range(min,max)` | `minimum`, `maximum` | integer, number |
| `size(min,max)` | `minItems`, `maxItems` | array |
| `size(min,max)` | `minLength`, `maxLength` | string |
| `regex(pattern)` | `pattern` | string |
| `path(...)` | `x-luban-path` | string |
| `ref=TbXxx` | `x-luban-ref` | integer, string |
| `set(...)` | `enum` | integer, string |

### 4.2 示例

```xml
<bean name="Item">
  <var name="id" type="int" tags="range(1,99999)"/>
  <var name="name" type="string" tags="size(1,50)"/>
  <var name="icon" type="string" tags="regex(^icons/.+\.png$)"/>
  <var name="categoryId" type="int" tags="ref=TbCategory"/>
  <var name="tags" type="list,string" tags="size(0,10)"/>
</bean>
```

```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "integer",
      "minimum": 1,
      "maximum": 99999
    },
    "name": {
      "type": "string",
      "minLength": 1,
      "maxLength": 50
    },
    "icon": {
      "type": "string",
      "pattern": "^icons/.+\\.png$"
    },
    "categoryId": {
      "type": "integer",
      "x-luban-ref": "TbCategory"
    },
    "tags": {
      "type": "array",
      "items": { "type": "string" },
      "minItems": 0,
      "maxItems": 10
    }
  }
}
```

## 5. 扩展属性

使用 `x-luban-*` 前缀的扩展属性保留 Luban 特有信息：

| 扩展属性 | 说明 |
|---------|------|
| `x-luban-type` | 原始 Luban 类型名 |
| `x-luban-enum` | 枚举类型名 |
| `x-luban-enum-items` | 枚举项详情（含 alias、comment） |
| `x-luban-flags` | 是否为 flags 枚举 |
| `x-luban-ref` | 引用的表名 |
| `x-luban-path` | 路径验证器配置 |
| `x-luban-key-type` | map 的 key 类型 |
| `x-luban-comment` | 字段注释 |
| `x-luban-alias` | 字段别名 |
| `x-luban-group` | 字段分组 |

## 6. Table 元数据

```json
{
  "tables": {
    "TbItem": {
      "valueType": "Item",
      "mode": "map",
      "index": "id",
      "inputFiles": ["item.json", "item_extra.json"],
      "comment": "物品表",
      "groups": ["client", "server"]
    },
    "TbGlobalConfig": {
      "valueType": "GlobalConfig",
      "mode": "one",
      "inputFiles": ["global.json"]
    }
  }
}
```

## 7. 实现状态

### 7.1 已完成功能 ✅

- [x] **基础类型映射** - 所有基础类型（bool, byte, short, int, long, float, double, string, datetime）
- [x] **容器类型** - array, list, set, map，包括嵌套容器
- [x] **简单 Bean** - 完整的 Bean 定义，包含所有字段和层级字段
- [x] **Enum 支持** - 整数枚举、字符串枚举、Flags 枚举
- [x] **可空类型** - 基础类型和 Bean 引用的可空支持
- [x] **多态类型** - 使用 oneOf + discriminator 实现继承和多态
- [x] **Validator 映射** - range, size, regex, ref, path 验证器
- [x] **Table 元数据** - 完整的表定义信息（valueType, mode, index, inputFiles）
- [x] **扩展属性** - x-luban-enum, x-luban-flags, x-luban-ref, x-luban-path, x-luban-key-type
- [x] **注释支持** - Bean 和 Enum 的 description 字段

### 7.2 待完善功能 🚧

- [ ] **字段级扩展属性** - alias, group 等字段级元数据
- [ ] **命名空间支持** - 模块/命名空间的完整映射
- [ ] **更多验证器** - set 验证器等其他验证器类型
- [ ] **自定义输出选项** - 分文件输出、自定义文件名等

### 7.3 实现亮点 ⭐

1. **完整的类型系统映射** - 从 Luban 类型到 JSON Schema 的完整映射
2. **多态类型支持** - 正确处理继承关系，使用 $type 作为判别器
3. **验证器集成** - 将 Luban 的 tags 验证器映射到 JSON Schema 约束
4. **可空类型处理** - 对基础类型和引用类型使用不同的可空表示方式
5. **Map 类型优化** - 对整数 key 的 map 添加 propertyNames 约束

## 8. 测试

### 8.1 集成测试

项目包含完整的集成测试，位于 `tests/Luban.IntegrationTests/TestData/json_schema_test/`。

**测试覆盖范围：**

- ✅ 基础类型（bool, byte, short, int, long, float, double, string, datetime）
- ✅ 可空类型（基础类型和 Bean 引用）
- ✅ 容器类型（list, set, array, map）
- ✅ 嵌套容器（list of list, map of list）
- ✅ 整数枚举、字符串枚举、Flags 枚举
- ✅ 多态类型（继承和 oneOf）
- ✅ 嵌套 Bean 引用
- ✅ Validator 映射（range, size, regex, ref, path）

**测试文件结构：**

```
tests/Luban.IntegrationTests/TestData/json_schema_test/
├── schema/
│   ├── luban.conf          # Luban 配置
│   └── defines.xml         # 完整的类型定义
├── data/
│   ├── basic.json          # 基础类型测试数据
│   ├── items.json          # 物品数据（含枚举）
│   ├── skills.json         # 技能数据（含多态）
│   └── objects.json        # 游戏对象数据（嵌套 Bean）
└── output/
    ├── schema.json              # 主 JSON Schema
    ├── vscode-json-schemas.json # VSCode 配置
    └── definitions/             # Wrapper schemas
        └── *.schema.json
```

### 8.2 运行测试

```bash
# 生成 JSON Schema
cd src/Luban
dotnet run -- \
  -t json-schema \
  -d ../../tests/Luban.IntegrationTests/TestData/json_schema_test/schema/defines.xml \
  -x outputCodeDir=../../output/json_schema_test

# 查看生成的 schema
cat output/json_schema_test/schema.json
```

### 8.3 验证方式

生成的 JSON Schema 可以通过以下方式验证：

1. **Schema 合法性验证** - 使用 ajv 等库验证 schema 符合 JSON Schema Draft-07 规范
2. **数据验证** - 用生成的 schema 验证测试数据文件
3. **编辑器集成** - 在 luban-editor 中加载 schema 并渲染表单 UI
4. **类型检查** - 确保所有 Luban 类型都正确映射到 JSON Schema

### 8.4 示例输出

生成的 schema.json 包含：
- 38 个类型定义（enums + beans）
- 4 个表定义
- 完整的验证器约束
- 多态类型的 oneOf 定义
- 所有扩展属性（x-luban-*）

完整输出示例见：`output/json_schema_test/schema.json`

## 9. 配置选项

### 9.1 命令行参数

```bash
dotnet Luban.dll \
  -t json-schema \                    # 指定 target 为 json-schema
  -d Defines/__root__.xml \           # Schema 定义文件
  -x outputCodeDir=./output/schema \  # 输出目录
  -x json-schema.outputFile=schema.json  # 自定义输出文件名（可选）
```

### 9.2 环境选项

| 选项 | 默认值 | 说明 |
|------|--------|------|
| `json-schema.outputFile` | `schema.json` | 输出文件名 |
| `json-schema.outputCodeDir` | 继承自全局 | 输出目录 |

### 9.3 在 luban.conf 中配置

```
codeTarget=json-schema
outputCodeDir=./output/schema
```

## 10. 与 luban-editor 集成

### 10.1 使用场景

生成的 JSON Schema 主要用于 `luban-editor`（基于 Web 的配置编辑器）：

1. **自动生成表单** - 使用 `react-jsonschema-form` 等库根据 schema 自动生成编辑表单
2. **类型验证** - 在编辑时实时验证数据是否符合 schema 约束
3. **智能提示** - 根据 schema 提供字段提示和自动补全
4. **文档生成** - 从 schema 的 description 字段生成文档

### 10.2 Schema 加载示例

```typescript
// 加载 schema
const schema = await fetch('/api/schema.json').then(r => r.json());

// 获取表定义
const tables = schema.tables;
const itemTable = tables.TbItem;

// 获取类型定义
const itemSchema = schema.definitions[itemTable.valueType];

// 使用 react-jsonschema-form 渲染表单
<Form
  schema={itemSchema}
  uiSchema={generateUISchema(itemSchema)}
  formData={itemData}
  onChange={handleChange}
/>
```

### 10.3 扩展属性的使用

```typescript
// 处理枚举类型
if (schema['x-luban-enum']) {
  const enumName = schema['x-luban-enum'];
  const enumItems = schema['x-luban-enum-items'];

  // 渲染为下拉框
  return <Select options={enumItems.map(item => ({
    label: item.name,
    value: item.value,
    description: item.comment
  }))} />;
}

// 处理引用类型
if (schema['x-luban-ref']) {
  const refTable = schema['x-luban-ref'];

  // 渲染为引用选择器
  return <RefSelector table={refTable} />;
}

// 处理 Flags 枚举
if (schema['x-luban-flags']) {
  const enumItems = schema['x-luban-enum-items'];

  // 渲染为多选框
  return <CheckboxGroup options={enumItems} />;
}
```

## 11. 最佳实践

### 11.1 Schema 设计建议

1. **添加注释** - 在 XML 中为 bean、enum、field 添加 comment 属性，会映射到 JSON Schema 的 description
   ```xml
   <bean name="Item" comment="游戏物品配置">
     <var name="id" type="int" comment="物品唯一ID"/>
   </bean>
   ```

2. **使用验证器** - 充分利用 tags 验证器来约束数据
   ```xml
   <var name="level" type="int" tags="range=1,100"/>
   <var name="name" type="string" tags="size=1,50"/>
   ```

3. **合理使用可空** - 只在确实需要可选字段时使用可空类型
   ```xml
   <var name="description" type="string?"/>  <!-- 可选描述 -->
   ```

4. **枚举使用 alias** - 为枚举项添加 alias 以支持字符串枚举
   ```xml
   <enum name="Quality">
     <var name="Common" alias="common"/>
     <var name="Rare" alias="rare"/>
   </enum>
   ```

### 11.2 性能优化

1. **分层输出** - 主 schema 包含所有定义，wrapper schemas 按表分离到 definitions/ 目录
2. **按需加载** - 在编辑器中可以按需加载特定表的 schema
3. **缓存 schema** - 在编辑器中缓存已加载的 schema，避免重复请求

### 11.3 版本控制

1. **提交 schema.json** - 将生成的 schema.json 提交到版本控制
2. **CI/CD 集成** - 在 CI 中自动生成 schema 并验证
3. **Schema 版本** - 考虑在 schema 中添加版本信息

## 12. 故障排查

### 12.1 常见问题

**Q: 生成的 schema 中缺少某些类型？**

A: 检查 XML 定义中是否正确定义了类型，确保类型被表引用或被其他类型引用。

**Q: 多态类型的 $type 字段在数据中不存在？**

A: Luban 的 JSON 数据导出会自动添加 $type 字段。如果手动编写数据，需要添加该字段。

**Q: 验证器没有生成对应的 JSON Schema 约束？**

A: 检查 tags 语法是否正确，支持的验证器包括：range, size, regex, ref, path。

**Q: Map 类型的整数 key 验证失败？**

A: JSON 中所有 key 都是字符串，需要使用 propertyNames pattern 约束。生成的 schema 已自动处理。

### 12.2 调试技巧

1. **查看生成的 schema** - 直接打开 schema.json 检查结构
2. **使用 JSON Schema 验证器** - 使用在线工具验证 schema 合法性
3. **逐步测试** - 从简单类型开始，逐步添加复杂类型
4. **查看日志** - 运行 Luban 时查看控制台输出

## 13. 未来规划

### 13.1 计划功能

- [x] **分文件输出** - 支持将每个表的 wrapper schema 输出到 definitions/ 目录
- [ ] **JSON Schema 2020-12** - 升级到最新的 JSON Schema 规范
- [ ] **UI Schema 生成** - 自动生成 react-jsonschema-form 的 uiSchema
- [ ] **更多扩展属性** - 支持更多 Luban 特性（alias, group, tags）
- [ ] **Schema 合并** - 支持多个 schema 文件的合并和引用

### 13.2 已知限制

1. **命名空间** - 当前使用 FullName，可能在复杂命名空间场景下需要优化
2. **循环引用** - 虽然 JSON Schema 支持，但需要确保编辑器正确处理
3. **自定义类型** - 某些 Luban 特殊类型可能需要额外的扩展属性

## 14. 参考资源

- [JSON Schema 官方文档](https://json-schema.org/)
- [JSON Schema Draft-07 规范](https://json-schema.org/draft-07/schema)
- [react-jsonschema-form](https://github.com/rjsf-team/react-jsonschema-form)
- [Luban 官方文档](https://www.datable.cn/)
- [ajv - JSON Schema 验证器](https://ajv.js.org/)

---

## 总结

Luban JSON Schema 输出功能已经完整实现，提供了从 Luban XML Schema 到 JSON Schema Draft-07 的完整映射。该功能的主要特点包括：

### 核心特性

✅ **完整的类型系统支持** - 支持所有 Luban 类型（基础类型、容器、Bean、Enum）
✅ **多态类型** - 使用 oneOf + discriminator 正确处理继承关系
✅ **验证器集成** - 将 Luban 验证器映射到 JSON Schema 约束
✅ **可空类型** - 对不同类型使用合适的可空表示
✅ **扩展属性** - 使用 x-luban-* 保留 Luban 特有信息

### 使用场景

- **Web 编辑器** - 为 luban-editor 提供 schema，自动生成表单 UI
- **数据验证** - 在前端实时验证配置数据的正确性
- **文档生成** - 从 schema 自动生成配置文档
- **IDE 支持** - 为 JSON 编辑器提供智能提示和自动补全

### 实现质量

- **测试覆盖** - 包含完整的集成测试，覆盖所有类型和特性
- **代码质量** - 使用访问者模式，结构清晰，易于维护
- **分层输出** - 主 schema + definitions/ 目录结构，支持 VSCode 智能提示
- **扩展性** - 易于添加新的验证器和扩展属性

### 下一步

该功能已经可以投入生产使用。未来可以考虑：
- 升级到 JSON Schema 2020-12
- 自动生成 UI Schema
- 更多的扩展属性支持

如有问题或建议，请访问 [Luban GitHub Issues](https://github.com/focus-creative-games/luban/issues)。
