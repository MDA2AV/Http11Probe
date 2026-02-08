FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Servers/EmbedIOServer/ src/Servers/EmbedIOServer/
RUN dotnet restore src/Servers/EmbedIOServer/EmbedIOServer.csproj
RUN dotnet publish src/Servers/EmbedIOServer/EmbedIOServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "EmbedIOServer.dll", "8080"]
