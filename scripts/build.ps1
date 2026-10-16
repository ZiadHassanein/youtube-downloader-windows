param([string]$Version='4.1.0', [switch]$Preview)
& (Join-Path $PSScriptRoot 'package.ps1') -Version $Version -Preview:$Preview
