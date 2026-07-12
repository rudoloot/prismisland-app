[CmdletBinding()]
param(
    [string]$ProjectPath = (Join-Path $PSScriptRoot '..\..'),

    [string]$UnityPath,

    [string]$UnityEditorRoot = (Join-Path $env:ProgramFiles 'Unity\Hub\Editor'),

    [Parameter(Mandatory = $true)]
    [string]$VersionName,

    [Parameter(Mandatory = $true)]
    [int]$VersionCode,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [ValidateSet('Apk', 'Aab')]
    [string]$ArtifactFormat = 'Apk',

    [bool]$Development = $true,

    [string]$BuildReportPath,

    [string]$LogPath,

    [switch]$UseGraphics,

    [switch]$DryRun,

    [ValidateSet('Text', 'Json')]
    [string]$OutputFormat = 'Text'
)

Set-StrictMode -Version 2.0
$startedAtUtc = [DateTime]::UtcNow

function Get-FullPath {
    param([Parameter(Mandatory = $true)][string]$Path, [AllowNull()][string]$BasePath)

    if ([IO.Path]::IsPathRooted($Path)) { return [IO.Path]::GetFullPath($Path) }
    if (-not [string]::IsNullOrWhiteSpace($BasePath)) { return [IO.Path]::GetFullPath((Join-Path $BasePath $Path)) }
    return [IO.Path]::GetFullPath($Path)
}

function New-AndroidBuildReport {
    param(
        [Parameter(Mandatory = $true)][string]$Status,
        [Parameter(Mandatory = $true)][string]$Message,
        [AllowNull()][string]$ResolvedProjectPath,
        [AllowNull()][string]$ResolvedUnityPath,
        [AllowNull()][string]$UnityVersion,
        [AllowNull()][string]$ResolvedOutputPath,
        [AllowNull()][string]$ResolvedBuildReportPath,
        [AllowNull()][string]$ResolvedLogPath,
        [AllowNull()][Nullable[int]]$UnityExitCode,
        [AllowNull()][Nullable[long]]$TotalSize,
        [object[]]$Arguments = @()
    )

    [pscustomobject][ordered]@{
        SchemaVersion   = '1.0'
        Operation       = 'AndroidBuild'
        GeneratedAtUtc  = [DateTime]::UtcNow.ToString('o')
        Status          = $Status
        Message         = $Message
        ProjectPath     = $ResolvedProjectPath
        UnityPath       = $ResolvedUnityPath
        UnityVersion    = $UnityVersion
        VersionName     = $VersionName
        VersionCode     = $VersionCode
        ArtifactFormat  = $ArtifactFormat
        Development     = $Development
        OutputPath      = $ResolvedOutputPath
        BuildReportPath = $ResolvedBuildReportPath
        LogPath         = $ResolvedLogPath
        UnityExitCode   = $UnityExitCode
        TotalSize       = $TotalSize
        DurationSeconds = [Math]::Round(([DateTime]::UtcNow - $startedAtUtc).TotalSeconds, 3)
        Arguments       = @($Arguments)
    }
}

function Complete-AndroidBuild {
    param([Parameter(Mandatory = $true)]$Report, [Parameter(Mandatory = $true)][int]$ExitCode)

    if ($OutputFormat -eq 'Json') {
        $Report | ConvertTo-Json -Depth 5
    }
    else {
        Write-Output ("Android build: {0}" -f $Report.Status)
        Write-Output $Report.Message
        if ($Report.ProjectPath) { Write-Output ("Project: {0}" -f $Report.ProjectPath) }
        if ($Report.UnityVersion) { Write-Output ("Unity: {0}" -f $Report.UnityVersion) }
        if ($Report.OutputPath) { Write-Output ("Artifact: {0}" -f $Report.OutputPath) }
        if ($null -ne $Report.TotalSize) { Write-Output ("Artifact size: {0} bytes" -f $Report.TotalSize) }
        if ($Report.BuildReportPath) { Write-Output ("Build report: {0}" -f $Report.BuildReportPath) }
        if ($Report.LogPath) { Write-Output ("Editor log: {0}" -f $Report.LogPath) }
        if ($Report.Status -eq 'DryRun') { Write-Output ("Command: {0} {1}" -f $Report.UnityPath, ($Report.Arguments -join ' ')) }
    }

    exit $ExitCode
}

function Stop-ConfigurationError {
    param(
        [Parameter(Mandatory = $true)][string]$Message,
        [AllowNull()][string]$ResolvedProjectPath,
        [AllowNull()][string]$ResolvedUnityPath,
        [AllowNull()][string]$UnityVersion,
        [AllowNull()][string]$ResolvedOutputPath,
        [AllowNull()][string]$ResolvedBuildReportPath,
        [AllowNull()][string]$ResolvedLogPath
    )

    $report = New-AndroidBuildReport -Status 'ConfigurationError' -Message $Message `
        -ResolvedProjectPath $ResolvedProjectPath -ResolvedUnityPath $ResolvedUnityPath `
        -UnityVersion $UnityVersion -ResolvedOutputPath $ResolvedOutputPath `
        -ResolvedBuildReportPath $ResolvedBuildReportPath -ResolvedLogPath $ResolvedLogPath `
        -UnityExitCode $null -TotalSize $null
    Complete-AndroidBuild -Report $report -ExitCode 2
}

function ConvertTo-WindowsCommandLineArgument {
    param([AllowEmptyString()][string]$Value)

    if ($null -eq $Value -or $Value.Length -eq 0) { return '""' }
    if ($Value -notmatch '[\s"]') { return $Value }

    $escaped = [regex]::Replace($Value, '(\\*)"', '$1$1\"')
    $escaped = [regex]::Replace($escaped, '(\\+)$', '$1$1')
    return '"' + $escaped + '"'
}

$resolvedProjectPath = $null
$resolvedUnityPath = $null
$projectUnityVersion = $null
$resolvedOutputPath = $null
$resolvedBuildReportPath = $null
$resolvedLogPath = $null

try {
    $resolvedProjectPath = Get-FullPath -Path $ProjectPath -BasePath $null
}
catch {
    Stop-ConfigurationError -Message ("Project path is invalid: {0}" -f $_.Exception.Message) -ResolvedProjectPath $ProjectPath
}

if (-not (Test-Path -LiteralPath $resolvedProjectPath -PathType Container)) {
    Stop-ConfigurationError -Message 'Project path does not exist.' -ResolvedProjectPath $resolvedProjectPath
}

foreach ($requiredDirectory in @('Assets', 'Packages', 'ProjectSettings')) {
    if (-not (Test-Path -LiteralPath (Join-Path $resolvedProjectPath $requiredDirectory) -PathType Container)) {
        Stop-ConfigurationError -Message ("Unity project directory is missing: {0}" -f $requiredDirectory) -ResolvedProjectPath $resolvedProjectPath
    }
}

$projectVersionPath = Join-Path $resolvedProjectPath 'ProjectSettings\ProjectVersion.txt'
if (-not (Test-Path -LiteralPath $projectVersionPath -PathType Leaf)) {
    Stop-ConfigurationError -Message 'ProjectSettings\ProjectVersion.txt is missing.' -ResolvedProjectPath $resolvedProjectPath
}

try {
    $versionContent = Get-Content -Raw -LiteralPath $projectVersionPath -ErrorAction Stop
    $versionMatch = [regex]::Match($versionContent, '(?m)^m_EditorVersion:\s*(?<version>\S+)\s*$')
    if (-not $versionMatch.Success) { Stop-ConfigurationError -Message 'ProjectVersion.txt does not contain m_EditorVersion.' -ResolvedProjectPath $resolvedProjectPath }
    $projectUnityVersion = $versionMatch.Groups['version'].Value
}
catch {
    Stop-ConfigurationError -Message ("Could not read ProjectVersion.txt: {0}" -f $_.Exception.Message) -ResolvedProjectPath $resolvedProjectPath
}

try {
    if ($UnityPath) {
        $resolvedUnityPath = Get-FullPath -Path $UnityPath -BasePath $null
    }
    else {
        $resolvedEditorRoot = Get-FullPath -Path $UnityEditorRoot -BasePath $null
        $resolvedUnityPath = Join-Path $resolvedEditorRoot ("{0}\Editor\Unity.exe" -f $projectUnityVersion)
    }
}
catch {
    Stop-ConfigurationError -Message ("Unity path is invalid: {0}" -f $_.Exception.Message) -ResolvedProjectPath $resolvedProjectPath -UnityVersion $projectUnityVersion
}

if (-not (Test-Path -LiteralPath $resolvedUnityPath -PathType Leaf)) {
    Stop-ConfigurationError -Message ("Unity executable was not found for project version {0}." -f $projectUnityVersion) `
        -ResolvedProjectPath $resolvedProjectPath -ResolvedUnityPath $resolvedUnityPath -UnityVersion $projectUnityVersion
}

if ([string]::IsNullOrWhiteSpace($VersionName) -or $VersionName.IndexOfAny([char[]]"`r`n") -ge 0) {
    Stop-ConfigurationError -Message 'VersionName must be a non-empty single-line value.' -ResolvedProjectPath $resolvedProjectPath -ResolvedUnityPath $resolvedUnityPath -UnityVersion $projectUnityVersion
}
if ($VersionCode -le 0) {
    Stop-ConfigurationError -Message 'VersionCode must be a positive integer.' -ResolvedProjectPath $resolvedProjectPath -ResolvedUnityPath $resolvedUnityPath -UnityVersion $projectUnityVersion
}

try {
    $resolvedOutputPath = Get-FullPath -Path $OutputPath -BasePath $resolvedProjectPath
    $expectedExtension = if ($ArtifactFormat -eq 'Apk') { '.apk' } else { '.aab' }
    if (-not [string]::Equals([IO.Path]::GetExtension($resolvedOutputPath), $expectedExtension, [StringComparison]::OrdinalIgnoreCase)) {
        Stop-ConfigurationError -Message ("OutputPath extension must be {0} for {1}." -f $expectedExtension, $ArtifactFormat) `
            -ResolvedProjectPath $resolvedProjectPath -ResolvedUnityPath $resolvedUnityPath -UnityVersion $projectUnityVersion -ResolvedOutputPath $resolvedOutputPath
    }

    $assetsPath = (Get-FullPath -Path (Join-Path $resolvedProjectPath 'Assets') -BasePath $null).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if ($resolvedOutputPath.StartsWith($assetsPath, [StringComparison]::OrdinalIgnoreCase)) {
        Stop-ConfigurationError -Message 'OutputPath must be outside Assets.' `
            -ResolvedProjectPath $resolvedProjectPath -ResolvedUnityPath $resolvedUnityPath -UnityVersion $projectUnityVersion -ResolvedOutputPath $resolvedOutputPath
    }

    if ([string]::IsNullOrWhiteSpace($BuildReportPath)) { $BuildReportPath = $resolvedOutputPath + '.build.json' }
    if ([string]::IsNullOrWhiteSpace($LogPath)) { $LogPath = "Logs\android-$VersionCode-build.log" }
    $resolvedBuildReportPath = Get-FullPath -Path $BuildReportPath -BasePath $resolvedProjectPath
    $resolvedLogPath = Get-FullPath -Path $LogPath -BasePath $resolvedProjectPath
}
catch {
    Stop-ConfigurationError -Message ("Output path is invalid: {0}" -f $_.Exception.Message) `
        -ResolvedProjectPath $resolvedProjectPath -ResolvedUnityPath $resolvedUnityPath -UnityVersion $projectUnityVersion `
        -ResolvedOutputPath $resolvedOutputPath -ResolvedBuildReportPath $resolvedBuildReportPath -ResolvedLogPath $resolvedLogPath
}

if ($resolvedOutputPath -eq $resolvedBuildReportPath -or $resolvedOutputPath -eq $resolvedLogPath -or $resolvedBuildReportPath -eq $resolvedLogPath) {
    Stop-ConfigurationError -Message 'OutputPath, BuildReportPath, and LogPath must be different files.' `
        -ResolvedProjectPath $resolvedProjectPath -ResolvedUnityPath $resolvedUnityPath -UnityVersion $projectUnityVersion `
        -ResolvedOutputPath $resolvedOutputPath -ResolvedBuildReportPath $resolvedBuildReportPath -ResolvedLogPath $resolvedLogPath
}

$buildEntryPointPath = Join-Path $resolvedProjectPath 'Assets\Editor\Build\BuildAndroid.cs'
if (-not (Test-Path -LiteralPath $buildEntryPointPath -PathType Leaf)) {
    Stop-ConfigurationError -Message 'Assets\Editor\Build\BuildAndroid.cs is missing. Copy the project-owned build template before running.' `
        -ResolvedProjectPath $resolvedProjectPath -ResolvedUnityPath $resolvedUnityPath -UnityVersion $projectUnityVersion `
        -ResolvedOutputPath $resolvedOutputPath -ResolvedBuildReportPath $resolvedBuildReportPath -ResolvedLogPath $resolvedLogPath
}

$unityArguments = @('-batchmode', '-buildTarget', 'Android')
if (-not $UseGraphics) { $unityArguments += '-nographics' }
$unityArguments += @(
    '-projectPath', $resolvedProjectPath,
    '-executeMethod', 'RemoteDevLoop.Build.BuildAndroid.Execute',
    '-rdlOutputPath', $resolvedOutputPath,
    '-rdlReportPath', $resolvedBuildReportPath,
    '-rdlVersionName', $VersionName,
    '-rdlVersionCode', $VersionCode.ToString(),
    '-rdlArtifactFormat', $ArtifactFormat,
    '-rdlDevelopment', $Development.ToString(),
    '-logFile', $resolvedLogPath,
    '-forgetProjectPath'
)

if ($DryRun) {
    $report = New-AndroidBuildReport -Status 'DryRun' -Message 'Android build command validated; no files or processes were changed.' `
        -ResolvedProjectPath $resolvedProjectPath -ResolvedUnityPath $resolvedUnityPath -UnityVersion $projectUnityVersion `
        -ResolvedOutputPath $resolvedOutputPath -ResolvedBuildReportPath $resolvedBuildReportPath -ResolvedLogPath $resolvedLogPath `
        -UnityExitCode $null -TotalSize $null -Arguments $unityArguments
    Complete-AndroidBuild -Report $report -ExitCode 0
}

try {
    foreach ($directory in @((Split-Path $resolvedOutputPath -Parent), (Split-Path $resolvedBuildReportPath -Parent), (Split-Path $resolvedLogPath -Parent))) {
        if (-not (Test-Path -LiteralPath $directory -PathType Container)) { New-Item -ItemType Directory -Force -Path $directory -ErrorAction Stop | Out-Null }
    }
    foreach ($staleFile in @($resolvedOutputPath, $resolvedBuildReportPath, $resolvedLogPath)) {
        if (Test-Path -LiteralPath $staleFile -PathType Leaf) { Remove-Item -LiteralPath $staleFile -Force -ErrorAction Stop }
    }
}
catch {
    Stop-ConfigurationError -Message ("Could not prepare build output paths: {0}" -f $_.Exception.Message) `
        -ResolvedProjectPath $resolvedProjectPath -ResolvedUnityPath $resolvedUnityPath -UnityVersion $projectUnityVersion `
        -ResolvedOutputPath $resolvedOutputPath -ResolvedBuildReportPath $resolvedBuildReportPath -ResolvedLogPath $resolvedLogPath
}

$unityExitCode = $null
$consoleOutput = @()
$invocationError = $null
try {
    if ([IO.Path]::GetExtension($resolvedUnityPath) -eq '.exe') {
        $argumentLine = (@($unityArguments | ForEach-Object { ConvertTo-WindowsCommandLineArgument -Value ([string]$_) }) -join ' ')
        $startInfo = New-Object System.Diagnostics.ProcessStartInfo
        $startInfo.FileName = $resolvedUnityPath
        $startInfo.Arguments = $argumentLine
        $startInfo.UseShellExecute = $false
        $startInfo.CreateNoWindow = $true
        $startInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true
        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $startInfo
        if (-not $process.Start()) { throw 'The Unity process did not start.' }
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $stdout = $stdoutTask.GetAwaiter().GetResult()
        $stderr = $stderrTask.GetAwaiter().GetResult()
        $unityExitCode = [int]$process.ExitCode
        $process.Dispose()
        $consoleOutput = @($stdout, $stderr | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    }
    else {
        $global:LASTEXITCODE = 0
        $consoleOutput = @(& $resolvedUnityPath @unityArguments 2>&1)
        $unityExitCode = [int]$LASTEXITCODE
    }
}
catch {
    $invocationError = $_.Exception.Message
    $unityExitCode = 1
}

if (-not (Test-Path -LiteralPath $resolvedLogPath -PathType Leaf) -and ($consoleOutput.Count -gt 0 -or $invocationError)) {
    $fallbackLog = @()
    if ($invocationError) { $fallbackLog += $invocationError }
    $fallbackLog += @($consoleOutput | ForEach-Object { $_.ToString() })
    Set-Content -LiteralPath $resolvedLogPath -Value $fallbackLog -Encoding UTF8
}

$unityBuildReport = $null
$buildReportError = $null
if (-not (Test-Path -LiteralPath $resolvedBuildReportPath -PathType Leaf)) {
    $buildReportError = 'Unity did not produce an Android build report.'
}
else {
    try { $unityBuildReport = Get-Content -Raw -LiteralPath $resolvedBuildReportPath | ConvertFrom-Json } catch { $buildReportError = "Could not parse Android build report: $($_.Exception.Message)" }
}

$status = 'Succeeded'
$message = 'Android build succeeded.'
$processExitCode = 0
$totalSize = if ($unityBuildReport) { [Nullable[long]][long]$unityBuildReport.TotalSize } else { $null }

if ($invocationError) {
    $status = 'Failed'; $message = "Unity could not be started: $invocationError"; $processExitCode = 1
}
elseif ($unityExitCode -ne 0) {
    $status = 'Failed'; $message = "Unity exited with code $unityExitCode. See the Editor log for evidence."; $processExitCode = 1
}
elseif ($buildReportError) {
    $status = 'Failed'; $message = $buildReportError; $processExitCode = 1
}
elseif ($unityBuildReport.Status -ne 'Succeeded') {
    $status = 'Failed'; $message = [string]$unityBuildReport.Message; $processExitCode = 1
}
elseif (-not (Test-Path -LiteralPath $resolvedOutputPath -PathType Leaf) -or (Get-Item -LiteralPath $resolvedOutputPath).Length -le 0) {
    $status = 'Failed'; $message = 'Unity reported success but no non-empty Android artifact was produced.'; $processExitCode = 1
}

$report = New-AndroidBuildReport -Status $status -Message $message `
    -ResolvedProjectPath $resolvedProjectPath -ResolvedUnityPath $resolvedUnityPath -UnityVersion $projectUnityVersion `
    -ResolvedOutputPath $resolvedOutputPath -ResolvedBuildReportPath $resolvedBuildReportPath -ResolvedLogPath $resolvedLogPath `
    -UnityExitCode $unityExitCode -TotalSize $totalSize -Arguments $unityArguments
Complete-AndroidBuild -Report $report -ExitCode $processExitCode
