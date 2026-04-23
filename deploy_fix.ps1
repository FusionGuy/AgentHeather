$ErrorActionPreference = "Stop"

# Get Azure access token
$token = az account get-access-token --resource https://management.azure.com --query accessToken -o tsv
if (-not $token) { throw "Failed to get access token" }

Write-Host "Got access token, deploying..."

# Deploy using Kudu zipdeploy with bearer token
$zipPath = "C:\temp\heather_deploy.zip"
$uri = "https://heather-demo-chat.scm.azurewebsites.net/api/zipdeploy?isAsync=false"

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type"  = "application/zip"
}

$response = Invoke-WebRequest -Uri $uri -Method Post -InFile $zipPath -Headers $headers -UseBasicParsing
Write-Host "Deploy status: $($response.StatusCode)"

# Restart
Write-Host "Restarting..."
az webapp restart --name heather-demo-chat --resource-group RG-Marc.Merritt 2>$null
Write-Host "Done! https://heather-demo-chat.azurewebsites.net"
