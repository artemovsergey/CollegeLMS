FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
USER $APP_UID
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["CollegeLMS.API/CollegeLMS.csproj", "CollegeLMS.API/"]
RUN dotnet restore "CollegeLMS.API/CollegeLMS.csproj"
COPY . .
WORKDIR "/src/CollegeLMS.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY CollegeLMS.API/appsettings.Production.json .
ENTRYPOINT ["dotnet", "CollegeLMS.dll"]
