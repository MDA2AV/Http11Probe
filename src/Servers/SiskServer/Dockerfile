FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Servers/SiskServer/ src/Servers/SiskServer/
RUN dotnet restore src/Servers/SiskServer/SiskServer.csproj
RUN dotnet publish src/Servers/SiskServer/SiskServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "SiskServer.dll", "8080"]
