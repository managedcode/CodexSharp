# Testing Strategy

## Goal

Verify `ManagedCode.CodexSharpSDK` behavior against TypeScript SDK semantics with deterministic automated tests.

## Test levels used in this repository

- Primary: TUnit behavior tests in `tests`
- Secondary: NativeAOT smoke publish in `tests/AotSmoke`
- Optional CI matrix: real Codex CLI integration (`.github/workflows/real-integration.yml`)

## Principles

- Test observable behavior, not implementation details.
- Use the real installed `codex` CLI for process interaction tests; do not use `FakeCodexProcessRunner` doubles.
- Treat `codex` as a prerequisite for real integration runs (`CODEX_REAL_INTEGRATION=1`) and install it in CI/local setup before running those tests.
- Cover error paths and cancellation paths.
- Keep protocol parser coverage for all supported event/item kinds.
- Keep a large-stream parser performance profile test to catch regressions.

## Commands

- build: `dotnet build ManagedCode.CodexSharpSDK.slnx -c Release -warnaserror`
- test: `dotnet test --solution ManagedCode.CodexSharpSDK.slnx -c Release`
- coverage: `dotnet test --solution ManagedCode.CodexSharpSDK.slnx -c Release -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml`
- aot-smoke: `dotnet publish tests/AotSmoke/ManagedCode.CodexSharpSDK.AotSmoke.csproj -c Release -r osx-arm64 /p:PublishAot=true`

TUnit on Microsoft Testing Platform does not support `--filter`; run focused tests with `-- --treenode-filter "/*/*/<ClassName>/*"`.

## Test map

- Client lifecycle and concurrency: [CodexClientTests.cs](../../tests/Unit/CodexClientTests.cs)
- `CodexClient` API surface behavior: [CodexClientTests.cs](../../tests/Unit/CodexClientTests.cs)
- CodexThread run/stream/failure behavior: [CodexThreadTests.cs](../../tests/Unit/CodexThreadTests.cs)
- CLI arg/env/config behavior: [CodexExecTests.cs](../../tests/Unit/CodexExecTests.cs)
- Real process integration behavior: [CodexExecIntegrationTests.cs](../../tests/Integration/CodexExecIntegrationTests.cs)
- Real Codex CLI integration behavior (env-gated): [RealCodexIntegrationTests.cs](../../tests/Integration/RealCodexIntegrationTests.cs)
- Protocol parser behavior: [ThreadEventParserTests.cs](../../tests/Unit/ThreadEventParserTests.cs)
- Protocol parser large-stream performance profile: [ThreadEventParserPerformanceTests.cs](../../tests/Performance/ThreadEventParserPerformanceTests.cs)
- Serialization and schema temp file behavior: [TomlConfigSerializerTests.cs](../../tests/Unit/TomlConfigSerializerTests.cs), [OutputSchemaFileTests.cs](../../tests/Unit/OutputSchemaFileTests.cs)
