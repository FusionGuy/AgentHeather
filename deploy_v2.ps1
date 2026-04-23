$ErrorActionPreference = "Continue"

Write-Host "Stopping webapp to clear deployment locks..."
az webapp stop --name heather-demo-chat --resource-group RG-Marc.Merritt 2>&1 | Out-Null
Start-Sleep -Seconds 10

Write-Host "Deploying via zipdeploy..."
$token = (az account get-access-token --resource https://management.azure.com --query accessToken -o tsv 2>&1 | Where-Object { $_ -notmatch 'Warning|UserWarning' }).Trim()

$headers = @{
    Authorization  = "Bearer $token"
    "Content-Type" = "application/zip"
}

try {
    $response = Invoke-WebRequest -Uri "https://heather-demo-chat.scm.azurewebsites.net/api/zipdeploy" -Method Post -InFile "C:\temp\heather_deploy.zip" -Headers $headers -UseBasicParsing -TimeoutSec 300
    Write-Host "Deploy status: $($response.StatusCode)"
} catch {
    Write-Host "Deploy error: $($_.Exception.Message)"
    $statusCode = $_.Exception.Response.StatusCode
    Write-Host "HTTP Status: $statusCode"
    
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $body = $reader.ReadToEnd()
        Write-Host "Response body: $body"
    }
}

Write-Host "Starting webapp..."
az webapp start --name heather-demo-chat --resource-group RG-Marc.Merritt 2>&1 | Out-Null
Write-Host "Done!"
