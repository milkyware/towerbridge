[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]$Label,
    [Parameter(Mandatory = $true)]
    [string]$Repo,
    [Parameter(Mandatory = $true)]
    [string]$PRNumber,
    [Parameter()]
    [bool]$RemovalCondition = $false
)
begin
{
    $InformationPreference = 'Continue'
    if ($env:RUNNER_DEBUG)
    {
        $DebugPreference = 'Continue'
        $VerbosePreference = 'Continue'
    }
}
process
{
    # 1. Ensure the label exists in the repository
    Write-Debug "Checking if label '$Label' exists in repo '$Repo'"
    $repoLabels = gh label list --repo $Repo --json name --limit 100 | ConvertFrom-Json | Select-Object -ExpandProperty name

    if ($repoLabels -notcontains $Label)
    {
        Write-Information "Creating label '$Label' in repo '$Repo'"
        gh label create $Label --repo $Repo | Out-Null
    }

    # 2. Add Label
    if ($RemovalCondition)
    {
        Write-Information "Removing label '$Label' from PR #$PRNumber"
        gh pr edit $PRNumber --repo $Repo --remove-label $Label | Out-Null
        return
    }

    Write-Information "Adding label '$Label' to PR #$PRNumber"
    gh pr edit $PRNumber --repo $Repo --add-label $Label | Out-Null
}
