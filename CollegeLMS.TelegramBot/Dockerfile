FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CollegeLMS.TelegramBot/CollegeLMS.TelegramBot.csproj CollegeLMS.TelegramBot/
RUN dotnet restore CollegeLMS.TelegramBot/CollegeLMS.TelegramBot.csproj

COPY CollegeLMS.TelegramBot/ CollegeLMS.TelegramBot/
RUN dotnet publish CollegeLMS.TelegramBot/CollegeLMS.TelegramBot.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CollegeLMS.TelegramBot.dll"]
