<#
.SYNOPSIS
  Hook helper: reads PostToolUse stdin JSON, checks if the changed file is a C# file
  under Assets/, and outputs a systemMessage reminder to run check-watched.ps1.
#>
$stdin = [Console]::In.ReadToEnd()
if (-not $stdin) { exit 0 }

try {
    $obj = $stdin | ConvertFrom-Json
    $filePath = $obj.tool_input.file_path
    if (-not $filePath) { $filePath = $obj.tool_response.filePath }
    if (-not $filePath) { exit 0 }

    $normalized = $filePath -replace '\\', '/'
    if ($normalized -match '^Assets/.*\.cs$') {
        $msg = '{"systemMessage":"C# file changed. Run .agents/check-watched.ps1 to check which agents/skills need updating."}'
        Write-Output $msg
    }
} catch {
    # Silently ignore parse errors
}
exit 0
