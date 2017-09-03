$ghPagesCheckoutFolder = '../../../Composable-gh-pages'

if(!(Test-Path $ghPagesCheckoutFolder))
{
    Push-Location '../../../'
    Write-Host "Missing gh-pages checkout"
    git clone --single-branch --branch gh-pages git@github.com:mlidbom/Composable.git Composable-gh-pages
    Pop-Location
    return
}

$targetFolder = (Get-Item $ghPagesCheckoutFolder).FullName
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