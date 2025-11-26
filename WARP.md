# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

This is a **SimHub plugin** that exposes racing telemetry data via an HTTP JSON API. The plugin is designed to work with Loupedeck devices and other external applications that need access to real-time dashboard data.

**Key Architecture:**
- Single C# class (`DashboardDataProvider.cs`) implementing SimHub's `IPlugin` and `IDataPlugin` interfaces
- Embedded HTTP server running on `localhost:8080` using `HttpListener`
- Thread-safe data access with multiple locks for HTTP listener, latest data, and target time
- Persistent settings stored in `%AppData%\SimHub\DashboardDataProvider\settings.json`

## Build Commands

```powershell
# Build in Release mode (for SimHub deployment)
dotnet build -c Release

# Build in Debug mode
dotnet build -c Debug

# Clean build artifacts
dotnet clean

# Restore NuGet packages
dotnet restore
```

**MSBuild (Visual Studio):**
```powershell
# Build using MSBuild
msbuild DashboardDataProviderPlugin.sln /p:Configuration=Release

# Clean
msbuild DashboardDataProviderPlugin.sln /t:Clean
```

## Development Workflow

### Building and Testing the Plugin

1. **Build**: Compile in Release mode to generate the DLL
2. **Deploy**: Copy `bin\Release\DashboardDataProviderPlugin.dll` to SimHub plugins directory
   - Default location: `C:\Program Files (x86)\SimHub\Plugins\`
3. **Restart SimHub**: Required for plugin to load
4. **Verify**: Check SimHub Plugins tab for "Dashboard Data Provider" status

### Testing HTTP Endpoints

```powershell
# Test GET endpoint
Invoke-WebRequest -Uri "http://localhost:8080/dashboarddata/" -Method Get | Select-Object -ExpandProperty Content | ConvertFrom-Json

# Test adjust endpoint (increment by 0.1 seconds)
$body = @{ delta = 0.1 } | ConvertTo-Json
Invoke-WebRequest -Uri "http://localhost:8080/dashboarddata/adjust" -Method Post -Body $body -ContentType "application/json"

# Test set target endpoint
$body = @{ targetTime = 120.5 } | ConvertTo-Json
Invoke-WebRequest -Uri "http://localhost:8080/dashboarddata/settarget" -Method Post -Body $body -ContentType "application/json"

# Test reset to fastest lap (uses FastestLapTime from _latestData)
Invoke-WebRequest -Uri "http://localhost:8080/dashboarddata/resettofast" -Method Post -ContentType "application/json"

# Test reset to last lap (uses LastLapTime from _latestData)
Invoke-WebRequest -Uri "http://localhost:8080/dashboarddata/resettolast" -Method Post -ContentType "application/json"
```

## Code Architecture

### Plugin Lifecycle
- **Init()**: Called once on plugin load. Initializes HTTP server, loads settings, registers SimHub properties
- **DataUpdate()**: Called every frame when game is running. Updates `_latestData` with current telemetry
- **End()**: Called on plugin unload. Stops HTTP server and cleans up threads

### HTTP Server Architecture
- Runs on background thread (`_httpThread`)
- Listens on `http://localhost:8080/dashboarddata/`
- **GET**: Returns latest telemetry data as JSON
- **POST**: Handles target time adjustments and resets
- Thread-safe access to shared data using locks

### SimHub Integration Points
- **Dependencies**: Requires `GameReaderCommon.dll` and `SimHub.Plugins.dll` from SimHub installation
- **Game Data Access**: Via `GameData.NewData` object in `DataUpdate()` method
- **SimHub Properties**: Reads `DataCorePlugin.CustomExpression.CompactDelta` and `PersistantTrackerPlugin.SessionBestLiveDeltaSeconds`
- **Exposed Properties**: 
  - `DashboardData.PluginStatus` (Initialized/Running/Game Not Running/Stopped)
  - `DashboardData.TargetTime` (current target lap time)

### Data Flow
1. SimHub calls `DataUpdate()` every frame with fresh `GameData`
2. Plugin extracts telemetry fields and stores in `_latestData` (thread-safe)
3. HTTP GET requests return `_latestData` as JSON
4. HTTP POST requests modify `_targetTime` and persist to `settings.json`
5. Target time changes are reflected in next `DataUpdate()` cycle

## Key Considerations

### Thread Safety
- Three separate locks: `_latestDataLock`, `_listenerLock`, `_targetTimeLock`
- Always acquire locks when accessing `_latestData` or `_targetTime`
- HTTP server runs on background thread; must synchronize with SimHub's main thread

### SimHub Plugin References
- **SimHub DLLs are NOT in the repository** - they must be referenced from the SimHub installation directory
- Default path: `C:\Program Files (x86)\SimHub\`
- Required DLLs: `GameReaderCommon.dll`, `SimHub.Plugins.dll`
- If SimHub is installed elsewhere, update `.csproj` `<HintPath>` elements

### Settings Persistence
- Target time is persisted to `%AppData%\SimHub\DashboardDataProvider\settings.json`
- Auto-creates directory if it doesn't exist
- Silent failure on settings save/load (falls back to 0.0 target time)

### Testing Without SimHub
HTTP server will not start without SimHub context, but you can:
1. Test JSON serialization logic independently
2. Mock `PluginManager` for unit tests
3. Test HTTP endpoints only when plugin is loaded in SimHub

## HTTP API Reference

All endpoints expect `Content-Type: application/json` for POST requests.

**GET /dashboarddata/**
- Returns: Current telemetry data including `LastLapTime`, `CurrentLapTime`, `FastestLapTime`, `CompactDelta`, `SessionBestLiveDeltaSeconds`, `TargetTime`

**POST /dashboarddata/settarget**
- Body: `{ "targetTime": <double> }`
- Sets target time to exact value

**POST /dashboarddata/adjust**
- Body: `{ "delta": <double> }`
- Adjusts target time by delta (can be negative)

**POST /dashboarddata/resettofast**
- Body: None required
- Sets target time to fastest lap time from current `_latestData`
- Returns error if no fastest lap time is available

**POST /dashboarddata/resettolast**
- Body: None required
- Sets target time to last lap time from current `_latestData`
- Returns error if no last lap time is available

## File Locations

- **Plugin DLL**: `bin\Release\DashboardDataProviderPlugin.dll` (after build)
- **SimHub Plugins Directory**: `C:\Program Files (x86)\SimHub\Plugins\`
- **Settings File**: `%AppData%\SimHub\DashboardDataProvider\settings.json`
- **Source Code**: Single file at `DashboardDataProvider.cs`

## Dependencies

- **.NET Framework 4.8** (target framework)
- **Newtonsoft.Json 13.0.3** (NuGet package for JSON serialization)
- **SimHub Installation** (provides `GameReaderCommon.dll` and `SimHub.Plugins.dll`)

## Troubleshooting

**Plugin not loading in SimHub:**
- Verify DLL is in correct plugins directory
- Check SimHub logs for errors
- Ensure .NET Framework 4.8 is installed

**HTTP server not responding:**
- Confirm plugin status is "Running" in SimHub Plugins tab
- Check Windows Firewall isn't blocking localhost:8080
- Verify SimHub is running and a game is active

**Data not updating:**
- Check that a racing sim is actually running and generating telemetry
- Verify `DataUpdate()` is being called (check plugin status shows "Running")
- Ensure SimHub properties `CompactDelta` and `SessionBestLiveDeltaSeconds` exist
