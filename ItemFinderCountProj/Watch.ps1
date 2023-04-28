Function Register-Watcher {
    param ($folder)
    Write-Host $folder
    $filter = "*.cs"
    $watcher = New-Object IO.FileSystemWatcher $folder, $filter -Property @{ 
        IncludeSubdirectories = $true
        EnableRaisingEvents = $true
    }

    $changeAction = [scriptblock]::Create('    
    # This is the code which will be executed every time a file change is detected
    $path = $Event.SourceEventArgs.FullPath
    $name = $Event.SourceEventArgs.Name
    $changeType = $Event.SourceEventArgs.ChangeType
    $timeStamp = $Event.TimeGenerated
    Write-Host "The file $name was $changeType at $timeStamp"
    ' + "& `"$PSScriptRoot\Build.ps1`"")

    Register-ObjectEvent $Watcher -EventName "Changed" -Action $changeAction
}

Register-Watcher "$PSScriptRoot"