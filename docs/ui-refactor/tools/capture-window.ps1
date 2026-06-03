param(
    [string]$TitlePattern,
    [string]$Output,
    [switch]$ActiveWindow
)

if ([string]::IsNullOrWhiteSpace($Output)) {
    throw "Output is required."
}

Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class CaptureWindowNative {
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
"@

function Get-WindowTitle([IntPtr]$Handle) {
    $builder = New-Object System.Text.StringBuilder 512
    [void][CaptureWindowNative]::GetWindowText($Handle, $builder, $builder.Capacity)
    return $builder.ToString()
}

function Find-WindowByTitle([string]$Pattern) {
    $script:foundHandle = [IntPtr]::Zero
    $callback = [CaptureWindowNative+EnumWindowsProc]{
        param([IntPtr]$hWnd, [IntPtr]$lParam)
        if (-not [CaptureWindowNative]::IsWindowVisible($hWnd)) {
            return $true
        }

        $title = Get-WindowTitle $hWnd
        if ($title -and $title -like "*$Pattern*") {
            $script:foundHandle = $hWnd
            return $false
        }

        return $true
    }

    [void][CaptureWindowNative]::EnumWindows($callback, [IntPtr]::Zero)
    return $script:foundHandle
}

if ($ActiveWindow) {
    $handle = [CaptureWindowNative]::GetForegroundWindow()
} else {
    if ([string]::IsNullOrWhiteSpace($TitlePattern)) {
        throw "TitlePattern is required unless ActiveWindow is specified."
    }
    $handle = Find-WindowByTitle $TitlePattern
}

if ($handle -eq [IntPtr]::Zero) {
    throw "Target window was not found."
}

$rect = New-Object CaptureWindowNative+RECT
if (-not [CaptureWindowNative]::GetWindowRect($handle, [ref]$rect)) {
    throw "Failed to read target window bounds."
}

$width = $rect.Right - $rect.Left
$height = $rect.Bottom - $rect.Top
if ($width -le 0 -or $height -le 0) {
    throw "Target window has invalid bounds."
}

$outputPath = [System.IO.Path]::GetFullPath($Output)
$outputDir = [System.IO.Path]::GetDirectoryName($outputPath)
if (-not [string]::IsNullOrWhiteSpace($outputDir)) {
    [System.IO.Directory]::CreateDirectory($outputDir) | Out-Null
}

$bitmap = New-Object System.Drawing.Bitmap($width, $height)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
try {
    $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
    $bitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output $outputPath
} finally {
    $graphics.Dispose()
    $bitmap.Dispose()
}
