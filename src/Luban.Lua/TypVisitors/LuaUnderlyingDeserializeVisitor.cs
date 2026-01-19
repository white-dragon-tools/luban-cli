// Copyright 2025 Code Philosophy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Luban.Defs;
using Luban.Lua.TemplateExtensions;
using Luban.Types;
using Luban.TypeVisitors;

namespace Luban.Lua.TypVisitors;

public class LuaUnderlyingDeserializeVisitor : DecoratorFuncVisitor<string, string>
{
    public static LuaUnderlyingDeserializeVisitor Ins { get; } = new();

    public DefField CurrentField { get; set; }

    public override string DoAccept(TType type, string x)
    {
        return $"{type.Apply(LuaDeserializeMethodNameVisitor.Ins)}({x})";
    }

    public override string Accept(TBean type, string x)
    {
        // Check if this is a Roblox native type
        if (LuaBinTemplateExtension.IsRobloxNativeType(type.DefBean))
        {
            var nativeExpr = LuaBinTemplateExtension.DeserializeRobloxNative(type.DefBean, x);
            // Handle nullable types: check for nil before accessing fields
            if (type.IsNullable)
            {
                return $"({x} and {nativeExpr} or nil)";
            }
            return nativeExpr;
        }
        // Default behavior: use beans table deserializer
        return $"beans['{type.DefBean.FullName}']._deserialize({x})";
    }

    public override string Accept(TArray type, string x)
    {
        var deserializer = type.ElementType.Apply(LuaDeserializeMethodNameVisitor.Ins);
        var hasObjectFactory = CurrentField?.HasTag("ObjectFactory") ?? false;
        return hasObjectFactory
            ? $"readArray({x}, {deserializer}, true)"
            : $"readArray({x}, {deserializer})";
    }

    public override string Accept(TList type, string x)
    {
        var deserializer = type.ElementType.Apply(LuaDeserializeMethodNameVisitor.Ins);
        var hasObjectFactory = CurrentField?.HasTag("ObjectFactory") ?? false;
        return hasObjectFactory
            ? $"readList({x}, {deserializer}, true)"
            : $"readList({x}, {deserializer})";
    }

    public override string Accept(TSet type, string x)
    {
        var deserializer = type.ElementType.Apply(LuaDeserializeMethodNameVisitor.Ins);
        var hasObjectFactory = CurrentField?.HasTag("ObjectFactory") ?? false;
        return hasObjectFactory
            ? $"readSet({x}, {deserializer}, true)"
            : $"readSet({x}, {deserializer})";
    }

    public override string Accept(TMap type, string x)
    {
        return $"readMap({x}, {type.ValueType.Apply(LuaDeserializeMethodNameVisitor.Ins)})";
    }
}
