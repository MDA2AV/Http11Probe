FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Servers/NancyServer/ src/Servers/NancyServer/
RUN dotnet restore src/Servers/NancyServer/NancyServer.csproj
RUN dotnet publish src/Servers/NancyServer/NancyServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "NancyServer.dll", "9006"]
