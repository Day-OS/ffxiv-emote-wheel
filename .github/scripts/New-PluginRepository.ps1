param(
    [Parameter(Mandatory)] [string] $ManifestPath,
    [Parameter(Mandatory)] [string] $Repository,
    [Parameter(Mandatory)] [string] $Tag,
    [string] $OutputPath = 'repo.json'
)

$ErrorActionPreference = 'Stop'
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
if ($manifest.InternalName -ne 'EmoteWheel' -or !$manifest.AssemblyVersion) {
    throw 'The Emote Wheel package manifest is invalid.'
}

$zip = "https://github.com/$Repository/releases/download/$Tag/latest.zip"
foreach ($field in @('DownloadLinkInstall', 'DownloadLinkUpdate', 'DownloadLinkTesting')) {
    $manifest | Add-Member -NotePropertyName $field -NotePropertyValue $zip -Force
}
$manifest | Add-Member -NotePropertyName LastUpdate -NotePropertyValue ([DateTimeOffset]::UtcNow.ToUnixTimeSeconds()) -Force

$json = ConvertTo-Json -InputObject @($manifest) -Depth 8
$path = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
[System.IO.File]::WriteAllText($path, $json + "`n")
