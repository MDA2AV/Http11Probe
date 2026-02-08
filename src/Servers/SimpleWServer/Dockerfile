FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Servers/SimpleWServer/ src/Servers/SimpleWServer/
RUN dotnet restore src/Servers/SimpleWServer/SimpleWServer.csproj
RUN dotnet publish src/Servers/SimpleWServer/SimpleWServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "SimpleWServer.dll", "8080"]
