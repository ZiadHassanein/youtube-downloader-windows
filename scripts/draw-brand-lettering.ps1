$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore
$projectRoot = Split-Path -Parent $PSScriptRoot
# The selected vector contours preserve the approved lettering. The V's
# vertices are mirrored exactly around x=125.5, with a sharp lower point.
$art = Get-Content -LiteralPath (Join-Path $projectRoot 'assets/brand/wordmark.geometry.json') -Raw | ConvertFrom-Json
$symbolsPath = Join-Path $projectRoot 'src/Downloader.Desktop/Themes/Symbols.xaml'
$symbols = [IO.File]::ReadAllText($symbolsPath)
foreach ($entry in @(@{Key='BrandMarkGeometry';Data=$art.mark;Width=64;Height=64}, @{Key='BrandInitialGeometry';Data=$art.initial;Width=$art.canvas.width;Height=$art.canvas.height}, @{Key='BrandLettersGeometry';Data=$art.letters;Width=$art.canvas.width;Height=$art.canvas.height}, @{Key='BrandArrowGeometry';Data=$art.arrow;Width=$art.canvas.width;Height=$art.canvas.height}, @{Key='BrandIconArrowGeometry';Data=$art.iconArrow;Width=64;Height=64})) {
    $bounds = [Windows.Media.Geometry]::Parse($entry.Data).Bounds
    if ($bounds.Left -lt 0 -or $bounds.Top -lt 0 -or $bounds.Right -gt $entry.Width -or $bounds.Bottom -gt $entry.Height) { throw "Invalid canvas bounds for $($entry.Key)." }
    if ($symbols.Contains('x:Key="' + $entry.Key + '"')) {
        $symbols = [regex]::Replace($symbols, '(<Geometry x:Key="' + $entry.Key + '">).*?(</Geometry>)', { param($m) $m.Groups[1].Value + $entry.Data + $m.Groups[2].Value })
    } else {
        $symbols = $symbols.Replace('  <SolidColorBrush x:Key="BrandFill"', '  <Geometry x:Key="' + $entry.Key + '">' + $entry.Data + '</Geometry>' + "`r`n" + '  <SolidColorBrush x:Key="BrandFill"')
    }
}
$symbols = [regex]::Replace($symbols, '<!-- Original.*?-->', '<!-- Approved integrated angular V/down-arrow and geometric ydrop; one shared vector pair for both themes. -->')
[IO.File]::WriteAllText($symbolsPath, $symbols, [Text.UTF8Encoding]::new($false))
Write-Output 'Applied the approved integrated Vydrop wordmark to the shared geometry resources.'
