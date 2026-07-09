FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY agentbridge/agentbridge.csproj agentbridge/
RUN dotnet restore agentbridge/agentbridge.csproj

COPY agentbridge/ agentbridge/
RUN dotnet publish agentbridge/agentbridge.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "agentbridge.dll"]
