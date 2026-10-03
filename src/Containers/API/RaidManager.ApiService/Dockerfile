FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore RaidManager.sln
RUN dotnet publish src/Containers/API/RaidManager.ApiService/RaidManager.ApiService.csproj \
    -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
# The .NET images define a non-root "app" user; APP_UID is its id.
USER $APP_UID
ENTRYPOINT ["dotnet", "RaidManager.ApiService.dll"]
