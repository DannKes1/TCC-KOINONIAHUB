# Evidências de testes — KoinoniaHub

## Etapa 0 — Migração para .NET 10 (24/09/2026)

Ambiente: Windows, PowerShell, PostgreSQL local. Visual Studio 2022 mantido; validação feita pela CLI.

### Versões

```
PS> dotnet --version
10.0.401

PS> dotnet ef --version
Entity Framework Core .NET Command-line Tools
10.0.12
```

Runtime em execução nos testes: `.NET 10.0.12` (reportado pelo adaptador xUnit).

### Restauração e compilação (`api/`)

```
PS> dotnet restore
Restauração concluída (6,3s)

Construir êxito em 6,4s

PS> dotnet build --no-restore
  KoinoniaHub.API net10.0 êxito (5,9s) → KoinoniaHub.API\bin\Debug\net10.0\KoinoniaHub.API.dll
  KoinoniaHub.API.Tests net10.0 êxito (2,5s) → KoinoniaHub.API.Tests\bin\Debug\net10.0\KoinoniaHub.API.Tests.dll

Construir êxito em 8,8s
```

0 avisos, 0 erros. Nenhum pacote precisou de patch diferente do entregue (JwtBearer 10.0.12, EF Tools/Design 10.0.12, Npgsql EF 10.0.3, Swashbuckle 10.2.3).

### Teste de fumaça (`api/`)

```
PS> dotnet test --no-build
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v3.1.5+1b188a7b0a (64-bit .NET 10.0.12)
[xUnit.net 00:00:00.35]   Discovering: KoinoniaHub.API.Tests
[xUnit.net 00:00:00.39]   Discovered:  KoinoniaHub.API.Tests
[xUnit.net 00:00:00.41]   Starting:    KoinoniaHub.API.Tests
[xUnit.net 00:00:03.46]   Finished:    KoinoniaHub.API.Tests
  KoinoniaHub.API.Tests teste net10.0 êxito (4,5s)

Resumo do teste: total: 3; falhou: 0; bem-sucedido: 3; ignorado: 0; duração: 4,5s
```

Casos: `RotaProtegida_SemCookie_Retorna401`, `CadastroInicial_Login_EListagem_Funcionam`, `Login_ComSenhaErrada_Retorna400`. O log confirmou a criação do esquema completo no SQLite em memória (tabelas, chaves estrangeiras e índices únicos, inclusive o filtro `"Ativo" = true`), fechando a decisão da seção 7.1 do Plano por SQLite em memória.

### Banco real (`api/KoinoniaHub.API/`)

```
PS> dotnet ef database update
No migrations were applied. The database is already up to date.
Done.
```

Ocorrência registrada: a primeira execução falhou com `42701: coluna "ConviteExpiraEm" da relação "Usuarios" já existe`. Causa: a tabela `__EFMigrationsHistory` guardava a migration `ConvitePrimeiroAcessoUsuario` com o id antigo `20260809201703`, enquanto o arquivo do projeto é `20260811041229` (migration regenerada após já ter sido aplicada). Correção, sem alteração de dados:

```sql
UPDATE "__EFMigrationsHistory"
SET "MigrationId" = '20260811041229_ConvitePrimeiroAcessoUsuario'
WHERE "MigrationId" = '20260809201703_ConvitePrimeiroAcessoUsuario';
```

Não relacionada ao .NET 10; ocorreria igualmente no .NET 8.

### API no ar

- `dotnet run --launch-profile https` → `Now listening on: https://localhost:7054`.
- `https://localhost:7054/swagger/v1/swagger.json` gerado corretamente (`"openapi": "3.0.4"`, `securitySchemes.Bearer` presente).
- Swagger UI inicialmente exibiu "Unable to render this definition" por cache do navegador com a UI do Swashbuckle 6.6.2; resolvido com limpeza de cache. Sem alteração de código.
- Front (`web/`, `npm run dev`): login e listagem funcionando como antes.

### Definição de pronto da Etapa 0

| Critério | Resultado |
|---|---|
| Build da solution (API + testes) sem erros | OK |
| 3 testes de fumaça verdes | OK |
| Migration existente aplica limpo no PostgreSQL | OK (após correção do histórico) |
| API sobe, login e listagem funcionam pelo front | OK |