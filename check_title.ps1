$dllPath = "d:/AI AGENT/spineKuaijieanjian/KeyMacro/bin/Release/net9.0-windows/KeyMacro.dll"
$bytes = [System.IO.File]::ReadAllBytes($dllPath)

function Find-Bytes($haystack, $needle) {
    for ($i = 0; $i -le $haystack.Length - $needle.Length; $i++) {
        $match = $true
        for ($j = 0; $j -lt $needle.Length; $j++) {
            if ($haystack[$i + $j] -ne $needle[$j]) { $match = $false; break }
        }
        if ($match) { return $i }
    }
    return -1
}

# Search for the new title in UTF-16LE
$newTitle = [char]0x5FEB + [char]0x6377 + [char]0x952E + [char]0x52A9 + [char]0x624B + ' v2.0 | 2026-05-02 00:20'
$needle = [System.Text.Encoding]::Unicode.GetBytes($newTitle)
$pos = Find-Bytes $bytes $needle
Write-Host "New title search: pos=$pos"

# Also search for just 'v2.0' in UTF-8
$v2 = [System.Text.Encoding]::UTF8.GetBytes('v2.0')
$pos2 = Find-Bytes $bytes $v2
Write-Host "v2.0 (UTF8): pos=$pos2"

# Search for the old title in UTF-16LE
$oldTitle = [char]0x5FEB + [char]0x6377 + [char]0x952E + [char]0x52A9 + [char]0x624B
$oldNeedle = [System.Text.Encoding]::Unicode.GetBytes($oldTitle)
$pos3 = Find-Bytes $bytes $oldNeedle
Write-Host "Old title (just chars): pos=$pos3"
Write-Host "DLL timestamp: $((Get-Item $dllPath).LastWriteTime)"
