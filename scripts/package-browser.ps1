param([string]$OutputDirectory, [switch]$Preview, [switch]$NoArchive)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
if (!$OutputDirectory) { $OutputDirectory=Join-Path $taskRoot $(if($Preview) {'dist/ViFree-browser-preview'} else {'dist/ViFree-browser'}) }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$taskKey=[IO.File]::ReadAllText((Join-Path $taskRoot 'browser/public-key.txt')).Trim()
foreach ($taskBrowser in @('chromium','firefox')) {
    $taskFolder=Join-Path $OutputDirectory $taskBrowser
    New-Item -ItemType Directory -Path $taskFolder -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $taskRoot 'browser/common/connection.js'),(Join-Path $taskRoot 'browser/common/video-detection.js'),(Join-Path $taskRoot 'browser/common/background.js'),(Join-Path $taskRoot 'browser/common/popup.js'),(Join-Path $taskRoot 'browser/common/popup.html'),(Join-Path $taskRoot 'browser/common/popup.css') -Destination $taskFolder -Force
    Copy-Item -LiteralPath (Join-Path $taskRoot 'assets/header-logo.png') -Destination (Join-Path $taskFolder 'icon.png') -Force
    $taskManifest=@{manifest_version=3;name=$(if($Preview){'ViFree Preview'}else{'ViFree'});version='1.1.0';description='Send the video you are watching to ViFree for review.';permissions=@('activeTab','scripting','contextMenus','nativeMessaging');action=@{default_popup='popup.html';default_icon='icon.png'};icons=@{'128'='icon.png'}}
    if ($taskBrowser -eq 'chromium') { $taskManifest.key=$taskKey; $taskManifest.background=@{service_worker='background.js'} }
    else { $taskManifest.background=@{scripts=@('connection.js','video-detection.js','background.js')}; $taskManifest.browser_specific_settings=@{gecko=@{id=$(if($Preview){'vifree-preview@ziadhassanein.github.io'}else{'vifree@ziadhassanein.github.io'});strict_min_version='140.0';data_collection_permissions=@{required=@('none')}}} }
    [IO.File]::WriteAllText((Join-Path $taskFolder 'manifest.json'),($taskManifest | ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
}
Copy-Item -LiteralPath (Join-Path $taskRoot 'browser/README.md') -Destination (Join-Path $OutputDirectory 'README.md') -Force
Copy-Item -LiteralPath (Join-Path $taskRoot 'LICENSE') -Destination (Join-Path $OutputDirectory 'LICENSE.txt') -Force
if($NoArchive) { Write-Output $OutputDirectory; return }
$taskZip=Join-Path (Split-Path -Parent $OutputDirectory) $(if($Preview){'ViFree-browser-preview.zip'}else{'ViFree-browser.zip'})
Compress-Archive -LiteralPath (Join-Path $OutputDirectory 'chromium'),(Join-Path $OutputDirectory 'firefox'),(Join-Path $OutputDirectory 'README.md'),(Join-Path $OutputDirectory 'LICENSE.txt') -DestinationPath $taskZip -Force
Write-Output $taskZip
