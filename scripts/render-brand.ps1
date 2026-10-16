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
$wordmarkData = $symbols.SelectSingleNode("//*[@x:Key='BrandWordmarkGeometry']", $namespaces).InnerText
$wordmark = [Windows.Media.Geometry]::Parse($wordmarkData)
foreach ($variant in @(@{Name='light';Ink='#272329'}, @{Name='dark';Ink='#F6F2F4'})) {
    $lockup = '<svg xmlns="http://www.w3.org/2000/svg" width="594" height="156" viewBox="0 0 198 52" role="img" aria-label="Vydrop"><title>Vydrop</title><desc>Red download arrow and tray with fixed vector Vydrop lettering.</desc><path fill="' + $fill.Color + '" transform="translate(0 1) scale(.78)" d="' + $mark + '"/><path fill="' + $variant.Ink + '" transform="translate(61 10) scale(1.26)" d="' + $wordmarkData + '"/></svg>'
    [IO.File]::WriteAllText((Join-Path $projectRoot ('assets/brand-logo-' + $variant.Name + '.svg')), $lockup, [Text.UTF8Encoding]::new($false))
    $visual = [Windows.Media.DrawingVisual]::new()
    $drawing = $visual.RenderOpen()
    $drawing.PushTransform([Windows.Media.ScaleTransform]::new(3,3))
    $drawing.PushTransform([Windows.Media.MatrixTransform]::new(.78,0,0,.78,0,1))
    $drawing.DrawGeometry($brush, $null, $geometry); $drawing.Pop()
    $drawing.PushTransform([Windows.Media.MatrixTransform]::new(1.26,0,0,1.26,61,10))
    $drawing.DrawGeometry([Windows.Media.SolidColorBrush]::new([Windows.Media.ColorConverter]::ConvertFromString($variant.Ink)), $null, $wordmark)
    $drawing.Pop(); $drawing.Pop(); $drawing.Close()
    $bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new(594,156,96,96,[Windows.Media.PixelFormats]::Pbgra32)
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
