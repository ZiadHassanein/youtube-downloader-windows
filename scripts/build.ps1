param([string]$Version='4.0.0-preview.1', [switch]$Preview=$true)
& (Join-Path $PSScriptRoot 'package.ps1') -Version $Version -Preview:$Preview
