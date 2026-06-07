# Install the latest contexttax binary on Windows (x64).
$ErrorActionPreference = 'Stop'
$repo = 'PavelTkachenk0/ContextTax'
$dir  = Join-Path $env:LOCALAPPDATA 'Programs\contexttax'
$exe  = Join-Path $dir 'contexttax.exe'
$url  = "https://github.com/$repo/releases/latest/download/contexttax-win-x64.exe"

New-Item -ItemType Directory -Force -Path $dir | Out-Null
Write-Host "Downloading contexttax-win-x64.exe …"
Invoke-WebRequest -Uri $url -OutFile $exe

$userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
if ($userPath -notlike "*$dir*") {
    [Environment]::SetEnvironmentVariable('Path', "$userPath;$dir", 'User')
    Write-Host "Added $dir to your PATH — restart the shell."
}
& $exe --version
Write-Host "try:  Get-Clipboard | contexttax response -e"
