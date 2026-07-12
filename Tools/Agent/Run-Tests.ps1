[CmdletBinding()]
param(
    [string]$ProjectPath = (Join-Path $PSScriptRoot '..\..'),

    [string]$UnityPath,

    [string]$UnityEditorRoot = (Join-Path $env:ProgramFiles 'Unity\Hub\Editor'),

    [ValidateSet('EditMode', 'PlayMode')]
    [string]$TestPlatform = 'EditMode',

    [string]$ResultsPath,

    [string]$LogPath,

    [string]$TestFilter,

    [string]$TestCategory,

    [switch]$UseGraphics,

    [switch]$AllowNoTests,

    [switch]$DryRun,

    [ValidateSet('Text', 'Json')]
    [string]$OutputFormat = 'Text'
)

Set-StrictMode -Version 2.0
$startedAtUtc = [DateTime]::UtcNow

function Get-FullPath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [AllowNull()][string]$BasePath
    )

    if ([IO.Path]::IsPathRooted($Path)) {
        return [IO.Path]::GetFullPath($Path)
    }

    if (-not [string]::IsNullOrWhiteSpace($BasePath)) {
        return [IO.Path]::GetFullPath((Join-Path $BasePath $Path))
    }

    return [IO.Path]::GetFullPath($Path)
}

function New-UnityTestReport {
    param(
        [Parameter(Mandatory = $true)][string]$Status,
        [Parameter(Mandatory = $true)][string]$Message,
        [AllowNull()][string]$ResolvedProjectPath,
        [AllowNull()][string]$ResolvedUnityPath,
        [AllowNull()][string]$UnityVersion,
        [AllowNull()][string]$ResolvedResultsPath,
        [AllowNull()][string]$ResolvedLogPath,
        [AllowNull()][Nullable[int]]$UnityExitCode,
        [AllowNull()][Nullable[int]]$Total,
        [AllowNull()][Nullable[int]]$Passed,
        [AllowNull()][Nullable[int]]$Failed,
        [AllowNull()][Nullable[int]]$Skipped,
        [AllowNull()][Nullable[int]]$Inconclusive,
        [object[]]$Arguments = @()
    )

    [pscustomobject][ordered]@{
        SchemaVersion   = '1.0'
        Operation       = 'UnityTests'
        GeneratedAtUtc  = [DateTime]::UtcNow.ToString('o')
        Status          = $Status
        Message         = $Message
        TestPlatform    = $TestPlatform
        ProjectPath     = $ResolvedProjectPath
        UnityPath       = $ResolvedUnityPath
        UnityVersion    = $UnityVersion
        UnityExitCode   = $UnityExitCode
        ResultsPath     = $ResolvedResultsPath
        LogPath         = $ResolvedLogPath
        Total           = $Total
        Passed          = $Passed
        Failed          = $Failed
        Skipped         = $Skipped
        Inconclusive    = $Inconclusive
        DurationSeconds = [Math]::Round(([DateTime]::UtcNow - $startedAtUtc).TotalSeconds, 3)
        Arguments       = @($Arguments)
    }
}

function Complete-UnityTestRun {
    param(
        [Parameter(Mandatory = $true)]$Report,
        [Parameter(Mandatory = $true)][int]$ExitCode
    )

    if ($OutputFormat -eq 'Json') {
        $Report | ConvertTo-Json -Depth 5
    }
    else {
        Write-Output ("Unity tests: {0}" -f $Report.Status)
        Write-Output $Report.Message
        if ($Report.UnityVersion) { Write-Output ("Unity: {0}" -f $Report.UnityVersion) }
        if ($Report.ProjectPath) { Write-Output ("Project: {0}" -f $Report.ProjectPath) }
        if ($null -ne $Report.Total) {
            Write-Output ("Results: total={0}; passed={1}; failed={2}; skipped={3}; inconclusive={4}" -f $Report.Total, $Report.Passed, $Report.Failed, $Report.Skipped, $Report.Inconclusive)
        }
        if ($Report.ResultsPath) { Write-Output ("Result XML: {0}" -f $Report.ResultsPath) }
        if ($Report.LogPath) { Write-Output ("Editor log: {0}" -f $Report.LogPath) }
        if ($Report.Status -eq 'DryRun') {
            Write-Output ("Command: {0} {1}" -f $Report.UnityPath, ($Report.Arguments -join ' '))
        }
    }

    exit $ExitCode
}

function Stop-ConfigurationError {
    param(
        [Parameter(Mandatory = $true)][string]$Message,
        [AllowNull()][string]$ResolvedProjectPath,
        [AllowNull()][string]$ResolvedUnityPath,
        [AllowNull()][string]$UnityVersion,
        [AllowNull()][string]$ResolvedResultsPath,
        [AllowNull()][string]$ResolvedLogPath
    )

    $report = New-UnityTestReport -Status 'ConfigurationError' -Message $Message `
        -ResolvedProjectPath $ResolvedProjectPath -ResolvedUnityPath $ResolvedUnityPath `
        -UnityVersion $UnityVersion -ResolvedResultsPath $ResolvedResultsPath `
        -ResolvedLogPath $ResolvedLogPath -UnityExitCode $null -Total $null `
        -Passed $null -Failed $null -Skipped $null -Inconclusive $null
    Complete-UnityTestRun -Report $report -ExitCode 2
}

function Get-IntegerAttribute {
    param(
        [Parameter(Mandatory = $true)][System.Xml.XmlElement]$Element,
        [Parameter(Mandatory = $true)][string[]]$Names
    )

    foreach ($name in $Names) {
        if (-not $Element.HasAttribute($name)) { continue }
        $value = 0
        if ([int]::TryParse($Element.GetAttribute($name), [ref]$value)) {
            return $value
        }
    }

    return 0
}

function ConvertTo-WindowsCommandLineArgument {
    param([AllowEmptyString()][string]$Value)

    if ($null -eq $Value -or $Value.Length -eq 0) {
        return '""'
    }
    if ($Value -notmatch '[\s"]') {
        return $Value
    }

    # CommandLineToArgvW-compatible quoting: backslashes before a quote or the
    # closing delimiter must be doubled so paths and filters arrive unchanged.
    $escaped = [regex]::Replace($Value, '(\\*)"', '$1$1\"')
    $escaped = [regex]::Replace($escaped, '(\\+)$', '$1$1')
    return '"' + $escaped + '"'
}

$resolvedProjectPath = $null
$resolvedUnityPath = $null
$projectUnityVersion = $null
$resolvedResultsPath = $null
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
    $directoryPath = Join-Path $resolvedProjectPath $requiredDirectory
    if (-not (Test-Path -LiteralPath $directoryPath -PathType Container)) {
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
    if (-not $versionMatch.Success) {
        Stop-ConfigurationError -Message 'ProjectVersion.txt does not contain m_EditorVersion.' -ResolvedProjectPath $resolvedProjectPath
    }
    $projectUnityVersion = $versionMatch.Groups['version'].Value
}
catch {
    Stop-ConfigurationError -Message ("Could not read ProjectVersion.txt: {0}" -f $_.Exception.Message) -ResolvedProjectPath $resolvedProjectPath
}

try {
    if (-not [string]::IsNullOrWhiteSpace($UnityPath)) {
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

$platformName = $TestPlatform.ToLowerInvariant()
if ([string]::IsNullOrWhiteSpace($ResultsPath)) {
    $ResultsPath = "TestResults\unity-$platformName.xml"
}
if ([string]::IsNullOrWhiteSpace($LogPath)) {
    $LogPath = "Logs\unity-$platformName-tests.log"
}

try {
    $resolvedResultsPath = Get-FullPath -Path $ResultsPath -BasePath $resolvedProjectPath
    $resolvedLogPath = Get-FullPath -Path $LogPath -BasePath $resolvedProjectPath
}
catch {
    Stop-ConfigurationError -Message ("Output path is invalid: {0}" -f $_.Exception.Message) `
        -ResolvedProjectPath $resolvedProjectPath -ResolvedUnityPath $resolvedUnityPath `
        -UnityVersion $projectUnityVersion -ResolvedResultsPath $resolvedResultsPath -ResolvedLogPath $resolvedLogPath
}

if ([string]::Equals($resolvedResultsPath, $resolvedLogPath, [StringComparison]::OrdinalIgnoreCase)) {
    Stop-ConfigurationError -Message 'ResultsPath and LogPath must be different files.' `
        -ResolvedProjectPath $resolvedProjectPath -ResolvedUnityPath $resolvedUnityPath `
        -UnityVersion $projectUnityVersion -ResolvedResultsPath $resolvedResultsPath -ResolvedLogPath $resolvedLogPath
}

$unityArguments = @('-batchmode')
if (-not $UseGraphics) {
    $unityArguments += '-nographics'
}
$unityArguments += @(
    '-projectPath', $resolvedProjectPath,
    '-runTests',
    '-testPlatform', $TestPlatform,
    '-testResults', $resolvedResultsPath,
    '-logFile', $resolvedLogPath,
    '-forgetProjectPath'
)
if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
    $unityArguments += @('-testFilter', $TestFilter)
}
if (-not [string]::IsNullOrWhiteSpace($TestCategory)) {
    $unityArguments += @('-testCategory', $TestCategory)
}

if ($DryRun) {
    $report = New-UnityTestReport -Status 'DryRun' -Message 'Unity test command validated; no files or processes were changed.' `
        -ResolvedProjectPath $resolvedProjectPath -ResolvedUnityPath $resolvedUnityPath `
        -UnityVersion $projectUnityVersion -ResolvedResultsPath $resolvedResultsPath `
        -ResolvedLogPath $resolvedLogPath -UnityExitCode $null -Total $null `
        -Passed $null -Failed $null -Skipped $null -Inconclusive $null -Arguments $unityArguments
    Complete-UnityTestRun -Report $report -ExitCode 0
}

try {
    foreach ($outputDirectory in @((Split-Path $resolvedResultsPath -Parent), (Split-Path $resolvedLogPath -Parent))) {
        if (-not (Test-Path -LiteralPath $outputDirectory -PathType Container)) {
            New-Item -ItemType Directory -Force -Path $outputDirectory -ErrorAction Stop | Out-Null
        }
    }
    foreach ($staleOutput in @($resolvedResultsPath, $resolvedLogPath)) {
        if (Test-Path -LiteralPath $staleOutput -PathType Leaf) {
            Remove-Item -LiteralPath $staleOutput -Force -ErrorAction Stop
        }
    }
}
catch {
    Stop-ConfigurationError -Message ("Could not prepare output paths: {0}" -f $_.Exception.Message) `
        -ResolvedProjectPath $resolvedProjectPath -ResolvedUnityPath $resolvedUnityPath `
        -UnityVersion $projectUnityVersion -ResolvedResultsPath $resolvedResultsPath -ResolvedLogPath $resolvedLogPath
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
        if (-not $process.Start()) {
            throw 'The Unity process did not start.'
        }
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

$total = $null
$passed = $null
$failed = $null
$skipped = $null
$inconclusive = $null
$resultState = $null
$resultError = $null

if (-not (Test-Path -LiteralPath $resolvedResultsPath -PathType Leaf)) {
    $resultError = 'Unity did not produce a test result XML file.'
}
else {
    try {
        [xml]$resultDocument = Get-Content -Raw -LiteralPath $resolvedResultsPath -ErrorAction Stop
        $testRun = $resultDocument.SelectSingleNode('/test-run')
        if ($null -eq $testRun) {
            throw 'The XML root element is not test-run.'
        }

        $resultState = $testRun.GetAttribute('result')
        $total = Get-IntegerAttribute -Element $testRun -Names @('total', 'testcasecount')
        $passed = Get-IntegerAttribute -Element $testRun -Names @('passed')
        $failed = Get-IntegerAttribute -Element $testRun -Names @('failed')
        $skipped = Get-IntegerAttribute -Element $testRun -Names @('skipped')
        $inconclusive = Get-IntegerAttribute -Element $testRun -Names @('inconclusive')
    }
    catch {
        $resultError = "Could not parse Unity test result XML: $($_.Exception.Message)"
    }
}

$status = 'Passed'
$message = 'Unity tests passed.'
$processExitCode = 0

if ($invocationError) {
    $status = 'Failed'
    $message = "Unity could not be started: $invocationError"
    $processExitCode = 1
}
elseif ($unityExitCode -ne 0) {
    $status = 'Failed'
    $message = "Unity exited with code $unityExitCode. See the Editor log for evidence."
    $processExitCode = 1
}
elseif ($resultError) {
    $status = 'Failed'
    $message = $resultError
    $processExitCode = 1
}
elseif ($resultState -ne 'Passed' -or $failed -gt 0) {
    $status = 'Failed'
    $message = "Unity test result was $resultState with $failed failed test(s)."
    $processExitCode = 1
}
elseif ($total -eq 0 -and -not $AllowNoTests) {
    $status = 'Failed'
    $message = 'Unity reported zero tests. Use -AllowNoTests only when that is intentional.'
    $processExitCode = 1
}

$report = New-UnityTestReport -Status $status -Message $message `
    -ResolvedProjectPath $resolvedProjectPath -ResolvedUnityPath $resolvedUnityPath `
    -UnityVersion $projectUnityVersion -ResolvedResultsPath $resolvedResultsPath `
    -ResolvedLogPath $resolvedLogPath -UnityExitCode $unityExitCode -Total $total `
    -Passed $passed -Failed $failed -Skipped $skipped -Inconclusive $inconclusive `
    -Arguments $unityArguments
Complete-UnityTestRun -Report $report -ExitCode $processExitCode
