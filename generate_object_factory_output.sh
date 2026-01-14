#!/bin/bash

# Run Luban for object_factory test to generate output files
cd tests/Luban.IntegrationTests/bin/Debug/net8.0

# Run Luban CLI
./Luban.dll \
  --conf ../../TestData/object_factory/schema/luban.conf \
  -t test \
  -c lua-bin \
  -d json

echo "Generated files:"
find . -name "*.lua" -o -name "*.json" | head -20

# Copy to expected directories for manual inspection
mkdir -p ../../TestData/object_factory/expected/lua
mkdir -p ../../TestData/object_factory/expected/json

find . -name "schema.lua" -exec cp {} ../../TestData/object_factory/expected/lua/ \;
find . -name "tbskill.json" -exec cp {} ../../TestData/object_factory/expected/json/ \;

echo "Copied generated files to expected/ directories"
