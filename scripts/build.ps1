param([string]$Version='2.0.0')
& (Join-Path $PSScriptRoot 'package.ps1') -Version $Version
