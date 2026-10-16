$ErrorActionPreference = 'Stop'
if ($env:GITHUB_EVENT_NAME -ne 'push' -or $env:GITHUB_REF_TYPE -ne 'tag' -or
    $env:GITHUB_REF_NAME -notmatch '^v\d+\.\d+\.\d+$' -or !$env:GH_RELEASE_TOKEN) {
    throw 'Release publishing requires an explicit version tag and the job token.'
}
$sourceRepository = 'ZiadHassanein/ViFree'
$repository = $sourceRepository
if ($env:GITHUB_REPOSITORY -ne $sourceRepository) { throw 'Unexpected source repository.' }
& git merge-base --is-ancestor HEAD origin/main
if ($LASTEXITCODE -ne 0) { throw 'Only a commit already merged to main can be released.' }
$version = $env:GITHUB_REF_NAME.Substring(1)
$root = Split-Path -Parent $PSScriptRoot
$sumPath = Join-Path $root 'dist/checksums-desktop.json'
$sum = Get-Content -LiteralPath $sumPath -Raw | ConvertFrom-Json
$expectedName = "Vydrop-v$version-win-x64.zip"
if ($sum.File -ne $expectedName) { throw 'The artifact version does not match the tag.' }
$zip = Join-Path $root ('dist/' + $expectedName)
if ((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash -ne $sum.Hash) { throw 'The tested artifact checksum does not match.' }
$headers = @{Authorization = 'Bearer ' + $env:GH_RELEASE_TOKEN; Accept = 'application/vnd.github+json'; 'X-GitHub-Api-Version' = '2022-11-28'}
$destination = Invoke-RestMethod -Uri "https://api.github.com/repos/$repository" -Headers $headers
if ($destination.private -or $destination.full_name -ne $repository -or $destination.default_branch -ne 'main') {
    throw 'Releases must use the main public Vydrop repository.'
}
$api = "https://api.github.com/repos/$repository/releases"
$url = "https://github.com/$repository/releases/download/$($env:GITHUB_REF_NAME)/$expectedName"
$body = @"
**[Download for Windows (.zip)]($url)**

Extract the ZIP and open **Vydrop.exe**. Windows 10/11 x64. Missing media tools are prepared automatically.

- MP4, MKV and WebM video where inspected streams are compatible.
- MP3, M4A, Opus, WAV and FLAC audio with source-aware conversion choices.
- Queue, playlist selection, SRT/VTT subtitles and supported public sites.
- English/Arabic and light/dark themes.
- Browser companion 1.3.0 inside Browser: Download by Vydrop outside the playing video's border.
- Automatic daily verified yt-dlp/Deno checks and one compatible update-and-retry.
- Settings keeps video errors in the download screen; manual app updates use this repository.

**Upgrading from ViFree or YouTube Downloader to Vydrop:** close the old app, download this ZIP once and extract it into a new writable folder. Stable settings, history and verified media tools retain their existing storage location; preview preferences stay separate. Enable Browser integration in Vydrop to refresh its executable path, then reload/load the Browser folder's companion as described in Browser/README.md. Existing companion IDs and permission scope are retained.

SHA-256: $($sum.Hash.ToLowerInvariant())

By **Ziad Hassanein**. Only save content you have permission to download.
"@
$payload = @{tag_name = $env:GITHUB_REF_NAME; target_commitish = 'main'; name = "Vydrop $($env:GITHUB_REF_NAME)"; body = $body; draft = $true; prerelease = $false} | ConvertTo-Json
$release = Invoke-RestMethod -Uri $api -Method Post -Headers $headers -ContentType 'application/json' -Body $payload
$name = [Uri]::EscapeDataString($expectedName)
$upload = "https://uploads.github.com/repos/$repository/releases/$($release.id)/assets?name=$name"
$asset = Invoke-RestMethod -Uri $upload -Method Post -Headers $headers -ContentType 'application/zip' -InFile $zip
if ($asset.state -ne 'uploaded') { throw 'The ZIP did not finish uploading. The release remains a draft.' }
for($attempt=0; $attempt -lt 10 -and !$asset.digest; $attempt++) {
    Start-Sleep -Milliseconds 500
    $asset=Invoke-RestMethod -Uri "https://api.github.com/repos/$repository/releases/assets/$($asset.id)" -Headers $headers
}
if ($asset.size -ne (Get-Item -LiteralPath $zip).Length -or $asset.digest -ne ('sha256:'+$sum.Hash.ToLowerInvariant())) { throw 'The uploaded ZIP digest did not match. The release remains a draft.' }
$releaseAssets=Invoke-RestMethod -Uri "$api/$($release.id)/assets" -Headers $headers
if (@($releaseAssets).Count -ne 1 -or $releaseAssets[0].name -ne $expectedName) { throw 'The release must contain exactly one ZIP asset.' }
$published = Invoke-RestMethod -Uri "$api/$($release.id)" -Method Patch -Headers $headers -ContentType 'application/json' -Body '{"draft":false,"make_latest":"true"}'
Write-Output "Published $($published.html_url)"
