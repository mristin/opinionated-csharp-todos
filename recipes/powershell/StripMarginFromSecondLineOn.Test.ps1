<#
.SYNOPSIS
This script tests StripMargin function in-lined from RenderTaskList.ps1.

.DESCRIPTION
This is a simple development environment for StripMargin used in
RenderTaskList.ps1. We did not want to set up the whole test framework such
as Pester since there is too little to test for.

In the future, we might consider introducing Pester if the number of functions
grow.
#>

$nl = [Environment]::NewLine

function StripMarginFromSecondLineOn([string]$Text)
{
    $lines = $Text.Split(@("`r`n", "`r", "`n"), [StringSplitOptions]::None)
    if ($lines.Count -lt 2)
    {
        return $Text
    }

    if($lines.Count -eq 2)
    {
        $lines[1] = $lines[1].TrimStart()
        return ($lines -Join $nl)
    }

    $minLength = $null
    for($i = 1; $i -lt $lines.Length; $i++)
    {
        # Empty lines are ignored since IDEs sometimes strip them away and
        # do not indent them with whitespace.
        if ($lines[$i].Length -eq 0)
        {
            continue
        }

        if (($null -eq $minLength) -or ($lines[$i].Length -lt $minLength))
        {
            $minLength = $lines[$i].Length
        }
    }

    if($null -eq $minLength)
    {
        # All lines from the second on were empty.
        return $Text
    }

    $commonMarginLength = 0
    $stop = $false
    for($cursor = 0; !$stop -and ($cursor -lt $minLength); $cursor++)
    {
        $charAtCursor = $null

        for($i = 1; !$stop -and ($i -lt $lines.Length); $i++)
        {
            # Skip empty lines, see the comment above
            if ($lines[$i].Length -eq 0)
            {
                continue
            }

            if ($null -eq $charAtCursor)
            {
                $charAtCursor = $lines[$i][$cursor]
                if (($charAtCursor -ne " ") -and ($charAtCursor -ne "`t"))
                {
                    $commonMarginLength = $cursor
                    $stop = $true
                }
            }
            else
            {
                if ($lines[$i][$cursor] -ne $charAtCursor)
                {
                    $commonMarginLength = $cursor
                    $stop = $true
                }
            }
        }
    }

    for($i = 1; $i -lt $lines.Length; $i++)
    {
        # Skip empty lines, see the comment above
        if ($lines[$i].Length -eq 0)
        {
            continue
        }
        $lines[$i] = $lines[$i].Substring($commonMarginLength)
    }

    $result = $lines -Join $nl

    return $result
}

function Test
{
    $cases = @(
    [System.Tuple]::Create(
            "",
            "",
            "Empty"),

    [System.Tuple]::Create(
            "This is a one-liner.",
            "This is a one-liner.",
            "One-liner"),

    [System.Tuple]::Create(
            "AAA${nl}BBB",
            "AAA${nl}BBB",
            "Two-liner without margin"),

    [System.Tuple]::Create(
            "AAA${nl}   BBB",
            "AAA${nl}BBB",
            "Two-liner with margin"),

    [System.Tuple]::Create(
            "AAA${nl}",
            "AAA${nl}",
            "Two-liner with empty second line"),

    [System.Tuple]::Create(
            "AAA${nl}BBB${nl}CCC",
            "AAA${nl}BBB${nl}CCC",
            "Three-liner without margin"),

    [System.Tuple]::Create(
            "AAA${nl}  BBB${nl}  CCC",
            "AAA${nl}BBB${nl}CCC",
            "Three-liner with identical margins"),

    [System.Tuple]::Create(
            "AAA${nl}    BBB${nl}  CCC",
            "AAA${nl}  BBB${nl}CCC",
            "Three-liner with decreasing margins"),

    [System.Tuple]::Create(
            "AAA${nl}  BBB${nl}    CCC",
            "AAA${nl}BBB${nl}  CCC",
            "Three-liner with increasing margins"),

    [System.Tuple]::Create(
            "AAA${nl}${nl}CCC",
            "AAA${nl}${nl}CCC",
            "Three-liner with empty middle line and no margin"),

    [System.Tuple]::Create(
            "AAA${nl}${nl}  CCC",
            "AAA${nl}${nl}CCC",
            "Three-liner with empty middle line and margin in the last line"),

    [System.Tuple]::Create(
            "AAA${nl}  BBB${nl}",
            "AAA${nl}BBB${nl}",
            "Three-liner with empty last line and margin in the middle"),

    [System.Tuple]::Create(
            "AAA${nl}  BBB${nl}    CCC${nl}  DDD",
            "AAA${nl}BBB${nl}  CCC${nl}DDD",
            "Four-liner with increasing and decreasing margins"),

    [System.Tuple]::Create(
            "AAA${nl}${nl}    CCC${nl}  DDD",
            "AAA${nl}${nl}  CCC${nl}DDD",
            "Four-liner with variable margins and empty second line")
    )

    foreach ($cs in $cases)
    {
        $text = $cs.Item1
        $expected = $cs.Item2
        $label = $cs.Item3

        $got = StripMarginFromSecondLineOn($text)
        if ($expected -ne $got)
        {
            throw ("${label}: " +
                    "expected $( $expected|ConvertTo-Json ), " +
                    "got: $( $got|ConvertTo-Json )")
        }
        else
        {
            Write-Host "${label}: OK"
        }
    }
}

Test