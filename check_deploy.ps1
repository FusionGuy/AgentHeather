$ErrorActionPreference = "Continue"

# Get token
$token = (az account get-access-token --resource https://management.azure.com --query accessToken -o tsv 2>&1 | Where-Object { $_ -notmatch 'Warning' })
Write-Host "Token length: $($token.Length)"

# Check latest deployments
$headers = @{ Authorization = "Bearer $token" }
try {
    $deployments = Invoke-RestMethod -Uri "https://heather-demo-chat.scm.azurewebsites.net/api/deployments" -Headers $headers
    foreach ($d in $deployments[0..2]) {
        Write-Host "$($d.id) status=$($d.status) complete=$($d.complete) active=$($d.active)"
    }
} catch {
    Write-Host "Error checking deployments: $_"
}

# Delete deploy lock if it exists
try {
    $lockResult = Invoke-RestMethod -Uri "https://heather-demo-chat.scm.azurewebsites.net/api/vfs/site/deployments/deploy.lock" -Method Delete -Headers $headers -ErrorAction SilentlyContinue
    Write-Host "Deleted deploy lock"
} catch {
    Write-Host "No deploy lock or error: $($_.Exception.Message)"
}

# Now try deploying
Write-Host "Deploying zip..."
try {
    $response = Invoke-WebRequest -Uri "https://heather-demo-chat.scm.azurewebsites.net/api/publish?type=zip" -Method Post -InFile "C:\temp\heather_deploy.zip" -Headers @{ Authorization = "Bearer $token"; "Content-Type" = "application/zip" } -UseBasicParsing
    Write-Host "Deploy status: $($response.StatusCode)"
} catch {
    Write-Host "Deploy via /api/publish failed: $($_.Exception.Message)"
    
    # Try wardeploy as alternative
    Write-Host "Trying wardeploy alternative..."
    try {
        $response2 = Invoke-WebRequest -Uri "https://heather-demo-chat.scm.azurewebsites.net/api/zipdeploy" -Method Post -InFile "C:\temp\heather_deploy.zip" -Headers @{ Authorization = "Bearer $token"; "Content-Type" = "application/zip" } -UseBasicParsing
        Write-Host "zipdeploy status: $($response2.StatusCode)"
    } catch {
        Write-Host "zipdeploy also failed: $($_.Exception.Message)"
        Write-Host "Response: $($_.Exception.Response.StatusCode)"
    }
}

Write-Host "Restarting app..."
az webapp restart --name heather-demo-chat --resource-group RG-Marc.Merritt 2>&1 | Out-Null
Write-Host "Done!"
