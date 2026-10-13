param([string]$CaptureDirectory)
& (Join-Path $PSScriptRoot 'check.ps1') -CaptureDirectory $CaptureDirectory
