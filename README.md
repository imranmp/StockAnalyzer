# StockAnalyzer

A small .NET 9 console utility to fetch and aggregate analyst recommendation data for stock tickers using the Finnhub API. It loads tickers from `Tickers.txt`, fetches recommendation data, stores results in JSON, and exports a CSV summary.

> Note: This repository contains a lightweight utility intended for personal or experimental use. Configure a Finnhub API token before running.

## Features

- Load a list of tickers from `Tickers.txt`.
- Fetch analyst recommendations from Finnhub (`/api/v1/stock/recommendation`).
- Merge and store analysis results in `AnalysisResult.json`.
- Export results to `AnalysisResult.csv`.
- Simple scoring system to rank tickers by analyst sentiment.

## Quickstart

Prerequisites

- .NET 9 SDK
- A Finnhub API token (free tier available at https://finnhub.io)

Configuration

1. Open `src/StockAnalyzer/appsettings.json` and set the `FinnhubToken` value, or set the `FinnhubToken` environment variable.

Running locally

From the repository root run:

```bash
cd src/StockAnalyzer
dotnet run --project StockAnalyzer.csproj
```

The app will:
- Read tickers from `Tickers.txt` in the project folder.
- Fetch recommendation data from Finnhub for each ticker.
- Merge results with any existing `AnalysisResult.json`.
- Write a new `AnalysisResult.json` and `AnalysisResult.csv`.

## Configuration options

- `Tickers.txt` — tab-delimited file with ticker and (optional) company name.
- `appsettings.json` — contains `FinnhubToken` and logging configuration.

## Output files

- `AnalysisResult.json` — persisted aggregated analysis data.
- `AnalysisResult.csv` — CSV export of the current analysis.

## Project structure

- `src/StockAnalyzer/Program.cs` — DI and HttpClient setup.
- `src/StockAnalyzer/Services/App.cs` — application orchestration.
- `src/StockAnalyzer/Services/AnalysisFetcher.cs` — HTTP client calls to Finnhub.
- `src/StockAnalyzer/Services/FileStorage.cs` — load/save JSON, open CSV stream.
- `src/StockAnalyzer/Services/TickerProvider.cs` — reads `Tickers.txt`.
- `src/StockAnalyzer/Models/Analysis.cs` — model and scoring logic.
- `src/StockAnalyzer/Converters` — custom JSON converters.

## Notes and suggestions

- The code includes TODOs for improving configurability (pass input/output paths, limit number of tickers, support different separators).
- The Finnhub API enforces rate limits; consider batching or adding delay/queueing for large ticker lists.
- Consider adding unit tests and a validation step for `Tickers.txt`.

## Troubleshooting

> **Tip:** If you see errors about missing `FinnhubToken`, set it in `appsettings.json` or export the `FinnhubToken` environment variable.

If HTTP requests fail, inspect logs (console) for retry messages and errors. The project uses a resilience pipeline for the named `Finnhub` HttpClient.

## Acknowledgements

This is a personal utility inspired by small data-collection tools and public financial APIs.
