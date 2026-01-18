dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishAot=true -o ./publish-nativaot Orgi.Core/Orgi.Core.csproj

mkdir -p orgi-linux-x64
cp ./publish-nativaot/Orgi.Core orgi-linux-x64/
cp install.sh orgi-linux-x64/
tar -czf orgi-linux-x64.tar.gz orgi-linux-x64
