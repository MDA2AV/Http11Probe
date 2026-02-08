FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Servers/WatsonServer/ src/Servers/WatsonServer/
RUN dotnet restore src/Servers/WatsonServer/WatsonServer.csproj
RUN dotnet publish src/Servers/WatsonServer/WatsonServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "WatsonServer.dll", "8080"]
