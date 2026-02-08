FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Servers/ServiceStackServer/ src/Servers/ServiceStackServer/
RUN dotnet restore src/Servers/ServiceStackServer/ServiceStackServer.csproj
RUN dotnet publish src/Servers/ServiceStackServer/ServiceStackServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ServiceStackServer.dll"]
