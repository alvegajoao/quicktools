param(
    [string]$Repository = "https://github.com/alvegajoao/quicktools.git",
    [string]$Branch = "main",
    [string]$Message = "Initial QuickTools project"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Error "Git is not installed or is not available in PATH. Install Git for Windows first: https://git-scm.com/download/win"
}

dotnet build QuickTools.csproj --configuration Release

if (-not (Test-Path ".git")) {
    git init
    git branch -M $Branch
}

git remote remove origin 2>$null
git remote add origin $Repository
git add .
git commit -m $Message
git push -u origin $Branch
