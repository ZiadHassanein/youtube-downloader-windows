param([string]$Version='3.0.0')
& (Join-Path $PSScriptRoot 'package.ps1') -Version $Version
