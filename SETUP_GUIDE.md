# Setup and Installation Guide

## Prerequisites

- **SimHub** installed
- **Loupedeck** software installed
- **Visual Studio 2022** or later (for building)
- **.NET Framework 4.8** or higher

## Part 1: Dashboard Data Provider Plugin (SimHub)

### Step 1: Build the Plugin

1. Open `DashboardDataProviderPlugin.sln` in Visual Studio
2. Build in **Release** mode: `Build > Build Solution`
3. Find the compiled DLL at: `bin\Release\DashboardDataProviderPlugin.dll`

### Step 2: Install to SimHub

1. Locate your SimHub plugins directory:
   - Typically: `C:\Program Files (x86)\SimHub\`
   - Or check SimHub settings for custom plugin path

2. Copy the DLL to the plugins folder:
   ```
   DashboardDataProviderPlugin.dll → SimHub\Plugins\
   ```

3. Restart SimHub

4. Verify plugin loaded:
   - Go to **Plugins** tab in SimHub
   - Look for "Dashboard Data Provider" in the list
   - Status should show "Running"

### Step 3: Test the HTTP Server

Once SimHub and the plugin are running, test the API:

```powershell
# PowerShell command
Invoke-WebRequest -Uri "http://localhost:8080/dashboarddata/" -Method Get | ConvertTo-Json

# Or use curl
curl http://localhost:8080/dashboarddata/
```

Expected response:
```json
{
  "LastLapTime": "00:01:30.1860000",
  "CurrentLapTime": "00:00:31.3470000",
  "FastestLapTime": "00:01:30.1860000",
  "CompactDelta": "0.0",
  "SessionBestLiveDeltaSeconds": "-7.56",
  "TargetTime": 120.5
}
```

## Part 2: Loupedeck SimHub Integration Plugin

### Step 1: Build the Plugin

1. Open `src/SimHubIntegrationPlugin.csproj` in Visual Studio
2. Build in **Release** mode: `Build > Build Solution`
3. Compiled plugin: `src/bin/Release/`

### Step 2: Package the Plugin

The plugin needs to be packaged as `.lplug4` file:

1. Ensure you have the Loupedeck SDK installed
2. Use Loupedeck CLI to package:
   ```bash
   loupedeck plugin package --input src/bin/Release --output SimHubIntegration.lplug4
   ```
   
   Or if using Visual Studio Loupedeck extension:
   - Right-click project > **Package Plugin**

### Step 3: Install to Loupedeck

**Method A: Using Loupedeck Software**
1. Open Loupedeck software
2. Go to **Plugins** section
3. Click **+ Install Plugin**
4. Select the `.lplug4` file
5. Restart Loupedeck

**Method B: Manual Installation**
1. Find Loupedeck plugins folder:
   - `%AppData%\Loupedeck\Plugins\`
   
2. Extract `.lplug4` file to plugins folder:
   ```bash
   # The .lplug4 is a ZIP archive
   Expand-Archive -Path "SimHubIntegration.lplug4" -DestinationPath "$env:AppData\Loupedeck\Plugins\SimHubIntegration"
   ```

3. Restart Loupedeck software

### Step 4: Configure on Device

1. Launch Loupedeck software
2. Open your device configuration
3. Navigate to the middle row where you want the delta display
4. Add a new trigger:
   - Search for "Delta Display"
   - Drag it to the 4 center boxes in the middle row
5. Save configuration

## Testing

### Test 1: Verify HTTP Server is Running

```powershell
# Should return 200 and JSON data
Invoke-WebRequest -Uri "http://localhost:8080/dashboarddata/" -Method Get
```

### Test 2: Test Adjust Endpoint

```powershell
# Increase target by 0.1 seconds
$body = @{ delta = 0.1 } | ConvertTo-Json
Invoke-WebRequest -Uri "http://localhost:8080/dashboarddata/adjust" -Method Post -Body $body -ContentType "application/json"

# Expected response:
# { "status": "success", "targetTime": 120.6 }
```

### Test 3: Test Reset Endpoints

```powershell
# Reset to fastest lap
$body = @{ fastestLapTime = 119.250 } | ConvertTo-Json
Invoke-WebRequest -Uri "http://localhost:8080/dashboarddata/resettofast" -Method Post -Body $body -ContentType "application/json"

# Reset to last lap
$body = @{ lastLapTime = 120.100 } | ConvertTo-Json
Invoke-WebRequest -Uri "http://localhost:8080/dashboarddata/resettolast" -Method Post -Body $body -ContentType "application/json"
```

### Test 4: Launch Sim and Verify Display

1. Start SimHub
2. Launch your racing sim
3. Go on track to generate telemetry
4. Check Loupedeck device:
   - Delta display should show current delta
   - Background should be **GREEN** (faster than target) or **YELLOW** (slower)
   - Color updates as you drive

## Troubleshooting

### HTTP Server Not Responding

**Problem**: `Invoke-WebRequest` times out on `http://localhost:8080`

**Solutions**:
1. Verify SimHub is running
2. Check plugin loaded in SimHub Plugins tab
3. Check Windows Firewall allows localhost:8080
4. Look in SimHub logs for errors

**Enable logs**:
```powershell
# Add to DashboardDataProvider.cs if needed (after PluginManager.SetPropertyValue)
# System.IO.File.AppendAllText("C:\temp\dashboarddata.log", $"[{DateTime.Now}] Data updated\n")
```

### Loupedeck Plugin Not Loading

**Problem**: Delta Display trigger not appearing in Loupedeck

**Solutions**:
1. Rebuild plugin in Visual Studio
2. Re-package `.lplug4` file
3. Fully restart Loupedeck software
4. Check plugin folder permissions
5. Verify `.lplug4` contains all required files

### Delta Not Updating

**Problem**: Delta display shows 0.0 and doesn't change while driving

**Solutions**:
1. Verify `SessionBestLiveDeltaSeconds` is being sent from SimHub
2. Check URL in DataLoader points to correct port (8080)
3. Verify target time is set (check settings.json in AppData)
4. Make sure you're on track and game is running

## File Locations

### SimHub Plugin Settings
```
%AppData%\SimHub\DashboardDataProvider\settings.json
```

### Loupedeck Plugin Configuration
```
%AppData%\Loupedeck\Plugins\SimHubIntegration\
```

### SimHub Plugins
```
C:\Program Files (x86)\SimHub\Plugins\
```

## Next Steps

1. Build and install Dashboard Data Provider plugin
2. Test HTTP endpoints
3. Build and install Loupedeck plugin
4. Configure delta display on device
5. Launch sim and verify everything works

See `DELTA_DISPLAY.md` in Loupedeck plugin for feature documentation.
