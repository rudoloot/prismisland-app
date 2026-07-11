param(
    [string]$tagName,
    [string]$apkPath
)

$configFile = Join-Path $PSScriptRoot "github_config.json"
if (-not (Test-Path $configFile)) {
    Write-Error "github_config.json not found in Editor directory. Please configure your repository and token."
    exit 1
}

$config = Get-Content $configFile | ConvertFrom-Json
$owner = $config.owner
$repo = $config.repo
$token = $config.token

if (-not $owner -or -not $repo -or -not $token) {
    Write-Error "Invalid github_config.json. Must contain 'owner', 'repo', and 'token'."
    exit 1
}

if (-not (Test-Path $apkPath)) {
    Write-Error "APK file not found at path: $apkPath"
    exit 1
}

Write-Host "[GitHub Release] Creating release $tagName for $owner/$repo..."

$headers = @{
    "Authorization" = "Bearer $token"
    "Accept" = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
}

$releaseBody = @{
    "tag_name" = $tagName
    "target_commitish" = "main"
    "name" = $tagName
    "body" = "Automatic Build Release $tagName"
    "draft" = $false
    "prerelease" = $false
} | ConvertTo-Json

try {
    $createUri = "https://api.github.com/repos/$owner/$repo/releases"
    $response = Invoke-RestMethod -Uri $createUri -Method Post -Headers $headers -Body $releaseBody -ContentType "application/json"
    $releaseId = $response.id
    $uploadUrlTemplate = $response.upload_url
    $uploadUrl = $uploadUrlTemplate.Split("{")[0]
} catch {
    Write-Error "Failed to create GitHub release: $_"
    exit 1
}

Write-Host "[GitHub Release] Uploading APK to release (ID: $releaseId)..."

try {
    $fileName = [System.IO.Path]::GetFileName($apkPath)
    $uploadUri = "$($uploadUrl)?name=$fileName"
    
    $fileBytes = [System.IO.File]::ReadAllBytes($apkPath)
    
    $uploadHeaders = @{
        "Authorization" = "Bearer $token"
        "Accept" = "application/vnd.github+json"
        "X-GitHub-Api-Version" = "2022-11-28"
    }

    $uploadResponse = Invoke-RestMethod -Uri $uploadUri -Method Post -Headers $uploadHeaders -Body $fileBytes -ContentType "application/vnd.android.package-archive"
    Write-Host "[GitHub Release] Successfully uploaded $fileName to GitHub Releases!"
    Write-Host "[GitHub Release] Download URL: $($uploadResponse.browser_download_url)"
} catch {
    Write-Error "Failed to upload APK to release: $_"
    exit 1
}
