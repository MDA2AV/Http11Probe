FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Servers/NetCoreServerFramework/ src/Servers/NetCoreServerFramework/
RUN dotnet restore src/Servers/NetCoreServerFramework/NetCoreServerFramework.csproj
RUN dotnet publish src/Servers/NetCoreServerFramework/NetCoreServerFramework.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "NetCoreServerFramework.dll", "8080"]
