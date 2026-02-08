FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Servers/GenHttpServer/ src/Servers/GenHttpServer/
RUN dotnet restore src/Servers/GenHttpServer/GenHttpServer.csproj
RUN dotnet publish src/Servers/GenHttpServer/GenHttpServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "GenHttpServer.dll", "8080"]
