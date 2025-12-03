# Dashboard Data Monitor
# Periodically fetches and displays data from SimHub dashboard endpoint

param(
    [int]$IntervalSeconds = 2,
    [string]$Url = "http://localhost:8080/dashboarddata/"
)

Write-Host "Dashboard Data Monitor" -ForegroundColor Cyan
Write-Host "Fetching from: $Url" -ForegroundColor Cyan
Write-Host "Update interval: $IntervalSeconds seconds" -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop`n" -ForegroundColor Yellow

$iteration = 0

while ($true) {
    Clear-Host
    $iteration++
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    
    try {
        $response = Invoke-RestMethod -Uri $Url -Method Get -TimeoutSec 5
        
        Write-Host "=== Dashboard Data Monitor ===" -ForegroundColor Cyan
        Write-Host "Iteration: $iteration | Time: $timestamp`n" -ForegroundColor Green
        
        # Display the response
        if ($response -is [string]) {
            Write-Host $response
        } else {
            $response | ConvertTo-Json -Depth 10 | Write-Host
        }
        
    } catch {
        Write-Host "=== Dashboard Data Monitor ===" -ForegroundColor Cyan
        Write-Host "Iteration: $iteration | Time: $timestamp`n" -ForegroundColor Red
        Write-Host "Error fetching data: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Start-Sleep -Seconds $IntervalSeconds
}
