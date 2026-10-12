@echo off
setlocal
title YouTube Downloader - first-time setup
where winget.exe >nul 2>nul
if errorlevel 1 (
  echo Windows Package Manager is required. Install Microsoft's App Installer,
  echo then run this setup again. Download: https://apps.microsoft.com/detail/9nblggh4nns1
  pause
  exit /b 1
)
call :ensure yt-dlp.exe yt-dlp.yt-dlp
if errorlevel 1 goto failed
call :ensure ffmpeg.exe yt-dlp.FFmpeg
if errorlevel 1 goto failed
call :ensure deno.exe DenoLand.Deno
if errorlevel 1 goto failed
echo.
echo Setup complete. Open YouTubeDownloader.exe.
pause
exit /b 0

:ensure
if exist "%~dp0tools\%~1" exit /b 0
if exist "%LOCALAPPDATA%\Microsoft\WinGet\Links\%~1" exit /b 0
where "%~1" >nul 2>nul
if not errorlevel 1 exit /b 0
echo.
echo Installing %~2 ...
winget install --id %~2 --exact --source winget
exit /b %errorlevel%

:failed
echo.
echo Setup could not finish. Review the message above and run setup again.
pause
exit /b 1
