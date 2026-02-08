#!/bin/bash

# Update all expected Lua files to match new Luau-compatible template

TEST_DATA_DIR="tests/Luban.IntegrationTests/TestData"

for test_case in class_method object_factory string_enum; do
    lua_file="$TEST_DATA_DIR/$test_case/expected/lua/schema.lua"

    if [ -f "$lua_file" ]; then
        echo "Updating $lua_file..."

        # Create a temporary file for sed operations
        temp_file=$(mktemp)

        # 1. Replace "local ipairs = ipairs" with comment version
        sed 's/^local ipairs = ipairs$/-- local ipairs = ipairs  -- Not used, commented out to satisfy Luau linter/' "$lua_file" > "$temp_file"

        # 2. Replace "local ctor = class.ctor" with rawget version
        # Use backslash-newline for portability (BSD sed on macOS does not support \n in replacement)
        sed -i '' 's/local ctor = class\.ctor$/        -- Use rawget to safely check for ctor without triggering Luau type error\
        local ctor = rawget(class, '\''ctor'\'')/' "$temp_file"

        # 3. Remove "local v" from readList and readSet functions
        sed -i '' '/local function readList(bs, keyFun)/,/end/ { /^[[:space:]]*local v$/d; }' "$temp_file"
        sed -i '' '/local function readSet(bs, keyFun)/,/end/ { /^[[:space:]]*local v$/d; }' "$temp_file"

        # 4. Add else branch to readNullableBool
        # Use a temp script file for multi-line sed to ensure portability
        sed_script=$(mktemp)
        cat > "$sed_script" << 'SEDEOF'
/local function readNullableBool(bs)/,/end/ {
    /return readBool(bs)/ {
        a\
        else\
            return nil
    }
}
SEDEOF
        sed -i '' -f "$sed_script" "$temp_file"
        rm "$sed_script"

        # Copy the temp file back
        cp "$temp_file" "$lua_file"
        rm "$temp_file"

        echo "Updated $lua_file"
    else
        echo "File not found: $lua_file"
    fi
done

echo "All expected Lua files updated!"
