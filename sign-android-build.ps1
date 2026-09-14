<#
.SYNOPSIS
    Publishes the Release Android build and signs the APK with apksigner.

.NOTES
    Requires:
      - Keystore at $env:ANDROID_KEYSTORE_PATH (defaults to C:\CodeSigning\allaroundestimates.keystore)
      - Password in $env:ANDROID_KEYSTORE_PASSWORD (used for both store and key password)
      - Key alias in $env:ANDROID_KEY_ALIAS (defaults to allaroundestimates)
      - apksigner.bat under an Android SDK build-tools version; set $env:APKSIGNER_PATH to override
        auto-detection (defaults to the highest installed build-tools version under $env:ANDROID_HOME)
#>

$ErrorActionPreference = "Stop"

$keystorePath = if ($env:ANDROID_KEYSTORE_PATH) { $env:ANDROID_KEYSTORE_PATH } else { "C:\CodeSigning\allaroundestimates.keystore" }
$keystorePassword = $env:ANDROID_KEYSTORE_PASSWORD
$keyAlias = if ($env:ANDROID_KEY_ALIAS) { $env:ANDROID_KEY_ALIAS } else { "allaroundestimates" }

if (-not $keystorePassword) {
    throw "Set `$env:ANDROID_KEYSTORE_PASSWORD before running this script."
}
if (-not (Test-Path $keystorePath)) {
    throw "Keystore not found at $keystorePath. Set `$env:ANDROID_KEYSTORE_PATH if it lives elsewhere."
}

$apksigner = $env:APKSIGNER_PATH
if (-not $apksigner) {
    $androidHome = if ($env:ANDROID_HOME) { $env:ANDROID_HOME } else { "C:\Android" }
    $candidate = Get-ChildItem (Join-Path $androidHome "build-tools") -Directory -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending | Select-Object -First 1
    if ($candidate) {
        $apksigner = Join-Path $candidate.FullName "apksigner.bat"
    }
}
if (-not $apksigner -or -not (Test-Path $apksigner)) {
    throw "apksigner.bat not found. Set `$env:APKSIGNER_PATH to its full path."
}

Write-Host "Publishing..."
dotnet publish -c Release -f net10.0-android `
    -p:AndroidSdkDirectory="$(if ($env:ANDROID_HOME) { $env:ANDROID_HOME } else { 'C:\Android' })" `
    -p:JavaSdkDirectory="$(if ($env:JAVA_HOME) { $env:JAVA_HOME } else { 'C:\Program Files\Java\jdk-17' })"

$publishDir = "bin\Release\net10.0-android\publish"
$unsignedApk = Get-ChildItem $publishDir -Filter "*-Signed.apk" | Select-Object -First 1
if (-not $unsignedApk) {
    throw "Publish did not produce an APK in $publishDir"
}

$outputApk = Join-Path $publishDir "AllAroundEstimates-release.apk"
Copy-Item $unsignedApk.FullName $outputApk -Force

Write-Host "Signing $outputApk..."
& $apksigner sign --ks $keystorePath --ks-key-alias $keyAlias `
    --ks-pass "pass:$keystorePassword" --key-pass "pass:$keystorePassword" `
    $outputApk

Write-Host "Done. Signed APK is at $outputApk"
