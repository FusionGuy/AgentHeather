$ErrorActionPreference = "Continue"

# Wait for SCM to stabilize
Write-Host "Waiting 60s for SCM to stabilize..."
Start-Sleep -Seconds 60

# Get token fresh
$token = (az account get-access-token --resource https://management.azure.com --query accessToken -o tsv 2>&1 | Where-Object { $_ -notmatch 'Warning|UserWarning' }).Trim()
$headers = @{ Authorization = "Bearer $token" }

# Verify SCM is healthy
Write-Host "`n=== Checking SCM health ==="
try {
    $info = Invoke-RestMethod -Uri "https://heather-demo-chat.scm.azurewebsites.net/api/deployments" -Headers $headers -TimeoutSec 30
    Write-Host "SCM is healthy. Found $($info.Count) deployments."
} catch {
    Write-Host "SCM still unhealthy: $($_.Exception.Message)"
    Write-Host "Waiting another 60s..."
    Start-Sleep -Seconds 60
}

# Try deploy with fresh token
$token = (az account get-access-token --resource https://management.azure.com --query accessToken -o tsv 2>&1 | Where-Object { $_ -notmatch 'Warning|UserWarning' }).Trim()

Write-Host "`n=== Deploying ==="
az webapp deploy --name heather-demo-chat --resource-group RG-Marc.Merritt --src-path C:\temp\heather_deploy.zip --type zip --track-status false 2>&1 | ForEach-Object { Write-Host $_ }

Write-Host "`nRestarting..."
az webapp restart --name heather-demo-chat --resource-group RG-Marc.Merritt 2>&1 | Out-Null
Write-Host "Done!"
