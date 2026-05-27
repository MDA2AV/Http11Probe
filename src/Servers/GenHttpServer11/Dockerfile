FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Servers/GenHttpServer11/ src/Servers/GenHttpServer11/
RUN dotnet restore src/Servers/GenHttpServer11/GenHttpServer.csproj
RUN dotnet publish src/Servers/GenHttpServer11/GenHttpServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "GenHttpServer.dll", "8080"]
