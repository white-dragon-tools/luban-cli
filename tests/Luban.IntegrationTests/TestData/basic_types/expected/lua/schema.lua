local setmetatable = setmetatable
local pairs = pairs
local ipairs = ipairs
local tinsert = table.insert
local function SimpleClass()
local class = {}
class.__index = class
class.New = function(...)
local ctor = class.ctor
local o = ctor and ctor(...) or {}
setmetatable(o, class)
return o
end
return class
end
local function get_map_size(m)
local n = 0
for _ in pairs(m) do
n = n + 1
end
return n
end
local enums =
{
}
local tables =
{
{ name='TbItem', file='tbitem', mode='map', index='id', value_type='Item' },
}
local function InitTypes(methods)
local readBool = methods.readBool
local readByte = methods.readByte
local readShort = methods.readShort
local readFshort = methods.readFshort
local readInt = methods.readInt
local readFint = methods.readFint
local readLong = methods.readLong
local readFlong = methods.readFlong
local readFloat = methods.readFloat
local readDouble = methods.readDouble
local readSize = methods.readSize
local readString = methods.readString
local function readList(bs, keyFun)
local list = {}
local v
for i = 1, readSize(bs) do
tinsert(list, keyFun(bs))
end
return list
end
local readArray = readList
local function readSet(bs, keyFun)
local set = {}
local v
for i = 1, readSize(bs) do
tinsert(set, keyFun(bs))
end
return set
end
local function readMap(bs, keyFun, valueFun)
local map = {}
for i = 1, readSize(bs) do
local k = keyFun(bs)
local v = valueFun(bs)
map[k] = v
end
return map
end
local function readNullableBool(bs)
if readBool(bs) then
return readBool(bs)
end
end
local beans = {}
do
local class = {
{ name='id', type='integer'},
{ name='name', type='string'},
{ name='price', type='number'},
{ name='enabled', type='boolean'},
}
beans['Item'] = class
end
local beans = {}
do
local class = SimpleClass()
class._id = 2289459
class._type_ = 'Item'
local id2name = { }
class._deserialize = function(bs)
local o = {
id = readInt(bs),
name = readString(bs),
price = readFloat(bs),
enabled = readBool(bs),
}
setmetatable(o, class)
return o
end
beans[class._type_] = class
end
return { enums = enums, beans = beans, tables = tables }
end
return { InitTypes = InitTypes }
