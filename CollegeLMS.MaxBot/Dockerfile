FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY CollegeLMS.MaxBot/CollegeLMS.MaxBot.csproj CollegeLMS.MaxBot/
RUN dotnet restore CollegeLMS.MaxBot/CollegeLMS.MaxBot.csproj
COPY . .
RUN dotnet publish CollegeLMS.MaxBot/CollegeLMS.MaxBot.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CollegeLMS.MaxBot.dll"]
