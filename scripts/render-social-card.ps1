$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase
$cardRoot = Split-Path -Parent $PSScriptRoot
[xml]$cardSymbols = Get-Content -LiteralPath (Join-Path $cardRoot 'src/Downloader.Desktop/Themes/Symbols.xaml') -Raw
$cardNamespaces = [Xml.XmlNamespaceManager]::new($cardSymbols.NameTable)
$cardNamespaces.AddNamespace('x', 'http://schemas.microsoft.com/winfx/2006/xaml')
function CardBrush([string]$Color) { [Windows.Media.SolidColorBrush]::new([Windows.Media.ColorConverter]::ConvertFromString($Color)) }
function CardGeometry([string]$Key) { [Windows.Media.Geometry]::Parse($cardSymbols.SelectSingleNode("//*[@x:Key='$Key']", $cardNamespaces).InnerText) }
$cardVisual = [Windows.Media.DrawingVisual]::new()
$cardDrawing = $cardVisual.RenderOpen()
$cardDrawing.DrawRectangle((CardBrush '#181B20'), $null, [Windows.Rect]::new(0, 0, 1200, 630))
$cardDrawing.PushTransform([Windows.Media.TranslateTransform]::new(228, 100))
$cardDrawing.PushTransform([Windows.Media.ScaleTransform]::new(.78, .78))
$cardDrawing.DrawGeometry((CardBrush '#E63D4D'), $null, (CardGeometry 'BrandInitialGeometry'))
$cardDrawing.DrawGeometry((CardBrush '#FFFFFF'), $null, (CardGeometry 'BrandArrowGeometry'))
$cardDrawing.DrawGeometry((CardBrush '#F6F2F4'), $null, (CardGeometry 'BrandLettersGeometry'))
$cardDrawing.Pop(); $cardDrawing.Pop()
foreach ($cardLine in @(
    @{ Text = 'Free video & audio downloader'; Size = 38; Y = 375; Ink = '#F6F2F4' },
    @{ Text = 'YouTube + 1,000+ supported sites'; Size = 28; Y = 438; Ink = '#C5B5C0' },
    @{ Text = 'Windows 10/11 x64  ·  English & Arabic  ·  Open source'; Size = 22; Y = 502; Ink = '#C5B5C0' }
)) {
    $cardText = [Windows.Media.FormattedText]::new($cardLine.Text, [Globalization.CultureInfo]::InvariantCulture, [Windows.FlowDirection]::LeftToRight, [Windows.Media.Typeface]::new('Segoe UI'), $cardLine.Size, (CardBrush $cardLine.Ink), 1.0)
    $cardDrawing.DrawText($cardText, [Windows.Point]::new((1200 - $cardText.Width) / 2, $cardLine.Y))
}
$cardDrawing.Close()
$cardBitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new(1200, 630, 96, 96, [Windows.Media.PixelFormats]::Pbgra32)
$cardBitmap.Render($cardVisual)
$cardMetadata = [Windows.Media.Imaging.BitmapMetadata]::new('png')
$cardMetadata.SetQuery('/tEXt/{str=origin}', 'Original 1200x630 social card rendered from the approved Vydrop vector geometry by scripts/render-social-card.ps1; no cursor; not an application screenshot.')
$cardEncoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
$cardEncoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($cardBitmap, $null, $cardMetadata, $null))
$cardStream = [IO.MemoryStream]::new()
try {
    $cardEncoder.Save($cardStream)
    foreach ($cardFile in @('assets/social-card.png', 'docs/assets/social-card.png')) {
        [IO.File]::WriteAllBytes((Join-Path $cardRoot $cardFile), $cardStream.ToArray())
    }
} finally { $cardStream.Dispose() }
Write-Output 'Rendered the 1200x630 social card from the unchanged approved logo.'
