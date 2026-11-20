<#
.SYNOPSIS
  通用 Python 启动包装脚本（动态定位 python，禁止写死绝对路径）。

.DESCRIPTION
  本项目里 python.exe 未必在 PATH 上（典型地只装在用户目录
  %LOCALAPPDATA%\Programs\Python\PythonXY\python.exe）。直接用写死的绝对路径
  调用 python 既违反 CLAUDE.md「路径动态化规则」，也无法被一条可复用的 allow
  规则稳定命中，导致每次执行都弹权限确认。

  本脚本按以下优先级动态查找可用的 python，并把全部参数原样转发给它：
    1. PATH 上的 python / python3
    2. py launcher（py -3）
    3. %LOCALAPPDATA%\Programs\Python\*\python.exe（取版本号最高的）
    4. C:\Program Files\Python*\python.exe
  在 PowerShell 7 中用 & 直接调用本脚本，可通过 -c 传入多行源码。
  通过 powershell.exe -File 启动且从管道读取 Python 源码时，省略末尾的单个减号。

.PARAMETER WhichOnly
  仅解析并打印将要使用的 python 可执行文件路径，不实际执行（用于自检）。

.EXAMPLE
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" ".claude/scripts/excel_read.py" --path foo.xlsx

.EXAMPLE
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" -WhichOnly

.EXAMPLE
  & './.claude/scripts/run-python.ps1' -c "print('ok')"
#>

function Resolve-Python {
    # 1. PATH 上的 python / python3（用 Get-Command 而非 where，避开 WindowsApps 0 字节占位 stub）
    foreach ($name in @("python", "python3")) {
        $cmd = Get-Command $name -ErrorAction SilentlyContinue |
            Where-Object { $_.Source -and (Test-Path $_.Source) -and ((Get-Item $_.Source).Length -gt 0) } |
            Select-Object -First 1
        if ($cmd) { return @{ Exe = $cmd.Source; Prefix = @() } }
    }

    # 2. py launcher
    $py = Get-Command "py" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($py) { return @{ Exe = $py.Source; Prefix = @("-3") } }

    # 3. %LOCALAPPDATA%\Programs\Python\*\python.exe（取版本最高）
    $userRoot = Join-Path $env:LOCALAPPDATA "Programs\Python"
    if (Test-Path $userRoot) {
        $exe = Get-ChildItem -Path $userRoot -Filter "python.exe" -Recurse -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending | Select-Object -First 1
        if ($exe) { return @{ Exe = $exe.FullName; Prefix = @() } }
    }

    # 4. C:\Program Files\Python*\python.exe（取版本最高）
    $exe = Get-ChildItem -Path "$env:ProgramFiles\Python*" -Filter "python.exe" -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending | Select-Object -First 1
    if ($exe) { return @{ Exe = $exe.FullName; Prefix = @() } }

    return $null
}

function Invoke-ProjectPython {
    [CmdletBinding()]
    param(
        [switch]$WhichOnly,
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$PyArgs
    )

    $env:PYTHONUTF8 = "1"
    $env:PYTHONIOENCODING = "utf-8"

    $resolved = Resolve-Python
    if (-not $resolved) {
        [Console]::Error.WriteLine("run-python.ps1: 未找到可用的 Python 解释器（已查找 PATH / py launcher / %LOCALAPPDATA%\Programs\Python / Program Files）。")
        exit 127
    }

    if ($WhichOnly) {
        Write-Output $resolved.Exe
        exit 0
    }

    & $resolved.Exe @($resolved.Prefix + $PyArgs)
    exit $LASTEXITCODE
}

# Windows PowerShell 5.1 的 -File 收到孤立的减号时，普通脚本入口能正常报错；高级参数绑定必须留在函数内。
Invoke-ProjectPython @args
