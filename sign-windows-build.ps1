<#
.SYNOPSIS
    Publishes the self-contained, unpackaged Windows build and signs the exe.

.NOTES
    Requires:
      - PFX certificate at $env:CODE_SIGN_PFX_PATH (defaults to C:\CodeSigning\AllAroundEstimates.pfx)
      - Password in $env:CODE_SIGN_PFX_PASSWORD
      - signtool.exe at $env:SIGNTOOL_PATH (defaults to C:\BuildTools\signtool\signtool.exe)
#>

$ErrorActionPreference = "Stop"

$pfxPath     = if ($env:CODE_SIGN_PFX_PATH) { $env:CODE_SIGN_PFX_PATH } else { "C:\CodeSigning\AllAroundEstimates.pfx" }
$pfxPassword = $env:CODE_SIGN_PFX_PASSWORD
$signtool    = if ($env:SIGNTOOL_PATH) { $env:SIGNTOOL_PATH } else { "C:\BuildTools\signtool\signtool.exe" }

if (-not $pfxPassword) {
    throw "Set `$env:CODE_SIGN_PFX_PASSWORD before running this script."
}
if (-not (Test-Path $pfxPath)) {
    throw "Certificate not found at $pfxPath. Set `$env:CODE_SIGN_PFX_PATH if it lives elsewhere."
}
if (-not (Test-Path $signtool)) {
    throw "signtool.exe not found at $signtool. Set `$env:SIGNTOOL_PATH if it lives elsewhere."
}

Write-Host "Publishing..."
dotnet publish -c Release -f net10.0-windows10.0.19041.0 `
    -p:TargetFrameworks=net10.0-windows10.0.19041.0 `
    -r win-x64 --self-contained -p:WindowsAppSDKSelfContained=true

$publishDir = "bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"
$exePath = Join-Path $publishDir "AllAroundEstimates.exe"

if (-not (Test-Path $exePath)) {
    throw "Publish did not produce $exePath"
}

Write-Host "Signing $exePath..."
& $signtool sign /f $pfxPath /p $pfxPassword /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 $exePath

Write-Host "Done. Signed build is in $publishDir"
