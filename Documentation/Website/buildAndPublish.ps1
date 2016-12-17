$ghPagesCheckoutFolder = '../../../Composable-gh-pages'

if(!(Test-Path $ghPagesCheckoutFolder))
{
    Write-Host "Missing gh-pages checkout"
    return
}

$targetFolder = (Get-Item ).FullName
Push-Location $targetFolder
git checkout -f 
git clean -fd
git pull

Pop-Location

bundle.bat exec jekyll build --destination $targetFolder

Push-Location $targetFolder

git add .
git commit -a -m "Automatic commit created by automated build and publish script."
git push

Pop-Location