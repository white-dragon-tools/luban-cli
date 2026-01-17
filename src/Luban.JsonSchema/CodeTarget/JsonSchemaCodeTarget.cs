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

using System.Text.Json;
using System.Text.Json.Nodes;
using Luban.CodeTarget;
using Luban.Defs;
using Luban.JsonSchema.TypeVisitors;
using Luban.Utils;

namespace Luban.JsonSchema.CodeTarget;

[CodeTarget("json-schema")]
public class JsonSchemaCodeTarget : CodeTargetBase
{
    public override string FileHeader => "";

    protected override string FileSuffixName => "json";

    protected override IReadOnlySet<string> PreservedKeyWords => new HashSet<string>();

    public override void Handle(GenerationContext ctx, OutputFileManifest manifest)
    {
        var schema = GenerateSchema(ctx);
        var outputFileName = EnvManager.Current.GetOptionOrDefault(Name, "outputFile", true, "schema.json");
        manifest.AddFile(CreateOutputFile(outputFileName, schema));
    }

    private string GenerateSchema(GenerationContext ctx)
    {
        var root = new JsonObject
        {
            ["$schema"] = "http://json-schema.org/draft-07/schema#",
            ["$id"] = "luban-schema"
        };

        // Generate definitions
        var definitions = new JsonObject();

        // Process enums
        foreach (var @enum in ctx.ExportEnums)
        {
            definitions[@enum.FullName] = GenerateEnumSchema(@enum);
        }

        // Process beans
        foreach (var bean in ctx.ExportBeans)
        {
            definitions[bean.FullName] = GenerateBeanSchema(bean);
        }

        root["definitions"] = definitions;

        // Generate tables metadata
        var tables = new JsonObject();
        foreach (var table in ctx.ExportTables)
        {
            tables[table.FullName] = GenerateTableMeta(table);
        }
        root["tables"] = tables;

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        };
        return root.ToJsonString(options);
    }

    private JsonObject GenerateEnumSchema(DefEnum @enum)
    {
        var schema = new JsonObject();

        if (@enum.IsStringEnum)
        {
            schema["type"] = "string";
            var enumValues = new JsonArray();
            foreach (var item in @enum.Items)
            {
                enumValues.Add(item.Value);
            }
            schema["enum"] = enumValues;
        }
        else
        {
            schema["type"] = "integer";
            var enumValues = new JsonArray();
            foreach (var item in @enum.Items)
            {
                enumValues.Add(item.IntValue);
            }
            schema["enum"] = enumValues;
        }

        // Add extension properties
        schema["x-luban-enum"] = @enum.FullName;

        if (@enum.IsFlags)
        {
            schema["x-luban-flags"] = true;
        }

        // Add enum items details
        var items = new JsonArray();
        foreach (var item in @enum.Items)
        {
            var itemObj = new JsonObject
            {
                ["name"] = item.Name
            };
            if (@enum.IsStringEnum)
            {
                itemObj["value"] = item.Value;
            }
            else
            {
                itemObj["value"] = item.IntValue;
            }
            if (!string.IsNullOrEmpty(item.Alias))
            {
                itemObj["alias"] = item.Alias;
            }
            if (!string.IsNullOrEmpty(item.Comment))
            {
                itemObj["comment"] = item.Comment;
            }
            items.Add(itemObj);
        }
        schema["x-luban-enum-items"] = items;

        if (!string.IsNullOrEmpty(@enum.Comment))
        {
            schema["description"] = @enum.Comment;
        }

        return schema;
    }

    private JsonObject GenerateBeanSchema(DefBean bean)
    {
        // Check if this is an abstract type (has children)
        if (bean.IsAbstractType)
        {
            return GeneratePolymorphicBeanSchema(bean);
        }

        return GenerateConcreteBeanSchema(bean);
    }

    private JsonObject GenerateConcreteBeanSchema(DefBean bean)
    {
        var schema = new JsonObject
        {
            ["type"] = "object"
        };

        var properties = new JsonObject();
        var required = new JsonArray();

        // If this bean has a parent (is a subtype), add $type discriminator
        if (bean.ParentDefType != null)
        {
            properties["$type"] = new JsonObject
            {
                ["const"] = bean.Name
            };
            required.Add("$type");
        }

        // Add all hierarchy fields
        foreach (var field in bean.HierarchyFields)
        {
            var fieldSchema = field.CType.Apply(JsonSchemaTypeVisitor.Ins);

            // Handle nullable types
            if (field.CType.IsNullable)
            {
                fieldSchema = WrapNullable(fieldSchema);
            }

            // Add field metadata
            if (!string.IsNullOrEmpty(field.Comment))
            {
                fieldSchema["description"] = field.Comment;
            }

            // Process validators from tags
            ApplyValidators(fieldSchema, field);

            properties[field.Name] = fieldSchema;

            // Add to required if not nullable
            if (!field.CType.IsNullable)
            {
                required.Add(field.Name);
            }
        }

        schema["properties"] = properties;

        if (required.Count > 0)
        {
            schema["required"] = required;
        }

        schema["additionalProperties"] = false;

        if (!string.IsNullOrEmpty(bean.Comment))
        {
            schema["description"] = bean.Comment;
        }

        return schema;
    }

    private JsonObject GeneratePolymorphicBeanSchema(DefBean bean)
    {
        var schema = new JsonObject();

        var oneOf = new JsonArray();
        foreach (var child in bean.HierarchyNotAbstractChildren)
        {
            oneOf.Add(new JsonObject
            {
                ["$ref"] = $"#/definitions/{child.FullName}"
            });
        }
        schema["oneOf"] = oneOf;

        schema["discriminator"] = new JsonObject
        {
            ["propertyName"] = "$type"
        };

        if (!string.IsNullOrEmpty(bean.Comment))
        {
            schema["description"] = bean.Comment;
        }

        return schema;
    }

    private void ApplyValidators(JsonObject schema, DefField field)
    {
        // Check field tags first (from XML tags attribute)
        if (field.Tags != null)
        {
            foreach (var (tagName, tagValue) in field.Tags)
            {
                ApplyValidatorTag(schema, tagName, tagValue, field.CType);
            }
        }

        // Also check type tags
        if (field.CType.Tags != null)
        {
            foreach (var (tagName, tagValue) in field.CType.Tags)
            {
                ApplyValidatorTag(schema, tagName, tagValue, field.CType);
            }
        }
    }

    private void ApplyValidatorTag(JsonObject schema, string tagName, string tagValue, Types.TType type)
    {
        switch (tagName.ToLower())
        {
            case "range":
                ApplyRangeValidator(schema, tagValue);
                break;
            case "size":
                ApplySizeValidator(schema, tagValue, type);
                break;
            case "regex":
                schema["pattern"] = tagValue;
                break;
            case "ref":
                schema["x-luban-ref"] = tagValue;
                break;
            case "path":
                schema["x-luban-path"] = tagValue;
                break;
        }
    }

    private void ApplyRangeValidator(JsonObject schema, string value)
    {
        // Parse range(min,max) or range(min,) or range(,max)
        var parts = value.Split(',');
        if (parts.Length >= 1 && !string.IsNullOrEmpty(parts[0]))
        {
            if (double.TryParse(parts[0], out var min))
            {
                schema["minimum"] = min;
            }
        }
        if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[1]))
        {
            if (double.TryParse(parts[1], out var max))
            {
                schema["maximum"] = max;
            }
        }
    }

    private void ApplySizeValidator(JsonObject schema, string value, Types.TType type)
    {
        var parts = value.Split(',');
        int? min = null;
        int? max = null;

        if (parts.Length >= 1 && !string.IsNullOrEmpty(parts[0]))
        {
            if (int.TryParse(parts[0], out var minVal))
            {
                min = minVal;
            }
        }
        if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[1]))
        {
            if (int.TryParse(parts[1], out var maxVal))
            {
                max = maxVal;
            }
        }

        // Apply based on type
        if (type is Types.TString)
        {
            if (min.HasValue) schema["minLength"] = min.Value;
            if (max.HasValue) schema["maxLength"] = max.Value;
        }
        else if (type is Types.TArray or Types.TList or Types.TSet)
        {
            if (min.HasValue) schema["minItems"] = min.Value;
            if (max.HasValue) schema["maxItems"] = max.Value;
        }
    }

    private JsonObject GenerateTableMeta(DefTable table)
    {
        var meta = new JsonObject
        {
            ["valueType"] = table.ValueTType.DefBean.FullName,
            ["mode"] = table.Mode.ToString().ToLower()
        };

        if (!string.IsNullOrEmpty(table.Index))
        {
            meta["index"] = table.Index;
        }

        if (table.InputFiles.Count > 0)
        {
            var files = new JsonArray();
            foreach (var file in table.InputFiles)
            {
                files.Add(file);
            }
            meta["inputFiles"] = files;
        }

        if (!string.IsNullOrEmpty(table.Comment))
        {
            meta["comment"] = table.Comment;
        }

        return meta;
    }

    private JsonObject WrapNullable(JsonObject schema)
    {
        // For simple types with "type" property, convert to array with null
        if (schema.TryGetPropertyValue("type", out var typeNode) && typeNode is JsonValue typeValue)
        {
            var typeStr = typeValue.GetValue<string>();
            schema["type"] = new JsonArray { typeStr, "null" };
            return schema;
        }

        // For $ref types, wrap in oneOf with null
        if (schema.TryGetPropertyValue("$ref", out _))
        {
            return new JsonObject
            {
                ["oneOf"] = new JsonArray
                {
                    schema,
                    new JsonObject { ["type"] = "null" }
                }
            };
        }

        // For oneOf types (polymorphic), add null option
        if (schema.TryGetPropertyValue("oneOf", out var oneOfNode) && oneOfNode is JsonArray oneOfArray)
        {
            oneOfArray.Add(new JsonObject { ["type"] = "null" });
            return schema;
        }

        return schema;
    }

    // These methods are required by base class but we override Handle() so they're not used
    public override void GenerateTables(GenerationContext ctx, List<DefTable> tables, CodeWriter writer) { }
    public override void GenerateTable(GenerationContext ctx, DefTable table, CodeWriter writer) { }
    public override void GenerateBean(GenerationContext ctx, DefBean bean, CodeWriter writer) { }
    public override void GenerateEnum(GenerationContext ctx, DefEnum @enum, CodeWriter writer) { }
}
