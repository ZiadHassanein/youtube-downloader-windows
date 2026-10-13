$ErrorActionPreference = 'Stop'
if ($env:GITHUB_EVENT_NAME -ne 'push' -or $env:GITHUB_REF_TYPE -ne 'tag' -or
    $env:GITHUB_REF_NAME -notmatch '^v\d+\.\d+\.\d+$' -or !$env:GH_RELEASE_TOKEN) {
    throw 'Release publishing requires an explicit version tag and the job token.'
}
$repository = 'ZiadHassanein/ViFree'
if ($env:GITHUB_REPOSITORY -ne $repository) { throw 'Unexpected release repository.' }
& git merge-base --is-ancestor HEAD origin/main
if ($LASTEXITCODE -ne 0) { throw 'Only a commit already merged to main can be released.' }
$version = $env:GITHUB_REF_NAME.Substring(1)
$root = Split-Path -Parent $PSScriptRoot
$sumPath = Join-Path $root 'dist/checksums-desktop.json'
$sum = Get-Content -LiteralPath $sumPath -Raw | ConvertFrom-Json
$expectedName = "ViFree-v$version-win-x64.zip"
if ($sum.File -ne $expectedName) { throw 'The artifact version does not match the tag.' }
$zip = Join-Path $root ('dist/' + $expectedName)
if ((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash -ne $sum.Hash) { throw 'The tested artifact checksum does not match.' }
$headers = @{Authorization = 'Bearer ' + $env:GH_RELEASE_TOKEN; Accept = 'application/vnd.github+json'; 'X-GitHub-Api-Version' = '2022-11-28'}
$api = "https://api.github.com/repos/$repository/releases"
$url = "https://github.com/$repository/releases/download/$($env:GITHUB_REF_NAME)/$expectedName"
$body = @"
**[Download for Windows (.zip)]($url)**

**ViFree by Ziad Hassanein** — the new name and cobalt/white play-ribbon identity for YouTube Downloader.

Extract the ZIP and open ViFree.exe. Windows 10/11 x64; missing media tools are prepared automatically.

- YouTube videos and playlists as MP4, or MP3 at 192/320 kbps.
- English/Arabic and light/dark themes.
- Verified manual updates; private settings and resume records.

**Existing users:** close the older app and download this ZIP once into a new writable folder. ViFree reuses your existing private settings, history and prepared tools. Your downloaded media stays in place.

Windows checks and portable startup passed. The app is unsigned; clean-machine compatibility is not newly certified. Only save content you have permission to download.
"@
$payload = @{tag_name = $env:GITHUB_REF_NAME; target_commitish = $env:GITHUB_SHA; name = "ViFree $($env:GITHUB_REF_NAME)"; body = $body; draft = $true; prerelease = $false} | ConvertTo-Json
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
