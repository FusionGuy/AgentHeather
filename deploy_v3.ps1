$ErrorActionPreference = "Continue"

$token = (az account get-access-token --resource https://management.azure.com --query accessToken -o tsv 2>&1 | Where-Object { $_ -notmatch 'Warning|UserWarning' }).Trim()
$headers = @{ Authorization = "Bearer $token" }

# Check for in-progress deployments
Write-Host "=== Checking deployments ==="
$deployments = Invoke-RestMethod -Uri "https://heather-demo-chat.scm.azurewebsites.net/api/deployments" -Headers $headers
foreach ($d in $deployments) {
    Write-Host "  $($d.id): status=$($d.status) complete=$($d.complete) active=$($d.active)"
    # Delete incomplete deployments
    if (-not $d.complete) {
        Write-Host "  -> Deleting incomplete deployment $($d.id)"
        Invoke-RestMethod -Uri "https://heather-demo-chat.scm.azurewebsites.net/api/deployments/$($d.id)" -Method Delete -Headers $headers -ErrorAction SilentlyContinue
    }
}

# Check app settings
Write-Host "`n=== App Settings ==="
$settings = az webapp config appsettings list --name heather-demo-chat --resource-group RG-Marc.Merritt -o json 2>&1 | Where-Object { $_ -notmatch 'Warning|UserWarning' }
$settingsObj = $settings | ConvertFrom-Json
foreach ($s in $settingsObj) {
    if ($s.name -match "SCM|BUILD|ENABLE_ORYX") {
        Write-Host "  $($s.name) = $($s.value)"
    }
}

# Ensure build is disabled
Write-Host "`n=== Setting ENABLE_ORYX_BUILD=false ==="
az webapp config appsettings set --name heather-demo-chat --resource-group RG-Marc.Merritt --settings ENABLE_ORYX_BUILD=false SCM_DO_BUILD_DURING_DEPLOYMENT=false 2>&1 | Out-Null

# Wait a bit for settings to propagate
Start-Sleep -Seconds 5

# Try deploy via OneDeploy API (different from zipdeploy)
Write-Host "`n=== Deploying via OneDeploy API ==="
$deployHeaders = @{
    Authorization  = "Bearer $token"
    "Content-Type" = "application/zip"
}
try {
    # The /api/publish endpoint is the newer OneDeploy API
    $response = Invoke-WebRequest -Uri "https://heather-demo-chat.scm.azurewebsites.net/api/publish?type=zip&restart=true&clean=true&ignorestack=true" -Method Post -InFile "C:\temp\heather_deploy.zip" -Headers $deployHeaders -UseBasicParsing -TimeoutSec 300
    Write-Host "Deploy status: $($response.StatusCode)"
    Write-Host "Response: $($response.Content)"
} catch {
    Write-Host "OneDeploy failed: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $body = $reader.ReadToEnd()
            Write-Host "Response body: $body"
        } catch {}
    }
}

Write-Host "`nRestarting..."
az webapp restart --name heather-demo-chat --resource-group RG-Marc.Merritt 2>&1 | Out-Null
Write-Host "Done!"
