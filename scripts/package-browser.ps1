param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
if (!$OutputDirectory) { $OutputDirectory=Join-Path $taskRoot 'dist/ViFree-browser-preview' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$taskKey=[IO.File]::ReadAllText((Join-Path $taskRoot 'browser/public-key.txt')).Trim()
foreach ($taskBrowser in @('chromium','firefox')) {
    $taskFolder=Join-Path $OutputDirectory $taskBrowser
    New-Item -ItemType Directory -Path $taskFolder -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $taskRoot 'browser/common/background.js'),(Join-Path $taskRoot 'browser/common/popup.js'),(Join-Path $taskRoot 'browser/common/popup.html'),(Join-Path $taskRoot 'browser/common/popup.css') -Destination $taskFolder -Force
    Copy-Item -LiteralPath (Join-Path $taskRoot 'assets/header-logo.png') -Destination (Join-Path $taskFolder 'icon.png') -Force
    $taskManifest=@{manifest_version=3;name='ViFree Preview';version='0.1.0';description='Send a public video link to ViFree Preview for review.';permissions=@('activeTab','contextMenus','nativeMessaging');action=@{default_popup='popup.html';default_icon='icon.png'};icons=@{'128'='icon.png'}}
    if ($taskBrowser -eq 'chromium') { $taskManifest.key=$taskKey; $taskManifest.background=@{service_worker='background.js'} }
    else { $taskManifest.background=@{scripts=@('background.js')}; $taskManifest.browser_specific_settings=@{gecko=@{id='vifree-preview@ziadhassanein.github.io';strict_min_version='140.0';data_collection_permissions=@{required=@('none')}}} }
    [IO.File]::WriteAllText((Join-Path $taskFolder 'manifest.json'),($taskManifest | ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
}
Copy-Item -LiteralPath (Join-Path $taskRoot 'browser/README.md') -Destination (Join-Path $OutputDirectory 'README.md') -Force
Copy-Item -LiteralPath (Join-Path $taskRoot 'LICENSE') -Destination (Join-Path $OutputDirectory 'LICENSE.txt') -Force
$taskZip=Join-Path (Split-Path -Parent $OutputDirectory) 'ViFree-browser-preview.zip'
Compress-Archive -LiteralPath (Join-Path $OutputDirectory 'chromium'),(Join-Path $OutputDirectory 'firefox'),(Join-Path $OutputDirectory 'README.md'),(Join-Path $OutputDirectory 'LICENSE.txt') -DestinationPath $taskZip -Force
Write-Output $taskZip
