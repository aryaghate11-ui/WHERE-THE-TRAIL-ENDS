$folders = Get-ChildItem -Path "Assets" -Directory
foreach ($folder in $folders) {
    Write-Host "Processing Assets/$($folder.Name)"
    git add "Assets/$($folder.Name)"
    $status = git status --porcelain
    if ($status) {
        git commit -m "Add Assets/$($folder.Name)"
        git push origin main
    } else {
        Write-Host "Nothing to commit for Assets/$($folder.Name)"
    }
}

Write-Host "Processing remaining files"
git add .
$status = git status --porcelain
if ($status) {
    git commit -m "Add remaining files"
    git push origin main
}
Write-Host "Done!"
