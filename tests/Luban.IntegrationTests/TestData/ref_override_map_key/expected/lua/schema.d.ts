type deserializer = (item: unknown) => unknown

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
	mode: "map" | "list" | "one" | "singleton" | "single" | "array";
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

}