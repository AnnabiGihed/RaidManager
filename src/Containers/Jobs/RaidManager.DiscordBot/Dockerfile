FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore RaidManager.sln
RUN dotnet publish src/Containers/Jobs/RaidManager.DiscordBot/RaidManager.DiscordBot.csproj \
    -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
# The .NET images define a non-root "app" user; APP_UID is its id.
USER $APP_UID
ENTRYPOINT ["dotnet", "RaidManager.DiscordBot.dll"]
