# 数据验证器

Luban 提供了多种内置验证器，用于在配置加载时验证数据的有效性。

## 内置验证器列表

### 1. constructor 验证器

**功能**: 验证字符串字段的值必须是某个 Bean 类型或其子类。

#### 使用场景
当字段值是一个类型名称字符串，需要确保该类型存在并且是指定基类的子类时使用。例如：
- 技能触发器类型（`DamageTrigger`、`HealTrigger` 等）
- 效果类型（`BuffEffect`、`DamageEffect` 等）
- 任何需要多态配置的场景

#### 配置方式
在 bean 字段的类型定义中使用 `#(constructor=BaseTypeName)` 语法。

**基类名称支持两种格式：**
- **简化名称**：`constructor=BaseTrigger` - 在当前模块中查找
- **完整名称**：`constructor=constructor.BaseTrigger` - 使用完整的 `module.TypeName` 格式

**推荐使用简化名称**，更简洁易读。

#### Schema 示例
```xml
<bean name="BaseTrigger">
    <var name="id" type="int"/>
    <var name="name" type="string"/>
</bean>

<bean name="DamageTrigger" parent="BaseTrigger">
    <var name="damage" type="int"/>
</bean>

<bean name="HealTrigger" parent="BaseTrigger">
    <var name="healAmount" type="int"/>
</bean>

<bean name="SkillConfig">
    <var name="skillId" type="int"/>
    <!-- 使用简化名称（推荐） -->
    <var name="triggerType" type="string#(constructor=BaseTrigger)"/>
</bean>

<!-- 或者使用完整名称（不推荐） -->
<bean name="AnotherConfig">
    <var name="triggerType" type="string#(constructor=module.submodule.BaseTrigger)"/>
</bean>
```

#### 数据示例
```json
[
  {
    "skillId": 1,
    "triggerType": "DamageTrigger"  // ✅ 有效
  },
  {
    "skillId": 2,
    "triggerType": "HealTrigger"     // ✅ 有效
  },
  {
    "skillId": 3,
    "triggerType": "BaseTrigger"     // ✅ 有效
  },
  {
    "skillId": 4,
    "triggerType": "InvalidTrigger"  // ❌ 无效，类型不存在
  }
]
```

#### 验证规则
- 字段值**不能为空**
- 字段值**必须是已定义的 Bean 类型名称**（支持简化名称或完整名称）
- 该 Bean 类型**必须是指定基类的子类或基类本身**
- 类型名称**区分大小写**（精确匹配）
- **必须使用类型名**，不支持别名

#### 错误信息
验证失败时会显示详细的错误信息，包括所有有效类型：

```
ERROR|记录 TbSkillConfig[4].triggerType = 'InvalidTrigger' (来自文件:xxx.json) 类型不存在。有效类型: [BaseTrigger, DamageTrigger, HealTrigger]
```

#### Lua 代码生成
对于使用 `constructor` 验证器的字段，生成的 Lua 代码会自动使用 `methods.getClass()` 将类型名称字符串转换为类引用：

```lua
class._deserialize = function(bs)
    local o = table.clone(bs)
    -- 自动将 triggerType 字符串转换为类引用
    o.triggerType = methods.getClass(bs.triggerType)
    setmetatable(o, class)
    return o
end
```

#### 注意事项
- 基类（`constructor=...` 中指定的类型）**必须是 Bean 类型**，不能是接口或枚举
- 不支持泛型 Bean
- 如果字段可能为空，应使用 `?#(constructor=...)` 语法（Luban 的标准可空语法）

---

### 2. ref 验证器

**功能**: 验证引用字段指向的记录在其他表中存在。

详见官方文档：https://www.datable.cn/docs/

---

### 3. path 验证器

**功能**: 验证资源路径字段指向的文件存在。

详见官方文档：https://www.datable.cn/docs/

---

### 4. range 验证器

**功能**: 验证数值字段在指定范围内。

详见官方文档：https://www.datable.cn/docs/

---

## 自定义验证器

你可以创建自定义验证器来满足项目的特殊需求。详见：

- [源码位置](../src/Luban.DataValidator.Builtin/)
- [验证器基类](../src/Luban.Core/Validator/DataValidatorBase.cs)
- [注册机制](../src/Luban.Core/CustomBehaviour/CustomBehaviourManager.cs)

## 测试

运行验证器测试：

```bash
# 测试 constructor 验证器
npm run test:constructor

# 修改测试数据后需要重新构建
npm run build
```

测试数据位置：`tests/Luban.IntegrationTests/TestData/constructor_validator/`
