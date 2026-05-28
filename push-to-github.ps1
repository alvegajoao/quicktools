# QuickTools — push inicial para GitHub
# Corre este script na pasta do projeto: .\push-to-github.ps1

Set-Location $PSScriptRoot

# Reinicializa (ou usa o repo existente) e garante que a branch se chama main
git init
git branch -M main

# Configura identidade local
git config user.name  "João Alvega"
git config user.email "joao.alvega@heraprime.com"

# Adiciona remote (ignora erro se já existir)
git remote remove origin 2>$null
git remote add origin https://github.com/alvegajoao/quicktools.git

# Stage de tudo e commit inicial
git add .
git commit -m "feat: initial commit — QuickTools WPF app"

# Push
# O Git vai pedir credenciais GitHub na primeira vez (usa o Windows Credential Manager)
git push -u origin main

Write-Host ""
Write-Host "Feito! Projeto disponível em https://github.com/alvegajoao/quicktools" -ForegroundColor Green
