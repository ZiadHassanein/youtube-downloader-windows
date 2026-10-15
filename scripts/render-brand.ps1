$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase
$projectRoot = Split-Path -Parent $PSScriptRoot
[xml]$symbols = Get-Content -LiteralPath (Join-Path $projectRoot 'src/Downloader.Desktop/Themes/Symbols.xaml') -Raw
$namespaces = [Xml.XmlNamespaceManager]::new($symbols.NameTable)
$namespaces.AddNamespace('x', 'http://schemas.microsoft.com/winfx/2006/xaml')
$mark = $symbols.SelectSingleNode("//*[@x:Key='PlayMark']", $namespaces).InnerText
$geometry = [Windows.Media.Geometry]::Parse($mark)
$fill = $symbols.SelectSingleNode("//*[@x:Key='BrandFill']", $namespaces)
$brush = [Windows.Media.LinearGradientBrush]::new()
$brush.StartPoint = [Windows.Point]::Parse($fill.StartPoint)
$brush.EndPoint = [Windows.Point]::Parse($fill.EndPoint)
foreach ($stop in $fill.ChildNodes) {
    $brush.GradientStops.Add([Windows.Media.GradientStop]::new(
        [Windows.Media.ColorConverter]::ConvertFromString($stop.Color),
        [double]::Parse($stop.Offset, [Globalization.CultureInfo]::InvariantCulture)))
}
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
Write-Output 'Rendered transparent ViFree mark and seven native icon sizes from Symbols.xaml.'
