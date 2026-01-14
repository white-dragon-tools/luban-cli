#!/bin/bash
# Script to download and install Luau for Windows

set -e

LUAU_VERSION="0.703"
LUAU_DIR=".luau"
LUAU_EXE="$LUAU_DIR/luau.exe"
LUAU_ANALYZE="$LUAU_DIR/luau-analyze.exe"

# Check if already installed
if [ -f "$LUAU_ANALYZE" ]; then
    echo "Luau is already installed at $LUAU_ANALYZE"
    exit 0
fi

echo "Downloading Luau v${LUAU_VERSION}..."

# Create directory
mkdir -p "$LUAU_DIR"

# Download for Windows x64
LUAU_URL="https://github.com/luau-lang/luau/releases/download/${LUAU_VERSION}/luau-windows.zip"
curl -L -o "$LUAU_DIR/luau-windows.zip" "$LUAU_URL"

# Extract
unzip -o "$LUAU_DIR/luau-windows.zip" -d "$LUAU_DIR"

# Cleanup
rm "$LUAU_DIR/luau-windows.zip"

# Verify installation
if [ -f "$LUAU_ANALYZE" ]; then
    echo "Luau installed successfully!"
    echo "  luau: $LUAU_EXE"
    echo "  luau-analyze: $LUAU_ANALYZE"

    # Add to PATH for current session
    export PATH="$LUAU_DIR:$PATH"

    # Verify
    luau-analyze --version
else
    echo "ERROR: Failed to install Luau"
    exit 1
fi
