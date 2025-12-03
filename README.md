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

You can either install a prebuilt DLL from GitHub Releases or build the plugin from source.

### Option A: Install from GitHub releases (recommended)

1. Download the latest release from: https://github.com/kubofisla/SimHubIntegrationPlugin/releases
2. Close SimHub.
3. Copy the plugin DLL from the release into your SimHub installation directory, for example:
   - `C:\\Program Files (x86)\\SimHub\\DashboardDataProviderPlugin.dll`
4. Start SimHub again.
5. In SimHub, open the **Plugins** tab and verify that **Dashboard Data Provider** is listed and running.

### Option B: Build from scratch

Follow `SETUP_GUIDE.md` in this repository for step-by-step instructions on building the plugin from source and deploying it into SimHub.

## Requirements

- SimHub
- .NET Framework 4.8 or higher
- Newtonsoft.Json NuGet package (included in project)

## Usage with Loupedeck

To visualize the telemetry data on a Loupedeck device, you can use the separate Loupedeck plugin project:

- **SimHubIntegrationPlugin**: https://github.com/kubofisla/SimHubIntegrationPlugin

That plugin consumes the HTTP API described above (for example `GET /dashboarddata/` and `POST /dashboarddata/adjust`) and displays the target time and delta data on the Loupedeck. If you build your own Loupedeck integration instead, you should:

1. Call `GET /dashboarddata/` to fetch current telemetry and target time.
2. Use buttons/dials to increment/decrement target time via `POST /dashboarddata/adjust`.
3. Display the target time and current delta on the Loupedeck display.
