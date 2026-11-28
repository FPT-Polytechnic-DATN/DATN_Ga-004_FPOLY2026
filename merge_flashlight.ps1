$ErrorActionPreference = 'Stop'

$dstPath = "Assets\Asset\Scenes\Map1.unity"
$srcPath = "Map1_KhanhG2_36df0b9.unity"

if (!(Test-Path $dstPath)) { throw "Khong tim thay Map1.unity" }
if (!(Test-Path $srcPath)) { throw "Khong tim thay file Map1_KhanhG2_36df0b9.unity" }

$dst = [System.IO.File]::ReadAllText((Resolve-Path $dstPath))
$src = [System.IO.File]::ReadAllText((Resolve-Path $srcPath))

if ($dst.Contains("12326640") -or $dst.Contains("d86ca2f4159e6a942abd8dfc52b38395")) {
    throw "Map1 hien tai da co Flashlight, dung de tranh chen trung."
}

$pattern = '(?ms)^--- !u!1001 &12326640\r?\n.*?(?=^--- !u!114 &43901096(?:\r?\n|$))'
$match = [regex]::Match($src, $pattern)

if (!$match.Success) { throw "Khong tach duoc du lieu Flashlight tu file KhanhG2." }

$block = $match.Value

if (!$block.Contains("12326641") -or !$block.Contains("12326642") -or !$block.Contains("d86ca2f4159e6a942abd8dfc52b38395")) {
    throw "Du lieu Flashlight khong day du."
}

$sceneRoots = '--- !u!1660057539 &9223372036854775807'
if (!$dst.Contains($sceneRoots)) { throw "Khong tim thay SceneRoots trong Map1 hien tai." }

$nl = "`r`n"
if ($dst.Contains("`n") -and !$dst.Contains("`r`n")) { $nl = "`n" }

$rootPattern = '(?m)^  m_Roots:\r?\n'
if ([regex]::Matches($dst, $rootPattern).Count -ne 1) {
    throw "So luong m_Roots khong dung bang 1."
}

$dst = [regex]::Replace(
    $dst,
    $rootPattern,
    [System.Text.RegularExpressions.MatchEvaluator]{
        param($m)
        $m.Value + "  - {fileID: 12326640}" + $nl
    },
    1
)

$dst = $dst.Replace($sceneRoots, $block + $nl + $sceneRoots)

[System.IO.File]::WriteAllText(
    (Resolve-Path $dstPath).Path,
    $dst,
    [System.Text.UTF8Encoding]::new($false)
)

Write-Host "DA CHEN FLASHLIGHT THANH CONG." -ForegroundColor Green