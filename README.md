# QuickTools

![Build](https://github.com/alvegajoao/quicktools/actions/workflows/build.yml/badge.svg)

**QuickTools** é uma aplicação desktop para Windows, desenvolvida em **C# / .NET 8 com WPF**, que reúne várias ferramentas rápidas de produtividade, automação e gestão do sistema numa interface simples, moderna e inspirada no Windows 11.

A aplicação foi criada para facilitar pequenas tarefas do dia a dia, como automatizar cliques, executar ações rápidas do sistema, agendar ações de energia e alternar entre modos de energia do Windows.

---

## ✨ Funcionalidades

### 🖱️ Auto Clicker

O QuickTools inclui um sistema de **Auto Clicker** para automatizar cliques do rato.

Funcionalidades principais:

* Iniciar e parar o auto clicker manualmente
* Atalho global para ligar/desligar rapidamente
* Clique automático com controlo visível
* Utilização da API nativa do Windows para simular inputs
* Cursor visível enquanto o auto clicker está ativo
* Execução em segundo plano com suporte para cancelamento

Por defeito, o atalho global do Auto Clicker é:

```txt
F6
```

---

### ⚡ Quick Toggle

O **Quick Toggle** é uma roda de ações rápidas que aparece junto ao cursor do rato, permitindo executar funções úteis do sistema de forma imediata.

Exemplos de ações:

* Silenciar/ativar som
* Controlar volume
* Bloquear o PC
* Tirar screenshot
* Abrir definições do Windows
* Executar ações rápidas configuradas

Por defeito, o atalho do Quick Toggle é:

```txt
F7
```

A roda pode suportar até 8 ações fixadas.

---

### ⏱️ Power Scheduler

O **Power Scheduler** permite agendar ações de energia no Windows.

Ações suportadas:

* Desligar
* Reiniciar
* Suspender
* Hibernar

Funcionalidades:

* Agendamento por data e hora
* Suporte para múltiplos eventos
* Pausar ou retomar eventos individualmente
* Remover eventos agendados
* Confirmação antes de ações críticas como desligar ou reiniciar

Esta funcionalidade é útil para desligar o PC automaticamente após downloads, renderizações, tarefas longas ou períodos de inatividade.

---

### 🔋 Power Modes

O QuickTools permite visualizar e alterar os planos de energia do Windows através do `powercfg`.

Funcionalidades:

* Listar planos de energia disponíveis
* Ver qual o plano atualmente ativo
* Alternar rapidamente entre modos de energia
* Suporte para planos como Equilibrado, Alto Desempenho e outros planos disponíveis no sistema
* Deteção de planos indisponíveis, como Ultimate Performance, quando o Windows não os expõe

---

### ⚙️ Settings

A área de definições permite configurar o comportamento da aplicação.

Inclui opções como:

* Tema visual
* Arranque com o Windows
* Configuração de atalhos
* Importação/exportação de definições em JSON
* Preferências locais da aplicação

As definições são guardadas localmente em:

```txt
%AppData%\QuickTools\settings.json
```

---

## 🖥️ Interface

A aplicação tem uma interface simples e moderna, com foco em rapidez e facilidade de utilização.

Secções principais:

* Dashboard
* Auto Clicker
* Quick Toggle
* Power Scheduler
* Power Modes
* Settings

---

## 📦 Download

A versão mais recente da aplicação é gerada automaticamente através do GitHub Actions.

Podes fazer download aqui:

```txt
https://github.com/alvegajoao/quicktools/releases/tag/latest
```

Depois:

1. Faz download do ficheiro `QuickTools-win-x64.zip`
2. Extrai o ZIP
3. Executa `QuickTools.exe`

---

## 🚀 Build automática

Sempre que é feito push para a branch `main`, o GitHub Actions compila automaticamente a aplicação.

O processo faz:

1. Checkout do código
2. Instalação/configuração do .NET
3. Restore das dependências
4. Build da solução
5. Publish da aplicação para Windows x64
6. Criação do ficheiro ZIP
7. Publicação como artifact/release

---

## 🛠️ Executar localmente

### Requisitos

Para desenvolvimento:

* Windows
* .NET 8 SDK
* Visual Studio ou outro editor compatível com projetos WPF

### Restaurar dependências

```powershell
dotnet restore
```

### Executar a aplicação

```powershell
dotnet run
```

### Compilar

```powershell
dotnet build QuickTools.sln
```

### Compilar em Release

```powershell
dotnet build QuickTools.sln --configuration Release
```

---

## 📁 Estrutura do projeto

```txt
QuickTools/
├── .github/workflows/
├── Converters/
├── Helpers/
├── Models/
├── Services/
├── ViewModels/
├── Views/
├── scripts/
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── QuickPickerWindow.xaml
├── QuickPickerWindow.xaml.cs
├── QuickTools.csproj
└── QuickTools.sln
```

---

## 🔐 Segurança e permissões

Algumas funcionalidades podem depender das permissões do Windows.

Notas importantes:

* Apps executadas como administrador podem não aceitar inputs simulados por uma app não elevada
* Algumas ações do sistema podem exigir permissões adicionais
* Suspender e hibernar dependem das capacidades e políticas de energia do Windows
* Alterações em Wi-Fi ou configurações de sistema podem exigir aprovação do utilizador
* A opção “Start with Windows” utiliza a chave de arranque do utilizador atual no Registo do Windows

---

## ⚠️ Aviso do Windows SmartScreen

Como a aplicação ainda não está assinada digitalmente, o Windows pode mostrar um aviso ao abrir o `.exe`, especialmente se tiver sido descarregado da Internet.

Se aparecer a mensagem:

```txt
Windows protected your PC
```

podes escolher:

```txt
More info → Run anyway
```

Para uma versão pública mais profissional, o ideal será assinar o executável com um certificado de code signing.

---

## 🔄 Atualizações

As builds publicadas podem verificar a release `latest` no GitHub para identificar versões mais recentes da aplicação.

Quando existir uma nova versão, a aplicação pode descarregar o novo ficheiro, substituir os ficheiros locais e reiniciar.

---

## 🧭 Roadmap

Possíveis melhorias futuras:

* Ícone na system tray
* Controlo rápido para parar o Auto Clicker
* Captura personalizada de atalhos
* Instalador oficial
* Screenshots no README
* Mais ações no Quick Toggle
* Mais opções de automação
* Testes automáticos para serviços principais

---

## 🧑‍💻 Tecnologias usadas

* C#
* .NET 8
* WPF
* Windows API
* GitHub Actions
* PowerShell
* `powercfg`

---

## 📌 Objetivo

O objetivo do QuickTools é juntar várias ferramentas pequenas, mas úteis, numa só aplicação leve para Windows.

Em vez de ter várias apps separadas para automatizar cliques, gerir energia, abrir ações rápidas ou mudar planos de energia, o QuickTools centraliza tudo numa interface simples e prática.

---

## 📄 Licença

Este projeto é pessoal/experimental.
Define uma licença antes de distribuir publicamente, por exemplo MIT, GPL ou proprietária.

