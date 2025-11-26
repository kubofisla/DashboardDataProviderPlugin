while ($true) {
    # Do not clear the console so previous output stays visible
    # Clear-Host

    try {
        # Call the endpoint
        $response = Invoke-WebRequest -Uri "http://localhost:8080/dashboarddata/" -Method Get

        # If the response is JSON, parse it and display nicely
        $data = $response.Content | ConvertFrom-Json

        Write-Host "==== $(Get-Date -Format "HH:mm:ss") ====" -ForegroundColor Cyan
        $data | Format-List  # Or use Format-Table, or just "$data"
        Write-Host ""  # blank line between updates
    }
    catch {
        Write-Host "Request failed: $_" -ForegroundColor Red
    }

    Start-Sleep -Seconds 2
}
