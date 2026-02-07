FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Servers/GlyphServer/ src/Servers/GlyphServer/
RUN dotnet restore src/Servers/GlyphServer/GlyphServer.csproj
RUN dotnet publish src/Servers/GlyphServer/GlyphServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "GlyphServer.dll", "9001"]
