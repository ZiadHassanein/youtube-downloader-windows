param([string]$Version='3.0.2')
& (Join-Path $PSScriptRoot 'package.ps1') -Version $Version
