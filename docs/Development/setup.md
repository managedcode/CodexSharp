# Development Setup

## Prerequisites

- .NET SDK `10.0.103` (see `global.json`)
- Codex CLI available locally (`codex` in PATH) for real runtime usage
- Git with submodule support

## Bootstrap

```bash
git submodule update --init --recursive
dotnet restore ManagedCode.CodexSharpSDK.slnx
```

## Local validation

```bash
dotnet build ManagedCode.CodexSharpSDK.slnx -c Release -warnaserror
dotnet test --solution ManagedCode.CodexSharpSDK.slnx -c Release
dotnet format ManagedCode.CodexSharpSDK.slnx
```

Focused run (TUnit/MTP):

```bash
dotnet test --project tests/CodexSharpSDK.Tests.csproj -c Release -- --treenode-filter "/*/*/ThreadEventParserTests/*"
```

## AOT smoke check

```bash
dotnet publish tests/AotSmoke/ManagedCode.CodexSharpSDK.AotSmoke.csproj -c Release -r osx-arm64 /p:PublishAot=true
```

## Packaging check

```bash
dotnet pack src/CodexSharpSDK.csproj -c Release --no-build -o artifacts
```

## CI/workflows

- CI: `.github/workflows/ci.yml`
- Release: `.github/workflows/release.yml`
- CodeQL: `.github/workflows/codeql.yml`
- TypeScript sync watcher: `.github/workflows/typescript-sdk-watch.yml`
- Real integration matrix: `.github/workflows/real-integration.yml`
