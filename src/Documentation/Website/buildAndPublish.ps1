Push-Location $PSScriptRoot #knowing which folder we are in is good :)

$buildFolder = "$PSScriptRoot\_site"
$ghPagesCheckoutFolder = "$PSScriptRoot/../../../../Composable-gh-pages"

if(!(Test-Path $ghPagesCheckoutFolder))
{
    Write-Host "Missing gh-pages checkout. Cloning"
    Push-Location "$ghPagesCheckoutFolder/.."
    git clone --quiet --single-branch --branch gh-pages 'https://github.com/mlidbom/Composable.git' Composable-gh-pages
    Pop-Location
}


$ghPagesCheckoutFolder = (Get-Item $ghPagesCheckoutFolder).FullName
Push-Location $ghPagesCheckoutFolder
git config core.autocrlf true
git config --global core.safecrlf false

git checkout -f 
git clean -fd
git pull

Pop-Location

pwd
.\build.bat

robocopy.exe /MIR "$buildFolder" "$ghPagesCheckoutFolder" /XD ".git" "apidocs" ".asciidoctor" /XF ".gitignore" "CNAME"

Push-Location $ghPagesCheckoutFolder

git add .
git commit --quiet -a -m "Automatic commit created by automated build and publish script."
git push --quiet

Pop-Location
Pop-Location