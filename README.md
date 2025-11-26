# Dashboard Data Provider Plugin

A SimHub plugin that exposes dashboard telemetry data via HTTP for use with Loupedeck and other devices.

## Features

- Exposes racing telemetry data via HTTP JSON API on `http://localhost:8080/dashboarddata/`
- Manages target lap time with increment/decrement controls
- Persists target time settings between sessions
- Real-time lap timing data (current, last, fastest)
- Delta information from session best

## API Endpoints

### GET /dashboarddata/
Returns current telemetry data in JSON format:
```json
{
  "LastLapTime": 123.456,
  "CurrentLapTime": 45.123,
  "FastestLapTime": 122.100,
  "CompactDelta": "+1.234",
  "SessionBestLiveDeltaSeconds": "1.23",
  "TargetTime": 120.500
}
```

### POST /dashboarddata/settarget
Set target lap time directly.
```json
{
  "targetTime": 120.5
}
```

### POST /dashboarddata/adjust
Adjust target time by delta (e.g., +0.1 or -0.1).
```json
{
  "delta": 0.1
}
```

### POST /dashboarddata/resettofast
Reset target time to fastest lap time (reads from current telemetry data).
- No request body required
- Returns error if no fastest lap time is available

### POST /dashboarddata/resettolast
Reset target time to last lap time (reads from current telemetry data).
- No request body required
- Returns error if no last lap time is available

## Installation

1. Build the plugin: `dotnet build` or compile in Visual Studio
2. Copy the built DLL to your SimHub plugins directory
3. Restart SimHub
4. The plugin will start the HTTP server automatically

## Requirements

- SimHub
- .NET Framework 4.8 or higher
- Newtonsoft.Json NuGet package (included in project)

## Usage with Loupedeck

Create a Loupedeck plugin that:
1. Calls `GET /dashboarddata/` to fetch current telemetry and target time
2. Uses buttons/dials to increment/decrement target time via `POST /dashboarddata/adjust`
3. Displays the target time and current delta on the Loupedeck display
