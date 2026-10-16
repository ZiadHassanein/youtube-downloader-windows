$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase
$projectRoot = Split-Path -Parent $PSScriptRoot
# Original monoline lettering: squared bowls, an open r shoulder and a hooked y.
# The larger red V, shared canvas and baseline are preserved.
$skeleton = [Windows.Media.Geometry]::Parse(@'
M25.6,10.6 L31.7,26.7 M38.6,10.6 L31.4,29.6 Q30.1,32.95 26.7,32.95
M56.4,6.1 V26.8 M56.4,19 C56.4,13.7 54.4,10.8 49.5,10.8 C44.6,10.8 42.7,13.7 42.7,19 V19.6 C42.7,24.8 44.6,27 49.5,27 C54.4,27 56.4,24.8 56.4,19.6
M62.4,27 V11 M62.4,16.5 C63.2,12.8 65.2,10.8 69.9,10.8
M80,10.8 C75,10.8 73,13.4 73,18.8 V19.6 C73,24.7 75,27 80,27 C85,27 87,24.7 87,19.6 V18.8 C87,13.4 85,10.8 80,10.8 Z
M92.6,10.8 V32.95 M92.6,19 C92.6,13.7 94.7,10.8 99.5,10.8 C104.3,10.8 106.4,13.7 106.4,19 V19.6 C106.4,24.8 104.3,27 99.5,27 C94.7,27 92.6,24.8 92.6,19.6
'@)
$pen = [Windows.Media.Pen]::new([Windows.Media.Brushes]::Black, 2.3)
$pen.StartLineCap = [Windows.Media.PenLineCap]::Flat
$pen.EndLineCap = [Windows.Media.PenLineCap]::Flat
$pen.LineJoin = [Windows.Media.PenLineJoin]::Round
$outlines = $skeleton.GetWidenedPathGeometry($pen, .035, [Windows.Media.ToleranceType]::Absolute)
# Oblique stem cuts echo the V's angled terminal without closing the counters.
$cuts = [Windows.Media.Geometry]::Parse('M55.25,6.1 H57.55 L55.25,7.8 Z M61.25,11 H63.55 L61.25,12.7 Z M91.45,10.8 H93.75 L91.45,12.5 Z M69,9.65 H69.9 V11.95 Z')
$letters = ([Windows.Media.Geometry]::Combine($outlines, $cuts, [Windows.Media.GeometryCombineMode]::Exclude, $null)).GetFlattenedPathGeometry(.025, [Windows.Media.ToleranceType]::Absolute).Clone()
function Transform-LetterPoints($shape, [Windows.Media.Matrix]$matrix) {
    foreach ($figure in $shape.Figures) {
        $figure.StartPoint = $matrix.Transform($figure.StartPoint)
        foreach ($segment in $figure.Segments) {
            if ($segment -is [Windows.Media.PolyLineSegment]) {
                for ($i=0; $i -lt $segment.Points.Count; $i++) { $segment.Points[$i] = $matrix.Transform($segment.Points[$i]) }
            } elseif ($segment -is [Windows.Media.LineSegment]) { $segment.Point = $matrix.Transform($segment.Point) }
            else { throw 'The flattened lettering must contain only line segments.' }
        }
    }
}
# Bake transforms into points: Geometry.ToString does not carry a Transform to SVG.
Transform-LetterPoints $letters ([Windows.Media.Matrix]::new(1,0,-.06,1,1.68,0))
$xScale = (103.5 - 25) / $letters.Bounds.Width
Transform-LetterPoints $letters ([Windows.Media.Matrix]::new($xScale,0,0,1,25 - $letters.Bounds.Left * $xScale,0))
if ($letters.Bounds.Left -lt 0 -or $letters.Bounds.Right -gt 104.066 -or $letters.Bounds.Top -lt 0 -or $letters.Bounds.Bottom -gt 34.104) {
    throw 'The lettering must fit the shared desktop/browser wordmark canvas.'
}
$data = $letters.ToString([Globalization.CultureInfo]::InvariantCulture)
$symbolsPath = Join-Path $projectRoot 'src/Downloader.Desktop/Themes/Symbols.xaml'
$symbols = [IO.File]::ReadAllText($symbolsPath)
$symbols = [regex]::Replace($symbols, '(<Geometry x:Key="BrandLettersGeometry">).*?(</Geometry>)', { param($m) $m.Groups[1].Value + $data + $m.Groups[2].Value })
$symbols = $symbols.Replace('Fixed vector lettering: custom V, Inter Regular ydrop; see assets/fonts/Inter-LICENSE.txt.', 'Original vector lettering: larger red V and custom monoline ydrop with angled terminals.')
[IO.File]::WriteAllText($symbolsPath, $symbols, [Text.UTF8Encoding]::new($false))
$svgData = $data -replace '^F[01]\s*', ''
$svgRule = if ($data.StartsWith('F1')) { 'nonzero' } else { 'evenodd' }
$popupPath = Join-Path $projectRoot 'browser/common/popup.html'
$popup = [IO.File]::ReadAllText($popupPath)
$popup = [regex]::Replace($popup, '<path fill="currentColor"[^>]*/>', { param($m) '<path fill="currentColor" fill-rule="' + $svgRule + '" d="' + $svgData + '"/>' })
[IO.File]::WriteAllText($popupPath, $popup, [Text.UTF8Encoding]::new($false))

Write-Output $letters.Bounds
