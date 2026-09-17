Add-Type -AssemblyName System.Drawing

function Get-RoundedRectPath([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $path.AddArc($x, $y, $d, $d, 180, 90)
    $path.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $path.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $path.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    return $path
}

function Draw-Icon([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $bmp.SetResolution(96, 96)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([System.Drawing.Color]::Transparent)

    $rect = New-Object System.Drawing.RectangleF 0, 0, $size, $size
    $r = [float]($size * 0.22)
    $path = Get-RoundedRectPath 0 0 $size $size $r
    $c1 = [System.Drawing.Color]::FromArgb(0x7C, 0x6C, 0xF7)
    $c2 = [System.Drawing.Color]::FromArgb(0x4B, 0x3F, 0xE3)
    $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush -ArgumentList $rect, $c1, $c2, 45.0
    $g.FillPath($bg, $path)
    $bg.Dispose(); $path.Dispose()

    $cx = [float]($size * 0.44)
    $cy = [float]($size * 0.44)
    $rad = [float]($size * 0.20)
    $pw = [float]($size * 0.06)
    $white = [System.Drawing.Color]::White
    $pen = New-Object System.Drawing.Pen -ArgumentList $white, $pw
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawEllipse($pen, ($cx - $rad), ($cy - $rad), ($rad * 2), ($rad * 2))
    $g.DrawLine($pen, ($cx + $rad * 0.7), ($cy + $rad * 0.7), ($cx + $rad * 1.55), ($cy + $rad * 1.55))

    $lineColor = [System.Drawing.Color]::FromArgb(0x4B, 0x3F, 0xE3)
    $lineBrush = New-Object System.Drawing.SolidBrush -ArgumentList $lineColor
    $lw = $rad * 0.9
    $lh = [float]($size * 0.04)
    $ly = $cy - $rad * 0.4
    for ($i = 0; $i -lt 3; $i++) {
        $g.FillRectangle($lineBrush, ($cx - $lw / 2), ($ly + $i * $lh * 2.2), $lw, $lh)
    }
    $lineBrush.Dispose(); $pen.Dispose(); $g.Dispose()
    return $bmp
}

function New-Ico([int[]]$sizes, [string]$outPath) {
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter $ms
    $bw.Write([UInt16]0)
    $bw.Write([UInt16]1)
    $bw.Write([UInt16]$sizes.Length)

    $pngs = @()
    foreach ($s in $sizes) {
        $bmp = Draw-Icon $s
        $pms = New-Object System.IO.MemoryStream
        $bmp.Save($pms, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngs += , $pms.ToArray()
        $pms.Dispose(); $bmp.Dispose()
    }
    $offset = 6 + 16 * $sizes.Length
    for ($i = 0; $i -lt $sizes.Length; $i++) {
        $s = $sizes[$i]
        $w = if ($s -ge 256) { 0 } else { $s }
        $bw.Write([byte]$w)
        $bw.Write([byte]$w)
        $bw.Write([byte]0)
        $bw.Write([byte]0)
        $bw.Write([UInt16]1)
        $bw.Write([UInt16]32)
        $bw.Write([UInt32]$pngs[$i].Length)
        $bw.Write([UInt32]$offset)
        $offset += $pngs[$i].Length
    }
    foreach ($p in $pngs) { $bw.Write($p) }
    [System.IO.File]::WriteAllBytes($outPath, $ms.ToArray())
    $bw.Dispose(); $ms.Dispose()
}

$out = "e:\qc\src\QuickOcr\Assets\app.ico"
New-Ico @(256, 128, 64, 48, 32, 24, 16) $out
$info = Get-Item $out
"ICO 生成完成：$($info.FullName) 大小 $([math]::Round($info.Length / 1KB, 1))KB"
