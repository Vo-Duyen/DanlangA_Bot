Add-Type -AssemblyName 'System.Drawing'
$path = 'C:\Users\Admin\Desktop\DanlangA_Bot\assets\rem--l1\spritesheet.webp'
$img = [System.Drawing.Image]::FromFile($path)
Write-Host "Width: $($img.Width) Height: $($img.Height) PixelFormat: $($img.PixelFormat)"
$img.Dispose()
