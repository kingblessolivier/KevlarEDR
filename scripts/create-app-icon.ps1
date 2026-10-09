# Recreate our original vector shield/check artwork as a multi-resolution Windows icon.
# Uses the installed Windows WPF renderer; no external artwork or icon font is bundled.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore, WindowsBase
$projectRoot = Split-Path -Parent $PSScriptRoot
$assetDirectory = Join-Path $projectRoot 'src\KevlarEDR.Desktop\Assets'
New-Item -ItemType Directory -Path $assetDirectory -Force | Out-Null
$drawing = New-Object Windows.Media.DrawingVisual
$context = $drawing.RenderOpen()
$blue = [Windows.Media.BrushConverter]::new().ConvertFromString('#1769E0')
$context.DrawRoundedRectangle($blue, $null, [Windows.Rect]::new(0,0,48,48), 10, 10)
$context.DrawGeometry([Windows.Media.Brushes]::White, $null, [Windows.Media.Geometry]::Parse('M24,8 L38,13 L37,26 Q34,36 24,41 Q14,36 11,26 L10,13 Z'))
$context.DrawGeometry($null, [Windows.Media.Pen]::new($blue,3), [Windows.Media.Geometry]::Parse('M17,24 L22,29 L31,19'))
$context.Close()
$iconImages = @()
foreach ($size in @(16,24,32,48,64,128,256)) {
    $bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new($size,$size,96*$size/48,96*$size/48,[Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($drawing)
    $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = [IO.MemoryStream]::new()
    $encoder.Save($stream)
    $iconImages += [pscustomobject]@{Size=$size;Bytes=$stream.ToArray()}
    $stream.Dispose()
}
$output = [IO.File]::Create((Join-Path $assetDirectory 'KevlarEDR.ico'))
$writer = [IO.BinaryWriter]::new($output)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$iconImages.Count)
    $offset = 6 + 16*$iconImages.Count
    foreach ($iconImage in $iconImages) {
        $dimension = if ($iconImage.Size -eq 256) { 0 } else { $iconImage.Size }
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
        $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$iconImage.Bytes.Length); $writer.Write([uint32]$offset)
        $offset += $iconImage.Bytes.Length
    }
    foreach ($iconImage in $iconImages) { $writer.Write([byte[]]$iconImage.Bytes) }
} finally { $writer.Dispose(); $output.Dispose() }
Write-Output 'Created original KevlarEDR icon in 7 sizes.'
