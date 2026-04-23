$ErrorActionPreference = "Continue"

$token = (az account get-access-token --resource https://management.azure.com --query accessToken -o tsv 2>&1 | Where-Object { $_ -notmatch 'Warning|UserWarning' }).Trim()
Write-Host "Token acquired (length: $($token.Length))"

$headers = @{ Authorization = "Bearer $token" }

# Step 1: Delete the failed deployment that's blocking new ones
Write-Host "`n--- Deleting failed deployments ---"
try {
    $deployments = Invoke-RestMethod -Uri "https://heather-demo-chat.scm.azurewebsites.net/api/deployments" -Headers $headers
    foreach ($d in $deployments) {
        if ($d.status -eq 3) {  # status 3 = failed
            Write-Host "Deleting failed deployment: $($d.id)"
            try {
                Invoke-RestMethod -Uri "https://heather-demo-chat.scm.azurewebsites.net/api/deployments/$($d.id)" -Method Delete -Headers $headers
                Write-Host "  Deleted."
            } catch {
                Write-Host "  Delete error: $($_.Exception.Message)"
            }
        }
    }
} catch {
    Write-Host "Could not list deployments: $($_.Exception.Message)"
}

# Step 2: Republish fresh
Write-Host "`n--- Publishing application ---"
$deployDir = "C:\temp\heather_deploy"
$zipPath = "C:\temp\heather_deploy.zip"

if (Test-Path $deployDir) { Remove-Item -Recurse -Force $deployDir }
dotnet publish HeatherDemoApp.csproj -c Release -r linux-x64 --self-contained false -o $deployDir
Set-Content -Path "$deployDir\.deployment" -Value "[config]`nSCM_DO_BUILD_DURING_DEPLOYMENT=false"

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Add-Type -Assembly "System.IO.Compression.FileSystem"
[System.IO.Compression.ZipFile]::CreateFromDirectory($deployDir, $zipPath)
$sizeMB = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
Write-Host "Zip created: $sizeMB MB"

# Step 3: Deploy using ARM REST API (bypasses Kudu SCM entirely)
Write-Host "`n--- Deploying via ARM API ---"
$subscriptionId = (az account show --query id -o tsv 2>&1 | Where-Object { $_ -notmatch 'Warning|UserWarning' }).Trim()
$armUri = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/RG-Marc.Merritt/providers/Microsoft.Web/sites/heather-demo-chat/extensions/onedeploy?api-version=2024-04-01"
$armHeaders = @{
    Authorization  = "Bearer $token"
    "Content-Type" = "application/json"
}
$body = @{
    properties = @{
        packageUri = ""
        type       = "zip"
        clean      = $true
        restart    = $true
    }
} | ConvertTo-Json

# Actually, ARM onedeploy needs a packageUri (SAS URL). Let's use Kudu with clean flag instead.
# Try az webapp deploy which uses the newer /api/publish endpoint
Write-Host "Using az webapp deploy with --clean..."
$env:SCM_DO_BUILD_DURING_DEPLOYMENT = "false"
az webapp deploy --name heather-demo-chat --resource-group RG-Marc.Merritt --src-path $zipPath --type zip --clean true 2>&1 | ForEach-Object { Write-Host $_ }

Write-Host "`n--- Restarting ---"
az webapp restart --name heather-demo-chat --resource-group RG-Marc.Merritt 2>&1 | Out-Null
Write-Host "Done! https://heather-demo-chat.azurewebsites.net"
