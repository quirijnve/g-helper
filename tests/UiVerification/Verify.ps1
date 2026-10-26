param(
    [string]$Source = (Join-Path $PSScriptRoot '../..'),
    [string]$Output = (Join-Path $PSScriptRoot '../../artifacts/ui-verification'),
    [string]$Dotnet = 'dotnet'
)
$ErrorActionPreference = 'Stop'
$sourcePath = (Resolve-Path -LiteralPath $Source).Path
$outputPath = [IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
# Work exclusively on a disposable COPY; production constructors and entry point stay intact.
$appCopy = Join-Path $outputPath 'app'
if (Test-Path -LiteralPath $appCopy) { throw "Choose a fresh output directory: $appCopy already exists." }
Copy-Item -LiteralPath (Join-Path $sourcePath 'app') -Destination $appCopy -Recurse
foreach ($name in @('Settings', 'Fans', 'Extra', 'Updates', 'Handheld', 'Matrix', 'Slash', 'OverlayConfig')) {
    $file = Join-Path $appCopy ($name + '.cs')
    $code = [IO.File]::ReadAllText($file)
    $className = if ($name -eq 'Settings') { 'SettingsForm' } else { $name }
    $match = [regex]::Match($code, "public $className\(\)\s*\{")
    if (-not $match.Success) { throw "Missing constructor: $className" }
    $start = $match.Index + $match.Length
    $depth = 1; $end = $start
    while ($depth -gt 0) {
        if ($code[$end] -eq '{') { $depth++ }
        if ($code[$end] -eq '}') { $depth-- }
        $end++
    }
    $appearance = if ($code.Contains('private void InitializeAppearance') -or ($name -eq 'Settings' -and (Test-Path (Join-Path $appCopy 'Settings.Appearance.cs')))) { 'InitializeAppearance();' } else { '' }
    $body = "`n InitializeComponent(); $appearance InitTheme(true);`n"
    $code = $code.Substring(0, $start) + $body + $code.Substring($end - 1)
    # Extra's field initializer reads the OS power plan; omit it in the UI-only copy.
    $code = $code.Replace('ClamshellModeControl clamshellControl = new ClamshellModeControl();', 'ClamshellModeControl clamshellControl = null!;')
    $code = $code.Replace('public AniMatrixControl matrixControl = Program.settingsForm.matrixControl;', 'public AniMatrixControl matrixControl = null!;')
    $code = $code.Replace('SlashDevice? slash = Program.settingsForm.matrixControl.deviceSlash;', 'SlashDevice? slash = null;')
    # No hardware response to system power messages during screenshot capture.
    if ($name -eq 'Settings') {
        $code = $code.Replace('protected override void WndProc(ref Message m)', 'private void UnusedHardwareWndProc(ref Message m)')
    }
    [IO.File]::WriteAllText($file, $code)
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'UiVerificationEntry.cs') -Destination $appCopy
& $Dotnet build (Join-Path $appCopy 'GHelper.csproj') -c Release -p:GITHUB_ACTIONS=true -p:StartupObject=GHelper.UiVerificationEntry -p:WarningLevel=0 --nologo -v:q
if ($LASTEXITCODE -ne 0) { throw 'UI verification build failed.' }
$executable = Join-Path $appCopy 'bin/x64/Release/net10.0-windows/GHelper.exe'
foreach ($theme in @('light', 'dark')) {
    # Startup-local configuration takes priority over the user's real G-Helper config.
    [IO.File]::WriteAllText((Join-Path (Split-Path $executable) 'config.json'), ('{"ui_mode":"' + $theme + '","model":"UI verification"}'))
    $run = Start-Process -FilePath $executable -ArgumentList @('"' + $outputPath + '"', $theme) -Wait -PassThru -WindowStyle Hidden
    if ($run.ExitCode -ne 0) { throw "Verification failed for $theme." }
}
