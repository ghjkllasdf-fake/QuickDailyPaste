Add-Type -AssemblyName System.Drawing

function Make-Icon([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.Clear([System.Drawing.Color]::Transparent)

    # Rounded rectangle
    $r = [int]($size * 0.2)
    $d = $r * 2
    $w = $size - 1
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0, 0, $d, $d, 180, 90)
    $path.AddArc($w - $d, 0, $d, $d, 270, 90)
    $path.AddArc($w - $d, $w - $d, $d, $d, 0, 90)
    $path.AddArc(0, $w - $d, $d, $d, 90, 90)
    $path.CloseFigure()

    # Blue fill
    $blue = [System.Drawing.Color]::FromArgb(255, 49, 120, 230)
    $brush = New-Object System.Drawing.SolidBrush($blue)
    $g.FillPath($brush, $path)
    $brush.Dispose()

    # Top highlight
    $g.SetClip($path)
    $hl = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(30, 255, 255, 255))
    $g.FillRectangle($hl, 0, 0, $size, [int]($size * 0.45))
    $hl.Dispose()

    # "Q" character
    $fs = [Math]::Max(8, $size * 0.58)
    $font = New-Object System.Drawing.Font("Segoe UI", $fs, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $sf = New-Object System.Drawing.StringFormat
    $sf.Alignment = [System.Drawing.StringAlignment]::Center
    $sf.LineAlignment = [System.Drawing.StringAlignment]::Center

    # Shadow
    $shadow = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(50, 0, 0, 0))
    $sr = New-Object System.Drawing.RectangleF([float]1, [float]1, [float]$size, [float]$size)
    $g.DrawString("Q", $font, $shadow, $sr, $sf)
    $shadow.Dispose()

    # White Q
    $tr = New-Object System.Drawing.RectangleF([float]0, [float](-$size * 0.02), [float]$size, [float]$size)
    $white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $g.DrawString("Q", $font, $white, $tr, $sf)
    $white.Dispose()

    $g.ResetClip()
    $font.Dispose()
    $sf.Dispose()
    $path.Dispose()
    $g.Dispose()
    return $bmp
}

# Generate multiple sizes
$allSizes = @(16, 32, 48, 256)
$pngArrays = @()
foreach ($sz in $allSizes) {
    $bmp = Make-Icon $sz
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngArrays += ,($ms.ToArray())
    $ms.Dispose()
    # Save 256 as preview
    if ($sz -eq 256) {
        $bmp.Save((Join-Path $PSScriptRoot "app_preview.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    $bmp.Dispose()
}

# Build ICO file
$out = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($out)

# Header
$bw.Write([uint16]0)            # reserved
$bw.Write([uint16]1)            # type = icon
$bw.Write([uint16]$allSizes.Count)

# Directory entries
$dataOffset = 6 + $allSizes.Count * 16
for ($i = 0; $i -lt $allSizes.Count; $i++) {
    $s = $allSizes[$i]
    [byte]$dim = if ($s -ge 256) { 0 } else { [byte]$s }
    $bw.Write($dim)              # width
    $bw.Write($dim)              # height
    $bw.Write([byte]0)           # palette
    $bw.Write([byte]0)           # reserved
    $bw.Write([uint16]1)         # planes
    $bw.Write([uint16]32)        # bpp
    $bw.Write([uint32]($pngArrays[$i].Length))
    $bw.Write([uint32]$dataOffset)
    $dataOffset += $pngArrays[$i].Length
}

# Image data
foreach ($png in $pngArrays) {
    $bw.Write($png)
}

$bytes = $out.ToArray()
[System.IO.File]::WriteAllBytes((Join-Path $PSScriptRoot "app.ico"), $bytes)
$bw.Dispose()
$out.Dispose()

$f = Get-Item (Join-Path $PSScriptRoot "app.ico")
Write-Host "app.ico created: $($f.Length) bytes (16/32/48/256px)"
