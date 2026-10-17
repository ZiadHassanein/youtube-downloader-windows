# GitHub's public release-asset counters. No app telemetry, API key or account is required.
$ErrorActionPreference = 'Stop'
$downloadRows = [Collections.Generic.List[object]]::new()
$downloadHeaders = @{ Accept = 'application/vnd.github+json'; 'X-GitHub-Api-Version' = '2022-11-28'; 'User-Agent' = 'Vydrop-download-counts' }
$downloadPage = 1
do {
    $downloadReleases = Invoke-RestMethod -Uri "https://api.github.com/repos/ZiadHassanein/Vydrop/releases?per_page=100&page=$downloadPage" -Headers $downloadHeaders -TimeoutSec 30
    foreach ($downloadRelease in @($downloadReleases)) {
        if ($downloadRelease.draft) { continue }
        foreach ($downloadAsset in $downloadRelease.assets) {
            if ($downloadAsset.name -notlike '*.zip') { continue }
            $downloadRows.Add([pscustomobject]@{ Release = $downloadRelease.tag_name; Asset = $downloadAsset.name; Downloads = [long]$downloadAsset.download_count })
        }
    }
    $downloadPage++
} while (@($downloadReleases).Count -eq 100)
$downloadRows | Format-Table -AutoSize
$downloadTotal = [long]($downloadRows | Measure-Object Downloads -Sum).Sum
Write-Output "Total ZIP downloads: $downloadTotal"
Write-Output 'These are download events, including repeats and automated downloads, not unique people, installations or active users.'
