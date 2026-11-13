FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy source files
COPY . ./

# Remove Windows build artifacts
RUN find . -type d \( -name bin -o -name obj -o -name .vs \) -prune -exec rm -rf {} +

# Restore packages inside Linux
RUN dotnet restore QamishloDeliveryWeb.csproj --force --no-cache

# Publish application
RUN dotnet publish QamishloDeliveryWeb.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "QamishloDeliveryWeb.dll"]