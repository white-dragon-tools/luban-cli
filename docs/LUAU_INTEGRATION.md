# Luau 集成检查

本目录包含用于检查 Luban 生成的 Lua 代码的 Luau 验证工具。

## 功能

- **自动发现 Luau 分析器**：在 PATH、项目 `.luau` 目录和 rokit 安装路径中查找
- **生成代码验证**：在集成测试运行时自动检查生成的 Lua 文件
- **详细错误报告**：显示类型错误、未使用变量、函数未使用等 Luau 警告

## 安装 Luau

### 方式 1：使用 rokit（推荐）

```bash
rokit init
rokit add luau-lang/luau
```

### 方式 2：手动下载

```bash
# Windows
npm run luau:install

# Linux/Mac
./scripts/install-luau.sh
```

安装后，Luau 可执行文件将位于：
- Windows: `.luau\luau-analyze.exe`
- Linux/Mac: `.luau/luau-analyze`

## 使用方法

### 运行 Luau 检查（独立）

检查所有测试数据中的 Lua 文件：

```bash
# 标准模式
npm run luau:check

# 严格模式
npm run luau:check:strict
```

### 在集成测试中使用

运行集成测试时，会自动对生成的 Lua 文件执行 Luau 验证：

```bash
cd tests/Luban.IntegrationTests
dotnet test
```

**验证行为**：
- 如果 Luau 分析器不可用，测试会跳过验证并输出提示
- 对于生成的代码，报告所有 Luau 警告和错误（但测试不会因此失败）
- 在控制台输出详细的 Luau 验证报告

### 理解 Luau 验证结果

Luau 检查会报告以下类型的问题：

#### 1. 类型错误 (TypeError)
```
TypeError: Key 'ctor' not found in table '{| New: any, __index: any |}'
```
这表示代码中访问了不存在的表键，可能是生成的代码问题。

#### 2. 未使用的局部变量 (LocalUnused)
```
LocalUnused: Variable 'ipairs' is never used; prefix with '_' to silence
```
生成代码中的常见情况，因为模板生成了所有可能的函数。

#### 3. 未使用的函数 (FunctionUnused)
```
FunctionUnused: Function 'SimpleClass' is never used; prefix with '_' to silence
```
同样常见于生成代码，因为函数可能在其他文件中使用。

#### 4. 隐式返回 (ImplicitReturn)
```
ImplicitReturn: Function 'readNullableBool' can implicitly return no values
```
函数可能在某些路径上没有显式返回值。

#### 5. 变量遮蔽 (LocalShadow)
```
LocalShadow: Variable 'beans' shadows previous declaration at line 96
```
在同一作用域中重复声明了变量。

## 配置

### LuauValidator 类

`LuauValidator` 类会自动查找 Luau 分析器：

1. **PATH 中的 luau-analyze**
2. **项目根目录的 .luau/luau-analyze**
3. **当前目录及父目录中的 .luau/luau-analyze**
4. **用户配置文件中的 rokit 工具路径**：
   - `~/.rokit/tools/luau/luau-analyze` (Linux/Mac)
   - `%USERPROFILE%\.rokit\tools\luau\luau-analyze.exe` (Windows)

### 验证严格度

当前的集成测试配置：
- **报告所有警告和错误**：在控制台显示完整报告
- **不因警告或错误失败测试**：因为生成的代码可能不符合严格类型检查
- **用于信息反馈**：帮助识别代码生成问题

### 自定义验证

如果需要更严格的验证，可以修改 `IntegrationTestBase.cs` 中的 `AssertLuauValid` 方法：

```csharp
protected void AssertLuauValid(string luaFilePath)
{
    if (!_luauValidator.IsAvailable)
    {
        Console.WriteLine($"Skipping Luau validation (analyzer not available): {Path.GetFileName(luaFilePath)}");
        return;
    }

    var result = _luauValidator.ValidateFile(luaFilePath);

    // 更严格：失败任何错误
    if (!result.IsValid && result.HasErrors)
    {
        Assert.Fail($"Luau validation failed for {Path.GetFileName(luaFilePath)}:\n{result.GetFormattedMessage()}");
    }

    // 或更严格：失败任何警告
    if (!result.IsValid && result.HasWarnings)
    {
        Assert.Fail($"Luau validation has warnings for {Path.GetFileName(luaFilePath)}:\n{result.GetFormattedMessage()}");
    }
}
```

## 生成代码中的常见 Luau 问题

### 1. 类型不兼容

Luau 是类型安全的，但生成的 Lua 代码可能不遵循严格类型规则。这是正常的。

**示例**：
```lua
-- 生成的代码可能使用 any 类型或动态表访问
local t = { foo = "bar" }
local value = t.baz  -- TypeError: Key 'baz' not found
```

### 2. 未使用的代码

模板通常会生成所有可能的函数和变量，即使某些未在当前文件中使用。这是预期的。

**示例**：
```lua
local function readByte() end  -- FunctionUnused
local function readShort() end -- FunctionUnused
local function readInt() end   -- 实际使用
```

### 3. 函数元表问题

Luau 对 `New`、`__index` 等元表方法有特定要求。

**示例**：
```lua
-- Luau 期望的类型
type MyClass = {
    New: (id: number, name: string) -> MyClass,
    __index: MyClass
}

-- 可能的错误
local instance = MyClass.ctor(...)  -- TypeError: Key 'ctor' not found
```

## 故障排查

### 问题：测试显示 "Skipping Luau validation (analyzer not available)"

**原因**：Luau 分析器未找到。

**解决方案**：
1. 运行安装脚本：`npm run luau:install`
2. 或使用 rokit：`rokit add luau-lang/luau`
3. 验证安装：`.luau/luau-analyze --version`

### 问题：Luau 检查报告大量错误

**原因**：生成的代码可能不符合 Luau 的严格类型检查。

**解决方案**：
1. 这是正常现象 - 生成的代码不一定类型安全
2. 使用验证报告作为改进代码生成器的参考
3. 如需通过 Luau 检查，修改生成模板以符合 Luau 类型系统

### 问题：测试因 Luau 错误而失败

**原因**：当前配置下不应发生（测试应通过）。

**解决方案**：
1. 检查 `IntegrationTestBase.cs` 中的 `AssertLuauValid` 方法
2. 确认配置为仅报告而非失败

## 参考

- [Luau 官方文档](https://luau.org/)
- [Luau Linting 文档](https://luau.org/lint)
- [Rokit GitHub](https://github.com/rojo-rbx/rokit)
- [Luau GitHub](https://github.com/luau-lang/luau)
