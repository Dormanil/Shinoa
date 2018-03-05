$cur_date = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

Get-ChildItem -File Databases | 
ForEach-Object {Write-Output $_.name.Substring(0, $_.name.IndexOf(".cs"))} | 
Where-Object {($_ -ne "IDatabaseContext") -and ($_ -like "*Context")} |
ForEach-Object {
dotnet ef migrations add -v -c "$_" "$($_.Substring(0, $_.IndexOf("Context")))_$cur_date"
dotnet ef database update -v -c "$_"
}