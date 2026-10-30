param([string]$Out = "D:\084json\Jukebox084\packaging\icon.png")

Add-Type -AssemblyName System.Drawing

$S = 4
$N = 256
$W = $N * $S

$bmp = New-Object System.Drawing.Bitmap($W, $W, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$g.ScaleTransform($S, $S)

$gold = [System.Drawing.Color]::FromArgb(255, 198, 162, 74)
$goldDim = [System.Drawing.Color]::FromArgb(130, 198, 162, 74)
$bone = [System.Drawing.Color]::FromArgb(255, 239, 230, 207)
$red = [System.Drawing.Color]::FromArgb(255, 163, 18, 27)
$redBright = [System.Drawing.Color]::FromArgb(255, 216, 38, 44)
$bgTop = [System.Drawing.Color]::FromArgb(255, 24, 24, 29)
$bgBottom = [System.Drawing.Color]::FromArgb(255, 4, 4, 6)

# background
$rect = New-Object System.Drawing.RectangleF(0, 0, $N, $N)
$bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $bgTop, $bgBottom, 90)
$g.FillRectangle($bg, $rect)
$bg.Dispose()

# vignette
$vp = New-Object System.Drawing.Drawing2D.GraphicsPath
$vp.AddEllipse(18, 18, 220, 220)
$pgb = New-Object System.Drawing.Drawing2D.PathGradientBrush($vp)
$pgb.CenterColor = [System.Drawing.Color]::FromArgb(0, 0, 0, 0)
$pgb.SurroundColors = @([System.Drawing.Color]::FromArgb(215, 0, 0, 0))
$g.FillPath($pgb, $vp)
$pgb.Dispose()
$vp.Dispose()

# noise
$rnd = New-Object System.Random(1337)
$noise = @(
    (New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(9, 255, 255, 255))),
    (New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(16, 255, 255, 255))),
    (New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(24, 255, 255, 255)))
)
for ($i = 0; $i -lt 3200; $i++) {
    $x = [single]($rnd.NextDouble() * $N)
    $y = [single]($rnd.NextDouble() * $N)
    $g.FillRectangle($noise[$i % 3], $x, $y, 0.55, 0.55)
}
$noise | ForEach-Object { $_.Dispose() }

# outer frame
$penFrame = New-Object System.Drawing.Pen($gold, 1.1)
$g.DrawRectangle($penFrame, 7.5, 7.5, 241, 241)
$penFrame.Dispose()
$penInner = New-Object System.Drawing.Pen($goldDim, 0.5)
$g.DrawRectangle($penInner, 11.5, 11.5, 233, 233)
$penInner.Dispose()

# corner brackets
$bp = New-Object System.Drawing.Pen($gold, 1.8)
$g.DrawLine($bp, 7.5, 22, 7.5, 7.5);  $g.DrawLine($bp, 7.5, 7.5, 22, 7.5)
$g.DrawLine($bp, 233, 7.5, 248.5, 7.5); $g.DrawLine($bp, 248.5, 7.5, 248.5, 22)
$g.DrawLine($bp, 7.5, 234, 7.5, 248.5); $g.DrawLine($bp, 7.5, 248.5, 22, 248.5)
$g.DrawLine($bp, 248.5, 234, 248.5, 248.5); $g.DrawLine($bp, 248.5, 248.5, 233, 248.5)
$bp.Dispose()

# diamond crest (upper half)
$cx = 128; $cy = 92; $d = 66; $dd = 60
$diamond = New-Object System.Drawing.Drawing2D.GraphicsPath
$diamond.AddPolygon(@(
    (New-Object System.Drawing.PointF($cx, ($cy - $d))),
    (New-Object System.Drawing.PointF(($cx + $d), $cy)),
    (New-Object System.Drawing.PointF($cx, ($cy + $d))),
    (New-Object System.Drawing.PointF(($cx - $d), $cy))
))
$pd = New-Object System.Drawing.Pen($gold, 2.0)
$g.DrawPath($pd, $diamond)
$pd.Dispose()
$diamondIn = New-Object System.Drawing.Drawing2D.GraphicsPath
$diamondIn.AddPolygon(@(
    (New-Object System.Drawing.PointF($cx, ($cy - $dd))),
    (New-Object System.Drawing.PointF(($cx + $dd), $cy)),
    (New-Object System.Drawing.PointF($cx, ($cy + $dd))),
    (New-Object System.Drawing.PointF(($cx - $dd), $cy))
))
$pdi = New-Object System.Drawing.Pen($goldDim, 0.6)
$g.DrawPath($pdi, $diamondIn)
$pdi.Dispose()
$diamondIn.Dispose()
$diamond.Dispose()

# vinyl record
$vx = 128; $vy = 92; $rOuter = 38
$vg = New-Object System.Drawing.Drawing2D.GraphicsPath
$vg.AddEllipse(($vx - $rOuter), ($vy - $rOuter), ($rOuter * 2), ($rOuter * 2))
$vgb = New-Object System.Drawing.Drawing2D.PathGradientBrush($vg)
$vgb.CenterColor = [System.Drawing.Color]::FromArgb(255, 20, 20, 23)
$vgb.SurroundColors = @([System.Drawing.Color]::FromArgb(255, 4, 4, 5))
$g.FillPath($vgb, $vg)
$vgb.Dispose()

$groove = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(90, 70, 70, 76), 0.32)
for ($r = 17; $r -lt $rOuter; $r += 2.2) {
    $g.DrawEllipse($groove, ($vx - $r), ($vy - $r), ($r * 2), ($r * 2))
}
$groove.Dispose()

$label = New-Object System.Drawing.SolidBrush($red)
$g.FillEllipse($label, ($vx - 12.5), ($vy - 12.5), 25, 25)
$label.Dispose()
$labelRing = New-Object System.Drawing.Pen($gold, 0.7)
$g.DrawEllipse($labelRing, ($vx - 12.5), ($vy - 12.5), 25, 25)
$labelRing.Dispose()
$hole = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 8, 8, 9))
$g.FillEllipse($hole, ($vx - 1.5), ($vy - 1.5), 3, 3)
$hole.Dispose()

# highlight arc
$hl = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(26, 255, 255, 255), 1.1)
$g.DrawArc($hl, ($vx - 34), ($vy - 34), 68, 68, 200, 45)
$hl.Dispose()

# small JUKEBOX header
$fontSmall = New-Object System.Drawing.Font("Georgia", 6.5, [System.Drawing.FontStyle]::Bold)
$brushGold = New-Object System.Drawing.SolidBrush($gold)
function Draw-Spaced([System.Drawing.Graphics]$gfx, [string]$text, [System.Drawing.Font]$fnt, [System.Drawing.Brush]$brush, [single]$centerX, [single]$y, [single]$gap) {
    $widths = @()
    foreach ($ch in $text.ToCharArray()) { $widths += $gfx.MeasureString([string]$ch, $fnt).Width }
    $total = ($widths | Measure-Object -Sum).Sum + $gap * ($text.Length - 1)
    $x = $centerX - $total / 2
    for ($i = 0; $i -lt $text.Length; $i++) {
        $gfx.DrawString([string]$text[$i], $fnt, $brush, [single]$x, $y)
        $x += $widths[$i] + $gap
    }
}
Draw-Spaced $g "JUKEBOX" $fontSmall $brushGold 128 14.5 1.15
$fontSmall.Dispose()

# LCB (below the crest)
$fontBig = New-Object System.Drawing.Font("Georgia", 38, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$fmt = New-Object System.Drawing.StringFormat
$fmt.Alignment = [System.Drawing.StringAlignment]::Center
$fmt.LineAlignment = [System.Drawing.StringAlignment]::Near

$shadow = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(200, 120, 10, 14))
$rectShadow = New-Object System.Drawing.RectangleF(1.4, 168.5, 256, 56)
$g.DrawString("LCB", $fontBig, $shadow, $rectShadow, $fmt)

$brushOutline = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 8, 8, 10))
$offsets = @(@(-1.1,0),@(1.1,0),@(0,-1.1),@(0,1.1),@(-0.8,-0.8),@(0.8,-0.8),@(-0.8,0.8),@(0.8,0.8))
foreach ($o in $offsets) {
    $r = New-Object System.Drawing.RectangleF($o[0], (166 + $o[1]), 256, 56)
    $g.DrawString("LCB", $fontBig, $brushOutline, $r, $fmt)
}
$brushBone = New-Object System.Drawing.SolidBrush($bone)
$rectText = New-Object System.Drawing.RectangleF(0, 166, 256, 56)
$g.DrawString("LCB", $fontBig, $brushBone, $rectText, $fmt)
$shadow.Dispose(); $brushOutline.Dispose(); $brushBone.Dispose(); $fontBig.Dispose(); $fmt.Dispose()

# underline accent
$ul = New-Object System.Drawing.Pen($redBright, 0.8)
$g.DrawLine($ul, 96, 209, 160, 209)
$ul.Dispose()

# seven sin dots
$sinColors = @(
    [System.Drawing.Color]::FromArgb(255, 226, 58, 50),
    [System.Drawing.Color]::FromArgb(255, 240, 140, 40),
    [System.Drawing.Color]::FromArgb(255, 240, 210, 75),
    [System.Drawing.Color]::FromArgb(255, 103, 179, 77),
    [System.Drawing.Color]::FromArgb(255, 59, 127, 212),
    [System.Drawing.Color]::FromArgb(255, 75, 75, 212),
    [System.Drawing.Color]::FromArgb(255, 143, 75, 212)
)
$startX = 128 - (3 * 12.5)
for ($i = 0; $i -lt 7; $i++) {
    $b = New-Object System.Drawing.SolidBrush($sinColors[$i])
    $g.FillEllipse($b, ($startX + $i * 12.5 - 2.4), 220.6, 4.8, 4.8)
    $b.Dispose()
}

$g.Dispose()

# downscale
$small = New-Object System.Drawing.Bitmap(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$sg = [System.Drawing.Graphics]::FromImage($small)
$sg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$sg.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$sg.DrawImage($bmp, 0, 0, 256, 256)
$sg.Dispose()
$bmp.Dispose()

$dir = Split-Path -Parent $Out
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force $dir | Out-Null }
$small.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$small.Dispose()
Write-Output "saved: $Out"
