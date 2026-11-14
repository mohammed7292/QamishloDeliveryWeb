# Build from the QamishloDeliveryWeb project directory (the directory containing this Dockerfile).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the complete project after .dockerignore excludes local Windows build artifacts.
COPY . ./

# Never reuse a Windows-generated project.assets.json in a Linux container.
RUN rm -rf ./bin ./obj \
    && dotnet restore QamishloDeliveryWeb.csproj --force --no-cache \
    && dotnet publish QamishloDeliveryWeb.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "QamishloDeliveryWeb.dll"]
