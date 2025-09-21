# Multi-stage Dockerfile for StockAnalyzer (.NET 9)

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file(s) and restore as distinct layers
COPY src/StockAnalyzer/StockAnalyzer.csproj src/StockAnalyzer/
RUN dotnet restore "src/StockAnalyzer/StockAnalyzer.csproj"

# Copy the rest of the source and publish
COPY . .
WORKDIR /src/src/StockAnalyzer
RUN dotnet publish -c Release -o /app/publish --no-restore

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Default environment
ENV DOTNET_ENVIRONMENT=Production

# Provide FinnhubToken via env var when running the container
# Example: -e FinnhubToken=YOUR_TOKEN

ENTRYPOINT ["dotnet", "StockAnalyzer.dll"]
