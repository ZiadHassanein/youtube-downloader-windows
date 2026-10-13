$ErrorActionPreference = 'Stop'
if ($env:GITHUB_EVENT_NAME -ne 'push' -or $env:GITHUB_REF_TYPE -ne 'tag' -or
    $env:GITHUB_REF_NAME -notmatch '^v\d+\.\d+\.\d+$' -or !$env:GH_RELEASE_TOKEN) {
    throw 'Release publishing requires an explicit version tag and the job token.'
}
$repository = 'ZiadHassanein/youtube-downloader-windows'
if ($env:GITHUB_REPOSITORY -ne $repository) { throw 'Unexpected release repository.' }
& git merge-base --is-ancestor HEAD origin/main
if ($LASTEXITCODE -ne 0) { throw 'Only a commit already merged to main can be released.' }
$version = $env:GITHUB_REF_NAME.Substring(1)
$root = Split-Path -Parent $PSScriptRoot
$sumPath = Join-Path $root 'dist/checksums-desktop.json'
$sum = Get-Content -LiteralPath $sumPath -Raw | ConvertFrom-Json
$expectedName = "YouTubeDownloader-v$version-win-x64.zip"
if ($sum.File -ne $expectedName) { throw 'The artifact version does not match the tag.' }
$zip = Join-Path $root ('dist/' + $expectedName)
if ((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash -ne $sum.Hash) { throw 'The tested artifact checksum does not match.' }
$headers = @{Authorization = 'Bearer ' + $env:GH_RELEASE_TOKEN; Accept = 'application/vnd.github+json'; 'X-GitHub-Api-Version' = '2022-11-28'}
$api = "https://api.github.com/repos/$repository/releases"
$url = "https://github.com/$repository/releases/download/$($env:GITHUB_REF_NAME)/$expectedName"
$body = @"
**[Download for Windows (.zip)]($url)**

Extract one ZIP and open YouTubeDownloader.exe. Windows 10/11 x64; no separate .NET runtime or setup script. Compatible media tools are reused, and missing tools are prepared privately on the first pasted link.

- Rebuilt WPF interface with an open layout, embedded English/Arabic fonts, light/dark themes, and crisp icons.
- MP4 videos and playlists, plus MP3 at 192 or 320 kbps.
- Shared download core, confirmed process cancellation, verified dependency acquisition, and recoverable manual updates.
- Preferences and playlist resume records remain private. Links are never saved.

For v1.0.14, download this ZIP manually and extract it into a new writable folder. Import settings if needed. Existing media is retained.

The Windows build, local fixture checks, and EXE-only startup check passed before this release. A small live sample verified MP4, both MP3 settings, and playlist metadata. Windows 10/11 clean-machine testing and other DPI settings remain unverified. The app is unsigned and Windows may show SmartScreen. Protected and sign-in-only content is not supported; only save content you have permission to download.
"@
$payload = @{tag_name = $env:GITHUB_REF_NAME; target_commitish = $env:GITHUB_SHA; name = "YouTube Downloader $($env:GITHUB_REF_NAME)"; body = $body; draft = $true; prerelease = $false} | ConvertTo-Json
$release = Invoke-RestMethod -Uri $api -Method Post -Headers $headers -ContentType 'application/json' -Body $payload
foreach ($file in @($zip, $sumPath)) {
    $name = [Uri]::EscapeDataString((Split-Path -Leaf $file))
    $upload = "https://uploads.github.com/repos/$repository/releases/$($release.id)/assets?name=$name"
    $type = if ($file.EndsWith('.zip')) { 'application/zip' } else { 'application/json' }
    $asset = Invoke-RestMethod -Uri $upload -Method Post -Headers $headers -ContentType $type -InFile $file
    if ($asset.state -ne 'uploaded') { throw 'A release asset did not finish uploading. The release remains a draft.' }
}
$published = Invoke-RestMethod -Uri "$api/$($release.id)" -Method Patch -Headers $headers -ContentType 'application/json' -Body '{"draft":false,"make_latest":"true"}'
Write-Output "Published $($published.html_url)"
