[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$baseUrl = "https://codexpet.top/assets/previews/rem--l1/webp"
$states = @("idle","waving","waiting","running","running-right","running-left","jumping","review","failed")
$destDir = "C:\Users\Admin\Desktop\DanlangA_Bot\assets\rem--l1\previews"
New-Item -ItemType Directory -Force -Path $destDir | Out-Null

Write-Host "=== Downloading preview animations ==="
foreach ($state in $states) {
    $outPath = Join-Path $destDir "$state.webp"
    if (-not (Test-Path $outPath)) {
        try {
            Invoke-WebRequest -Uri "$baseUrl/$state.webp" -OutFile $outPath -UseBasicParsing
            $sz = (Get-Item $outPath).Length
            Write-Host "OK $state.webp: $sz bytes"
        } catch {
            Write-Host "FAILED $state"
        }
    } else {
        Write-Host "Already $state.webp"
    }
}
Write-Host "Done."
