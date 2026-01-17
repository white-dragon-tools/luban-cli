# Luban JSON Schema 输出功能设计文档

## 1. 概述

### 1.1 背景

为支持 `luban-editor`（基于 Web 的 Luban 配置数据编辑器），需要将 Luban 的 XML Schema 转换为 JSON Schema 格式。JSON Schema 是一种标准的 schema 描述格式，可被 `react-jsonschema-form` 等前端库直接使用，自动生成表单 UI。

### 1.2 目标

- 新增 CodeTarget：`json-schema`
- 输出符合 JSON Schema Draft-07 规范的 schema 文件
- 完整映射 Luban 类型系统（基础类型、容器、Bean、Enum）
- 映射 Validator 到 JSON Schema 约束
- 支持多态类型（继承）

### 1.3 使用方式

```bash
dotnet Luban.dll \
  -t json-schema \
  -d Defines/__root__.xml \
  -x outputCodeDir=./output/schema
```

## 2. 架构设计

### 2.1 模块结构

```
src/Luban.JsonSchema/
├── Luban.JsonSchema.csproj
├── CodeTarget/
│   └── JsonSchemaCodeTarget.cs    # CodeTarget 入口
├── TypeVisitors/
│   └── JsonSchemaTypeVisitor.cs   # TType → JSON Schema 转换
└── Templates/
    └── json-schema/
        └── schema.sbn             # Scriban 模板（可选）
```

### 2.2 依赖关系

```
Luban.JsonSchema
    └── Luban.Core
```

### 2.3 核心流程

```
1. Luban CLI 加载 Schema，编译得到 DefAssembly
2. JsonSchemaCodeTarget.Handle() 被调用
3. 遍历 DefAssembly 中的 Beans、Enums、Tables
4. 使用 JsonSchemaTypeVisitor 转换每个类型
5. 输出 JSON Schema 文件
```

## 3. 输出格式设计

### 3.1 文件结构

输出单个 `schema.json` 文件，包含所有类型定义：

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
<bean name="Shape">
  <var name="id" type="int"/>
</bean>

<bean name="Circle" parent="Shape">
  <var name="radius" type="float"/>
</bean>

<bean name="Rectangle" parent="Shape">
  <var name="width" type="float"/>
  <var name="height" type="float"/>
</bean>
```

#### 基类（抽象）

```json
{
  "Shape": {
    "oneOf": [
      { "$ref": "#/definitions/Circle" },
      { "$ref": "#/definitions/Rectangle" }
    ],
    "discriminator": {
      "propertyName": "$type"
    }
  }
}
```

#### 子类（具体）

```json
{
  "Circle": {
    "type": "object",
    "properties": {
      "$type": { "const": "Circle" },
      "id": { "type": "integer" },
      "radius": { "type": "number" }
    },
    "required": ["$type", "id", "radius"],
    "additionalProperties": false
  },
  "Rectangle": {
    "type": "object",
    "properties": {
      "$type": { "const": "Rectangle" },
      "id": { "type": "integer" },
      "width": { "type": "number" },
      "height": { "type": "number" }
    },
    "required": ["$type", "id", "width", "height"],
    "additionalProperties": false
  }
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

## 7. MVP 范围

### 7.1 第一阶段（MVP）

- [x] 基础类型映射
- [x] 容器类型（array, list, set, map）
- [x] 简单 Bean
- [x] 简单 Enum（整数、字符串）
- [x] 可空类型
- [x] Table 元数据

### 7.2 第二阶段

- [ ] 多态类型（oneOf + discriminator）
- [ ] Validator 映射（range, size, regex）
- [ ] 扩展属性（comment, alias, group）

### 7.3 第三阶段

- [ ] ref 验证器（x-luban-ref）
- [ ] path 验证器
- [ ] flags 枚举完整支持
- [ ] 命名空间/模块支持

## 8. 测试计划

### 8.1 Mock Schema

创建测试用的 XML schema，覆盖各种类型组合：

```xml
<!-- test_schema.xml -->
<module name="test">
  <!-- 基础类型 -->
  <bean name="BasicTypes">
    <var name="boolVal" type="bool"/>
    <var name="intVal" type="int"/>
    <var name="floatVal" type="float"/>
    <var name="strVal" type="string"/>
    <var name="dateVal" type="datetime"/>
  </bean>

  <!-- 容器类型 -->
  <bean name="ContainerTypes">
    <var name="intList" type="list,int"/>
    <var name="strSet" type="set,string"/>
    <var name="intMap" type="map,string,int"/>
    <var name="nested" type="list,map,string,int"/>
  </bean>

  <!-- 枚举 -->
  <enum name="Color">
    <var name="Red" value="1"/>
    <var name="Green" value="2"/>
    <var name="Blue" value="3"/>
  </enum>

  <!-- 引用枚举 -->
  <bean name="ColoredItem">
    <var name="id" type="int"/>
    <var name="color" type="Color"/>
  </bean>

  <!-- 可空 -->
  <bean name="NullableFields">
    <var name="required" type="int"/>
    <var name="optional" type="int?"/>
    <var name="optionalStr" type="string?"/>
  </bean>

  <!-- 表定义 -->
  <table name="TbBasicTypes" value="BasicTypes" input="basic.json"/>
  <table name="TbColoredItem" value="ColoredItem" index="id" input="colored.json"/>
</module>
```

### 8.2 验证方式

1. 生成 JSON Schema
2. 使用 ajv 等库验证 schema 合法性
3. 用 schema 验证示例数据
4. 在 luban-editor 中加载并渲染表单
