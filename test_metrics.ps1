$exe = "C:\Users\Admin\Desktop\DanlangA_Bot\publish\DanlangA_Bot.exe"
Write-Host "Starting $exe..."
$proc = Start-Process -FilePath $exe -PassThru
Start-Sleep -Seconds 2

$info = Get-Process -Id $proc.Id
$ramMb = [Math]::Round($info.WorkingSet64 / 1MB, 2)
$cpu = $info.TotalProcessorTime.TotalMilliseconds

Write-Host "=== Process Verification ==="
Write-Host "Process ID: $($proc.Id)"
Write-Host "RAM WorkingSet: $ramMb MB"
Write-Host "Responding: $($info.Responding)"
Write-Host "CPU Total Time: $cpu ms"

# Test IPC Named Pipe
Write-Host "`n=== Testing IPC Named Pipe ==="
try {
    $pipe = New-Object System.IO.Pipes.NamedPipeClientStream(".", "pet_assistant_ipc", [System.IO.Pipes.PipeDirection]::InOut)
    $pipe.Connect(2000)
    $writer = New-Object System.IO.StreamWriter($pipe)
    $writer.AutoFlush = $true
    $reader = New-Object System.IO.StreamReader($pipe)

    $msg = '{"version":"1.0","event":"notify","payload":{"text":"Xin chào! Test IPC thành công.","mood":"happy","duration_ms":3000}}'
    $writer.WriteLine($msg)
    $resp = $reader.ReadLine()
    Write-Host "IPC Response: $resp"
    $pipe.Close()
} catch {
    Write-Host "IPC Test Failed: $_"
}

# Test Registry Autostart
Write-Host "`n=== Testing Registry Autostart ==="
$regVal = Get-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "DanlangDesktopPet" -ErrorAction SilentlyContinue
Write-Host "Registry Run Value: $($regVal.DanlangDesktopPet)"

# Clean up process
Write-Host "`nStopping process..."
Stop-Process -Id $proc.Id -Force
Write-Host "Done!"
