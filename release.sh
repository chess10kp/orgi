dotnet publish Orgi.Core \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:UseAppHost=true

mkdir -p orgi-linux-x64
cp ./Orgi.Core/bin/Release/net10.0/linux-x64/publish/Orgi.Core orgi-linux-x64/
cp install.sh orgi-linux-x64/
tar -czf orgi-linux-x64.tar.gz orgi-linux-x64
