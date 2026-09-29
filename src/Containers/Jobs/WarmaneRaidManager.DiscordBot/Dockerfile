FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore WarmaneRaidManager.sln
RUN dotnet publish src/Containers/Jobs/WarmaneRaidManager.DiscordBot/WarmaneRaidManager.DiscordBot.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WarmaneRaidManager.DiscordBot.dll"]
