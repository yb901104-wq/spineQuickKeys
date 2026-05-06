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

function Get-UTF16LEBytes($str) {
    return [System.Text.Encoding]::Unicode.GetBytes($str)
}

function Get-UTF8Bytes($str) {
    return [System.Text.Encoding]::UTF8.GetBytes($str)
}

$targets = @(
    [PSCustomObject]@{Name="luzhianjian"; Str=[char]0x5F55+[char]0x5236+[char]0x6309+[char]0x952E},
    [PSCustomObject]@{Name="tianjiabuzhou"; Str=[char]0x6DFB+[char]0x52A0+[char]0x6B65+[char]0x9AA4},
    [PSCustomObject]@{Name="shanchubuzhou"; Str=[char]0x5220+[char]0x9664+[char]0x6B65+[char]0x9AA4},
    [PSCustomObject]@{Name="bianjixulie"; Str=[char]0x7F16+[char]0x8F91+[char]0x5E8F+[char]0x5217}
)

Write-Host "=== UTF-16LE Search ==="
foreach ($t in $targets) {
    $b = Get-UTF16LEBytes $t.Str
    $hex = ($b | ForEach-Object { $_.ToString("X2") }) -join " "
    $pos = Find-Bytes $bytes $b
    Write-Host "$($t.Name) : pos=$pos  bytes=$hex"
}

Write-Host ""
Write-Host "=== Also check for common English ==="
$eng = @("KeyMacro", "SequenceEditor", "MacroSequence", "MacroStep", "_btnRecordKey", "CommitGridEdit", "_failedHotkeys", "InitializeComponent", "MakeStepButton", "LoadSequence", "SaveStepsFromGrid")
foreach ($s in $eng) {
    $b = Get-UTF8Bytes $s
    $pos = Find-Bytes $bytes $b
    Write-Host "$s : pos=$pos"
}
