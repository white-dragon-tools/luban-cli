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

        // Generate wrapper schema files for each table
        var generateWrappers = EnvManager.Current.GetOptionOrDefault(Name, "generateWrappers", true, "true");
        if (generateWrappers.ToLower() == "true")
        {
            var wrapperInfos = new List<(string schemaFile, List<string> inputFiles)>();

            foreach (var table in ctx.ExportTables)
            {
                var wrapperSchema = GenerateWrapperSchema(table, outputFileName);
                var wrapperFileName = $"{ToKebabCase(table.ValueTType.DefBean.Name)}.schema.json";
                manifest.AddFile(CreateOutputFile($"definitions/{wrapperFileName}", wrapperSchema));

                wrapperInfos.Add((wrapperFileName, table.InputFiles));
            }

            // Generate VSCode settings
            var generateVscodeSettings = EnvManager.Current.GetOptionOrDefault(Name, "generateVscodeSettings", true, "true");
            if (generateVscodeSettings.ToLower() == "true")
            {
                var vscodeSettings = GenerateVscodeSettings(wrapperInfos);
                manifest.AddFile(CreateOutputFile("vscode-json-schemas.json", vscodeSettings));
            }
        }
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

        // Collect table value types for DataFile variant generation
        var tableValueTypes = new HashSet<string>();
        foreach (var table in ctx.ExportTables)
        {
            tableValueTypes.Add(table.ValueTType.DefBean.FullName);
        }

        // Process beans
        foreach (var bean in ctx.ExportBeans)
        {
            definitions[bean.FullName] = GenerateBeanSchema(bean);

            // Generate XxxDataFile variant for standalone file usage (no $type, has $schema)
            if (bean.IsAbstractType)
            {
                foreach (var child in bean.HierarchyNotAbstractChildren)
                {
                    var fileVariantName = $"{child.FullName}DataFile";
                    definitions[fileVariantName] = GenerateFileVariantSchema(child);
                }
            }
            else if (bean.ParentDefType != null)
            {
                // Concrete type with parent - generate file variant
                var fileVariantName = $"{bean.FullName}DataFile";
                definitions[fileVariantName] = GenerateFileVariantSchema(bean);
            }
            else if (tableValueTypes.Contains(bean.FullName))
            {
                // Simple bean used as table value type - generate file variant for wrapper schema
                var fileVariantName = $"{bean.FullName}DataFile";
                definitions[fileVariantName] = GenerateFileVariantSchema(bean);
            }
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
        var schema = new JsonObject
        {
            ["type"] = "object"
        };

        // Collect all concrete child type names for $type enum
        var typeNames = new JsonArray();
        foreach (var child in bean.HierarchyNotAbstractChildren)
        {
            typeNames.Add(child.Name);
        }

        // Build properties with $type having explicit enum
        var properties = new JsonObject
        {
            ["$type"] = new JsonObject
            {
                ["type"] = "string",
                ["enum"] = typeNames,
                ["description"] = $"Type discriminator for {bean.Name} polymorphic type"
            }
        };
        schema["properties"] = properties;

        // $type is always required
        schema["required"] = new JsonArray { "$type" };

        // Build allOf with if/then for each concrete type
        var allOf = new JsonArray();
        foreach (var child in bean.HierarchyNotAbstractChildren)
        {
            var ifThen = new JsonObject
            {
                ["if"] = new JsonObject
                {
                    ["properties"] = new JsonObject
                    {
                        ["$type"] = new JsonObject { ["const"] = child.Name }
                    },
                    ["required"] = new JsonArray { "$type" }
                },
                ["then"] = new JsonObject
                {
                    ["$ref"] = $"#/definitions/{child.FullName}"
                }
            };
            allOf.Add(ifThen);
        }
        schema["allOf"] = allOf;

        // Keep discriminator for tools that support it
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
            if (min.HasValue)
            {
                schema["minLength"] = min.Value;
            }

            if (max.HasValue)
            {
                schema["maxLength"] = max.Value;
            }
        }
        else if (type is Types.TArray or Types.TList or Types.TSet)
        {
            if (min.HasValue)
            {
                schema["minItems"] = min.Value;
            }

            if (max.HasValue)
            {
                schema["maxItems"] = max.Value;
            }
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

    /// <summary>
    /// Generate a file variant schema for standalone file usage.
    /// This variant does NOT require $type (since the type is known from the file schema).
    /// Supports $schema property for VSCode intellisense.
    /// </summary>
    private JsonObject GenerateFileVariantSchema(DefBean bean)
    {
        var schema = new JsonObject
        {
            ["type"] = "object",
            ["description"] = $"Standalone file variant of {bean.Name}. Use this schema for files that contain only {bean.Name} data."
        };

        var properties = new JsonObject();
        var required = new JsonArray();

        // Add $schema property support for VSCode intellisense
        properties["$schema"] = new JsonObject
        {
            ["type"] = "string",
            ["description"] = "JSON Schema reference for editor intellisense"
        };

        // DO NOT add $type - this is for standalone files where type is known

        // Add all hierarchy fields
        foreach (var field in bean.HierarchyFields)
        {
            var fieldSchema = field.CType.Apply(JsonSchemaTypeVisitor.Ins);

            if (field.CType.IsNullable)
            {
                fieldSchema = WrapNullable(fieldSchema);
            }

            if (!string.IsNullOrEmpty(field.Comment))
            {
                fieldSchema["description"] = field.Comment;
            }

            ApplyValidators(fieldSchema, field);
            properties[field.Name] = fieldSchema;

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

        return schema;
    }

    /// <summary>
    /// Generate a wrapper schema file for a table.
    /// This allows VSCode to provide intellisense for individual data files.
    /// References XxxDataFile variant which supports $schema and doesn't require $type.
    /// </summary>
    private string GenerateWrapperSchema(DefTable table, string mainSchemaFileName)
    {
        var valueType = table.ValueTType.DefBean;

        // Use XxxDataFile variant which supports $schema property
        var dataFileVariant = $"{valueType.FullName}DataFile";

        var schema = new JsonObject
        {
            ["$schema"] = "http://json-schema.org/draft-07/schema#",
            ["$ref"] = $"../{mainSchemaFileName}#/definitions/{dataFileVariant}"
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        };
        return schema.ToJsonString(options);
    }

    /// <summary>
    /// Convert PascalCase to kebab-case
    /// </summary>
    private static string ToKebabCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var result = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    result.Append('-');
                }
                result.Append(char.ToLower(c));
            }
            else
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }

    /// <summary>
    /// Generate VSCode json.schemas configuration.
    /// This file can be merged into .vscode/settings.json
    /// </summary>
    private string GenerateVscodeSettings(List<(string schemaFile, List<string> inputFiles)> wrapperInfos)
    {
        var schemaDir = EnvManager.Current.GetOptionOrDefault(Name, "schemaDir", true, "./configs/schema");
        var dataDir = EnvManager.Current.GetOptionOrDefault(Name, "dataDir", true, "");

        var schemas = new JsonArray();

        foreach (var (schemaFile, inputFiles) in wrapperInfos)
        {
            // Group input files by whether they need array schema (*@ prefix)
            var objectFileMatches = new JsonArray();
            var arrayFileMatches = new JsonArray();

            foreach (var inputFile in inputFiles)
            {
                var pattern = inputFile;
                bool isArrayInput = false;

                // Check for *@ prefix (Luban convention for array JSON files)
                if (pattern.StartsWith("*@"))
                {
                    isArrayInput = true;
                    pattern = pattern.Substring(2);
                }

                // Normalize path separators
                pattern = pattern.Replace('\\', '/');

                // Remove leading ../ or ./
                while (pattern.StartsWith("../"))
                {
                    pattern = pattern.Substring(3);
                }
                while (pattern.StartsWith("./"))
                {
                    pattern = pattern.Substring(2);
                }

                // If pattern is a directory (no extension or ends with /), add **/*.json
                if (!pattern.Contains('.') || pattern.EndsWith("/"))
                {
                    pattern = pattern.TrimEnd('/') + "/**/*.json";
                }

                // Add dataDir prefix if configured
                if (!string.IsNullOrEmpty(dataDir))
                {
                    pattern = dataDir.TrimEnd('/') + "/" + pattern;
                }

                if (isArrayInput)
                {
                    arrayFileMatches.Add(pattern);
                }
                else
                {
                    objectFileMatches.Add(pattern);
                }
            }

            // Add schema entry for object files (direct reference to wrapper schema)
            if (objectFileMatches.Count > 0)
            {
                var schemaEntry = new JsonObject
                {
                    ["fileMatch"] = objectFileMatches,
                    ["url"] = $"{schemaDir}/definitions/{schemaFile}"
                };
                schemas.Add(schemaEntry);
            }

            // Add schema entry for array files (inline array schema with items reference)
            if (arrayFileMatches.Count > 0)
            {
                // Extract the definition name from the wrapper schema file name
                // e.g., "visual-data.schema.json" -> need to find the actual definition reference
                var definitionRef = schemaFile.Replace(".schema.json", "");
                // Convert kebab-case back to PascalCase for definition lookup
                var parts = definitionRef.Split('-');
                var pascalName = string.Join("", parts.Select(p => char.ToUpper(p[0]) + p.Substring(1)));

                var schemaEntry = new JsonObject
                {
                    ["fileMatch"] = arrayFileMatches,
                    ["schema"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["$ref"] = $"{schemaDir}/definitions/{schemaFile}"
                        }
                    }
                };
                schemas.Add(schemaEntry);
            }
        }

        var root = new JsonObject
        {
            ["json.schemas"] = schemas
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        };
        return root.ToJsonString(options);
    }
}
