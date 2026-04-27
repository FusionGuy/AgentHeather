$ErrorActionPreference = "Stop"
$deployDir = "C:\temp\heather_deploy"
$zipPath = "C:\temp\heather_deploy.zip"

# Step 1: Publish
Write-Host "Publishing application..."
if (Test-Path $deployDir) { Remove-Item -Recurse -Force $deployDir }
dotnet publish HeatherDemoApp.csproj -c Release -r linux-x64 --self-contained false -o $deployDir
Write-Host "Publish complete."

# Step 2: Add .deployment file to disable Oryx build on server
Set-Content -Path "$deployDir\.deployment" -Value "[config]`nSCM_DO_BUILD_DURING_DEPLOYMENT=false"

# Step 3: Zip
# IMPORTANT: Use Compress-Archive (not System.IO.Compression.ZipFile) because
# the latter writes Windows backslashes as path separators in zip entry names
# on Windows, which Linux App Service rsync rejects with "Invalid argument (22)".
# Compress-Archive writes proper forward-slash entries.
Write-Host "Creating zip archive..."
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$deployDir\*" -DestinationPath $zipPath -CompressionLevel Optimal -Force
$sizeMB = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
Write-Host "Zip created: $sizeMB MB"

# Step 4: Deploy using az webapp deploy (no remote build)
Write-Host "Deploying to heather-demo-chat..."
az webapp deploy --name heather-demo-chat --resource-group RG-Marc.Merritt --src-path $zipPath --type zip --track-status false
Write-Host "Deployment succeeded!"

# Step 5: Restart
Write-Host "Restarting app..."
az webapp restart --name heather-demo-chat --resource-group RG-Marc.Merritt 2>$null
Write-Host "Done! App available at https://heather-demo-chat.azurewebsites.net"
