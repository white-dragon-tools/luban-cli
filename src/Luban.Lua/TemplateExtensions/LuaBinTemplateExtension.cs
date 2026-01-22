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

using Luban.DataValidator.Builtin.Ref;
using Luban.DataValidator.Builtin.Type;
using Luban.Defs;
using Luban.Lua.TypVisitors;
using Luban.Types;
using Luban.Utils;
using Luban.Validator;
using Scriban.Runtime;

namespace Luban.Lua.TemplateExtensions;

public class LuaBinTemplateExtension : ScriptObject
{
    // Built-in Roblox native type mapping
    private static readonly Dictionary<string, string> RobloxNativeTypes = new()
    {
        { "Vector2", "Vector2.new" },
        { "Vector3", "Vector3.new" },
        { "Color3", "Color3.new" },
        { "CFrame", "CFrame.new" },
    };

    public static string Deserialize(string bufName, TType type, DefField field = null)
    {
        var visitor = LuaUnderlyingDeserializeVisitor.Ins;
        visitor.CurrentField = field;
        try
        {
            return type.Apply(visitor, bufName);
        }
        finally
        {
            visitor.CurrentField = null;
        }
    }

    public static bool HasConstructorValidator(DefField field)
    {
        return field.CType.Validators.Any(v => v is ConstructorValidator);
    }

    public static bool HasObjectFactory(DefField field)
    {
        return field.HasTag("ObjectFactory");
    }

    public static bool IsRobloxNativeType(DefBean bean)
    {
        return RobloxNativeTypes.ContainsKey(bean.Name);
    }

    public static string GetRobloxConstructor(DefBean bean)
    {
        return RobloxNativeTypes.TryGetValue(bean.Name, out var ctor) ? ctor : null;
    }

    // Generate deserialization expression for Roblox native types
    public static string DeserializeRobloxNative(DefBean bean, string varName)
    {
        var ctor = GetRobloxConstructor(bean);
        // Use HierarchyExportFields to include inherited fields
        var fields = bean.HierarchyExportFields;
        var args = string.Join(", ", fields.Select(f =>
            DeserializeRobloxField(f, $"{varName}.{f.Name}")));
        return $"{ctor}({args})";
    }

    private static string DeserializeRobloxField(DefField field, string expr)
    {
        if (field.CType is TBean beanType && IsRobloxNativeType(beanType.DefBean))
        {
            var nativeExpr = DeserializeRobloxNative(beanType.DefBean, expr);
            // Handle nullable nested Roblox types
            if (beanType.IsNullable)
            {
                return $"({expr} and {nativeExpr} or nil)";
            }
            return nativeExpr;
        }
        return expr;
    }

    public static RefOverrideInfo GetRefOverrideInfo(DefField field)
    {
        if (!field.HasTag("RefOverride"))
        {
            return null;
        }

        TType targetType = field.CType;
        bool isCollection = false;
        string collectionType = null;

        // Check if it's a collection type (array, list, set, map)
        if (targetType is TArray arr)
        {
            targetType = arr.ElementType;
            isCollection = true;
            collectionType = "array";
        }
        else if (targetType is TList list)
        {
            targetType = list.ElementType;
            isCollection = true;
            collectionType = "list";
        }
        else if (targetType is TSet set)
        {
            targetType = set.ElementType;
            isCollection = true;
            collectionType = "set";
        }
        else if (targetType is TMap map)
        {
            // For map, only process value type, ignore key
            targetType = map.ValueType;
            isCollection = true;
            collectionType = "map";
        }

        var refValidator = targetType.Validators.OfType<RefValidator>().FirstOrDefault();
        if (refValidator == null)
        {
            throw new Exception($"field:'{field}' has RefOverride tag but no #ref validator on {(isCollection ? "element type" : "field")}");
        }

        var tables = DefUtil.TrimBracePairs(refValidator.Args).Split(',').Select(s => s.Trim()).ToList();
        if (tables.Count > 1)
        {
            throw new Exception($"field:'{field}' RefOverride does not support multiple table references");
        }

        var refStr = tables[0];
        bool ignoreDefault = refStr.EndsWith("?");
        if (ignoreDefault)
        {
            refStr = refStr.Substring(0, refStr.Length - 1);
        }

        int sepIndex = refStr.IndexOf('@');
        if (sepIndex >= 0)
        {
            throw new Exception($"field:'{field}' RefOverride does not support list table index references");
        }

        var assembly = field.Assembly;
        var defTable = assembly.GetCfgTable(refStr);
        if (defTable == null)
        {
            throw new Exception($"field:'{field}' RefOverride ref table '{refStr}' not found");
        }

        if (!defTable.IsMapTable)
        {
            throw new Exception($"field:'{field}' RefOverride only supports map tables, but '{refStr}' is not a map table");
        }

        return new RefOverrideInfo
        {
            TableName = defTable.FullName,
            IsNullable = ignoreDefault || targetType.IsNullable,
            IsCollection = isCollection,
            CollectionType = collectionType
        };
    }

    public class RefOverrideInfo
    {
        public string TableName { get; set; }
        public bool IsNullable { get; set; }
        public bool IsCollection { get; set; }
        public string CollectionType { get; set; }
    }
}
