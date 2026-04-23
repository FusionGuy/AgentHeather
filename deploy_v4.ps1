$ErrorActionPreference = "Continue"

$token = (az account get-access-token --resource https://management.azure.com --query accessToken -o tsv 2>&1 | Where-Object { $_ -notmatch 'Warning|UserWarning' }).Trim()
$headers = @{ Authorization = "Bearer $token" }

# Delete ALL failed deployments  
Write-Host "=== Cleaning up failed deployments ==="
$deployments = Invoke-RestMethod -Uri "https://heather-demo-chat.scm.azurewebsites.net/api/deployments" -Headers $headers
foreach ($d in $deployments) {
    if ($d.status -ne 4) {  # Not successful
        Write-Host "  Deleting deployment $($d.id) (status=$($d.status), complete=$($d.complete))"
        Invoke-RestMethod -Uri "https://heather-demo-chat.scm.azurewebsites.net/api/deployments/$($d.id)" -Method Delete -Headers $headers -ErrorAction SilentlyContinue
    }
}

# Kill the SCM site to force restart
Write-Host "`n=== Restarting SCM site ==="
$subscriptionId = (az account show --query id -o tsv 2>&1 | Where-Object { $_ -notmatch 'Warning|UserWarning' }).Trim()
try {
    Invoke-RestMethod -Uri "https://heather-demo-chat.scm.azurewebsites.net/api/processes/0" -Method Delete -Headers $headers -ErrorAction SilentlyContinue
    Write-Host "SCM process killed"
} catch {
    Write-Host "Kill attempt: $($_.Exception.Message)"
}

Write-Host "Waiting 30s for SCM to restart..."
Start-Sleep -Seconds 30

# Verify SCM is back
Write-Host "`n=== Verifying SCM is back ==="
try {
    $env = Invoke-RestMethod -Uri "https://heather-demo-chat.scm.azurewebsites.net/api/environment" -Headers $headers
    Write-Host "SCM is up. Version: $($env.version)"
} catch {
    Write-Host "SCM not ready: $($_.Exception.Message)"
    Start-Sleep -Seconds 15
}

# Deploy
Write-Host "`n=== Deploying ==="
$deployHeaders = @{
    Authorization  = "Bearer $token"
    "Content-Type" = "application/octet-stream"
}
try {
    $bytes = [System.IO.File]::ReadAllBytes("C:\temp\heather_deploy.zip")
    $response = Invoke-WebRequest -Uri "https://heather-demo-chat.scm.azurewebsites.net/api/zipdeploy" -Method Post -Body $bytes -Headers $deployHeaders -UseBasicParsing -TimeoutSec 300
    Write-Host "Deploy status: $($response.StatusCode)"
} catch {
    Write-Host "Deploy failed: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $body = $reader.ReadToEnd()
            Write-Host "Response body: [$body]"
        } catch {}
    }
}

Write-Host "`nRestarting app..."
az webapp restart --name heather-demo-chat --resource-group RG-Marc.Merritt 2>&1 | Out-Null
Write-Host "Done!"
