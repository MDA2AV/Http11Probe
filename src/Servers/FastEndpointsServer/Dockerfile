FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Servers/FastEndpointsServer/ src/Servers/FastEndpointsServer/
RUN dotnet restore src/Servers/FastEndpointsServer/FastEndpointsServer.csproj
RUN dotnet publish src/Servers/FastEndpointsServer/FastEndpointsServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "FastEndpointsServer.dll"]
