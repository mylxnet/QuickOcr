# QuickOcr v1.0.1 一键发布脚本
# 生成 dist\QuickOcr_Setup.exe（单文件安装器，目标机无需安装 .NET）
# 用法：powershell -ExecutionPolicy Bypass -File build.ps1

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$main = Join-Path $root 'src\QuickOcr'
$sfx = Join-Path $root 'src\QuickOcr.Sfx'
$publish = Join-Path $main 'publish'
$dist = Join-Path $root 'dist'

Write-Host '1/3 发布主程序（self-contained）...'
dotnet publish $main -c Release -r win-x64 --self-contained true -o $publish

Write-Host '2/3 打包 publish.zip（供安装器嵌入）...'
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($publish, (Join-Path $sfx 'publish.zip'))

Write-Host '3/3 发布安装器（单文件 self-contained SFX）...'
Remove-Item -Recurse -Force $dist -ErrorAction SilentlyContinue
dotnet publish $sfx -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $dist

Write-Host ""
Write-Host "完成：$dist\QuickOcr_Setup.exe"
