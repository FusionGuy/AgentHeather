Write-Host "Waiting 30 seconds for app to start..."
Start-Sleep -Seconds 30
try {
    $r = Invoke-WebRequest -Uri "https://heather-demo-chat.azurewebsites.net" -UseBasicParsing -TimeoutSec 120
    Write-Host "Status: $($r.StatusCode)"
    if ($r.Content -match "heather-avatar") {
        Write-Host "SUCCESS: Heather avatar found on page!"
    } else {
        Write-Host "Avatar NOT found in HTML"
    }
} catch {
    Write-Host "Error: $($_.Exception.Message)"
}
