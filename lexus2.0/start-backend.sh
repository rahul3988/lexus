#!/bin/bash

# Bash script to start the backend API
echo "Starting Lexus 2.0 Backend API..."

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK not found. Please install .NET 8.0 SDK."
    exit 1
fi

echo "Found .NET version: $(dotnet --version)"

# Navigate to API directory
API_PATH="$(dirname "$0")/backend/Lexus2.0.API"
if [ ! -d "$API_PATH" ]; then
    echo "Error: API directory not found at $API_PATH"
    exit 1
fi

cd "$API_PATH" || exit 1

# Restore and build
echo "Restoring packages..."
dotnet restore
if [ $? -ne 0 ]; then
    echo "Error: Failed to restore packages"
    exit 1
fi

echo "Building project..."
dotnet build
if [ $? -ne 0 ]; then
    echo "Error: Build failed"
    exit 1
fi

# Run the API
echo "Starting API server on http://localhost:5000..."
echo "Press Ctrl+C to stop the server"
echo ""

dotnet run

