$out = "e:\Unity\Project\DLR\DemonLordRoguelike\Demon Lord Roguelike\Assets\Out\ice_chunk"
$framesDir = "$out\frames"
New-Item -ItemType Directory -Force -Path $framesDir | Out-Null

$userId   = "be829c7e-0d14-482e-baef-ce1c6fe308b0"
$objectId = "22a65582-ca43-405b-ae19-b4e105ee8e28"
$animId   = "b48250b9-a3e6-44be-9450-c25a0bfd5e64"
$base     = "https://backblaze.pixellab.ai/file/pixellab-characters/objects"

Invoke-WebRequest "$base/$userId/$objectId/rotations/unknown.png" -OutFile "$out\ice_chunk.png"
Write-Host "Static image OK"

for ($i = 0; $i -le 4; $i++) {
    $url  = "$base/$userId/$objectId/animations/$animId/unknown/$i.png"
    $dest = "$framesDir\frame_0$i.png"
    Invoke-WebRequest $url -OutFile $dest
    Write-Host "Frame $i OK"
}

Write-Host "Done."
