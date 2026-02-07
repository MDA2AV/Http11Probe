FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Servers/AspNetMinimal/ src/Servers/AspNetMinimal/
RUN dotnet restore src/Servers/AspNetMinimal/AspNetMinimal.csproj
RUN dotnet publish src/Servers/AspNetMinimal/AspNetMinimal.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "AspNetMinimal.dll"]
