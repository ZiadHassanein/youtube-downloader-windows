$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase
$projectRoot = Split-Path -Parent $PSScriptRoot
[xml]$symbols = Get-Content -LiteralPath (Join-Path $projectRoot 'src/Downloader.Desktop/Themes/Symbols.xaml') -Raw
$namespaces = [Xml.XmlNamespaceManager]::new($symbols.NameTable)
$namespaces.AddNamespace('x', 'http://schemas.microsoft.com/winfx/2006/xaml')
$mark = $symbols.SelectSingleNode("//*[@x:Key='BrandMarkGeometry']", $namespaces).InnerText
$geometry = [Windows.Media.Geometry]::Parse($mark)
$artConfig = Get-Content -LiteralPath (Join-Path $projectRoot 'assets/brand/wordmark.geometry.json') -Raw | ConvertFrom-Json
$iconArrowData = $symbols.SelectSingleNode("//*[@x:Key='BrandIconArrowGeometry']", $namespaces).InnerText
$iconArrow = [Windows.Media.Geometry]::Parse($iconArrowData)
$iconArrowPen = [Windows.Media.Pen]::new([Windows.Media.Brushes]::White, $artConfig.iconArrowStroke)
$iconArrowPen.LineJoin = [Windows.Media.PenLineJoin]::Miter
$iconArrowPen.MiterLimit = 2
$fill = $symbols.SelectSingleNode("//*[@x:Key='BrandFill']", $namespaces)
$brush = [Windows.Media.SolidColorBrush]::new([Windows.Media.ColorConverter]::ConvertFromString($fill.Color))
function New-BrandFrame($bitmap) {
    $metadata = [Windows.Media.Imaging.BitmapMetadata]::new('png')
    $metadata.SetQuery('/tEXt/{str=origin}', 'Original vector render of the approved Vydrop concept; source assets/brand/wordmark.geometry.json and approved-reference.png; rendered by scripts/render-brand.ps1. No cursor; not an app screenshot.')
    return [Windows.Media.Imaging.BitmapFrame]::Create($bitmap, $null, $metadata, $null)
}
function Render-Mark([int]$size) {
    $visual = [Windows.Media.DrawingVisual]::new()
    $drawing = $visual.RenderOpen()
    $drawing.PushTransform([Windows.Media.ScaleTransform]::new($size / 64.0, $size / 64.0))
    $drawing.DrawGeometry($brush, $null, $geometry)
    $drawing.DrawGeometry($null, $iconArrowPen, $iconArrow)
    $drawing.Pop()
    $drawing.Close()
    $bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new($size, $size, 96, 96, [Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add((New-BrandFrame $bitmap))
    $memory = [IO.MemoryStream]::new()
    $encoder.Save($memory)
    return ,$memory.ToArray()
}
[IO.File]::WriteAllBytes((Join-Path $projectRoot 'assets/header-logo.png'), (Render-Mark 512))
$markSvgData = $mark -replace '^F[01]\s*', ''
$markSvgRule = if ($mark.StartsWith('F1')) { 'nonzero' } else { 'evenodd' }
$iconArrowSvg = '<path fill="none" stroke="#FFFFFF" stroke-width="' + $artConfig.iconArrowStroke + '" stroke-linejoin="miter" stroke-miterlimit="2" d="' + $iconArrowData + '"/>'
$svg = '<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512" viewBox="0 0 64 64" role="img" aria-label="Vydrop download logo"><path fill="' + $fill.Color + '" fill-rule="' + $markSvgRule + '" d="' + $markSvgData + '"/>' + $iconArrowSvg + '</svg>'
[IO.File]::WriteAllText((Join-Path $projectRoot 'assets/brand-mark.svg'), $svg, [Text.UTF8Encoding]::new($false))
$initialData = $symbols.SelectSingleNode("//*[@x:Key='BrandInitialGeometry']", $namespaces).InnerText
$lettersData = $symbols.SelectSingleNode("//*[@x:Key='BrandLettersGeometry']", $namespaces).InnerText
$initial = [Windows.Media.Geometry]::Parse($initialData)
$letters = [Windows.Media.Geometry]::Parse($lettersData)
$arrowData = $symbols.SelectSingleNode("//*[@x:Key='BrandArrowGeometry']", $namespaces).InnerText
$arrow = [Windows.Media.Geometry]::Parse($arrowData)
$arrowPen = [Windows.Media.Pen]::new([Windows.Media.Brushes]::White, $artConfig.arrowStroke)
$arrowPen.LineJoin = [Windows.Media.PenLineJoin]::Miter
$arrowPen.MiterLimit = 2
$arrowSvg = '<path fill="none" stroke="#FFFFFF" stroke-width="' + $artConfig.arrowStroke + '" stroke-linejoin="miter" stroke-miterlimit="2" d="' + $arrowData + '"/>'
$initialSvgData = $initialData -replace '^F[01]\s*', ''
$initialSvgRule = if ($initialData.StartsWith('F1')) { 'nonzero' } else { 'evenodd' }
# WPF's F1 prefix selects nonzero fill; SVG carries that as a separate attribute.
$lettersSvgData = $lettersData -replace '^F[01]\s*', ''
$lettersSvgRule = if ($lettersData.StartsWith('F1')) { 'nonzero' } else { 'evenodd' }
$typeScale = [Math]::Min(128 / 966, 38 / 284)
$typeX = (128 - 966 * $typeScale) / 2
$typeY = (42 - 284 * $typeScale) / 2
$culture = [Globalization.CultureInfo]::InvariantCulture
$typeTransform = 'matrix(' + $typeScale.ToString($culture) + ' 0 0 ' + $typeScale.ToString($culture) + ' ' + $typeX.ToString($culture) + ' ' + $typeY.ToString($culture) + ')'
foreach ($variant in @(@{Name='light';Ink='#272329';Page='#FFFFFF'}, @{Name='dark';Ink='#F6F2F4';Page='#181B20'})) {
    $lockup = '<svg xmlns="http://www.w3.org/2000/svg" width="512" height="168" viewBox="0 0 128 42" role="img" aria-label="Vydrop"><title>Vydrop</title><desc>Approved integrated wordmark: a symmetrical sharp red V with a white-outlined download arrow, followed by matching geometric lettering.</desc><g transform="' + $typeTransform + '"><path fill="' + $fill.Color + '" fill-rule="' + $initialSvgRule + '" d="' + $initialSvgData + '"/>' + $arrowSvg + '<path fill="' + $variant.Ink + '" fill-rule="' + $lettersSvgRule + '" d="' + $lettersSvgData + '"/></g></svg>'
    [IO.File]::WriteAllText((Join-Path $projectRoot ('assets/brand-logo-' + $variant.Name + '.svg')), $lockup, [Text.UTF8Encoding]::new($false))
    foreach ($background in @($false, $true)) {
    $visual = [Windows.Media.DrawingVisual]::new()
    $drawing = $visual.RenderOpen()
    if ($background) { $drawing.DrawRectangle([Windows.Media.SolidColorBrush]::new([Windows.Media.ColorConverter]::ConvertFromString($variant.Page)), $null, [Windows.Rect]::new(0,0,512,168)) }
    $drawing.PushTransform([Windows.Media.ScaleTransform]::new(4,4))
    $drawing.PushTransform([Windows.Media.MatrixTransform]::new($typeScale,0,0,$typeScale,$typeX,$typeY))
    $drawing.DrawGeometry($brush, $null, $initial)
    $drawing.DrawGeometry($null, $arrowPen, $arrow)
    $drawing.DrawGeometry([Windows.Media.SolidColorBrush]::new([Windows.Media.ColorConverter]::ConvertFromString($variant.Ink)), $null, $letters)
    $drawing.Pop(); $drawing.Pop(); $drawing.Close()
    $bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new(512,168,96,96,[Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add((New-BrandFrame $bitmap))
    $prefix = if ($background) { 'brand-preview-' } else { 'brand-logo-' }
    $target = [IO.File]::Create((Join-Path $projectRoot ('assets/' + $prefix + $variant.Name + '.png')))
    try { $encoder.Save($target) } finally { $target.Dispose() }
    }
}
$popupSvg = $lockup.Replace('width="512" height="168"', 'width="128" height="42" class="brand-wordmark"').Replace('fill="' + $variant.Ink + '"', 'fill="currentColor"')
$popupPath = Join-Path $projectRoot 'browser/common/popup.html'
$popup = [regex]::Replace([IO.File]::ReadAllText($popupPath), '<header>.*?</header>', { param($m) '<header>' + $popupSvg + '</header>' })
[IO.File]::WriteAllText($popupPath, $popup, [Text.UTF8Encoding]::new($false))
$overlayPath = Join-Path $projectRoot 'browser/common/video-overlay.js'
$overlay = [regex]::Replace([IO.File]::ReadAllText($overlayPath), 'mark\.setAttribute\("d", "[^"]*"\);', { param($m) 'mark.setAttribute("d", "' + $markSvgData + '");' })
$overlay = [regex]::Replace($overlay, 'arrow\.setAttribute\("d", "[^"]*"\);', { param($m) 'arrow.setAttribute("d", "' + $iconArrowData + '");' })
$overlay = [regex]::Replace($overlay, 'arrow\.setAttribute\("stroke-width", "[^"]*"\);', { param($m) 'arrow.setAttribute("stroke-width", "' + $artConfig.iconArrowStroke + '");' })
[IO.File]::WriteAllText($overlayPath, $overlay, [Text.UTF8Encoding]::new($false))
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
Write-Output 'Rendered both theme wordmarks and V-only native/browser icons from the approved shared geometry.'
