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

using System.Text.Json.Nodes;
using Luban.Types;
using Luban.TypeVisitors;

namespace Luban.JsonSchema.TypeVisitors;

public class JsonSchemaTypeVisitor : ITypeFuncVisitor<JsonObject>
{
    public static JsonSchemaTypeVisitor Ins { get; } = new();

    public JsonObject Accept(TBool type)
    {
        return new JsonObject { ["type"] = "boolean" };
    }

    public JsonObject Accept(TByte type)
    {
        return new JsonObject
        {
            ["type"] = "integer",
            ["minimum"] = 0,
            ["maximum"] = 255
        };
    }

    public JsonObject Accept(TShort type)
    {
        return new JsonObject
        {
            ["type"] = "integer",
            ["minimum"] = -32768,
            ["maximum"] = 32767
        };
    }

    public JsonObject Accept(TInt type)
    {
        return new JsonObject { ["type"] = "integer" };
    }

    public JsonObject Accept(TLong type)
    {
        return new JsonObject { ["type"] = "integer" };
    }

    public JsonObject Accept(TFloat type)
    {
        return new JsonObject { ["type"] = "number" };
    }

    public JsonObject Accept(TDouble type)
    {
        return new JsonObject { ["type"] = "number" };
    }

    public JsonObject Accept(TEnum type)
    {
        return new JsonObject
        {
            ["$ref"] = $"#/definitions/{type.DefEnum.FullName}"
        };
    }

    public JsonObject Accept(TString type)
    {
        return new JsonObject { ["type"] = "string" };
    }

    public JsonObject Accept(TDateTime type)
    {
        return new JsonObject
        {
            ["type"] = "string",
            ["format"] = "date-time"
        };
    }

    public JsonObject Accept(TBean type)
    {
        return new JsonObject
        {
            ["$ref"] = $"#/definitions/{type.DefBean.FullName}"
        };
    }

    public JsonObject Accept(TArray type)
    {
        var schema = new JsonObject
        {
            ["type"] = "array",
            ["items"] = type.ElementType.Apply(this)
        };
        return schema;
    }

    public JsonObject Accept(TList type)
    {
        var schema = new JsonObject
        {
            ["type"] = "array",
            ["items"] = type.ElementType.Apply(this)
        };
        return schema;
    }

    public JsonObject Accept(TSet type)
    {
        var schema = new JsonObject
        {
            ["type"] = "array",
            ["items"] = type.ElementType.Apply(this),
            ["uniqueItems"] = true
        };
        return schema;
    }

    public JsonObject Accept(TMap type)
    {
        var schema = new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = type.ValueType.Apply(this)
        };

        // Add key type info as extension
        var keyTypeName = GetKeyTypeName(type.KeyType);
        schema["x-luban-key-type"] = keyTypeName;

        // For integer keys, add pattern constraint
        if (type.KeyType is TInt or TLong or TShort or TByte)
        {
            schema["propertyNames"] = new JsonObject
            {
                ["pattern"] = "^-?[0-9]+$"
            };
        }

        return schema;
    }

    private string GetKeyTypeName(TType keyType)
    {
        return keyType switch
        {
            TBool => "boolean",
            TByte or TShort or TInt or TLong => "integer",
            TFloat or TDouble => "number",
            TString => "string",
            TEnum e => e.DefEnum.FullName,
            _ => "string"
        };
    }
}
