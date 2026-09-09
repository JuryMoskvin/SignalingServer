FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/SignalingServer.Api/SignalingServer.Api.csproj src/SignalingServer.Api/
COPY src/SignalingServer.Application/SignalingServer.Application.csproj src/SignalingServer.Application/
RUN dotnet restore src/SignalingServer.Api/SignalingServer.Api.csproj

COPY src/ src/
RUN dotnet publish src/SignalingServer.Api/SignalingServer.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "SignalingServer.Api.dll"]
