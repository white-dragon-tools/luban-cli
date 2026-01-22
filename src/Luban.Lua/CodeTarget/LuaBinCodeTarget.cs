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

using Luban.CodeTarget;
using Luban.Lua.TemplateExtensions;
using Luban.Utils;
using Scriban;

namespace Luban.Lua.CodeTarget;

[CodeTarget("lua-bin")]
public class LuaBinCodeTarget : LuaCodeTargetBase
{
    protected override void OnCreateTemplateContext(TemplateContext ctx)
    {
        base.OnCreateTemplateContext(ctx);
        ctx.PushGlobal(new LuaBinTemplateExtension());
    }

    public override void Handle(GenerationContext ctx, OutputFileManifest manifest)
    {
        base.Handle(ctx, manifest);

        string outputSchemaFileName = EnvManager.Current.GetOptionOrDefault(Name, $"outputFile", true, DefaultOutputFileName);
        string dtsFileName = outputSchemaFileName.Replace(".lua", ".d.ts");
        manifest.AddFile(CreateOutputFile(dtsFileName, GenerateSchemaDts()));
    }

    private static string GenerateSchemaDts()
    {
        return @"type deserializer = (item: unknown) => unknown

export interface Methods{
    getClass:(beanName: string)=> unknown,
    getRef:(tableName: string, id:unknown)=> unknown,
    readList:(cfg:Array<unknown>,deserializer:deserializer)=>object,
    readSet:(cfg:Set<unknown>,deserializer:deserializer)=>object,
    readMap:(cfg:Record<string, unknown>,deserializer:deserializer)=>object,
    readListRef:(tableName: string, cfg:Array<unknown>)=>object,
    readMapRef:(tableName: string, cfg:Map<unknown, unknown>)=>object,
    readSetRef:(tableName: string, cfg:Set<unknown>)=>object,
}


export interface TableMeta {
	name: string;
	file: string;
	mode: ""map"" | ""list"" | ""one"" | ""singleton"" | ""single"" | ""array"";
	index?: string;
	value_type: string;
}


export interface DeserializerBean   {
    _deserialize: (data: unknown) => unknown
}

/** 初始化 */
export function InitTypes(methods:Methods, enableHotreload?:boolean):{
    /** class 类 */
    beans: Map<string, DeserializerBean>,

    /** 表元数据 */
    tables: TableMeta[]

}";
    }
}
