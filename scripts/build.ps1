param([string]$Version='2.0.1')
& (Join-Path $PSScriptRoot 'package.ps1') -Version $Version
