$ErrorActionPreference = "Stop"
$deployDir = "C:\temp\heather_deploy"
$zipPath = "C:\temp\heather_deploy.zip"

# Step 1: Publish
Write-Host "Publishing application..."
if (Test-Path $deployDir) { Remove-Item -Recurse -Force $deployDir }
dotnet publish HeatherDemoApp.csproj -c Release -r linux-x64 --self-contained false -o $deployDir
Write-Host "Publish complete."

# Step 2: Zip
Write-Host "Creating zip archive..."
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Add-Type -Assembly "System.IO.Compression.FileSystem"
[System.IO.Compression.ZipFile]::CreateFromDirectory($deployDir, $zipPath)
$sizeMB = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
Write-Host "Zip created: $sizeMB MB"

# Step 3: Deploy
Write-Host "Getting access token..."
$token = az account get-access-token --resource "https://management.azure.com/" --query accessToken -o tsv

Write-Host "Deploying to heather-demo-chat..."
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/zip"
}

$response = Invoke-RestMethod -Uri "https://heather-demo-chat.scm.azurewebsites.net/api/zipdeploy?isAsync=false" `
    -Method Post `
    -Headers $headers `
    -InFile $zipPath `
    -TimeoutSec 600
Write-Host "Deployment succeeded!"

# Step 4: Restart
Write-Host "Restarting app..."
az webapp restart --name heather-demo-chat --resource-group rg-marc.merritt
Write-Host "Done! App available at https://heather-demo-chat.azurewebsites.net"
