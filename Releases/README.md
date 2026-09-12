# Compiled Releases

Pre-built binaries for ad-hoc/sideload distribution (not published through the Play Store or Microsoft Store).

## AllAroundEstimates-v1.0-android.apk
Signed release APK. Sideload directly onto an Android device (enable "Install unknown apps" for whichever app you use to open the file).

## AllAroundEstimates-v1.0-win-x64.zip
Self-contained, unpackaged Windows x64 build. **Unzip the whole archive** before running `AllAroundEstimates.exe` — it depends on the other files unzipped alongside it (bundled .NET + Windows App SDK runtime), it will not run if copied out on its own.

Both builds are code-signed with a self-signed certificate, so Windows SmartScreen / lack of a trusted publisher warning is expected on first run until a CA-issued certificate is used instead.
