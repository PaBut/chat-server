all:
	 dotnet publish ChatServer/ChatServer.csproj -r linux-x64 --self-contained false /p:PublishSingleFile=true
	 cp ChatServer/bin/Release/net8.0/linux-x64/publish/ChatServer ipk24chat-server