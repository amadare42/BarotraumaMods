$Exclude = @('.idea','obj','bin','.ps1','ItemFinderCount.csproj','filelist.xml')
$destination = "$PSScriptRoot\..\ItemFinderCount"
$notThese = ($Exclude | ForEach-Object { [Regex]::Escape($_) }) -join '|'

Get-ChildItem -Path $PSScriptRoot -Recurse -File | 
    Where-Object{ $_.FullName -notmatch $notThese } | 
    ForEach-Object {
    $target = Join-Path -Path $destination -ChildPath $_.DirectoryName.Substring($PSScriptRoot.Length)
    if (!(Test-Path -Path $target -PathType Container)) {
        New-Item -Path $target -ItemType Directory | Out-Null
    }
    $_ | Copy-Item -Destination $target -Force
}
Copy-Item -Path "$PSScriptRoot\_filelist.xml" -Destination "$destination\filelist.xml" -Force