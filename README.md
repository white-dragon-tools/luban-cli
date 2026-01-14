# Luban for Roblox-TS

本项目基于 `luban v4.5 (0203b7a)` 开发，专门为 roblox-ts 项目提供配置编译支持。

> **前置阅读**: 请先阅读 `README.luban.4.5.md` 文档，了解原始 Luban 项目的基础功能。

## 项目定位

- **目标平台**: Roblox-TS 项目
- **输出格式**: JSON 数据文件 + Luau 代码 + TypeScript 定义文件
- **集成方式**: 通过 `rojo` 同步到 Roblox 项目

## 与原版 Luban 的区别

- ✅ 专注于 Roblox Luau 代码生成
- ✅ 提供 TypeScript 定义文件 (`.d.ts`)
- ✅ 支持 Flamework Reflect ID 集成
- ✅ 扩展字符串枚举类型
- ✅ 支持工厂函数模式

---

## 特性详解

### 1. 工厂函数 (ObjectFactory)

#### 使用场景
当需要从同一份配置数据创建多个独立的对象实例时使用。例如：
- 技能效果实例（每次释放技能创建新的效果对象）
- Buff 实例（同一个 Buff 配置可以应用到多个角色）
- 粒子效果实例（同一个配置创建多个粒子）

#### 配置方式
在 bean 的字段上添加标签 `tags="ObjectFactory=true"`

#### Schema 示例
```xml
<bean name="SkillConfig">
    <var name="id" type="int"/>
    <var name="name" type="string"/>
    <!-- 这个字段会被包装成工厂函数 -->
    <var name="effect" type="EffectData" tags="ObjectFactory=true"/>
</bean>

<bean name="EffectData">
    <var name="damage" type="int"/>
    <var name="duration" type="float"/>
</bean>
```

#### 生成代码示例
```lua
-- 生成的 Lua 代码
local config = {
    id = 1001,
    name = "火球术",
    effect = function()
        return {
            damage = 100,
            duration = 3.0
        }
    end
}

-- 使用方式：每次调用都创建新实例
local effect1 = config.effect()
local effect2 = config.effect()
-- effect1 和 effect2 是两个独立的对象
```

#### 注意事项
- 工厂函数是无参数的 `() => object` 形式
- 每次调用返回新的对象副本，互不影响
- 只对字段级别生效，不影响整个 bean

### 2. Flamework Reflect ID

#### 使用场景
当配置数据需要转换为特定的类实例时使用。例如：
- Buff 配置需要实例化为 Buff 类对象
- 技能配置需要实例化为 Skill 类对象
- AI 行为配置需要实例化为 Behavior 类对象

通过 Flamework 的反射机制，可以自动将配置数据转换为对应的类实例，无需手动编写转换代码。

#### 配置方式
在 bean 上添加标签 `tags="flameworkId={id}"`

#### ID 格式说明
ID 格式为：`文件路径@类名`

示例：
- `shared/plugins/buff-system/buff-core-plugin/components/buff-hooks@BuffHooks`
  - 文件路径：`shared/plugins/buff-system/buff-core-plugin/components/buff-hooks`
  - 类名：`BuffHooks`

#### Schema 示例
```xml
<bean name="BuffConfig" tags="flameworkId=shared/plugins/buff-system/buff-core@Buff">
    <var name="id" type="int"/>
    <var name="duration" type="float"/>
    <var name="stackable" type="bool"/>
</bean>
```

#### 生成代码示例
```lua
-- 生成的 Lua 代码会调用 runtime 库
local runtime = require("luban-runtime")

local config = runtime.createInstance(
    {
        id = 1001,
        duration = 5.0,
        stackable = true
    },
    "shared/plugins/buff-system/buff-core@Buff"
)

-- config 现在是 Buff 类的实例，而不是普通 table
```

#### Runtime 库要求
需要提供一个 runtime 库，实现以下功能：
- 根据 reflect id 查找对应的类型
- 调用构造函数：`new(configData, reflectId)`
  - 参数1：配置数据（table）
  - 参数2：reflect id（string）
- 返回类实例

#### 注意事项
- 可以与工厂函数组合使用
- 组合使用时，先通过 flamework 实例化，再包装成工厂函数
- 不需要类满足特定的接口或基类要求


### 3. TypeScript 引用定位

#### 使用场景
当生成的 TypeScript 定义文件需要引用项目中已有的类型时使用。例如：
- 配置表引用已定义的业务类型
- 避免重复定义类型
- 保持类型定义的一致性

#### 配置方式
在 table 上添加标签 `tags="type={路径}({类型名})"`

#### 路径格式
支持两种路径格式：

1. **相对路径**：`shared/plugins/foo(FooType)`
2. **node_modules**：`@rbxts/foo(FooType)`

#### Schema 示例
```xml
<!-- 引用项目中的类型 -->
<table name="TbBuff" value="BuffConfig" input="buffs.json"
       tags="type=shared/plugins/buff-system/buff-core(Buff)"/>

<!-- 引用 node_modules 中的类型 -->
<table name="TbItem" value="ItemConfig" input="items.json"
       tags="type=@rbxts/game-core(Item)"/>
```

#### 生成代码示例
```typescript
// 生成的 .d.ts 文件
import { Buff } from "shared/plugins/buff-system/buff-core";
import { Item } from "@rbxts/game-core";

// 根据 Luban 表类型生成对应的容器类型
export interface TbBuff {
    // Map 表
    get(key: string): Buff | undefined;
    getAll(): Map<string, Buff>;
}

export interface TbItem {
    // List 表
    getAll(): Array<Item>;
}

// 单例表直接使用类型
export const TbConfig: GameConfig;
```

#### 注意事项
- 只需要为 table 配置，不需要为每个 bean 配置
- 生成的容器类型取决于 Luban 的表类型（map/list/singleton）
- 多个 table 引用同一模块的不同类型时，import 语句会自动合并 

### 4. 字符串枚举类型

#### 使用场景
当枚举值需要使用字符串而不是数字时使用。例如：
- 物品类型：`"weapon"`, `"armor"`, `"consumable"`
- 状态标识：`"idle"`, `"running"`, `"jumping"`
- 配置键：`"easy"`, `"normal"`, `"hard"`

字符串枚举在配置文件中更具可读性，也更容易与外部系统集成。

#### 配置方式
在 enum 定义中设置 `value="string"`，并为每个枚举项指定字符串值

#### Schema 示例
```xml
<!-- 字符串枚举 -->
<enum name="ItemType" value="string">
    <var name="Weapon" value="weapon"/>
    <var name="Armor" value="armor"/>
    <var name="Consumable" value="consumable"/>
    <var name="Material" value="material"/>
</enum>

<!-- 数字枚举（原版 Luban 默认） -->
<enum name="ItemRarity" value="int">
    <var name="Common" value="1"/>
    <var name="Rare" value="2"/>
    <var name="Epic" value="3"/>
</enum>
```

#### 生成代码示例
```lua
-- 生成的 Lua 代码
local ItemType = {
    Weapon = "weapon",
    Armor = "armor",
    Consumable = "consumable",
    Material = "material"
}

-- 使用方式
local item = {
    type = ItemType.Weapon,  -- "weapon"
    name = "长剑"
}
```

#### 注意事项
- 必须为每个枚举项显式指定 `value` 属性，否则 Luban 会报错
- 字符串枚举不会生成 TypeScript 定义文件（只生成 table type）
- 在 TypeScript 侧可以使用字符串字面量类型来保证类型安全

---

## 特性组合使用

### ObjectFactory + Flamework Reflect ID

这两个特性可以组合使用，实现"每次调用工厂函数都创建新的类实例"的效果。

#### Schema 示例
```xml
<bean name="SkillConfig">
    <var name="id" type="int"/>
    <var name="name" type="string"/>
    <!-- 组合使用：工厂函数 + Flamework 实例化 -->
    <var name="effect" type="EffectData"
         tags="ObjectFactory=true;flameworkId=shared/effects/effect-core@Effect"/>
</bean>

<bean name="EffectData" tags="flameworkId=shared/effects/effect-core@Effect">
    <var name="damage" type="int"/>
    <var name="duration" type="float"/>
</bean>
```

#### 执行顺序
1. 先通过 Flamework 将配置数据实例化为 Effect 类对象
2. 再将实例化逻辑包装成工厂函数
3. 每次调用工厂函数都会创建新的 Effect 实例

#### 生成代码示例
```lua
local runtime = require("luban-runtime")

local config = {
    id = 1001,
    name = "火球术",
    effect = function()
        return runtime.createInstance(
            {
                damage = 100,
                duration = 3.0
            },
            "shared/effects/effect-core@Effect"
        )
    end
}

-- 使用方式
local effect1 = config.effect()  -- 创建第一个 Effect 实例
local effect2 = config.effect()  -- 创建第二个 Effect 实例
-- effect1 和 effect2 是两个独立的 Effect 类实例
```

### TypeScript 引用定位 + 其他特性

TypeScript 引用定位是在 table 级别配置的，可以与 bean 级别的特性（ObjectFactory、Flamework）自由组合。

#### Schema 示例
```xml
<!-- Table 引用 TypeScript 类型 -->
<table name="TbSkill" value="SkillConfig" input="skills.json"
       tags="type=shared/game-logic/skill-system(Skill)"/>

<!-- Bean 使用 ObjectFactory 和 Flamework -->
<bean name="SkillConfig">
    <var name="id" type="int"/>
    <var name="effect" type="EffectData"
         tags="ObjectFactory=true;flameworkId=shared/effects/effect-core@Effect"/>
</bean>
```

这样生成的 TypeScript 定义会引用 Skill 类型，而 Lua 代码会包含工厂函数和 Flamework 实例化逻辑。

---

## 开发状态

| 特性 | 状态 | 说明 |
|------|------|------|
| 工厂函数 (ObjectFactory) | 🔴 未开始 | 计划中 |
| Flamework Reflect ID | 🔴 未开始 | 计划中 |
| TypeScript 引用定位 | 🔴 未开始 | 计划中 |
| 字符串枚举类型 | 🔴 未开始 | 计划中 |

---

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

### 代码格式化

```bash
cd scripts
./format.sh    # Linux/Mac
format.bat     # Windows
```

---

## 项目结构

```
luban/
├── src/
│   ├── Luban/                      # CLI 入口
│   ├── Luban.Core/                 # 核心框架
│   ├── Luban.Lua/                  # Lua 代码生成器
│   ├── Luban.DataLoader.Builtin/   # 数据加载器
│   ├── Luban.DataValidator.Builtin/# 数据验证器
│   └── Luban.DataTarget.Builtin/   # 数据导出器
├── tests/
│   └── Luban.IntegrationTests/     # 集成测试
├── scripts/                        # 构建脚本
├── CLAUDE.md                       # 项目架构文档
├── README.luban.4.5.md            # 原版 Luban 文档
└── readme.md                       # 本文档
```

---

## 相关资源

- **原版 Luban 文档**: [README.luban.4.5.md](./README.luban.4.5.md)
- **项目架构文档**: [CLAUDE.md](./CLAUDE.md)
- **官方文档**: https://www.datable.cn/
- **示例项目**: https://github.com/focus-creative-games/luban_examples

---

## 贡献指南

### 添加新特性

1. 在对应的代码生成器项目中实现功能（如 `Luban.Lua/`）
2. 在 `tests/Luban.IntegrationTests/TestData/` 添加测试用例
3. 运行测试确保功能正常
4. 更新本文档

### 代码规范

- 使用 .NET 8.0
- 提交前运行 `dotnet format`
- 遵循现有代码风格

---

## 许可证

基于原版 Luban 项目开发，遵循相同的许可证。