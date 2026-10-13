param([string]$Version='2.0.2')
& (Join-Path $PSScriptRoot 'package.ps1') -Version $Version
