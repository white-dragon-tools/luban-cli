本项目基于 `luban v4.5 (0203b7a)` 开发,

你首先必须阅读 `README.luban.4.5.md` 文档, 了解原始 luban 项目功能.

本项目作为配置编译工具, 专门为 roblox-ts 开发适配.

本项目的编译成果为 `json`, 需要通过 `rojo` 同步到 roblox 项目中.

**区别**
- 专注于 roblox luau 代码生成
- 提供 typescript 定义文件
- 支持 flamework reflect id
- 提供更多特性


## 特性

**工厂函数**
- bean 的字段可以配置标签 `tags="ObjectFactory=true"`
- 拥有该标签的 bean 字段在`解析为lua对象`时将会:
    1. 生成 ()=>{} 的工厂函数
    2. 返回该函数作为对象

**Typescript reflect 定位**
- 原理:
    - 在 roblox-ts 项目中, 我们可以获取任意 class/interface 的 `id`, 格式为 `shared/plugins/buff-system/buff-core-plugin/components/buff-hooks@BuffHooks`
    - 通过该 id, 我们能够以反射获取 class 的类型对象.
- bean 可以配置标签 `tags="flameworkId={id}"`
- 拥有该标签的 bean 在`解析为lua对象`时将会:
    1. 获取 new 方法
    2. 调用 new 法, 以自身配置为参数1, 以`{id}` 为参数2 获取 lua 对象


**Typescript 引用定位**
- 我们需要在生成的 `.d.ts` 中, 引用已有的 ts 类型.
- 我们只需要为 luban table type 引用类型, 不需要为每个 bean 引用.
- table 可以配置标签 `tags="type={address}"`, 比如 `tags="type=shared/plugins/foo(FooType)"`
- 则生成的 ts 文件为 `import {FooType} from "shared/plugins/foo"`
- 支持 node_modules 规则: 比如 `tags="@rbxts/foo(FooType)"` 

**字符串枚举类型**
- 原 luban 只支持数字类型枚举
- 现在的规则: 如果 `value="string"`, 则认为是字符串类型枚举.