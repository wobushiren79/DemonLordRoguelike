<#
.SYNOPSIS
  Hook helper: PreToolUse on Write/Edit. Blocks direct edits to auto-generated
  *Bean.cs files that have a sibling *BeanPartial.cs (the canonical signal
  that the bean is auto-generated and extensions belong in the Partial).

.DESCRIPTION
  Reads PreToolUse stdin JSON, inspects tool_input.file_path.
  - If the target ends in BeanPartial.cs -> allow (these are hand-written).
  - If the target ends in Bean.cs AND a sibling <basename>Partial.cs exists in
    the same directory -> exit 2 with reason, telling the caller to edit the
    Partial file instead.
  - All other files (including hand-written *Bean.cs with no Partial sibling)
    pass through untouched.
#>
$stdin = [Console]::In.ReadToEnd()
if (-not $stdin) { exit 0 }

try {
    $obj = $stdin | ConvertFrom-Json
    $filePath = $obj.tool_input.file_path
    if (-not $filePath) { exit 0 }

    $normalized = $filePath -replace '\\', '/'

    # Only consider .cs files ending in 'Bean.cs' (which includes 'InfoBean.cs').
    # Files ending in 'Partial.cs' are explicitly allowed.
    if ($normalized -notmatch 'Bean\.cs$') { exit 0 }
    if ($normalized -match 'Partial\.cs$') { exit 0 }

    # Compute sibling Partial path: foo/BarBean.cs -> foo/BarBeanPartial.cs
    $partialPath = $normalized -replace 'Bean\.cs$', 'BeanPartial.cs'
    if (-not (Test-Path -LiteralPath $partialPath)) { exit 0 }

    $name = Split-Path $normalized -Leaf
    $partialName = Split-Path $partialPath -Leaf
    $reason = "BLOCKED: '$name' is auto-generated (sibling '$partialName' exists). " +
              "Per CLAUDE.md, *Bean.cs / *InfoBean.cs auto-generated files must NOT be edited directly. " +
              "Put extension methods, helper properties, or parsing logic in '$partialName' instead."
    [Console]::Error.WriteLine($reason)
    exit 2
} catch {
    # Parse errors: do not block.
}
exit 0
