$dllPath = "d:/AI AGENT/spineKuaijieanjian/KeyMacro/bin/Release/net9.0-windows/KeyMacro.dll"
$bytes = [System.IO.File]::ReadAllBytes($dllPath)
Write-Host "DLL: $dllPath"
Write-Host "Size: $($bytes.Length) bytes"

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

function Search-UTF16($haystack, $str) {
    $needle = [System.Text.Encoding]::Unicode.GetBytes($str)
    return Find-Bytes $haystack $needle
}

# Key feature checks
Write-Host "`n=== Feature Verification ==="
Write-Host "录制按键 (step record button): $( if((Search-UTF16 $bytes '录制按键') -ge 0){'FOUND'}else{'MISSING'} )"
Write-Host "添加步骤 (add step button): $( if((Search-UTF16 $bytes '添加步骤') -ge 0){'FOUND'}else{'MISSING'} )"
Write-Host "删除步骤 (del step button): $( if((Search-UTF16 $bytes '删除步骤') -ge 0){'FOUND'}else{'MISSING'} )"
Write-Host "暂停全部 (pause button): $( if((Search-UTF16 $bytes '暂停全部') -ge 0){'FOUND'}else{'MISSING'} )"
Write-Host "CommitGridEdit: $( if((Search-UTF16 $bytes 'CommitGridEdit') -ge 0){'FOUND'}else{'MISSING'} )"
Write-Host "_btnRecordKey: $( if((Search-UTF16 $bytes '_btnRecordKey') -ge 0){'FOUND'}else{'MISSING'} )"
Write-Host "_failedHotkeys: $( if((Search-UTF16 $bytes '_failedHotkeys') -ge 0){'FOUND'}else{'MISSING'} )"

# Button sizes - search for the size values in ASCII
Write-Host "`n=== Button Sizes Check ==="
$sizes = @('100', '95', '90', '80', '65')
foreach ($sz in $sizes) {
    $utf8needle = [System.Text.Encoding]::UTF8.GetBytes($sz + ',')
    $pos = Find-Bytes $bytes $utf8needle
    Write-Host "Size $sz`: $( if($pos -ge 0){'FOUND at '+$pos}else{'not found'} )"
}
