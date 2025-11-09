# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln ./
COPY AXMonitoringBU.Api/*.csproj ./AXMonitoringBU.Api/
COPY AXMonitoringBU.Blazor/*.csproj ./AXMonitoringBU.Blazor/

# Restore dependencies
RUN dotnet restore

# Copy all source files
COPY . .

# Build API
WORKDIR /src/AXMonitoringBU.Api
RUN dotnet build -c Release -o /app/api

# Build Blazor
WORKDIR /src/AXMonitoringBU.Blazor
RUN dotnet build -c Release -o /app/blazor

# Publish stage for API
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS api-publish
WORKDIR /app
COPY --from=build /app/api .
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["dotnet", "AXMonitoringBU.Api.dll"]

# Publish stage for Blazor
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS blazor-publish
WORKDIR /app
COPY --from=build /app/blazor .
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["dotnet", "AXMonitoringBU.Blazor.dll"]

