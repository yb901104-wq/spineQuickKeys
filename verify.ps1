$dllPath = "d:/AI AGENT/spineKuaijieanjian/KeyMacro/bin/Release/net9.0-windows/win-x64/KeyMacro.dll"
$bytes = [System.IO.File]::ReadAllBytes($dllPath)

# Search for UTF-8 and UTF-16 encoded strings
$targets = @("录制按键", "CommitGridEdit", "failedHotkeys", "_btnRecordKey", "添加步骤", "删除步骤")

foreach ($target in $targets) {
    $utf8 = [System.Text.Encoding]::UTF8.GetString($bytes)
    $unicode = [System.Text.Encoding]::Unicode.GetString($bytes)
    $foundUtf8 = $utf8 -match [regex]::Escape($target)
    $foundUnicode = $unicode -match [regex]::Escape($target)
    Write-Host "$target : UTF8=$foundUtf8 Unicode=$foundUnicode"
}

# Also check source file directly
Write-Host "---"
Write-Host "Source file contains 录制按键:"
$src = Get-Content "d:/AI AGENT/spineKuaijieanjian/KeyMacro/Forms/SequenceEditor.cs" -Raw
Write-Host ($src -match '录制按键')

# Check timestamp comparison
$srcTime = (Get-Item "d:/AI AGENT/spineKuaijieanjian/KeyMacro/Forms/SequenceEditor.cs").LastWriteTime
$dllTime = (Get-Item $dllPath).LastWriteTime
Write-Host "Source timestamp: $srcTime"
Write-Host "DLL timestamp: $dllTime"
