FROM mcr.microsoft.com/dotnet/sdk:9.0-preview AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY *.csproj .
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:9.0-preview
WORKDIR /app
COPY --from=build /app .
COPY Tickers.txt .

# Set environment variable for Finnhub token
ENV FinnhubToken=""

ENTRYPOINT ["dotnet", "StockAnalyzer.dll"] 