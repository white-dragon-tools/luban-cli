# Luban for Roblox-TS 特性实现计划

**创建日期**: 2026-01-14
**状态**: 待执行

## 实现顺序

```
Phase 1: 字符串枚举类型 (最简单，独立)
  ↓
Phase 2: 工厂函数 (中等复杂度，独立)
  ↓
Phase 3: Flamework Reflect ID (可复用 Phase 2 的逻辑)
  ↓
Phase 4: TypeScript 引用定位 (需要新建代码生成器)
```

---

## Phase 1: 字符串枚举类型

### 实现思路
扩展枚举系统，支持字符串的枚举定义。

### 核心逻辑
1. **Schema 解析**: 根据枚举值(如果含有任意字符串), 则认为是字符串枚举.
2. **类型系统**: 在 DefEnum 中添加 IsStringEnum 判断和 StringValue 存储
3. **编译逻辑**: Compile() 方法中分支处理字符串枚举
4. **数据加载**: 数据加载器需要支持字符串枚举值的解析
5. **Lua 生成**: 模板中生成字符串值而不是整数值

### 关键文件
- `src/Luban.Core/Defs/DefEnum.cs` - 添加字符串枚举支持
- `src/Luban.Lua/Templates/lua-lua/schema.sbn` - 模板支持字符串值
- `src/Luban.Core/DataLoader/` - 数据加载器支持字符串枚举

### 测试策略
创建 `tests/Luban.IntegrationTests/TestData/string_enum/` 测试用例，验证：
- 字符串枚举定义正确解析
- 生成的 Lua 代码包含字符串值
- 数字枚举不受影响
- 数据文件正确导出

---

## Phase 2: 工厂函数 (ObjectFactory)

### 实现思路
在字段级别检测 `ObjectFactory=true` 标签，将字段值包装成 Lua 函数。

### 核心逻辑
1. **标签检测**: 在生成 Lua 数据时，检查 DefField.HasTag("ObjectFactory")
2. **数据访问者**: 修改 ToLuaLiteralVisitor.Accept(DBean) 方法
3. **包装逻辑**: 如果字段有 ObjectFactory 标签，生成 `function() return {...} end`
4. **递归处理**: 需要在字段级别而不是 bean 级别处理

### 关键文件
- `src/Luban.Lua/DataVisitors/ToLuaLiteralVisitor.cs` - 修改数据生成逻辑
- 可能需要创建新的访问者或扩展现有访问者

### 实现难点
- 需要在访问者模式中传递字段信息（当前只传递数据）
- 可能需要重构访问者接口，添加上下文参数

### 测试策略
创建 `tests/Luban.IntegrationTests/TestData/object_factory/` 测试用例，验证：
- 带 ObjectFactory 标签的字段生成函数
- 函数调用返回正确的对象
- 多次调用返回独立的对象副本

---

## Phase 3: Flamework Reflect ID

### 实现思路
在 bean 级别检测 `flameworkId` 标签，生成调用 runtime 库的代码。

### 核心逻辑
1. **标签检测**: 在生成 Lua 数据时，检查 DefBean.HasTag("flameworkId")
2. **代码生成**: 生成 `runtime.createInstance(data, id)` 调用
3. **与工厂函数组合**: 如果同时有 ObjectFactory，先实例化再包装函数
4. **执行顺序**: flameworkId 实例化 → ObjectFactory 包装

### 关键文件
- `src/Luban.Lua/DataVisitors/ToLuaLiteralVisitor.cs` - 扩展数据生成逻辑
- 复用 Phase 2 的访问者扩展

### 实现难点
- 需要处理两个标签的组合使用
- 需要确定正确的执行顺序

### 测试策略
创建测试用例验证：
- flameworkId 单独使用
- flameworkId + ObjectFactory 组合使用
- 生成的代码调用 runtime 库

---

## Phase 4: TypeScript 引用定位

### 实现思路
创建新的 TypeScript 代码生成器，解析 table 的 `type` 标签，生成 import 语句。

### 核心逻辑
1. **创建项目**: 新建 `src/Luban.Typescript/` 项目
2. **代码生成器**: 创建 TypescriptCodeTarget 类
3. **标签解析**: 解析 `type=path(TypeName)` 格式
4. **Import 生成**: 生成 `import { TypeName } from "path"`
5. **类型定义**: 根据 table 类型生成 Map/Array/Singleton 定义
6. **Import 合并**: 同一模块的多个类型合并到一个 import

### 关键文件
- `src/Luban.Typescript/` - 新建项目
- `src/Luban.Typescript/CodeTarget/TypescriptCodeTarget.cs` - 代码生成器
- `src/Luban.Typescript/Templates/` - Scriban 模板
- `src/Luban/Luban.csproj` - 添加项目引用

### 实现难点
- 需要解析复杂的 type 标签格式
- 需要处理相对路径和 node_modules 路径
- 需要合并多个 import 语句

### 测试策略
创建测试用例验证：
- 相对路径引用
- node_modules 引用
- Import 语句正确生成
- 类型定义正确生成

---

## 实现建议

### 开发顺序
1. **Phase 1** 先实现，验证基础架构
2. **Phase 2** 和 **Phase 3** 可以一起实现（共享访问者扩展）
3. **Phase 4** 独立实现，不依赖前面的特性

### 技术债务
- Phase 2/3 可能需要重构访问者模式，添加上下文传递
- 考虑创建统一的标签处理机制

### 测试覆盖
- 每个 Phase 至少一个集成测试
- 测试特性组合使用的场景
- 测试边界情况和错误处理

---

## 下一步行动

选择以下方式之一开始实现：

1. **逐个实现**: 按 Phase 1 → 2 → 3 → 4 顺序实现
2. **并行实现**: Phase 1 和 Phase 4 可以并行（互不依赖）
3. **优先级实现**: 根据业务需求选择最重要的特性先实现

建议从 Phase 1 开始，因为它最简单，可以验证整个开发流程。
