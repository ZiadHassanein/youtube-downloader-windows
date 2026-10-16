$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase
$projectRoot = Split-Path -Parent $PSScriptRoot
[xml]$symbols = Get-Content -LiteralPath (Join-Path $projectRoot 'src/Downloader.Desktop/Themes/Symbols.xaml') -Raw
$namespaces = [Xml.XmlNamespaceManager]::new($symbols.NameTable)
$namespaces.AddNamespace('x', 'http://schemas.microsoft.com/winfx/2006/xaml')
$mark = $symbols.SelectSingleNode("//*[@x:Key='BrandMarkGeometry']", $namespaces).InnerText
$geometry = [Windows.Media.Geometry]::Parse($mark)
$fill = $symbols.SelectSingleNode("//*[@x:Key='BrandFill']", $namespaces)
$brush = [Windows.Media.SolidColorBrush]::new([Windows.Media.ColorConverter]::ConvertFromString($fill.Color))
function Render-Mark([int]$size) {
    $visual = [Windows.Media.DrawingVisual]::new()
    $drawing = $visual.RenderOpen()
    $drawing.PushTransform([Windows.Media.ScaleTransform]::new($size / 64.0, $size / 64.0))
    $drawing.DrawGeometry($brush, $null, $geometry)
    $drawing.Pop()
    $drawing.Close()
    $bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new($size, $size, 96, 96, [Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $memory = [IO.MemoryStream]::new()
    $encoder.Save($memory)
    return ,$memory.ToArray()
}
[IO.File]::WriteAllBytes((Join-Path $projectRoot 'assets/header-logo.png'), (Render-Mark 512))
$svg = '<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512" viewBox="0 0 64 64" role="img" aria-label="Vydrop download logo"><path fill="' + $fill.Color + '" d="' + $mark + '"/></svg>'
[IO.File]::WriteAllText((Join-Path $projectRoot 'assets/brand-mark.svg'), $svg, [Text.UTF8Encoding]::new($false))
$initialData = $symbols.SelectSingleNode("//*[@x:Key='BrandInitialGeometry']", $namespaces).InnerText
$lettersData = $symbols.SelectSingleNode("//*[@x:Key='BrandLettersGeometry']", $namespaces).InnerText
$initial = [Windows.Media.Geometry]::Parse($initialData)
$letters = [Windows.Media.Geometry]::Parse($lettersData)
# WPF's F1 prefix selects nonzero fill; SVG carries that as a separate attribute.
$lettersSvgData = $lettersData -replace '^F[01]\s*', ''
$lettersSvgRule = if ($lettersData.StartsWith('F1')) { 'nonzero' } else { 'evenodd' }
$markScale = [Math]::Min(30 / $geometry.Bounds.Width, 34 / $geometry.Bounds.Height)
$markX = (30 - $geometry.Bounds.Width * $markScale) / 2 - $geometry.Bounds.X * $markScale
$markY = 4 + (34 - $geometry.Bounds.Height * $markScale) / 2 - $geometry.Bounds.Y * $markScale
$typeScale = [Math]::Min(90 / 104.066, 30 / 34.104)
$typeX = 38 + (90 - 104.066 * $typeScale) / 2
$typeY = 6 + (30 - 34.104 * $typeScale) / 2
$culture = [Globalization.CultureInfo]::InvariantCulture
$markTransform = 'matrix(' + $markScale.ToString($culture) + ' 0 0 ' + $markScale.ToString($culture) + ' ' + $markX.ToString($culture) + ' ' + $markY.ToString($culture) + ')'
$typeTransform = 'matrix(' + $typeScale.ToString($culture) + ' 0 0 ' + $typeScale.ToString($culture) + ' ' + $typeX.ToString($culture) + ' ' + $typeY.ToString($culture) + ')'
foreach ($variant in @(@{Name='light';Ink='#272329'}, @{Name='dark';Ink='#F6F2F4'})) {
    $lockup = '<svg xmlns="http://www.w3.org/2000/svg" width="512" height="168" viewBox="0 0 128 42" role="img" aria-label="Vydrop"><title>Vydrop</title><desc>Slim red download arrow and tray, red V and lighter fixed vector lettering in a compact lockup.</desc><path fill="' + $fill.Color + '" transform="' + $markTransform + '" d="' + $mark + '"/><g transform="' + $typeTransform + '"><path fill="' + $fill.Color + '" d="' + $initialData + '"/><path fill="' + $variant.Ink + '" fill-rule="' + $lettersSvgRule + '" d="' + $lettersSvgData + '"/></g></svg>'
    [IO.File]::WriteAllText((Join-Path $projectRoot ('assets/brand-logo-' + $variant.Name + '.svg')), $lockup, [Text.UTF8Encoding]::new($false))
    $visual = [Windows.Media.DrawingVisual]::new()
    $drawing = $visual.RenderOpen()
    $drawing.PushTransform([Windows.Media.ScaleTransform]::new(4,4))
    $drawing.PushTransform([Windows.Media.MatrixTransform]::new($markScale,0,0,$markScale,$markX,$markY))
    $drawing.DrawGeometry($brush, $null, $geometry); $drawing.Pop()
    $drawing.PushTransform([Windows.Media.MatrixTransform]::new($typeScale,0,0,$typeScale,$typeX,$typeY))
    $drawing.DrawGeometry($brush, $null, $initial)
    $drawing.DrawGeometry([Windows.Media.SolidColorBrush]::new([Windows.Media.ColorConverter]::ConvertFromString($variant.Ink)), $null, $letters)
    $drawing.Pop(); $drawing.Pop(); $drawing.Close()
    $bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new(512,168,96,96,[Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $target = [IO.File]::Create((Join-Path $projectRoot ('assets/brand-logo-' + $variant.Name + '.png')))
    try { $encoder.Save($target) } finally { $target.Dispose() }
}
$sizes = @(16,24,32,48,64,128,256)
$frames = @($sizes | ForEach-Object { ,(Render-Mark $_) })
$stream = [IO.File]::Create((Join-Path $projectRoot 'assets/app.ico'))
$writer = [IO.BinaryWriter]::new($stream)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $dimension = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
        $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$frames[$i].Length); $writer.Write([uint32]$offset)
        $offset += $frames[$i].Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]]$frame) }
} finally { $writer.Dispose(); $stream.Dispose() }
Write-Output 'Rendered transparent Vydrop mark and seven native icon sizes from Symbols.xaml.'
