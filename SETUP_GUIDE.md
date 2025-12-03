# Setup and Developer Guide

This guide describes how to build the Dashboard Data Provider SimHub plugin from source, deploy it into a local SimHub installation, and test the HTTP endpoints. It is aimed at developers working from scratch on this repository.

## 1. Prerequisites

- **SimHub** installed (typically `C:\Program Files (x86)\SimHub`)
- **.NET Framework 4.8** Developer Pack
- **Visual Studio 2022** or later with *.NET desktop development* workload (or MSBuild / .NET SDK capable of building .NET Framework projects)
- **Windows PowerShell 5.1+** (default on modern Windows)

## 2. Open the solution

1. Clone or download this repository to a local folder.
2. Open `DashboardDataProviderPlugin.sln` in Visual Studio.

The main project is `DashboardDataProviderPlugin.csproj`; tests live in `DashboardDataProviderPlugin.Tests.csproj`.

## 3. Build the plugin DLL

### Option A: Visual Studio

1. In Visual Studio, select **Release** (or **Debug**) configuration.
2. Use **Build > Build Solution**.
3. The compiled plugin will be at:
   - `bin\\<Configuration>\\DashboardDataProviderPlugin.dll` (for example, `bin\\Release\\DashboardDataProviderPlugin.dll`).

### Option B: Command line

From the repository root:

```powershell
msbuild .\DashboardDataProviderPlugin.csproj /p:Configuration=Release
```

This produces `bin\\Release\\DashboardDataProviderPlugin.dll`.

## 4. Deploy into SimHub (deploy-simhub-plugin.ps1)

Use the included `deploy-simhub-plugin.ps1` script to copy the built DLL into your SimHub installation and restart SimHub.

### What the script does

- Stops any running `SimHub` process.
- Copies `bin\\<Configuration>\\DashboardDataProviderPlugin.dll` to the SimHub installation directory.
- Starts SimHub again.

By default the script assumes:

- SimHub is installed in `C:\\Program Files (x86)\\SimHub`.
- The destination DLL path is `C:\\Program Files (x86)\\SimHub\\DashboardDataProviderPlugin.dll`.

If your setup is different (for example you want to place the DLL under `SimHub\\Plugins`), adjust the variables in the script accordingly.

### Usage

From the repository root in PowerShell:

```powershell
# Deploy Release build (default)
.\deploy-simhub-plugin.ps1

# Deploy Debug build
.\deploy-simhub-plugin.ps1 -Configuration Debug
```

After deployment, open SimHub and confirm that the **Dashboard Data Provider** plugin appears in the **Plugins** tab and is running.

## 5. Test the HTTP endpoints (test_endpoints.ps1)

Once SimHub and the plugin are running, you can validate the API using `test_endpoints.ps1`.

The script will:

- Call `GET http://localhost:8080/dashboarddata/` and print the JSON payload.
- Call `POST http://localhost:8080/dashboarddata/resettofast` to exercise one of the write endpoints.

Run it from the repository root:

```powershell
.\test_endpoints.ps1
```

A typical response from the `GET /dashboarddata/` endpoint looks like:

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

If the script cannot reach the endpoint, verify that SimHub is running and the plugin is loaded.

## 6. Monitor live data (monitor-dashboard.ps1)

For continuous monitoring of the dashboard data while driving, use `monitor-dashboard.ps1`.

### Usage

```powershell
# Poll every 2 seconds (default)
.\monitor-dashboard.ps1

# Poll every second
.\monitor-dashboard.ps1 -IntervalSeconds 1

# Poll a custom URL
.\monitor-dashboard.ps1 -Url "http://localhost:8080/dashboarddata/" -IntervalSeconds 1
```

The script clears the console on each iteration and prints:

- Iteration number and timestamp.
- The JSON response from the `/dashboarddata/` endpoint, or an error message if the request failed.

Use this while on track in your sim to confirm that values update as expected.

## 7. Optional: run unit tests

The solution includes a test project `DashboardDataProviderPlugin.Tests.csproj` using MSTest.

- In Visual Studio, open **Test Explorer**, run all tests, and ensure they pass after your changes.

## 8. Using Loupedeck to visualize data (optional)

This repository only provides the SimHub plugin and HTTP API. To visualize the telemetry on a Loupedeck device (including delta display), use the separate Loupedeck plugin project:

- **SimHubIntegrationPlugin**: https://github.com/kubofisla/SimHubIntegrationPlugin

Follow that repositorys README for build, installation, and configuration instructions. It consumes the HTTP endpoints exposed by this Dashboard Data Provider plugin.

## 9. Troubleshooting

### HTTP server not responding

**Problem**: Requests to `http://localhost:8080/dashboarddata/` time out or fail.

**Checklist**:

1. Verify SimHub is running.
2. Confirm the Dashboard Data Provider plugin is loaded and running in SimHubs **Plugins** tab.
3. Check Windows Firewall rules for SimHub / port 8080.
4. Look in SimHub logs for plugin-related errors.

### Inspecting raw responses

To quickly inspect the raw JSON manually:

```powershell
Invoke-WebRequest -Uri "http://localhost:8080/dashboarddata/" -Method Get | Select-Object -ExpandProperty Content
```

or using `curl`:

```powershell
curl http://localhost:8080/dashboarddata/
```

If you continue to have issues with visualizing the data on a Loupedeck device, refer to the troubleshooting section in the `SimHubIntegrationPlugin` repository.
