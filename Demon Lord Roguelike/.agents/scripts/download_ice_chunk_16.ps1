$out = "e:\Unity\Project\DLR\DemonLordRoguelike\Demon Lord Roguelike\Assets\Out\ice_chunk"
$framesDir = "$out\frames16"
New-Item -ItemType Directory -Force -Path $framesDir | Out-Null

$userId   = "be829c7e-0d14-482e-baef-ce1c6fe308b0"
$objectId = "22a65582-ca43-405b-ae19-b4e105ee8e28"
$animId   = "50fc5aa8-90c2-4098-8c23-f22494f29510"
$base     = "https://backblaze.pixellab.ai/file/pixellab-characters/objects"

for ($i = 0; $i -le 15; $i++) {
    $url  = "$base/$userId/$objectId/animations/$animId/unknown/$i.png"
    $dest = "$framesDir\$i.png"
    Invoke-WebRequest $url -OutFile $dest
    Write-Host "Frame $i OK"
}

Write-Host "Download done."

# Compose 4x4 spritesheet
Add-Type -AssemblyName System.Drawing

$cols = 4
$rows = 4
$frameCount = 16

$frames = @()
for ($i = 0; $i -lt $frameCount; $i++) {
    $path = Join-Path $framesDir "$i.png"
    $frames += [System.Drawing.Image]::FromFile($path)
}

$fw = $frames[0].Width
$fh = $frames[0].Height
Write-Host "Frame size: ${fw}x${fh}"

$sheet = New-Object System.Drawing.Bitmap ($fw * $cols), ($fh * $rows)
$g = [System.Drawing.Graphics]::FromImage($sheet)

for ($i = 0; $i -lt $frameCount; $i++) {
    $x = ($i % $cols) * $fw
    $y = [math]::Floor($i / $cols) * $fh
    $g.DrawImage($frames[$i], $x, $y, $fw, $fh)
}

$g.Dispose()
$sheetPath = "$out\ice_chunk_fall_4x4.png"
$sheet.Save($sheetPath, [System.Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose()
foreach ($f in $frames) { $f.Dispose() }

Write-Host "Spritesheet saved: $sheetPath ($($fw*$cols)x$($fh*$rows)px, $frameCount frames)"
