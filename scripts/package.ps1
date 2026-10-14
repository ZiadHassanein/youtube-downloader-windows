param([string]$Version='3.0.2', [switch]$Preview)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME=Join-Path $projectRoot 'build/dotnet-home'
$env:NUGET_PACKAGES=Join-Path $projectRoot 'build/packages'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'
if($Version -notmatch '^\d+\.\d+\.\d+(?:-preview\.\d+)?$' -or ($Version.Contains('-preview') -ne $Preview.IsPresent)) { throw 'Use a three-part stable version, or a preview version with -Preview.' }
$assemblyVersion=[regex]::Match($Version,'^\d+\.\d+\.\d+').Value + '.0'
$package=Join-Path $projectRoot "dist/ViFree-$Version-win-x64"
New-Item -ItemType Directory -Path $package -Force | Out-Null
& dotnet publish (Join-Path $projectRoot 'src/Downloader.Desktop/Downloader.Desktop.csproj') -c Release -r win-x64 --self-contained true --disable-build-servers -p:ViFreePreview=$($Preview.IsPresent.ToString().ToLowerInvariant()) -p:Version=$Version -p:AssemblyVersion=$assemblyVersion -p:FileVersion=$assemblyVersion -o $package
if($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'THIRD-PARTY-NOTICES.txt') -Destination $package -Force
$instructions=@'
ViFree by Ziad Hassanein
Windows 10/11 x64. Extract this ZIP into a writable folder and open ViFree.exe.
No separate .NET runtime, installer, WinGet or tool setup script is needed.
On the first pasted link, the app checks compatible installed media tools and privately prepares only missing tools. Internet access and approximately 400 MB of free space may be required. Progress appears in the app. Global installations are never modified.
Updates are manual: choose Check for updates and then Install when a newer verified desktop package is available. Copies of YouTube Downloader require one manual ZIP download to switch to ViFree. Existing preferences and downloaded files are retained.
Preferences are saved automatically in LocalAppData/YouTubeDownloader. Existing settings and media are retained. Links are not saved.
Only download content you have permission to save. Protected and sign-in-only content is not supported. YouTube limits can still occur.
Downloads and support: https://github.com/ZiadHassanein/ViFree-Downloads
'@
if ($Preview) {
    $instructions=@'
ViFree Windows Preview by Ziad Hassanein
Extract this ZIP into a writable folder and open ViFree.exe. Windows x64 only.
The .NET runtime is bundled. Compatible media tools are detected; missing tools are downloaded and verified automatically on the first link. Internet access is needed, with up to approximately 400 MB for private media tools. No separate tool setup is required.
This preview has separate preferences and tool/resume state in LocalAppData/ViFreePreview. It does not replace the stable app or install stable updates. Pending queue links stay in this session only.
Output options exposes MP3/M4A/Opus/WAV/FLAC and subtitles. Queue supports multiple links and per-job recovery. Choose videos selects playlist entries. Other websites depend on yt-dlp and public access; DRM and sign-in media are unsupported.
Optional browser integration uses the separate companion ZIP. Ordinary app use needs no browser extension.
Test paste/quality selection, video/audio output, selected playlists, queue/retry/cancel, themes/languages, and folder access. Report issues before merging or publishing stable.
Clean Windows 10/11 VM compatibility has not been verified.
'@
}
$instructions += "`r`n`r`n" + [IO.File]::ReadAllText((Join-Path $projectRoot 'LICENSE'))
[IO.File]::WriteAllText((Join-Path $package 'README.txt'),$instructions,[Text.UTF8Encoding]::new($false))
$files=@('ViFree.exe','README.txt','THIRD-PARTY-NOTICES.txt')
$manifest=@{Version=$Version;Architecture='win-x64';Files=@{}}
foreach($name in $files) { $manifest.Files[$name]=(Get-FileHash -LiteralPath (Join-Path $package $name) -Algorithm SHA256).Hash }
[IO.File]::WriteAllText((Join-Path $package 'package.json'),($manifest | ConvertTo-Json -Depth 4),[Text.UTF8Encoding]::new($false))
$zip=Join-Path $projectRoot "dist/ViFree-v$Version-win-x64.zip"
Compress-Archive -LiteralPath (($files+@('package.json')) | ForEach-Object { Join-Path $package $_ }) -DestinationPath $zip -Force
$sum=@{File=(Split-Path -Leaf $zip);Hash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash}
[IO.File]::WriteAllText((Join-Path $projectRoot 'dist/checksums-desktop.json'),($sum | ConvertTo-Json),[Text.UTF8Encoding]::new($false))
Write-Output "Packaged $zip"
