#!/bin/bash
set -e

# Build for Linux
echo "Building for Linux x64..."
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishAot=true -o ./publish-nativaot-linux Orgi.Core/Orgi.Core.csproj

mkdir -p orgi-linux-x64
cp ./publish-nativaot-linux/Orgi.Core orgi-linux-x64/
cp install.sh orgi-linux-x64/
tar -czf orgi-linux-x64.tar.gz orgi-linux-x64

# Build for Windows
echo "Building for Windows x64..."
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishAot=true -o ./publish-nativaot-win Orgi.Core/Orgi.Core.csproj

mkdir -p orgi-win-x64
cp ./publish-nativaot-win/Orgi.Core.exe orgi-win-x64/
cp install.ps1 orgi-win-x64/
cd orgi-win-x64 && zip -r ../orgi-win-x64.zip . && cd ..

echo "Build complete!"
echo "Linux release: orgi-linux-x64.tar.gz"
echo "Windows release: orgi-win-x64.zip"
