FROM mcr.microsoft.com/dotnet/sdk:9.0-preview AS build
WORKDIR /src

# Copy project file(s) and restore (minimize context for faster rebuilds)
COPY *.csproj ./
RUN dotnet restore

# Copy remaining sources and publish
COPY . ./
# Use --no-restore because restore already ran.
RUN dotnet publish -c Release -o /app --no-restore 

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:9.0-preview
WORKDIR /app

# Copy published app
COPY --from=build /app ./

# Expose a volume for host read/write of input/output files
VOLUME ["/data"]

# Set environment variable placeholder for Finnhub token (override with -e at runtime)
ENV FinnhubToken=""

# Entrypoint supports additional args (app accepts filenames as arguments)
ENTRYPOINT ["dotnet", "StockAnalyzer.dll"]