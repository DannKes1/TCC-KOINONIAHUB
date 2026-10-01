# Etapa 0 — Migração para .NET 10 · resumo de decisões

Concluída em 24/09/2026 (Plano de Desenvolvimento, seções 5 e 6.1). Escrito retroativamente no fechamento da Etapa 2, conforme a regra "Resumo de decisões por etapa" (Plano 1.1).

## 1. Decisões tomadas dentro da margem de julgamento

**Versões fixadas no `csproj`.** net10.0; `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.12; `Microsoft.EntityFrameworkCore.Tools` 10.0.12; `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3; `Swashbuckle.AspNetCore` 10.2.3; `BCrypt.Net-Next` mantido em 4.0.3. Foram as últimas versões estáveis compatíveis com o runtime 10.0.12 instalado na máquina de desenvolvimento; o Swashbuckle 11.x foi descartado por ser destinado ao ASP.NET Core 11.

**Referência explícita a `Microsoft.EntityFrameworkCore.Design` 10.0.12.** A partir do EF Tools 10.0.6 a dependência no Design caiu para `>= 8.0.0` (dotnet/efcore #38124), e sem a referência na mesma versão o `dotnet ef migrations add` falha com `MissingMethodException`. Como a Etapa 1 são três migrations, isso foi resolvido antes. `PrivateAssets="all"` para não vazar a dependência para consumidores.

**Bloco do Swagger reescrito para o Microsoft.OpenApi 2.x.** O Swashbuckle 10 mudou o namespace (`Microsoft.OpenApi.Models` → `Microsoft.OpenApi`), tirou `Reference` do `OpenApiSecurityScheme` e passou a receber um delegado em `AddSecurityRequirement` (`OpenApiSecuritySchemeReference(nome, document)`). Foi a única alteração de código de produção da etapa, seguindo o exemplo "Bearer authentication" do guia de migração v10.

**`public partial class Program { }` no fim do `Program.cs`.** Exigido pelo `WebApplicationFactory<Program>` do projeto de testes, já que o `Program.cs` usa top-level statements.

**SQLite em memória em vez de Testcontainers (Plano 7.1).** Verificação estática de todo o código de consulta: não há `EF.Functions.ILike`, `FromSql`, tipos Npgsql nem funções exclusivas do PostgreSQL dentro de LINQ; o único SQL literal do modelo é o filtro do índice único de matrícula ativa, `"Ativo" = true`, válido em SQLite. Ganho: os testes rodam sem Docker e em segundos. Se alguma tradução futura falhar, a alternativa continua sendo Testcontainers.PostgreSql.

**Fábrica de testes sobe o `Program.cs` inteiro** e troca apenas o provedor do `DbContext` (remoção de `IDbContextOptionsConfiguration<KoinoniaHubDbContext>` e re-registro com `UseSqlite`), com `EnsureCreated()` para preservar índices únicos e chaves estrangeiras. `BaseAddress` https porque o cookie `kh_token` é `Secure` e o `CookieContainer` só o reenvia para https. Chave JWT de teste e demais configurações via `UseSetting`.

**README atualizado** para .NET 10 / EF Core 10, para o repositório não contradizer a monografia.

## 2. Arquivos criados ou alterados

- `api/KoinoniaHub.API/KoinoniaHub.API.csproj` — versões acima.
- `api/KoinoniaHub.API/Program.cs` — bloco do Swagger; `public partial class Program { }`.
- `api/KoinoniaHub.API.sln` — projeto de testes incluído.
- `api/KoinoniaHub.API.Tests/KoinoniaHub.API.Tests.csproj` — xunit 2.9.3, runner 3.1.5, Test.Sdk 18.0.1, Mvc.Testing 10.0.12, EF Sqlite 10.0.12.
- `api/KoinoniaHub.API.Tests/Infraestrutura/KoinoniaHubWebApplicationFactory.cs` — novo.
- `api/KoinoniaHub.API.Tests/Etapa0_FumacaTests.cs` — novo (3 casos: 401 sem cookie; cadastro inicial → login → listagem; login inválido → 400).
- `README.md`.

Nenhuma entidade, migration, DTO, rota, serviço ou tela foi alterada; o front não foi tocado.

## 3. Divergência registrada (resolvida na Etapa 2.4)

`AuthController.Login` e `RegistrarAdmin` gravavam o cookie `kh_token` e **também** devolviam `Token` no JSON — resquício da fase em que o front guardava o token no `localStorage`. A monografia (4.8) entrega o JWT "em um cookie do tipo httpOnly e Secure" justamente para que não fique acessível a scripts. Não foi alterado na Etapa 0 (só migração); entrou na Etapa 2.4.

## 4. O que o autor deve saber explicar na banca

- Por que o .NET 10 (LTS até 2028) e o que a migração exigiu: apenas o `csproj`, o bloco do Swagger e a referência ao EF Design; nenhuma regra de negócio mudou.
- O que o `WebApplicationFactory` faz: sobe a API real em memória, com o mesmo pipeline (CORS, middleware de Origin, autenticação por cookie), trocando só o banco — por isso os testes de autorização 403/400 valem como evidência de Segurança (Plano 7.5).
- Por que SQLite em memória é suficiente aqui e em que caso deixaria de ser (função exclusiva do PostgreSQL em uma consulta).
- Por que o teste de fumaça já prova cadastro inicial, login por cookie e uma rota autorizada por perfil.
