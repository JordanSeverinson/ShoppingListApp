# Downloads English Tesseract trained data into ./tessdata
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$dest = Join-Path $root "tessdata"
$url = "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata"
$outFile = Join-Path $dest "eng.traineddata"

New-Item -ItemType Directory -Force -Path $dest | Out-Null
Write-Host "Downloading eng.traineddata to $outFile ..."
Invoke-WebRequest -Uri $url -OutFile $outFile
Write-Host "Done."
