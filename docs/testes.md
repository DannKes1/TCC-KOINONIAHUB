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

| Critério                                        | Resultado                       |
| ----------------------------------------------- | ------------------------------- |
| Build da solution (API + testes) sem erros      | OK                              |
| 3 testes de fumaça verdes                       | OK                              |
| Migration existente aplica limpo no PostgreSQL  | OK (após correção do histórico) |
| API sobe, login e listagem funcionam pelo front | OK                              |

---

## Etapa 1.1 — Situação da aula: modelo + migration (25/09/2026)

Pré-condição atendida: Etapa 0 compilando e `Etapa0_FumacaTests` verde.

### Arquivos aplicados

9 arquivos copiados para os caminhos do repositório (entidade `Aula`, constantes `SituacaoAula`, `AulaRespostaDto`, `AulaServico`, `ChamadaServico`, `ContextoDeTeste`, `Etapa1_SituacaoAulaTests`, `dtos.ts`, `aulasServico.ts`). Nenhuma outra referência a `Consolidada` restava na API.

### Migration `20260925041839_SituacaoAula`

```
PS> dotnet ef migrations add SituacaoAula
An operation was scaffolded that may result in the loss of data. Please review the migration for accuracy.
Done.
```

Contagem antes do `database update`:

```sql
SELECT COUNT(*) FILTER (WHERE "Consolidada") AS consolidadas, COUNT(*) AS total FROM "Aulas";
-- consolidadas = 10 | total = 18
```

**Ocorrência registrada.** O `dotnet ef database update` foi executado antes de o corpo da migration ser substituído pelo entregue, de modo que rodou a versão gerada automaticamente:

```
ALTER TABLE "Aulas" DROP COLUMN "Consolidada";
ALTER TABLE "Aulas" ADD "Situacao" character varying(20) NOT NULL DEFAULT '';
```

Resultado: as 18 aulas ficaram com `Situacao = ''` e a informação de quais estavam consolidadas foi perdida — exatamente o risco que o corpo entregue (criar → converter → apagar) evita. Como a base é de teste, a correção foi feita por SQL:

```sql
UPDATE "Aulas" SET "Situacao" = 'EmAberto' WHERE "Situacao" = '';
ALTER TABLE "Aulas" ALTER COLUMN "Situacao" SET DEFAULT 'EmAberto';
UPDATE "Aulas" SET "Situacao" = 'Consolidada'
WHERE "Id" NOT IN (SELECT "Id" FROM "Aulas" ORDER BY "Data" DESC LIMIT 2);
```

Contagem depois: `Consolidada = 16`, `EmAberto = 2` (total 18). O arquivo `Migrations/20260925041839_SituacaoAula.cs` foi então substituído pelo corpo entregue (com o `UPDATE` de conversão), que é o versionado no repositório e o que rodará em qualquer outro banco. Lição para as próximas migrations editadas à mão: colar → salvar → só então `database update`.

### Build e testes (`api/`)

```
PS> dotnet build
  KoinoniaHub.API net10.0 êxito
  KoinoniaHub.API.Tests net10.0 êxito
Construir êxito em 3,9s   (0 avisos, 0 erros)

PS> dotnet test
Resumo do teste: total: 7; falhou: 0; bem-sucedido: 7; ignorado: 0; duração: 3,1s
```

Casos novos: `Etapa1_SituacaoAulaTests` (4). O esquema gerado pelo SQLite nos testes já traz `"Situacao" TEXT NOT NULL` em `Aulas`.

### Front (`web/`)

```
PS> npx vue-tsc -b
(sem saída — sem erros)
```

### Teste manual (RF31 / RNF 32.2)

1. Aulas da turma: consolidadas marcadas; as 2 em aberto exibidas como "Em aberto". Lançamento de chamada e consolidação de uma aula em aberto funcionaram.
2. Aula consolidada na tela de chamada: modo somente leitura ("Esta aula está consolidada. A chamada está em modo somente leitura.").
3. Pela API (Swagger, autenticado via Authorize): `POST /api/aulas/16/presencas` em aula consolidada → **400** `{"mensagem": "Somente aulas Em aberto permitem lançar ou alterar a chamada."}`.
4. `GET /api/aulas/16` → `"situacao": "Consolidada"`, `"pendenteFechamento": false`.

### Observação de ambiente

O Visual Studio 2022 17.13 recusa compilar `net10.0` (NETSDK1233 como erro; a partir do 17.14 é aviso). Build, testes e execução da API foram feitos pela CLI (`dotnet build/test/run`). Atualização do VS 2022 ou instalação do VS 2026 pendente.

---

## Etapa 1.2 — AceitesTermo: entidade + migration (26/09/2026)

Pré-condição atendida: Etapa 1.1 aplicada e os 7 testes verdes.

### Arquivos aplicados

5 arquivos copiados para os caminhos do repositório: entidade `AceiteTermo`,
constantes `MeioAceiteTermo`, `KoinoniaHubDbContext` (DbSet + FKs com
`DeleteBehavior.Restrict`), `ContextoDeTeste` (`Foreign Keys=True` na conexão
SQLite) e `Etapa1_AceiteTermoTests`. Sem alteração de API, serviço, controller
ou front nesta etapa (endpoints e telas do termo são a Etapa 5).

**Ocorrência registrada.** O primeiro `dotnet ef migrations add AceitesTermo`
foi executado antes de todos os arquivos estarem no lugar: com o DbContext
ainda na versão anterior, a migration saiu vazia (`Up()` sem operações). Na
sequência, o DbContext foi substituído sem que `AceiteTermo.cs` e
`MeioAceiteTermo.cs` tivessem sido copiados, e o build passou a falhar com
`CS0246` (tipo `AceiteTermo` não encontrado). Correção: criação dos dois
arquivos da entidade, `dotnet ef migrations remove` (removeu a migration vazia
e reverteu o snapshot) e novo `migrations add`. Nada chegou ao banco — o
`database update` só rodou após a conferência do arquivo regenerado. Lição,
complementando a da 1.1: `dotnet build` verde **antes** do `migrations add`.

### Migration `20260926193619_AceitesTermo`

Conferência antes do `database update`: o `Up()` contém apenas
`CreateTable("AceitesTermo")` com as 9 colunas, as duas FKs com
`onDelete: ReferentialAction.Restrict` e os dois `CreateIndex` — nenhuma
operação em outra tabela.

```
PS> dotnet ef database update
Applying migration '20260926193619_AceitesTermo'.
CREATE TABLE "AceitesTermo" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "UsuarioId" integer NOT NULL,
    "IgrejaId" integer NOT NULL,
    "TermoVersao" character varying(20) NOT NULL,
    "TermoHash" character varying(64) NOT NULL,
    "AceitoEm" timestamp with time zone NOT NULL,
    "Ip" character varying(45),
    "Meio" character varying(30) NOT NULL,
    "CriadoEm" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_AceitesTermo" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AceitesTermo_Igrejas_IgrejaId" FOREIGN KEY ("IgrejaId")
        REFERENCES "Igrejas" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AceitesTermo_Usuarios_UsuarioId" FOREIGN KEY ("UsuarioId")
        REFERENCES "Usuarios" ("Id") ON DELETE RESTRICT
);
CREATE INDEX "IX_AceitesTermo_IgrejaId" ON "AceitesTermo" ("IgrejaId");
CREATE INDEX "IX_AceitesTermo_UsuarioId" ON "AceitesTermo" ("UsuarioId");
Done.
```

Conferido no banco (pgAdmin): tabela `AceitesTermo` com as 9 colunas e as duas
FKs `ON DELETE RESTRICT`.

### Build e testes (`api/`)

```
PS> dotnet test
Resumo do teste: total: 10; falhou: 0; bem-sucedido: 10; ignorado: 0; duração: 4,1s
```

Casos novos: `Etapa1_AceiteTermoTests` (3) — `AceiteTermo_Gravado_PersisteTodosOsCampos`,
`Usuario_ComAceiteRegistrado_NaoPodeSerExcluido`,
`MeioAceiteTermo_ExpoeOsTresMeiosDaRnf42_5`. O log do SQLite em memória mostra
`AceitesTermo` criada com as duas FKs `RESTRICT`; com `Foreign Keys=True` na
conexão de teste, a exclusão do usuário com aceite falha de fato
(`DbUpdateException`), validando a RNF 42.3 no nível da entidade.

### Pendência

Evidência da Etapa 1 em banco limpo (migrations do zero + preservação das
aulas consolidadas pela `SituacaoAula`) será produzida ao fim da Etapa 1.3,
conforme seção 5 do LEIA-ME da 1.2.

---

## Etapa 1.3 — Pessoa enxuta · Bloco 1: API (26/09/2026)

Pré-condição atendida: Etapa 1.2 aplicada e os 10 testes verdes.

### Arquivos aplicados

13 arquivos copiados para os caminhos do repositório: entidade `Pessoa` (saem
`CPF`, `Telefone`, `Categoria`, `DataBatismo`, `DataMembresia`, `FotoUrl`,
`Observacoes`), DTOs de pessoa (criar/atualizar/resposta),
`MeusDadosAtualizarRequisicaoDto` (sai `Telefone`), `ParentescoRespostaDto`
(sai `ParenteTelefone`), `PessoaServico`, `PessoaImportacaoServico` (parser no
modelo do Plano 6.4; `EstadoCivil` passa a ser importado; mensagem da RNF 41.3;
`Situacao` deixa de ser lida — importados nascem Ativos), `MatriculaServico`,
`ParentescoServico`, `AuthServico`, e os testes novos `Etapa1_PessoaEnxutaTests`
e `RF41_ImportacaoTests`. Controllers, interfaces e rotas intactos.
Compilação verificada ANTES do `migrations add` (lição da 1.2): 0 erros, 0 avisos.

### Migration `20260926203116_PessoaEnxuta`

`Up()` conferido antes do `database update`: somente os sete `DropColumn` em
`Pessoas`; `Down()` com os sete `AddColumn`. O aviso "may result in the loss
of data" é esperado — a perda é intencional (RNF 7.5, princípio da necessidade).

```
PS> dotnet ef database update
Applying migration '20260926203116_PessoaEnxuta'.
ALTER TABLE "Pessoas" DROP COLUMN "CPF";
ALTER TABLE "Pessoas" DROP COLUMN "Categoria";
ALTER TABLE "Pessoas" DROP COLUMN "DataBatismo";
ALTER TABLE "Pessoas" DROP COLUMN "DataMembresia";
ALTER TABLE "Pessoas" DROP COLUMN "FotoUrl";
ALTER TABLE "Pessoas" DROP COLUMN "Observacoes";
ALTER TABLE "Pessoas" DROP COLUMN "Telefone";
Done.
```

A contagem prévia por `Categoria` (sugerida no guia como opcional) não foi
registrada. Base de desenvolvimento; sem impacto para as evidências.

### Build e testes (`api/`)

```
PS> dotnet test
Resumo do teste: total: 17; falhou: 0; bem-sucedido: 17; ignorado: 0; duração: 3,0s
```

Casos novos: `Etapa1_PessoaEnxutaTests` (2) — o modelo EF de `Pessoa` tem
exatamente as 17 colunas do DER; pessoa só com nome persiste (RNF 7.5) — e
`RF41_ImportacaoTests` (5) — colunas do modelo preenchem os campos; coluna fora
do modelo é ignorada (inclusive `Situacao`: importado nasce Ativo); e-mail
repetido ignora; nome repetido sem e-mail ignora e sinaliza (RNF 41.3, mensagem
exata do Plano 6.4); mais de 1.000 linhas lança erro (RNF 41.4). O esquema do
SQLite nos testes já cria `Pessoas` com as 17 colunas.

### Swagger (autenticado como Admin)

1. `GET /api/pessoas` → 200, itens **sem** `cpf`, `categoria`, `telefone`,
   `dataBatismo`, `dataMembresia`, `fotoUrl`, `observacoes`.
2. `POST /api/pessoas` ("Teste Etapa 1.3") → **201**, `Location` `/api/pessoas/70`.
3. `GET /api/pessoas/5/parentescos` → 200, itens com `parenteCelular`, sem
   `parenteTelefone`.

Front não testado nesta entrega: as telas de Pessoas, Meus Dados, Matrículas e
Parentescos quebram por design até o Bloco 2 (front da Etapa 1.3).

---

## Etapa 1.3 — Pessoa enxuta · Bloco 2: front (26/09/2026)

Pré-condição atendida: Bloco 1 aplicado (17 testes verdes, Swagger conferido).

### Arquivos aplicados

9 arquivos copiados para os caminhos do repositório: `dtos.ts` (`PessoaVM`,
`PessoaCriarDTO` e `ParentescoVM` sem os campos removidos), `pessoasServico.ts`,
`meusDadosServico.ts`, `parentescosServico.ts`, `matriculasServico.ts`, e as
telas `PaginaPessoasLista.vue` (sem filtro/coluna Categoria; formulário
reduzido), `PaginaMeusDados.vue` (bloco Nome/Situação; sem Telefone),
`PaginaMatriculasTurma.vue` (contato do parente só celular) e
`DialogImportarPessoas.vue` (arquivo-modelo com as 11 colunas do Plano 6.4).
Nenhuma rota, store ou componente compartilhado tocado.

### Typecheck (`web/`)

```
PS> npx vue-tsc -b
(sem saída — sem erros)
```

### Testes manuais (API + `npm run dev`)

| #   | Tela                                    | Resultado                                                                                |
| --- | --------------------------------------- | ---------------------------------------------------------------------------------------- |
| 1   | Pessoas → lista                         | OK — sem coluna/filtro Categoria; busca por nome/e-mail/celular.                         |
| 2   | Nova pessoa só com nome                 | OK — "Pessoa cadastrada" (RNF 7.5).                                                      |
| 3   | Editar pessoa                           | OK — carrega e salva; validação de celular mantida.                                      |
| 4   | Parentescos                             | OK — adicionar e remover.                                                                |
| 5   | Meus Dados                              | OK — bloco Nome/Situação; sem campo Telefone; salvar contato.                            |
| 6   | Matrículas → disponíveis / responsáveis | OK — lista sem categoria; contato do parente só celular.                                 |
| 7   | Baixar e importar modelo CSV            | OK — cabeçalho com as 11 colunas; 1ª importação criou as 3; reimportação ignorou as 3.   |
| 8   | Importar nome repetido sem e-mail       | OK — linha "Ignorado" com a mensagem da RNF 41.3 ("linha ignorada para conferência..."). |

---

## Etapa 1 — Fechamento: evidência em banco limpo (26/09/2026)

Definição de pronto: "`database update` limpo em base nova e em base existente"

- prova de que `SituacaoAula` preserva as aulas consolidadas (pendência
  registrada na 1.1, quando a base de desenvolvimento perdeu essa contagem).

Roteiro executado no banco descartável `koinoniahub_evidencia` (psql 18.1):

1. `dotnet ef database update 20260811041229_ConvitePrimeiroAcessoUsuario`
   (com `--connection` para o banco de evidência) → aplicou as 6 migrations do
   modelo antigo, `Done.`.
2. Dados mínimos inseridos no modelo antigo (igreja, pessoa com `Categoria`,
   departamento, matéria, 3 aulas com `Consolidada` = true/true/false):

```
 consolidadas | total
--------------+-------
            2 |     3
```

3. `dotnet ef database update` (as três migrations da Etapa 1: `SituacaoAula`,
   `AceitesTermo`, `PessoaEnxuta`) → `Done.`.
4. Resultados após as migrations:

```
  Situacao   | count
-------------+-------
 Consolidada |     2
 EmAberto    |     1
```

`Pessoas`: 17 colunas (Id, Nome, DataNascimento, Sexo, EstadoCivil, Celular,
Email, Endereco, Bairro, Cidade, Estado, CEP, IgrejaId, CriadoEm, AtualizadoEm,
DataInativacao, Situacao) — nenhuma das 7 removidas.

`AceitesTermo`: 9 colunas (Id, UsuarioId, IgrejaId, TermoVersao, TermoHash,
AceitoEm, Ip, Meio, CriadoEm).

5. `DROP DATABASE koinoniahub_evidencia;`.

Conclusão: as três migrations aplicam do zero e sobre base existente, a
conversão da `SituacaoAula` preserva as consolidadas, e o esquema final confere
com o DER. **Etapa 1 (Migrations) concluída.**

---

## Etapa 2.1 — Restrições de leitura para o Professor: RF11/RF28/RF34 (27/09/2026)

Pré-condição atendida: Etapa 1 concluída (28 = 17 + 11 testes ao fim desta etapa).

### Arquivos aplicados

19 arquivos, sem migration e sem front: `Perfis` (constantes dos perfis),
`GarantirAcessoPessoaAsync` e `ListarDepartamentosComAtribuicaoAtivaAsync` no
`AutorizacaoEbdServico`, DTOs reduzidos `PessoaTurmaRespostaDto` e
`PessoaDisponivelRespostaDto`, `ObterParaTurmaAsync` no `PessoaServico`,
`ListarPessoasDisponiveisReduzidoAsync` no `MatriculaServico`, filtro por
turmas no `PresencaHistoricoServico`, guard nos controllers de Pessoas,
Presenças, Matrículas e Parentescos, e os testes de integração com a semente
`CenarioAcessoPessoa`.

**Ocorrência registrada.** Na primeira aplicação, 2 dos 19 arquivos ficaram de
fora (`IPresencaHistoricoServico.cs` e `PessoaTurmaRespostaDto.cs`), e o build
acusou exatamente os dois (`CS0535` e `CS0246`). Copiados, build limpo. Nada
chegou ao banco (etapa sem migration).

### Regra (Plano 6.5)

Gestão passa sem verificação; Usuario só o próprio registro; Professor só
alunos com matrícula ativa em turma onde tenha atribuição ativa — 403 fora,
verificado **antes** do 404 (não revela se a pessoa existe). O guard também
cobre `GET /api/pessoas/{id}/parentescos` (decisão 4.3 do LEIA-ME 2.1:
**opção (a)** — Professor mantido no endpoint, limitado às suas turmas;
RNF 12.1 lida como gestão dos vínculos; registrado no Plano).

### Build e testes (`api/`)

```
PS> dotnet test
Resumo do teste: total: 28; falhou: 0; bem-sucedido: 28; ignorado: 0; duração: 6,8s
```

Casos novos: `RF11_PessoaAcessoTests` (5), `RF28_DisponiveisTests` (3),
`RF34_PresencasPessoaTests` (3) — incluem as contraprovas de que Admin segue
com DTO completo e Usuario acessa só o próprio registro.

### Evidências no Swagger (autenticado como Professor — Plano 7.5)

| RNF  | Resultado                                                                                                                                                                                                                    | Evidência                      |
| ---- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------ |
| 11.3 | `GET /api/pessoas/3` (fora da turma) → **403** "Você só pode consultar alunos das turmas em que possui atribuição ativa."; `GET /api/pessoas/5` (aluna da turma) → 200 apenas com `id, nome, situacao, celular, parentescos` | `docs/evidencias/RNF-11.3.png` |
| 28.3 | `GET /api/departamentos/2/pessoas-disponiveis` → itens apenas com `id, nome, situacao`                                                                                                                                       | `docs/evidencias/RNF-28.3.png` |
| 34.3 | `GET /api/pessoas/3/presencas` → **403**; `GET /api/pessoas/5/presencas` → 200 somente com presenças da turma 4 (Adolescentes)                                                                                               | `docs/evidencias/RNF-34.3.png` |

### Teste manual pelo front

Professor: Matrículas (lista de disponíveis e diálogo Responsáveis) ok;
Usuario: painel próprio ok; Admin: Pessoas/Parentescos inalterados.

---

## Etapa 2.2 — Verificação de Origin nas escritas: RNF 2.6 (27/09/2026)

Pré-condição atendida: Etapa 2.1 aprovada (28 verdes).

### Arquivos aplicados

4 arquivos, sem migration e sem front: middleware no `Program.cs` (entre
`UseCors` e `UseAuthentication`: POST/PUT/PATCH/DELETE com `Origin` fora de
`Cors:OrigensPermitidas` → 403 "Origem não autorizada."; sem `Origin` também
403, conforme OWASP CSRF Prevention Cheat Sheet — fonte conferida em
27/09/2026); `appsettings.Development.json` com as três origens locais (Vite
e Swagger nos dois perfis); fábrica de testes enviando `Origin` autorizado em
todo `HttpClient`; `RF2_OrigemTests`.

### Build e testes (`api/`)

```
PS> dotnet test
Resumo do teste: total: 36; falhou: 0; bem-sucedido: 36; ignorado: 0; duração: 4,6s
```

8 execuções novas em `RF2_OrigemTests` (5 Facts + Theory com 3 métodos de
escrita): origem estranha → 403 com a mensagem; origem autorizada passa; GET
sem Origin passa; POST sem Origin → 403; PUT/PATCH/DELETE de origem estranha →
403 **antes** da autenticação; OPTIONS não é bloqueado. (O LEIA-ME estimou 35
contando o Theory como 1 caso; são 3 execuções.)

### Evidências por curl (API no ar; Swagger não serve — origem autorizada)

| #   | Requisição                                                         | Resultado                                                                                                                    |
| --- | ------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------- |
| 1   | POST /api/auth/login com `Origin: https://sitio-malicioso.exemplo` | **403** `{"mensagem":"Origem não autorizada."}` → `RNF-2.6-403-origem-nao-autorizada.png`                                    |
| 2   | POST /api/auth/login sem `Origin`                                  | **403** (recomendação OWASP)                                                                                                 |
| 3   | POST /api/auth/login com `Origin: http://localhost:5173`           | **400** "Usuário ou senha inválidos." — atravessou o middleware e chegou à validação → `RNF-2.6-passa-origem-autorizada.png` |
| 4   | GET /api/meus-dados sem `Origin`                                   | **401** — leitura não é bloqueada                                                                                            |

Ocorrência registrada: na primeira execução dos curls, o escape de aspas do
PowerShell corrompeu o corpo JSON (o 403 do caso 1 independe do corpo, mas o
caso 3 devolveu 400 de model binding); refeitos com o operador `--%`, que
repassa a linha crua ao curl.

### Teste manual pelo front

Gerar convite de primeiro acesso (POST sem corpo), importar CSV (multipart) e
logout/login — os três fluxos de "requisição simples" que motivaram a RNF —
funcionando normalmente com a origem do Vite autorizada.

### Monografia

Seção 4.8 (trecho de CSRF reescrito com as duas medidas), RNF 2.6 na tabela do
RF2, referência OWASP [2026]a adicionada e a de senhas renomeada para [2026]b
— textos do LEIA-ME 2.2, seção 4, aplicados; passagens "texto atual"
conferidas contra o documento antes da substituição, e a fonte OWASP
verificada na URL citada.

---

## Etapa 2.3 — Guard do último administrador: RNF 14.3 (28/09/2026)

Pré-condição atendida: Etapa 2.2 aprovada (36 verdes).

### Arquivos aplicados

4 arquivos, sem migration e sem front: `ContarAdminsAtivosAsync` em
`IUsuarioRepositorio`/`UsuarioRepositorio` (Plano 6.7) e o guard em
`UsuarioServico.AtualizarAsync` — se o alvo é Admin ativo e o pedido o inativa
ou troca o perfil, e a contagem de Admins ativos da igreja é ≤ 1, a operação é
recusada com "Não é possível inativar ou alterar o perfil do único
administrador ativo da igreja." (400 pelo `catch` já existente no controller).
Ordem das verificações: autoinativação → RNF 14.3 → validação do perfil.
`<= 1` em vez de `== 1` bloqueia também contagem inconsistente (0).

### Build e testes (`api/`)

```
PS> dotnet test
Resumo do teste: total: 42; falhou: 0; bem-sucedido: 42; ignorado: 0; duração: 7,1s
```

Casos novos: `RF14_UltimoAdminTests` (6), com a semente `CenarioAdministradores`.
Observação registrada (LEIA-ME 2.3, seção 3): pela API, o ramo "inativar" da
14.3 para o próprio único Admin é precedido pela autoproteção ("Você não pode
desativar o seu próprio usuário"); o ramo efetivamente alcançável é o
rebaixamento de perfil — coberto pelos testes de integração — e o ramo
"inativar por outro ator" é provado por teste unitário do serviço.

### Evidência (Swagger, logado como o único Admin — Plano 7.5)

`PATCH /api/usuarios/1` com `{ "perfil": "Professor" }` → **400**
`{"mensagem":"Não é possível inativar ou alterar o perfil do único
administrador ativo da igreja."}` → `docs/evidencias/RNF-14.3-400-ultimo-admin.png`.

### Teste manual pelo front

Tela Usuários, como único Admin: alterar o próprio perfil → toast com a
mensagem da 14.3; perfil permanece Admin.

---

## Etapa 2.4 — Redefinição de acesso só por convite (RF15) e JWT só no cookie (28/09/2026) · fecha a Etapa 2

Pré-condição atendida: Etapa 2.3 aprovada (42 verdes).

### Arquivos aplicados

15 arquivos (7 API, 5 front, 3 docs) e 1 apagado
(`UsuarioResetarSenhaRequisicaoDto.cs`), sem migration: removidos a rota
`PATCH /api/usuarios/{id}/resetar-senha`, `ResetarSenhaAsync` e, no front, o
botão/diálogo "Resetar senha" — "Redefinir acesso" é o convite
(`POST /api/usuarios/{id}/convite`, RF15/CSU20); `[JsonIgnore]` em `Token` nos
DTOs de login e cadastro inicial (o controller segue gravando o cookie
`kh_token`; o corpo não traz o JWT — fecha a divergência registrada na Etapa 0);
tela de cadastro inicial passa a decidir por `UsuarioId`. Criados
`docs/decisoes/etapa-0.md` a `etapa-2.md` (definição de pronto do Plano v1.2).
Ocorrências da aplicação: um "a " solto após o `</template>` do
`PaginaCadastroInicial.vue` recebido (inócuo; removido ao aplicar) e a lista de
campos removidos no `etapa-1.md` citava 6 dos 7 (faltava `Observacoes`;
corrigido).

### Build e testes (`api/` e `web/`)

```
PS> dotnet test
Resumo do teste: total: 51; falhou: 0; bem-sucedido: 51; ignorado: 0; duração: 12,1s

PS> npx vue-tsc -b
(sem saída — sem erros)
```

Casos novos: `RF39_RF40_ConviteTests` (7) — hash SHA-256 no banco e validade de
7 dias (39.3/39.4); só Admin gera convite (39.1/15.1); expirado recusado sem
consumir (40.2); uso único com senha em BCrypt (40.1/40.3); convite novo
invalida o anterior (39.4/15.2); senha atual vale até o uso (CSU20); rota
resetar-senha → 404 (15.3) — e `RF2_TokenSomenteNoCookieTests` (2) — cadastro
inicial e login sem JWT no corpo, cookie httpOnly/secure sustenta a sessão.

### Teste manual pelo front

1. Redefinir acesso: convite gerado; senha antiga entrou **antes** do uso do
   link (CSU20); link usado em janela anônima definiu a nova; antiga passou a
   falhar e a nova entrou (15.3); reuso do link → "Convite inválido ou já
   utilizado." ✔
2. Coluna Ações só com Editar e convite — diálogo "Resetar senha" inexistente. ✔
3. DevTools no login: corpo com `pessoaId, expiraEm, usuarioId, emailUsuario,
perfil, igrejaId` e sem `token`; `Set-Cookie kh_token` httponly/secure. ✔
4. Cadastro inicial entra direto no painel autenticado. ✔

### Evidência (Plano 7.5)

`PATCH /api/usuarios/1/resetar-senha` (curl com `Origin` autorizado) → **404**
→ `docs/evidencias/RNF-15.3-404-resetar-senha.png`.

### Fechamento da Etapa 2

Definição de pronto atendida: RNFs 11.3/28.3/34.3, 2.6, 14.3 e 15.3
implementadas com testes e evidências; 51 execuções verdes;
`docs/decisoes/etapa-0.md` a `etapa-2.md` no repositório. Decisões da 2.4:
5.1 aceita (Vitest instala na abertura da Etapa 3, com os specs de menu por
perfil e `pendenteFechamento`); 5.2 aceita (linha na seção 9 do Plano sobre a
Figura 14, a substituir na conversão para o TCC 2). **Etapa 2 (Segurança de
leitura e escrita) concluída.**

## Etapa 3.1 — Regras de aula na API: consolidação, Não realizada e reabertura (RF32/RF33) (29/09/2026)

**Escopo.** RF33 completo na API (RNFs 33.1 a 33.7, CSU11 com os três fluxos alternativos)
e RF32 testado (RNFs 32.2, 32.3 e 32.6). `ConsolidarAsync` passou a exigir aula Em aberto e
chamada completa — chamada incompleta devolve 400 com `mensagem` e `alunosSemRegistro[]`
(`ChamadaIncompletaException`, primeira exceção própria do projeto, em `Aplicacao/Excecoes/`).
Rotas novas: `PATCH /api/aulas/{id}/nao-realizada` (gestão ou atribuição ativa) e
`PATCH /api/aulas/{id}/reabrir` (somente Admin; presenças preservadas). Sem migration e sem front.

**Mudança de comportamento registrada.** Consolidar aula já Consolidada era idempotente
(devolvia sucesso); pela RNF 33.2 agora é 400. O front já desabilitava o botão.

**Correção durante a conferência.** O `CenarioAula.cs` recebido não adicionava a matéria ao
contexto do EF (nada a referenciava no grafo, pois as aulas são criadas por teste), o que
derrubaria os 19 testes com violação de chave estrangeira; corrigido com `db.Add(materia)`
antes da aplicação.

**Testes automatizados** — `dotnet test`: **70 aprovados, 0 falhas** (51 anteriores + 19 novos):
`RF33_ConsolidarAulaTests` (5), `RF33_NaoRealizadaTests` (4), `RF33_ReabrirAulaTests` (5),
`RF32_ChamadaTests` (5 execuções, Theory incluída). Cobrem chamada incompleta com lista
correta (matrícula inativa fora), 403 de professor sem atribuição (33.1/33.5), preservação
de presenças na reabertura (33.6), isolamento por igreja (404) e o índice único de presença
no banco (32.3).

**Testes manuais (Swagger, perfil https, usuária Professor "Paula" e Admin):**

1. Aula 20: chamada de 1 de 2 alunos → consolidar → **400** com "A chamada deve ser concluída
   antes da consolidação: 1 aluno..." e `alunosSemRegistro` apontando só o aluno sem registro;
   chamada completa → **204**; GET mostra `situacao: Consolidada`, `pendenteFechamento: false`. ✔
2. Aula 21 sem chamada → `nao-realizada` → **204**; tentativa de chamada → 400 (RNF 32.2);
   tentativa de consolidar → 400 (RNF 33.2). ✔
3. Professor → `reabrir` → **403** (RNF 33.5, evidência abaixo); Admin → **204** e a aula
   voltou a aceitar chamada (RNF 33.6). ✔
4. Reabertura de aula Não realizada pelo Admin → **204** (CSU11 FA3). ✔

**Evidência:** `docs/evidencias/RNF-33.5-403-reabrir-professor.png`.

**Pendência conhecida até a 3.2:** o front ainda deriva `consolidada` e não exibe a situação
"Não realizada"; nenhuma aula deve ficar nesse estado nos dados de demonstração.

## Etapa 3.2 — Front das regras de aula + Vitest (RF31/RF32/RF33) (30/09/2026) · fecha a Etapa 3

**Escopo.** Vitest instalado (Plano 7.2: vitest 5.0.2, @vue/test-utils, jsdom) com specs ao lado
do código; módulo puro `dominio/situacaoAula.ts` (pendente, rótulo, severidade, filtro);
componente `TagSituacaoAula` substituindo as três tags derivadas do booleano `consolidada`,
que foi removido de `AulaVM`; tela de aulas com coluna de situação, filtro (+ "Pendentes de
fechamento"), aviso de pendentes e botões por situação e perfil (Reabrir só Admin), com
confirmação nas três operações; chamada somente leitura para Consolidada e Não realizada e
destaque "Sem registro" nas linhas apontadas pelo 400 da consolidação; menu lateral por perfil
(Usuario vê só Painel, Minhas Turmas e Meus Dados). Sem alteração de API.

**Ocorrência.** O pacote veio com 17 dos 18 arquivos — `TagSituacaoAula.spec.ts` faltou e foi
entregue em seguida; com ele a contagem fechou nos 21 casos previstos.

**Validação automatizada** (ambiente reproduzido com `npm ci` a partir do package-lock.json):
`vue-tsc -b` limpo (specs incluídos) · `npm run test` = **3 arquivos, 21 casos verdes**
(situacaoAula 12, TagSituacaoAula 5, MenuLateral 4) · `vite build` ok.

**Testes manuais (front + API, Professor "Paula" e Admin):**

1. Listagem com tags por situação, "Pendente" + aviso com contagem, filtro "Pendentes de
   fechamento" funcionando (RF31/RNF 31.3). ✔
2. Consolidar com chamada incompleta: toast "Chamada incompleta" e linhas destacadas com a tag
   "Sem registro" na chamada; após completar e salvar, consolidação → somente leitura
   (CSU11 FA1). ✔ (print CSU11-FA1-chamada-sem-registro.png)
3. Não realizada: tag própria, botões de fechar somem, chamada somente leitura com mensagem
   específica (CSU11 FA2). ✔
4. Reabrir: Professor não vê o botão; Admin reabre com confirmação e os registros de presença
   permanecem marcados na chamada (RNFs 33.5/33.6, CSU11 FA3). ✔
5. Menu por perfil: Usuario só com Painel/Minhas Turmas/Meus Dados; Professor sem
   Pessoas/Usuários (Plano 7.2). ✔

**Prints (capítulo de Resultados):** `docs/evidencias/RF31-CSU09-listagem-tres-tags.png` e
`docs/evidencias/CSU11-FA1-chamada-sem-registro.png`.

## Etapa 4.1 — Relatórios só com aulas Consolidadas (RNFs 35.5/37.5/38.4, RF36, CSU07) (30/09/2026)

**Escopo.** Os cinco relatórios (frequência da turma, painel de acompanhamento, ranking de
faltas, resumo do dia e minha frequência) passam a calcular exclusivamente sobre aulas
Consolidadas — o serviço anterior não aplicava nenhum filtro de situação. Aulas Não
realizadas ficam fora de tudo; aulas Em aberto com data já ocorrida (mesma regra de
`CalcularPendenteFechamento`, UTC) são devolvidas à parte em `aulasPendentes[]`, respeitando
o período consultado. O resumo do dia ganhou `totalAulasConsolidadas/NaoRealizadas/Pendentes`,
as listas `aulasPendentes[]` e `aulasNaoRealizadas[]` e, por turma, as contagens por situação
com `pendenteFechamento`. `IRelatorioEbdServico` e o controller não mudaram de assinatura —
o front atual segue funcionando; as telas são a 4.2. Sem migration.

**Testes automatizados** — `dotnet test`: **81 aprovados, 0 falhas** (70 + 11):
`RF35_RelatorioTests` (8) — semente com 2 Consolidadas, 1 Não realizada, 1 Em aberto vencida
com chamada lançada e 1 futura prova que a presença em aula não consolidada não conta, que
só a vencida vira pendente, o caso "Em aberto de hoje não é pendente" e o 403 do resumo do
dia para Professor (RNF 38.1); `RF27_MatriculaTests` (3) — movido da proposta da 3.1: 400 do
serviço, violação do índice único filtrado no banco e rematrícula após inativação.

**Testes manuais (Swagger, Admin, turma 4, período 01/08–30/09):**

1. Frequência da turma: `totalAulas` contando só Consolidadas e `aulasPendentes` com matéria
   e professor preenchidos. ✔
2. Resumo do dia 29/09: a aula Não realizada (id 23) em `aulasNaoRealizadas`, fora dos
   totais; contagens por situação na turma. ✔
3. (coberto pelo teste automatizado `ResumoDia_EmAbertoDeHoje_NaoEhPendente`)
4. Antes/depois da reabertura da aula 22 (06/09): totalAulas 8→7, percentual geral
   56,25%→57,14%, Ana 75%→71,43% (estava presente), Pedro 37,5%→42,86% (estava ausente);
   a aula entrou em `aulasPendentes`; reconsolidada, os números voltaram. ✔
   (prints RNF-35.5-frequencia-antes-reabertura.png e
   RNF-35.5-33.6-frequencia-depois-reabertura.png)

## Etapa 4.2 — Relatórios no front: pendentes, situação no resumo e exportação só para gestão (01/10/2026) · fecha a Etapa 4

**Escopo.** Gate de exportação num componente único (`BotoesExportacao`: CSV e Imprimir só
para Admin, Pastor e Superintendente — RNFs 35.4/36.4/37.4); `ListaAulasPendentes`
reutilizado nas abas Frequência, Acompanhamento e Ranking, em Minha Frequência e no Resumo
do dia (variante "Não realizadas"); Resumo do dia com tags de situação por turma, colunas
por situação e rodapé "Totais (só aulas Consolidadas)" (RNF 38.4 / Plano 6.10); rótulos
"Aulas consolidadas" nos cards. Conferência do RF3 encontrou o painel do usuário comum
calculando frequência sobre todos os registros do histórico, inclusive de aulas Em aberto;
correção: o histórico (RF34) passou a informar `situacaoAula` (campo aditivo na API, com
asserção no teste existente) e o painel conta só Consolidadas, mantendo o histórico completo
com a tag da aula. Sem migration, sem rota nova.

**Testes automatizados** — `dotnet test`: **81** (o RF34 ganhou uma asserção) ·
`npm run test`: **28 casos, 4 arquivos** (21 + `BotoesExportacao.spec` com 7) ·
`vue-tsc -b` limpo · `vite build` ok.

**Testes manuais:**

1. Exportação por perfil: Admin vê Imprimir + CSV em todas as tabelas; Paula (Professor) vê
   as mesmas abas sem nenhum botão de exportação e sem a aba Resumo do dia (RNF 38.1). ✔
   (print RNF-35.4-professor-sem-exportacao.png)
2. Bloco âmbar "Pendentes de fechamento (2)" nas abas de turma, com data/matéria/professor;
   cards "Aulas consolidadas". ✔
3. Resumo do dia 29/09: tags "1 não realizada(s)" e "1 pendente(s) de fechamento" na turma,
   rodapé só de Consolidadas e as duas listas abaixo da tabela. ✔
   (print RNF-38.4-resumo-dia-situacoes.png)
4. Minha frequência do aluno: "Aulas consolidadas" e pendentes à parte (CSU07). ✔
5. Painel do aluno: 4 cards contando só registros de aulas Consolidadas (5 presenças em 12)
   e coluna "Aula" com a tag de situação; o registro da aula Em aberto de 29/09 aparece no
   histórico e não entra no percentual. ✔ (print RF3-painel-aluno-consolidadas.png)

**Observação registrada para a Etapa 6** (junto com o diálogo de Pessoas): o painel do aluno
usa o histórico completo (sem recorte de período) enquanto Minha Frequência usa 90 dias —
por isso percentuais podem diferir (42% × 30% nos dados de teste); o rótulo "3 Meses" da
tabela do painel não corresponde ao filtro real. Alinhar período ou rótulo.

## Etapa 5.1 — Termo de Uso e Sigilo na API (RF42/RF43) (01/10/2026)

**Escopo.** Texto do termo versionado em código (`Dominio/Termos`), v1.0 vigente desde
01/10/2026, hash SHA-256 normalizado (independente de CRLF):
`9833a0509a1ec51b421e9ddb53d0d05f0dd338bcf8533628ce1d025ddc61ab92`. O texto é de autoria do
autor, com quatro ajustes estruturais do desenvolvimento (metadados e checkbox fora do texto
hasheado; hash e IP citados no parágrafo do registro) e quatro adições incorporadas antes da
aplicação: menção nominal à Lei nº 13.709/2018 (LGPD), compromisso 8 sobre dados de crianças
e adolescentes, cláusula de descumprimento e direitos dos titulares via canal da igreja —
esta última redigida genérica para o texto fixo seguir sem nada dinâmico (RNF 42.7; a
identificação da igreja e os canais vêm do cadastro, exibidos à parte). Endpoints
`GET /api/termo/vigente` (público; com sessão ou ?token= do convite devolve a igreja) e
`POST /api/termo/aceitar` (Meio = Login, idempotente por versão); gates nas RNFs 1.5
(registrar-admin, em transação explícita) e 40.5 (primeiro-acesso, sem consumir o convite
no 400); `termoPendente` no login (2.5/13.4); `aceiteTermo` em meus-dados e usuários (RF43);
registro imutável com versão, hash, meio, IP e data (42.2/42.3/42.5/42.8). Sem migration
(a tabela AceitesTermo existe desde a Etapa 1).

**Testes automatizados** — `dotnet test`: **98 aprovados, 0 falhas** (81 + 17 de
`RF42_TermoTests`; `Etapa0_FumacaTests`, `RF2_TokenSomenteNoCookieTests` e
`RF39_RF40_ConviteTests` passaram a enviar `AceiteTermoVersao` — o gate em ação).

**Testes manuais (Swagger):**

1. `termo/vigente` sem sessão: v1.0, hash conferido, igreja null; logado: igreja com
   nome/e-mail/telefone (RNF 42.7). ✔
2. `registrar-admin` sem aceite → 400 e nada criado; com `"1.0"` → 200,
   `termoPendente: false` (RNF 1.5/42.1). ✔ (print RNF-42.1-400-cadastro-sem-aceite.png)
3. Login de usuária antiga → `termoPendente: true`; aceitar "0.9" → 400; "1.0" → 200
   `meio: Login`; novo login → false; meus-dados e usuários com `aceiteTermo` (2.5/13.4/RF43). ✔
4. `primeiro-acesso` sem aceite válido → 400 e o convite continuou utilizável; com "1.0" →
   200 e senha definida (RNF 40.5). ✔
5. Banco: registros em AceitesTermo com os três meios (CadastroInicial, Login,
   PrimeiroAcesso), hash da v1.0 e IP (42.2/42.5/42.8). ✔
   (print RF42-registro-aceite-hash-meio-ip.png)

**Decisão registrada:** aprovado o filtro global que bloqueia as demais rotas autenticadas
(403) enquanto não houver aceite vigente — entra na 5.2 com teste.

**Aviso até a 5.2:** cadastro inicial e primeiro acesso pelas telas atuais recebem o 400 do
gate (o front ainda não envia o aceite); não demonstrar esses fluxos pelo front.

## Etapa 5.2 — Tela do Termo de Uso e gate global de aceite (RF42, RF43, RNF 2.5, RNF 42.1) — 03/10/2026

**Escopo.** Exigência de aceite da versão vigente do Termo de Uso e Sigilo em todo o sistema:
filtro global na API (`ExigeAceiteTermoFiltro`) devolvendo 403 `{ mensagem, termoPendente: true }`
para contas autenticadas sem aceite, dispensando apenas endpoints públicos (`[AllowAnonymous]`)
e os marcados com `[PermitirSemAceiteTermo]` (termo e logout); no front, tela `/termo` com guarda
global de rotas (`?redirecionar=`), termo embutido no cadastro inicial e no primeiro acesso
(botão desabilitado até o aceite — RNF 42.1), situação do aceite em Meus Dados e coluna Termo
na lista de Usuários (RF43).

**Correção durante a conferência.** O teste `Login_ComCookieDeContaPendente_NaoEhBloqueado`
falhou na primeira execução (Expected: OK / Actual: Forbidden): os endpoints públicos do
`AuthController` eram públicos por omissão (sem `[Authorize]`) e não tinham `[AllowAnonymous]`,
metadado que o filtro usa para dispensá-los. Adicionado `[AllowAnonymous]` às quatro actions
públicas (registrar-admin, login e primeiro-acesso GET/POST); os 108 testes passaram.

**Testes automatizados.**

- API (xUnit): `dotnet test` → **108 aprovados** (98 anteriores + 10 do novo `RF2_GateTermoTests`:
  403 com termoPendente em rotas autenticadas; escrita bloqueada antes de executar; termo e logout
  liberados; liberação imediata após o aceite na mesma sessão; conta com aceite não afetada;
  rotas públicas não exigem aceite; re-login com cookie de conta pendente não é bloqueado).
- Front (Vitest): `npm run test` → **45 aprovados em 7 arquivos** (17 novos: `autenticacaoStore.spec`
  com termoPendente persistido, `guardas.spec` com redirecionamento para /termo, `TermoUsoSigilo.spec`
  com cabeçalho dinâmico e checkbox controlando o botão). `vue-tsc -b` sem erros; `npm run build` ok.

**Testes manuais.**

1. Login de conta sem aceite → levado a `/termo?redirecionar=...`, sem menu; tentativa de navegar
   para `/pessoas` pela barra de endereço volta para `/termo`; sair e entrar de novo mantém a
   pendência. Evidências: `RF42-tela-termo.png`, `RNF-2.5-redirecionamento-termo.png`.
2. Com conta pendente, chamada direta à API pelo console (`GET /api/meus-dados`) → 403 com
   `{ mensagem, termoPendente: true }`. Evidência: `RNF-2.5-403-termo-pendente.png`.
3. Aceite na tela `/termo` → painel liberado na mesma sessão; Meus Dados exibe
   "Versão 1.0, aceito em 03/10/2026 (após o login)". Evidência: `RF43-meus-dados-termo.png`.
4. Cadastro inicial com termo embutido: cabeçalho acompanha o nome da igreja digitado; botão
   "Concluir Cadastro" desabilitado até marcar o aceite; ao concluir, entra direto no painel
   (aceite registrado no cadastro). Evidência: `RNF-42.1-concluir-desabilitado.png`.
5. Convite de primeiro acesso em janela anônima: termo exibido com a igreja do convite (via token,
   sem login); aceite obrigatório para definir a senha; login seguinte entra direto no painel.
   Evidência: `RF42-primeiro-acesso-termo.png`.
6. Lista de Usuários com coluna Termo: "Aceito v1.0" (com data no tooltip) e "Pendente" por conta.
   Evidência: `RF43-usuarios-coluna-termo.png`.

## Etapa 5.3 — Fluxos E2E com Playwright (E2E-01 a E2E-03) — 06/10/2026

**Escopo.** Os três fluxos ponta a ponta do Plano de Desenvolvimento (7.3), num Chromium real
contra a API no ambiente E2E e o front servido pelo Vite: E2E-01 cadastro inicial com termo,
E2E-02 login com termo pendente e E2E-03 ativação por convite. Semente pela própria API
(igreja nova a cada execução — sem SQL e sem limpeza de banco), `storageState` de Admin e
Professor gravados para os fluxos das próximas etapas, seletores por `data-testid`.

**Ambiente.** Banco dedicado `koinoniahub_e2e` (PostgreSQL local; o repositório traz
`docker-compose.e2e.yml` como alternativa na porta 5433), ambiente `E2E` da API
(`appsettings.E2E.json`, perfil `e2e`). Divergências com o esboço do Plano registradas em
`docs/decisoes/etapa-5.md`: `baseURL` http (Vite sem certificado, proxy para a API https) e
ambiente `E2E` em vez de `Testing` (nome já usado pela fábrica dos testes xUnit).

**Execução (06/10/2026).**

- `npm run test:e2e` → **3 passed** (chromium, 1 worker): E2E-01 (1,4s), E2E-02 (1,5s),
  E2E-03 (2,6s). Repetido com `--headed`: 3 passed.
- Suítes existentes inalteradas: `dotnet test` 108; Vitest 45; `vue-tsc -b` e build limpos.
- O que cada fluxo provou: E2E-01 — botão de conclusão desabilitado até o aceite (RNF 42.1),
  cabeçalho do termo acompanhando a igreja digitada (RNF 42.7), entrada direta no painel e
  Meus Dados com "no cadastro inicial"; E2E-02 — conta pendente presa em /termo sem menu,
  403 `{termoPendente:true}` em chamada direta à API (RNF 2.5), aceite liberando a mesma
  sessão e "após o login" em Meus Dados; E2E-03 — termo com a igreja do convite antes de
  qualquer login (via token), aceite obrigatório para definir a senha (RNF 40.5), link de uso
  único (RNF 40.1) e login final sem pendência, com "no primeiro acesso" em Meus Dados.

**Evidência.** `E2E-termo-3-fluxos-verdes.png` (relatório HTML do Playwright).

## Etapa 6.1 — Acabamento de UI (RNF 32.5, RNF 41.3, RF3/RF6, RF34) — 07/10/2026

**Escopo.** Itens de acabamento já previstos no Plano (seção 5, Etapa 6): dica da RNF 32.5 na
chamada; sinalização das linhas de importação ignoradas por nome repetido sem e-mail (RNF 41.3,
`paraConferencia` na API e tag "Conferir" na tela); painel do usuário comum com a mesma janela
(90 dias) e a mesma regra (só Consolidadas) de Minha Frequência, com rótulo "3 Meses" corrigido;
período visível em Minha Frequência; diálogo de histórico de Pessoas com totais só de aulas
Consolidadas e situação da aula por linha. Cálculo dos indicadores centralizado em
`dominio/indicadoresPresenca.ts`. Tela do termo (RNF 42.7) revisada sem alteração.

**Testes automatizados.**

- API (xUnit): `dotnet test` → **108 aprovados** (sem teste novo; `RF41_ImportacaoTests` ganhou
  asserções sobre `ParaConferencia`: nome repetido sem e-mail sinalizado, e-mail repetido não).
- Front (Vitest): `npm run test` → **54 aprovados em 9 arquivos** (45 + `pessoasServico.spec` com
  4 casos do parser do resultado de importação, Plano 7.2 + `indicadoresPresenca.spec` com 5 casos:
  só Consolidadas, percentual nulo sem aula, janela de 90 dias e limites). `vue-tsc -b` e build limpos.

**Testes manuais.**

1. Chamada de aula Em aberto: dica da RNF 32.5 exibida uma vez acima da tabela, com o texto do
   Plano 6.10, e placeholder "Justificativa administrativa (opcional)"; em aula Consolidada a dica
   não aparece. Evidência: `RNF-32.5-dica-observacao.png`.
2. Importação de CSV com três linhas (nome existente sem e-mail; e-mail já cadastrado; pessoa nova):
   resumo "Linhas lidas 3 · Criadas 1 · Ignoradas 2 · Erros 0 · Para conferência 1", toast "Linhas
   para conferência", linha sinalizada destacada no topo com "Ignorado" + "Conferir"; e-mail repetido
   só "Ignorado". Evidência: `RNF-41.3-importacao-conferir.png`.
3. Painel do usuário comum × Minha Frequência: cards rotulados "(últimos 90 dias)", mesmo período
   nas duas telas (08/07/2026 a 07/10/2026) e os mesmos números após a consolidação da aula
   (1 consolidada, 1 presença, 0 faltas, 100%); antes da consolidação a aula aparecia no histórico
   como "Em aberto" sem entrar nos totais. Evidência: `RF3-painel-aluno-90-dias.png`.
4. Pessoas → histórico: registro de aula Em aberto aparece com a tag "Em aberto", conta em
   "Registros no histórico" e não em Presenças/Faltas/Frequência. Evidência:
   `RF34-historico-pessoas-consolidadas.png`.

## Etapa 6.2 — Editar Dados da Igreja: RF44 / CSU24 (07/10/2026) · fecha a Etapa 6

**Escopo.** Requisito incluído pelo autor em 06/10/2026 (motivação: o canal de contato exibido
no cabeçalho do Termo de Uso e Sigilo, RNF 42.7). API: `PUT /api/igrejas/{id}` restrito ao
perfil Admin e à própria igreja do token, com validação pelo DTO (`[Required]` no nome,
`[EmailAddress]`, tamanhos); o serviço altera só os cinco campos do requisito (nome, cidade,
UF, e-mail, telefone), sem tocar Endereco/CEP/CNPJ/LogoUrl nem os registros de aceite;
`AtualizadoEm` preenchido pelo contexto. Front: rota `/igreja` (só Admin), item "Igreja" no
menu abaixo de Usuários e tela `PaginaIgreja.vue` no mesmo layout de Meus Dados (Descartar /
Salvar, validação local do nome e do telefone, erros por campo vindos do 400). Decisões em
`docs/decisoes/etapa-6.md`.

**Validação automatizada** (ambiente reproduzido a partir do package-lock.json):
`dotnet test` = **113 aprovados, 0 falhas** (108 anteriores + 5 de `RF44_IgrejaTests`:
200 com os campos alterados e `AtualizadoEm` preenchido; 403 para Professor; 403 para id de
outra igreja com a mensagem de isolamento; 400 com `errors.Nome` e `errors.Email`; termo
vigente refletindo o novo contato sem novo aceite) · `vue-tsc -b` limpo · `npm run test` =
**9 arquivos, 54 casos verdes** (MenuLateral cobre o item "Igreja" só para Admin) ·
`vite build` ok.

**Testes manuais (front em `http://localhost:5173`, API `https://localhost:7054`, Admin e
usuário sem perfil Admin):**

1. Admin → menu **Igreja**: campos carregados do banco; alterados e-mail
   (`secretaria@igreja.teste`), telefone e UF (`ro` → `RO` ao digitar) → **Salvar** → toast
   "Os dados da igreja foram atualizados. O Termo de Uso e Sigilo passa a exibir o novo
   contato." e "Última atualização" preenchida; após F5 os valores permanecem (CSU24 fluxo
   principal). ✔ (print RF44-tela-igreja.png)
2. `/termo` logado: cabeçalho "Canal para assuntos relacionados a dados pessoais" já mostra
   `secretaria@igreja.teste · 69999999999`; faixa "Você já aceitou a versão vigente" e botão
   Voltar — nenhum aceite ficou pendente e Meus Dados segue com o aceite original (RNF 44.5).
   A tela de primeiro acesso de um convite em aberto também exibe o novo contato. ✔
   (print RF44-termo-contato-atualizado.png)
3. Nome em branco → "Informe o nome da igreja." sem chamar a API; e-mail `secretariaigreja.teste`
   → **400** da API, mensagem geral "Alguns campos enviados são inválidos..." e
   "E-mail em formato inválido." abaixo do campo; Descartar restaura os valores (RNF 44.3,
   CSU24 FA1). ✔ (print RNF-44.3-400-email-invalido.png)
4. Usuário sem perfil Admin: item Igreja ausente do menu e `/igreja` digitado na barra
   redireciona ao painel; no Swagger, `PUT /api/igrejas/1` → **403** (RNF 44.1, CSU24 FA2).
   ✔ (print RNF-44.1-403-perfil-sem-permissao.png)
5. Admin no Swagger, `PUT /api/igrejas/999` → **403** `{"mensagem": "Acesso permitido apenas
aos dados da própria igreja."}` — recusa antes de consultar o banco (RNF 44.2). `GET
/api/igrejas/1` confirma os valores gravados e `atualizadoEm`. ✔
   (print RNF-44.2-403-outra-igreja.png)

**Observação.** O `PUT` substitui os cinco campos: um corpo parcial enviado pelo Swagger
(sem `cidade`/`estado`) grava `null` nesses campos. A tela envia sempre os cinco, então o
comportamento só aparece em chamada direta à API; registrado aqui para a 7.3 (tabela
CSU → teste) decidir se vale um teste ou um `[Required]` adicional.

**Prints (capítulo de Resultados):** `docs/evidencias/RF44-tela-igreja.png` e
`docs/evidencias/RF44-termo-contato-atualizado.png` (Figura 34 da monografia tem o protótipo;
este é o resultado real).

**Fechamento da Etapa 6.** 6.1 (acabamento: observação da chamada, "Conferir" na importação,
painel/minha frequência em 90 dias, histórico só de aulas consolidadas) e 6.2 (RF44) validadas:
113 xUnit + 54 Vitest + 3 Playwright, build limpo. Próxima: Etapa 7 (7.1 FallbackPolicy +
inventário de autorização + verificação de acesso à turma; 7.2 E2E-04..08; 7.3 tabela
CSU → teste).

## Etapa 6.3 — Responsividade: fluxo do professor a 360 px (08/10/2026)

**Escopo.** Leitura operacional dos RNFs "a interface deve ser responsiva" (RF3, RF5, RF6,
RF20, RF26, RF28, RF35, RF36), no recorte aprovado na `avaliacao-pre-etapa7.md` (seção 1):
ponto de corte 768 px; até ele a barra lateral sai do fluxo e o mesmo `MenuLateral` abre num
`Drawer` pelo botão da `BarraTopo`; telas do fluxo do professor e do aluno sem rolagem
horizontal da página a 360 × 640 (grades fluidas, colunas secundárias ocultas no celular,
alvo de toque de 40 px na caixa de presença); telas administrativas apenas utilizáveis com
rolagem. Só front e E2E; a API não muda. Decisões em `docs/decisoes/etapa-6.md`, 1.3.

**Validação automatizada** (ambiente reproduzido a partir do package-lock.json):
`vue-tsc -b` limpo · `npm run test` = **10 arquivos, 57 casos verdes** (54 + 3 de
`LayoutPrincipal.spec`: menu de desktop no DOM e Drawer fechado; o botão abre o Drawer com
os itens do perfil; navegar fecha) · `vite build` ok · `dotnet test` = **113** (inalterado) ·
`npm run test:e2e` = **8 aprovados** (3 no projeto `chromium` + 5 no projeto `mobile`:
E2E-01, E2E-02 com conta pendente própria e o novo **E2E-M1**, 3 casos — login a 360 px;
professor: painel → Drawer → Minhas Turmas → aulas → chamada → presenças → Meus Dados;
aluno: painel → Minha Frequência — com `semRolagemHorizontal` em cada tela e alvos de toque
verificados). O E2E rodou contra o PostgreSQL local no ambiente `E2E`.

**Testes manuais (DevTools a 360 × 640; Admin, Professor "Paula" e aluno):**

1. Desktop intacto acima de 768 px: barra lateral, sem botão ☰, proporções de Meus Dados e
   painéis iguais às anteriores. ✔
2. Drawer: barra lateral oculta, ☰ no cabeçalho, e-mail com reticências; o Drawer abre com os
   itens do perfil (itens altos) e fecha ao navegar; em 640 × 360 continua em Drawer. ✔
3. Chamada a 360 px (CSU10): Minhas Turmas só com Turma/Vínculo/Ações; aulas só com
   Data/Situação/Ações; diálogo de nova aula cabe; na chamada, Aluno e Presente visíveis sem
   rolar, Observação rolando dentro da tabela; salvar e consolidar com diálogos cabendo na
   tela. ✔
4. Aluno a 360 px (CSU07): painel com os indicadores em duas colunas; Minha Frequência com
   cartões em duas colunas e tabela rolando internamente. ✔
5. Administrativa (Pessoas) a 360 px: abre, tabela rola dentro do container, diálogo de nova
   pessoa a 96 vw — utilizável, sem polimento (fora do critério). ✔

**Avaliação do autor (09/10/2026).** O critério foi atendido, mas o resultado é uma tela
web comprimida, não uma interface de celular: ação principal cortada na tabela "Aulas em
aberto" do painel do professor, três ou quatro botões só com ícone por aula, ações cortadas
em Matrículas e Matérias. Como é o fluxo que o professor e o PO avaliam na 5.1, a Etapa 6.4
passa a incluir o fluxo operacional no celular (cartões com botão nomeado, chamada tocável
com barra fixa, Página da Turma como centro), com critério estrito — ver
`RECORTE-6.4-CELULAR.md` em `docs/decisoes/`.

**Prints (gerados pelo projeto `mobile` e copiados para `docs/evidencias/`):**
`RNF-responsividade-{login, termo, painel-professor, menu-drawer, minhas-turmas, aulas-turma,
chamada, presencas-aula, meus-dados, painel-aluno, minha-frequencia}.png`. Serão regenerados
na 6.4-C.

## Etapa 6.4 — Página da Turma e fluxo operacional no celular (09 a 14/10/2026) · fecha a Etapa 6

**Escopo.** RF20 (Tela de Turma) e a leitura estrita dos RNFs "a interface deve ser responsiva"
para as telas operacionais, conforme `docs/decisoes/recorte-6.4-celular.md` (aprovado em
09/10/2026 após a avaliação do autor sobre a 6.3): cada item de lista é um cartão com uma ação
principal nomeada, a chamada é tocável com barra Salvar · Consolidar, e nenhuma tela operacional
tem rolagem horizontal, nem interna, a 360 × 640. Cinco entregas: **6.4-A** (09/10) peças
reutilizáveis, Página da Turma com abas como rotas filhas, aba Aulas, Minhas Turmas e painel do
professor em cartões; **6.4-B** (10/10) chamada enxuta nos dois layouts, Presenças, painel do
aluno e Minha Frequência em cartões, voltar hierárquico, "Minha frequência" só para o vínculo
Aluno, Consolidar salvando as pendentes; **corretiva** (12/10) Chamada e Presenças no desktop em
grade de cartões, barra `sticky` sem cobrir o rodapé, severidade `warn` do PrimeVue 4, clique
duplo ignorado; **ajuste** (13/10) bloqueio de 500 ms após a gravação, `overflow` do `<main>` só
retirado na rota da chamada; **6.4-C** (14/10) abas Alunos e Matérias em cartões, contadores do
cabeçalho recontados a partir das abas, "Sem registro" visível desde a carga (campo aditivo
`Registrado` na chamada completa), Consolidar nomeando quem será gravado como ausente (CSU11,
passo 2 com o texto novo), confirmação ao sair ou recarregar com alterações não salvas, diálogos
e avisos globais no tema e dentro da tela do celular, E2E-M1 em modo estrito. Decisões em
`docs/decisoes/etapa-6.md`, 1.4 a 1.8.

**Validação automatizada (14/10/2026, ambiente do autor):** `dotnet build` e `dotnet test` =
**114 aprovados** (113 + `ChamadaCompleta_InformaRegistrado_PorMatricula`) · `vue-tsc -b` limpo ·
`npm run test` = **19 arquivos, 102 casos** (66 → 79 na 6.4-B: `chamada.spec` 6, `LinhaChamada.spec`
7; → 82 na corretiva: `usarAcaoUnica.spec` 2, `TagSituacaoAula.spec` +1; → 86 no ajuste:
`usarAcaoUnica` +1, `LayoutPrincipal.spec` +1, `PaginaChamadaAula.spec` 2; → 102 na 6.4-C:
`PaginaChamadaAula.spec` 10, `chamada.spec` 9, `PaginaMatriculasTurma.spec` 2,
`PaginaMateriasTurma.spec` 3) · `npm run build` ok · `npm run test:e2e` = **10 aprovados**
(3 no projeto `chromium` + 7 no projeto `mobile`: E2E-01, E2E-02, os três casos do **E2E-M1**
reescrito — professor: painel → Drawer → Minhas Turmas → Página da Turma com abas Alunos,
Matérias e Aulas em cartões → chamada pelo cartão da aula → "Sem registro" desde a carga e baixa
ao tocar → toque no meio da linha → dois toques crus em Salvar geram um único POST e um único
aviso → confirmação do Consolidar com os nomes dentro da tela → aula consolidada, barra some →
confirmação ao sair → Presenças em cartões → Meus Dados; aluno: painel → Minha Frequência — tudo
com `semRolagemHorizontal` **estrito** (nenhum container interno rola; só a faixa de abas) — e os
dois casos do **E2E-M2**: largura do documento e do `body` = 360 em Pessoas, Usuários, Igreja,
Relatórios, Turmas EBD, Importação aberta, nas quatro abas da turma, na Chamada e nas Presenças).
O E2E rodou contra o PostgreSQL local no ambiente `E2E`.

**Testes manuais (Admin, Professor "Paula" e aluno; 1366 × 768, 1920 × 1080 e 360 × 640):**

1. Página da Turma (RF20): `/departamentos/{id}` abre na aba Aulas; cabeçalho com nome, tipo,
   situação, contadores e equipe; aba Atribuições só para a gestão; voltar leva a Turmas EBD
   (gestão) ou Minhas Turmas (professor); as URLs antigas das abas continuam válidas. ✔
2. Celular: Painel do professor, Minhas Turmas, abas Aulas, Alunos e Matérias em cartões com
   ação principal nomeada e ⋮; "Abrir turma" em todo lugar; aluno sem "Abrir turma" e com
   "Minha frequência" só no vínculo Aluno; contadores do cabeçalho mudam após matricular ou
   criar matéria sem recarregar. ✔
3. Chamada nos dois layouts: topo curto com voltar para a turma (nome da turma no rótulo),
   "Presentes N de M", ⋮ com Recarregar e Limpar, visitantes numa linha, aviso da RNF 32.5 só
   ao abrir "+ observação", uma mensagem por vez; desktop em grade de cartões (3 colunas a
   1366, 5 a 1920) com "Alterações não salvas" / "Tudo salvo" na barra; celular em uma coluna;
   toque em qualquer ponto da linha alterna a presença; barra Salvar · Consolidar visível ao
   rolar e parando acima do rodapé no fim, nas três larguras. ✔
4. Consolidar: com alteração pendente ou aluno sem registro, a confirmação nomeia quem será
   gravado como ausente (lista com rolagem própria, cabendo a 360 px) e "Salvar e consolidar"
   grava e consolida; sem pendência, confirmação simples sem gravação antes; aula consolidada
   fica somente leitura e a barra some; Presenças mostra os listados como Ausente. ✔
5. CSU11 FA1 (recusa da API): aula com aluno matriculado depois da chamada salva → uma única
   mensagem âmbar, linha destacada "Sem registro", nenhuma mensagem vermelha junto. ✔
6. Alterações não salvas: voltar → "Sair da chamada" / "Continuar na chamada"; F5 → aviso do
   navegador; ⋮ → Recarregar → confirmação; sem alteração, nada pergunta. ✔
7. Clique duplo em Salvar (desktop e celular): um único aviso "Chamada salva com sucesso". ✔
8. Etiquetas âmbar (Pendente, Sem registro, Pastor, Convite pendente) e botão Reabrir em âmbar;
   Auxiliar em etiqueta escura. ✔
9. Diálogos e avisos (Consolidar, Marcar não realizada, Reabrir, Remover matrícula, Recarregar
   chamada): título em Merriweather verde escuro, ícone em círculo, botão de confirmação verde,
   recusa contornada; a 360 px dentro da tela com botões empilhados. ✔
10. Telas administrativas a 360 px (Pessoas, Usuários, Igreja, Relatórios, Turmas EBD, Importação,
    abas da turma): `document.documentElement.scrollWidth` = 360; o que for mais largo rola
    dentro do conteúdo; a barra do topo não se move. ✔
11. Painel do professor sem aulas em aberto: "Nenhuma aula em aberto no momento.", sem paginador. ✔

**Prints em `docs/evidencias/`:** `RF20-pagina-turma-desktop.png`; `RF32-chamada-desktop-1920.png`,
`RF32-chamada-alteracoes-nao-salvas.png`, `RF32-chamada-toque-duplo-360.png`,
`RF32-recarregar-com-alteracoes-360.png`; `RF33-chamada-consolidada-desktop.png`;
`RF34-presencas-desktop.png`; `RNF-32.5-dica-observacao.png`; `RNF-32.6-sem-registro-desde-a-carga.png`,
`RNF-32.6-sem-registro-baixa-ao-marcar.png`; `CSU11-FA1-sem-registro-desktop.png`,
`CSU11-passo2-consolidar-nomes-360.png`; `RNF-dialogos-tema-360.png`, `RNF-dialogos-tema-1366.png`;
`RNF-responsividade-usuarios-360.png`, `RNF-responsividade-pessoas-360.png`; e os 15 gerados pelo
projeto `mobile`: `RNF-responsividade-{login, termo, painel-professor, menu-drawer, minhas-turmas,
aulas-turma, turma-alunos, turma-materias, chamada, chamada-consolidar-sem-registro,
chamada-consolidada, presencas-aula, meus-dados, painel-aluno, minha-frequencia}.png`.

**Registro para o Plano:** aplicado na v1.4 (`docs/plano-de-desenvolvimento.md`).

---

## Etapa 6.5 — RF45: editar e excluir aula Em aberto (15/10/2026)

**Escopo.** Seção 6.14 do Plano: `PUT /api/aulas/{id}` e `DELETE /api/aulas/{id}` com a
autorização do cadastro (RNF 45.1: gestão ou atribuição ativa na turma); edição só de aula Em
aberto (45.2), com matéria da mesma turma e professor com atribuição ativa de Professor (45.4);
exclusão só de aula Em aberto e sem nenhum registro de presença (45.5, a mesma condição da 33.7).
Campos aditivos `Conteudo` e `PossuiPresencas` em `AulaRespostaDto` (decisão do autor em
15/10/2026): o primeiro para o diálogo de edição vir preenchido, o segundo para a tela não
oferecer Excluir quando já há chamada (CSU09 FA3). Na aba Aulas, "Editar aula" e "Excluir aula"
entram no ⋮ dos dois layouts, com `visible` pela situação e pelas presenças; o diálogo de nova
aula é reaproveitado em modo edição e ganha o campo "Conteúdo (opcional)" nos dois modos
(RF45 e CSU09 FA1 declaram o conteúdo editável); a exclusão passa pela confirmação comum do
App.vue, sem `acceptClass`. Nenhuma tabela nova. Primeira entrega produzida no Claude Code.
Decisões em `docs/decisoes/etapa-6.md`, 1.9.

**Validação automatizada (15/10/2026, ambiente do autor):** `dotnet build` e `dotnet test` =
**123 aprovados** (114 + `RF45_EditarAulaTests`, 9 casos: editar Em aberto → 200 e GET reflete;
Consolidada → 400; Não realizada → 400; matéria de outra turma → 400; professor sem atribuição
de Professor → 400; Professor de outra turma → 403; excluir sem presenças → 204 e some da
listagem; excluir com presenças → 400 e `possuiPresencas` verdadeiro na listagem; excluir
Consolidada → 400) · `vue-tsc -b` limpo · `npm run test` = **20 arquivos, 105 casos** (102 +
`PaginaAulasTurma.spec` 3: visibilidade de Editar/Excluir nos três estados, edição pelo diálogo
com `atualizarAula` e recontagem do cabeçalho, exclusão pela confirmação comum) · `npm run build`
ok · `npm run test:e2e` = **10 aprovados** (3 no projeto `chromium` + 7 no projeto `mobile`),
E2E-M1 em modo estrito sem alteração. O E2E rodou contra o PostgreSQL local no ambiente `E2E`.

**Testes manuais (professora Paula e Admin; desktop e 360 × 640):**

1. Nova aula com Tema e Conteúdo: aviso "Aula criada com sucesso.", aula Em aberto na lista. ✔
2. ⋮ → Editar aula (CSU09 FA1): diálogo "Editar aula" preenchido com data, matéria, professor,
   tema e conteúdo; tema alterado e salvo → "Aula atualizada com sucesso.", lista atualizada. ✔
3. Validação (RNF 45.3): matéria limpa → aviso âmbar "Selecione a matéria.", diálogo aberto. ✔
4. ⋮ → Excluir aula sem chamada (CSU09 FA2, RNF 45.5): confirmação "Excluir aula" com data e
   matéria, botão "Excluir" verde e "Cancelar" contornado → "Aula excluída com sucesso.", aula
   some, contador "Aulas em aberto" do cabeçalho recontado. ✔
5. Aula Em aberto com chamada salva (CSU09 FA3): ⋮ oferece "Editar aula" e não oferece
   "Excluir aula". ✔
6. Aula Consolidada ou Não realizada (RNF 45.2): ⋮ sem Editar e sem Excluir. ✔
7. Recusa direta pela API (CSU09 FA3 passo 2, Swagger como Admin): PUT em aula Consolidada →
   400 "Somente aulas Em aberto podem ser editadas."; DELETE em aula Em aberto com chamada →
   400 "Uma aula com registros de presença não pode ser excluída."; DELETE em aula Consolidada →
   400 "Somente aulas Em aberto podem ser excluídas.". ✔
8. Matéria de outra turma (RNF 45.4): PUT → 400 "A matéria informada não pertence à turma desta
   aula.". ✔
9. Professor sem atribuição na turma (RNF 45.1): PUT e DELETE → 403. ✔
10. Celular a 360 × 640: diálogo de edição com o campo Conteúdo cabendo na tela; confirmação de
    exclusão dentro da tela com botões empilhados e "Excluir" por cima; sem rolagem horizontal. ✔

**Prints em `docs/evidencias/`:** `RF45-dialogo-nova-aula-conteudo.png`, `RF45-editar-aula-desktop.png`,
`RF45-confirmar-excluir-desktop.png`, `RNF-45.5-sem-excluir-com-presenca.png`,
`RNF-45.2-put-consolidada-400.png`, `RNF-45.2-delete-consolidada-400.png`, `RF45-editar-aula-360.png`,
`RF45-confirmar-excluir-360.png`.

**Observação registrada:** para o Professor, `GET /api/aulas/{id}` de aula inexistente devolve
400 (o guarda de acesso lança antes do serviço); para a gestão, 404. Comportamento anterior à
6.5, não alterado; fica para a 7.1.

**Registro para o Plano (v1.5):** seção 4 (linha `api/aulas` e RF45 → Atende), seção 5 (linha
6.5 atendida em 15/10/2026), seção 6.14 (ATENDIDA; 9 casos; exclusão → 204), seção 7 (9 casos),
seção 9 (retirar a linha RF45; acrescentar o CSU09 passo 4 com "e, opcionalmente, o tema e o
conteúdo"). Texto completo no relato da entrega e em `docs/decisoes/etapa-6.md`, 1.9.

## Etapa 6.6 — RF12: parentesco derivado na leitura (16 e 17/10/2026)

**Escopo.** Seção 6.15 do Plano, recorte mínimo: o vínculo é gravado uma única vez; na leitura do
cadastro do parente o sistema apresenta o grau inverso (Pai/Mãe → Filho(a); Filho(a) → Pai, Mãe ou
"Pai ou mãe" pelo sexo da pessoa de origem; Irmão(ã) e Cônjuge simétricos; Responsável →
Dependente; texto livre repetido com "(informado no cadastro de X)"), com `derivado: true`, sem
segundo registro, sem migration e sem mudar o DER (RNFs 12.3/12.4; CSU13 passo 2). A derivada é
omitida quando a pessoa já tem registro explícito com a origem. Pelo lado derivado o `DELETE`
devolve 400 "Este vínculo foi registrado no cadastro de X e só pode ser removido por lá." (CSU13
FA1); na tela a linha derivada tem a tag "derivado", não tem o botão de remover e traz um botão ⓘ
que mostra e esconde a explicação na própria linha (sem tooltip nem Popover: nada montado fora da
tabela; funciona no toque; área de toque de 40 px), além de uma nota abaixo da tabela. Vale para `GET /api/pessoas/{id}/parentescos`
(gestão e Professor) e para o DTO reduzido do `GET /api/pessoas/{id}` (11.3). A `DataTable` do
diálogo de Parentescos foi extraída para `TabelaParentescos.vue` (mesma tabela, sem mudança visual
além da tag e do botão oculto); o diálogo "Responsáveis" do Professor ganhou só a tag.
Decisões em `docs/decisoes/etapa-6.md`, 1.10.

**Validação automatizada (16/10/2026, ambiente do autor):** `dotnet build` e `dotnet test` =
**136 aprovados** (123 + `RF12_ParentescoDerivadoTests`, 13 casos: Pai e Mãe → Filho(a) com o
`Id` da origem e um só registro no banco; Filho(a) por sexo Masculino → Pai, Feminino → Mãe, sem
sexo → "Pai ou mãe"; Irmão(ã) e Cônjuge simétricos; Responsável → Dependente; texto livre
"Padrinho" → "Padrinho (informado no cadastro de Origem …)"; registro explícito inverso omite a
derivada nos dois cadastros; DELETE pelo lado derivado → 400 e registro preservado, pelo lado de
origem → 204 e a derivada some; DELETE de id inexistente → 404; Professor vê a derivada no DTO
reduzido) · `vue-tsc -b` limpo · `npm run test` = **21 arquivos, 111 casos** (105 +
`TabelaParentescos.spec` 6: explícita com botão; derivada com tag "derivado", sem botão, com o ⓘ
fechado e a nota; ⓘ abre a explicação na própria linha e fecha ao clicar de novo; recarga da
lista fecha a explicação; `remover` emite o item; lista vazia) · `npm run build` ok · `npm run test:e2e` =
**10 aprovados** (3 no projeto `chromium` + 7 no projeto `mobile`), E2E-M1 estrito e E2E-M2 sem
alteração. O E2E rodou contra o PostgreSQL local no ambiente `E2E`.

**Testes manuais (17/10/2026; Admin e professora; desktop e 360 × 640):**

1. Pessoas → Parentescos de Maria (Feminino) → João como "Filho(a)" → Adicionar: "Parentesco
   cadastrado com sucesso.", linha "João — Filho(a)" com botão de remover. ✔
2. Parentescos de João: linha "Maria — Mãe" com a tag "derivado", sem botão de remover, ⓘ no
   lugar e nota "Vínculos marcados como derivado foram registrados no cadastro do parente e só
   podem ser removidos por lá." abaixo da tabela. Clicar no ⓘ mostra, abaixo do relacionamento,
   "Vínculo registrado no cadastro de Maria. Para removê-lo, abra os parentescos dessa pessoa.";
   clicar de novo esconde. Fechar o diálogo e navegar: nenhum balão fica na tela. ✔
3. Origem Masculino (Carlos) → João "Filho(a)": em João, "Carlos — Pai" derivado. Origem sem sexo
   (Ana) → João "Filho(a)": em João, "Ana — Pai ou mãe" derivado. ✔
4. Em Maria: Carlos como "Cônjuge" → em Carlos "Maria — Cônjuge" derivado; Ana como
   "Responsável" → em Ana "Maria — Dependente" derivado; "Padrinho" digitado para João → em João
   "Padrinho (informado no cadastro de Maria)" derivado. ✔
5. Em João, o seletor "Parente" ainda oferece Maria; registrar Maria como "Mãe" faz a derivada
   desaparecer e fica só a linha explícita, com botão. ✔
6. Em Maria, remover o "Filho(a)" com João pela lixeira: some em Maria; em João a linha explícita
   "Mãe" permanece. ✔
7. Swagger como Admin: `GET /api/pessoas/{idJoao}/parentescos` → linhas `derivado: true` com `id`
   igual ao do registro de origem; `DELETE /api/pessoas/{idJoao}/parentescos/{esse id}` → 400
   `{"mensagem":"Este vínculo foi registrado no cadastro de Carlos e só pode ser removido por lá."}`. ✔
8. Professora, aba Alunos → Responsáveis de aluno registrado como parente em outro cadastro: tag
   do tipo derivado seguida da tag "derivado"; `GET /api/pessoas/{idAluno}` →
   `parentescos[].derivado` presente. ✔
9. Celular 360 × 640 (passos 2 e 8): diálogos dentro da tela, sem rolagem horizontal da página;
   tags visíveis; toque no ⓘ abre e fecha a explicação. ✔

**Prints em `docs/evidencias/`:** `RF12-parentesco-origem-explicito.png`,
`RNF-12.3-parentesco-derivado-tag.png`, `RNF-12.3-filho-sem-sexo-pai-ou-mae.png`,
`RNF-12.3-delete-derivado-400.png`, `RNF-12.3-responsaveis-professor-derivado.png`,
`RNF-12.3-parentescos-360.png`.

**Corretivas dos testes manuais (17/10/2026):** (1) o balão do `v-tooltip` do ⓘ ficava preso na
tela ao fechar o diálogo ou recarregar a lista com o mouse em cima (montado no `body`, sem
`mouseleave`); (2) a troca por `title` nativo resolvia o balão, mas não funciona no toque. Solução
final: ⓘ é um botão (`type="button"`, `aria-label`, `aria-expanded`, 40 px) que mostra e esconde a
explicação como texto na própria linha. `TabelaParentescos.spec`: 4 → 6 casos; Vitest 109 → 111.

**Registro para o Plano (v1.6):** seção 4 (linha `api/pessoas/{id}/parentescos` e RF12 → Atende),
seção 5 (linha 6.6 atendida em 17/10/2026), seção 6.15 (ATENDIDA; microdecisões), seção 7.1
(13 casos), seção 7.2 (`TabelaParentescos.spec`, 6). Texto completo em `docs/decisoes/etapa-6.md`, 1.10.

---

## Etapa 7.1 — Autorização: perfil × atribuição, FallbackPolicy, `perfilConta` e 404 uniforme (17 e 18/10/2026)

**Escopo.** Seção 6.16 do Plano e decisões do autor: FallbackPolicy `RequireAuthenticatedUser()` em
`Program.cs` (rota sem atributo nasce protegida; públicos com `[AllowAnonymous]`); RNF 21.4 em
`AutorizacaoEbdServico` (ordem única nos três guardas: o recurso existe para a igreja → perfil
administrativo passa → perfil Professor → atribuição ativa; conta de perfil Usuário com atribuição
ativa → 403 "Esta operação exige perfil Professor ou administrativo."); `perfilConta` aditivo em
`AtribuicaoRespostaDto` (null = sem conta) e tags "Sem conta" / "Conta com perfil Usuário — não
acessa a turma" na coluna Pessoa da tela de Atribuições (RNF 21.5; texto curto "Perfil Usuário —
não acessa a turma" abaixo de 768 px); `POST /api/igrejas` removido (a igreja nasce só pelo RF1);
turma, matéria e aula inexistentes → 404 `{ mensagem }` para qualquer perfil
(`RecursoNaoEncontradoException` + `RecursoNaoEncontradoFiltro`, filtro global como o do termo).
Corretivas dos testes manuais (18/10): `GET /api/meus-dados/minhas-turmas` só devolve vínculos de
atribuição para conta Professor ou administrativa (o vínculo Aluno continua) e a rota
`/departamentos/:id` exige perfil gestor (`requerGestor`): perfil Usuário volta a Minhas Turmas com
um único aviso, em vez de carregar a página e receber cinco 403. Decisões em
`docs/decisoes/etapa-7.md`, 1.1.

**Validação automatizada (18/10/2026, ambiente do autor):** `dotnet build` e `dotnet test` =
**146 aprovados** (136 + `RF21_PerfilOperacionalTests`, 10 casos: Usuario com atribuição ativa →
403 com a mensagem da 21.4 em `GET /api/aulas?departamentoId` e em `POST /api/aulas/{id}/presencas`
sem gravar presença; Professor com atribuição → 200; minhas-turmas da conta Usuario só com o
vínculo "Aluno" e do Professor com "Professor"; rota sem atributo → 401 sem sessão e 200 com
sessão; `perfilConta` "Professor", "Usuario" e `null` na mesma listagem; `POST /api/igrejas` → 404
sem gravar; aula, matéria e turma inexistentes → 404 com `mensagem` para Professor e Admin;
`RF45_EditarAulaTests` passou a exigir 404 exato) · `vue-tsc -b` limpo · `npm run test` =
**23 arquivos, 118 casos** (111 + `atribuicoes.spec` 4: sem conta; perfil Usuário; texto curto na
tela compacta; Professor e administrativos sem aviso + `PaginaAtribuicoesDepartamento.spec` 1: tag
só nas linhas sem conta e com perfil Usuário + `guardas.spec` +2: perfil Usuário entrando por uma
aba vai para `/minhas-turmas` com um toast; Professor abre Aulas e continua barrado em Atribuições
sem toast) · `npm run build` ok · `npm run test:e2e` = **10 aprovados** (3 no projeto `chromium` +
7 no projeto `mobile`), E2E-M1 estrito e E2E-M2 sem alteração. O E2E rodou contra o PostgreSQL
local no ambiente `E2E`.

**Dados existentes (Plano 6.16, "antes de subir"):** consulta executada em 17/10/2026 no banco de
desenvolvimento — 3 atribuições ativas de pessoas sem conta e 2 de conta com perfil Usuario (ambas
da mesma pessoa); as duas foram encerradas na limpeza dos testes manuais.

```sql
SELECT i."Nome" AS igreja, d."Nome" AS turma, p."Id" AS pessoa_id, p."Nome" AS pessoa,
       a."Funcao", u."Id" AS usuario_id, u."Email", u."Perfil", u."Ativo" AS usuario_ativo,
       CASE WHEN u."Id" IS NULL THEN 'Sem conta' ELSE 'Conta com perfil Usuario' END AS situacao
FROM "Atribuicoes" a
JOIN "Pessoas"       p ON p."Id" = a."PessoaId"
JOIN "Departamentos" d ON d."Id" = a."DepartamentoId"
JOIN "Igrejas"       i ON i."Id" = d."IgrejaId"
LEFT JOIN "Usuarios" u ON u."PessoaId" = p."Id"
WHERE a."Ativo" = TRUE AND (u."Id" IS NULL OR u."Perfil" = 'Usuario')
ORDER BY i."Nome", d."Nome", p."Nome";
```

**Testes manuais (17 e 18/10/2026; Admin, professora e conta Usuario X; desktop e 360 × 640):**

1. Admin → turma → aba Atribuições → Nova Atribuição → pessoa X (conta de perfil Usuário),
   Auxiliar → Salvar: "Atribuição criada com sucesso." e a linha de X com a tag âmbar "Conta com
   perfil Usuário — não acessa a turma". ✔
2. Nova Atribuição → pessoa sem usuário → Auxiliar: tag "Sem conta"; a linha da professora (conta
   Professor) sem tag. ✔
3. Celular 360 × 640, aba Atribuições: tabela utilizável com rolagem dentro da aba, página sem
   deslizar; tag "Perfil Usuário — não acessa a turma" em no máximo duas linhas abaixo do nome
   (corretiva 2). ✔
4. Como X: painel do aluno abre; `GET /api/aulas?departamentoId=<turma>` → 403
   `{"mensagem":"Esta operação exige perfil Professor ou administrativo."}`. ✔
5. Como X: `POST /api/aulas/{aula Em aberto}/presencas` → 403 com a mesma mensagem; a chamada,
   aberta pelo Admin, continua sem registro. ✔
6. Professora → Minhas Turmas → Abrir turma → aba Aulas lista normalmente (200). ✔
7. Como professora: `GET /api/aulas/999999`, `GET /api/materias/999999`,
   `GET /api/aulas?departamentoId=999999` → 404 com "Aula não encontrada para esta igreja.",
   "Matéria não encontrada para esta igreja." e "Departamento não encontrado para esta igreja.";
   como Admin, também 404. ✔
8. Aba anônima (sem cookie): `GET /api/atribuicoes` → 401. ✔
9. Admin: `POST /api/igrejas` → 404; a rota não aparece mais no Swagger. ✔
10. Como X, após a corretiva 1: Minhas Turmas mostra a turma só como "Aluno" (sem "Abrir
    turma"); digitar `/departamentos/<id>/aulas` devolve a Minhas Turmas com um único aviso "Sem
    permissão". Limpeza: as duas atribuições de teste encerradas. ✔

**Prints em `docs/evidencias/`:** `RNF-21.5-tag-conta-usuario.png`, `RNF-21.5-tag-sem-conta.png`,
`RNF-21.5-atribuicoes-360.png`, `RNF-21.4-403-post-presencas.png`,
`RNF-21.4-minhas-turmas-usuario.png`, `RNF-21.4-404-aula-inexistente.png`,
`RNF-21.4-404-materia-inexistente.png`, `RNF-2.1-401-sem-sessao.png`,
`RF1-404-post-igrejas-removido.png`. O 403 do `GET /api/aulas?departamentoId` (passo 4) não tem
print próprio: a mesma mensagem está no print do passo 5 e no teste
`ListarAulas_UsuarioComAtribuicaoAtiva_Retorna403`.

**Corretivas dos testes manuais (18/10/2026):** (1) conta Usuário com atribuição abria a Página da
Turma por Minhas Turmas e recebia cinco toasts "Sem permissão" — `ListarMinhasTurmasAsync` filtra os
vínculos de atribuição pelo perfil e a guarda `requerGestor` da rota da turma devolve a Minhas
Turmas com um aviso só; xUnit +1, `guardas.spec` +2. (2) a tag a 360 px quebrava uma palavra por
linha — coluna Pessoa com `min-width: 240px`, tag com `white-space: normal` e texto curto abaixo de
768 px; `atribuicoes.spec` +1. Totais: xUnit 145 → 146; Vitest 115 → 118.

**Inventário das evidências de Segurança (capturadas × faltantes), para a tabela RNF → teste →
evidência do capítulo de Resultados (Plano 7.5):**

| RNF | Teste automatizado | Evidência |
| --- | --- | --- |
| 2.1 e x.1 "autenticado" (3.1, 4.1, 5.1, 6.1, 9.2, 19.1, 20.1, 23.1, 26.1, 34.1) | RF21 (401 da FallbackPolicy) | ✔ `RNF-2.1-401-sem-sessao.png` |
| 2.2 / 13.2 / 15.4 / 40.3 BCrypt | RF39_RF40 (hash no banco) | faltante: print da coluna `SenhaHash` (sem e-mail real) |
| 2.3 / 40.4 / 42.4 / 44.4 HTTPS | `UseHttpsRedirection` | faltante: print do redirecionamento de `http://localhost:5054` |
| 2.5 gate do termo | RF2_GateTermoTests | ✔ `RNF-2.5-403-termo-pendente.png`, `RNF-2.5-redirecionamento-termo.png` |
| 2.6 Origin | RF2_OrigemTests | ✔ `RNF-2.6-*.png` (2) |
| 4.2 / 5.2 / 6.2 / 34.3 próprios dados | RF34 | ✔ `RNF-34.3-*.png` (2) |
| 8.1, 9.1, 10.1, 12.1, 17.1, 18.1, 21.1, 22.1, 38.1, 41.1 gestão | `[Authorize(Roles)]`; RF44 (403) | ✔ `RNF-44.1-403-perfil-sem-permissao.png` (exemplo da família) |
| 8.2 perfil **e** atribuição; 21.4 | RF21 (403 em leitura e escrita; minhas-turmas) | ✔ `RNF-21.4-403-post-presencas.png`, `RNF-21.4-minhas-turmas-usuario.png` |
| 21.5 sinalização | RF21 (`perfilConta`); Vitest | ✔ `RNF-21.5-*.png` (3) |
| 11.3 | RF11 | ✔ `RNF-11.3-*.png` (2) |
| 13.1, 14.1, 15.1, 16.1, 39.1 só Admin | `[Authorize(Roles="Admin")]` | faltante: um 403 de Professor em `/api/usuarios` (um print para a família) |
| 14.3 | RF14 | ✔ `RNF-14.3-400-ultimo-admin.png` |
| 15.2 / 39.4 | RF39_RF40 | faltante: convite anterior → 404 após gerar o novo |
| 15.3 | RF39_RF40 (404 da rota) | ✔ `RNF-15.3-404-resetar-senha.png` |
| 17.3 / 44.2 isolamento | RF44 (403); RF33 (reabrir de outra igreja, 404) | ✔ `RNF-44.2-403-outra-igreja.png` |
| 21.3 só Professor como responsável | AulaServico; RF45 | faltante: 400 ao criar aula com auxiliar como professor |
| 24.1 a 33.1, 35.1, 37.1, 45.1 "gestão ou atribuição" | RF11/RF34 (Professor fora da turma); RF21 | ✔ `RNF-11.3-403-fora-da-turma.png`, `RNF-34.3-403-fora-da-turma.png` |
| 404 uniforme (turma, matéria, aula) | RF21 | ✔ `RNF-21.4-404-aula-inexistente.png`, `RNF-21.4-404-materia-inexistente.png` |
| 28.3 | RF28 | ✔ `RNF-28.3-disponiveis-dto-reduzido.png` |
| 33.5 | RF33 | ✔ `RNF-33.5-403-reabrir-professor.png` |
| 35.4 / 36.4 / 37.4 exportação | front | ✔ `RNF-35.4-professor-sem-exportacao.png` |
| 39.2 / 39.3 / 40.1 / 40.2 convite | RF39_RF40 | faltante: token exibido uma vez, hash no banco, mensagem de convite já usado |
| 42.1, 42.3, 43.2 | RF42 | ✔ `RNF-42.1-*.png` (2), `RF42-registro-aceite-hash-meio-ip.png`, `RF43-*.png` (2) |
| 44.1 / 44.2 / 44.3 | RF44 | ✔ `RNF-44.1-*`, `RNF-44.2-*`, `RNF-44.3-*.png` |
| 45.2 / 45.5 | RF45 | ✔ `RNF-45.2-*.png` (2) |
| RF1: `POST /api/igrejas` removido | RF21 (404) | ✔ `RF1-404-post-igrejas-removido.png` |

Faltantes (um print cada, opcionais para a banca, a capturar na 7.3 junto com a tabela CSU →
teste): BCrypt no banco; HTTPS; 403 de Professor em `/api/usuarios`; convite anterior invalidado;
21.3; token exibido uma vez e convite já usado (40.2).

**Registro para o Plano (v1.7):** seção 4 (linhas `api/igrejas`, `api/atribuicoes`,
`api/meus-dados/minhas-turmas` e nota do 404 uniforme; RF21 → Atende), seção 5 (linha 7.1 atendida
em 18/10/2026), seção 6.16 (ATENDIDA; decisões e microdecisões), seção 7.1 (10 casos), seção 7.2
(`atribuicoes.spec` 4, `PaginaAtribuicoesDepartamento.spec` 1, `guardas.spec` 7), seção 9 (POST
igrejas resolvido). Texto completo em `docs/decisoes/etapa-7.md`, 1.1.

---

## Etapa 7.2 — Fluxos E2E-04 a E2E-08 e corretiva do dia civil da igreja (18 e 19/10/2026)

**Escopo.** Seção 7.3 do Plano: E2E-04 (chamada até relatório, desktop e projeto `mobile`), E2E-05
(restrições do Professor e conta Usuario com atribuição, RNF 21.4), E2E-06 (reabertura pelo Admin),
E2E-07 (importação CSV, RNF 41.3) e E2E-08 (último administrador, RNF 14.3). A semente cria uma
turma própria por cenário que grava aula; o front só ganhou `data-testid`. **Corretiva descoberta
pelo E2E-04** (18/10, 21h38 locais): a Frequência da turma contava a aula, mas 0 alunos — o período
era lido como dia UTC e a matrícula do próprio dia, depois das 20h, caía no dia seguinte. Correção
(opção A do autor): `RelogioIgreja` lê `Igreja:FusoHorario` (padrão America/Porto_Velho) e converte
dias civis em instantes UTC nos relatórios RF35–RF38 (e RF6) e no "hoje" da pendência de
fechamento; armazenamento continua em UTC. Decisões em `docs/decisoes/etapa-7.md`, 1.2.

**Validação automatizada (18/10/2026, 22h14 locais, ambiente do autor):** `dotnet build` limpo e
`dotnet test` = **151 aprovados** (146 + `RF35_FusoHorarioTests`, 5 casos: instantes UTC do dia
local e `HojeLocal` às 21h; `DataCivil` com id Windows; matrícula às 23h locais do dia D entra no
relatório com fim = D; aula de hoje às 21h locais não é pendente na aula, na Frequência e no
Resumo do dia; aula de ontem é — relógio fixo por `FabricaComRelogioFixo`) · `vue-tsc -b` limpo ·
`npm run test` = **23 arquivos, 119 casos** (`situacaoAula.spec` +1: aula de hoje às 21h locais não
é pendente, a de ontem é) · `npm run build` ok · `npm run test:e2e` = **18 aprovados** (10 no
projeto `chromium`: E2E-01 a E2E-03, E2E-04, E2E-05 ×3, E2E-06, E2E-07, E2E-08; 8 no `mobile`:
E2E-01, E2E-02, E2E-04, E2E-M1 ×3, E2E-M2 ×2), contra o PostgreSQL local no ambiente `E2E`.

**O que cada fluxo afirma.** E2E-04: a confirmação de Consolidar nomeia os alunos sem registro e
oferece "Salvar e consolidar" (CSU11 passos 2–4); depois de completar e salvar, consolida e a
Frequência mostra 1 aula consolidada e 2 presenças; no celular, critério estrito em Aulas e Chamada
e largura do documento em Relatórios. E2E-05: menu do Professor sem Pessoas/Usuários/Igreja;
`GET /api/pessoas/{id}` de pessoa fora das turmas → 403 (aluno da turma → 200); sem CSV/Imprimir em
Frequência, Acompanhamento e Ranking; conta Usuario com Auxiliar ativa → 403 em
`GET /api/aulas?departamentoId`, turma só como "Aluno" em Minhas Turmas, sem "Abrir turma", e a
Página da Turma digitada volta a Minhas Turmas com aviso. E2E-06: reabrir preserva as presenças;
corrigir uma e consolidar de novo grava as três como presentes. E2E-07: Criadas 2, Ignoradas 2,
Para conferência 1; reimportar o mesmo arquivo não cria nada. E2E-08: inativar a própria conta →
"Você não pode desativar o seu próprio usuário."; trocar o próprio perfil → mensagem da 14.3; a
conta segue Admin e Ativa (também conferido por PATCH direto → 400).

**Testes manuais (perfil `https`, banco de desenvolvimento):** T1 corretiva do dia civil, após 20h
locais de 18/10 ✔ · T2 chamada até relatório, desktop e 360 px ✔ · T3 restrições do Professor e
conta Usuario ✔ · T4 reabertura ✔ · T5 importação ✔ · T6 último Admin ✔ (a troca de perfil foi para
Pastor; a mensagem da 14.3 é a mesma para qualquer perfil diferente de Admin). As aulas de T2 a T6
têm data 19/10/2026 (testes concluídos na madrugada de 19/10).

**Prints (`docs/evidencias/`):** Playwright, projeto `mobile`: `RNF-responsividade-e2e04-aula-nova.png`,
`-aulas-turma.png`, `-chamada-incompleta-confirmacao.png`, `-chamada-completa.png`,
`-chamada-consolidada.png`, `-relatorio-frequencia.png`. Manuais: `RF35-relatorio-matricula-noite.png`,
`RNF-31.3-aula-hoje-nao-pendente.png`, `CSU11-confirmacao-sem-registro.png`,
`CSU15-frequencia-apos-consolidar.png` (gravado na 7.2 como `CSU12-…`; renomeado na 7.3, porque a Frequência da turma é o CSU15), `RNF-21.4-usuario-com-atribuicao-minhas-turmas.png`,
`RNF-35.4-professor-sem-exportacao.png`, `CSU11-reabrir-preserva.png`, `CSU11-reconsolidada.png`,
`RNF-41.3-importacao-conferencia.png`, `RNF-14.3-perfil-ultimo-admin.png`.

**Registro para o Plano:** seção 7.3 (linha do E2E-04 reescrita para o comportamento atual do
CSU11; E2E-05 a E2E-08 ✔ 7.2), critério de data (linha 285: "hoje (UTC)" → dia civil da igreja,
fuso configurável, armazenamento em UTC), seção 5 (linha 7.2 atendida em 18/10/2026), seções 7.1 a
7.3 (`RF35_FusoHorarioTests` 5; `situacaoAula.spec` +1; projeto `mobile` com E2E-04) e totais
(151 · 119 · 18). Texto completo no relato da entrega e em `docs/decisoes/etapa-7.md`, 1.2.

---

## Etapa 7.3 — Resultados: tabela CSU → teste, testes de Segurança e roteiros de evidência (19 a 22/10/2026)

**Escopo.** Plano v1.8, seção 5 (linha 7.3), 6.17 e 7.5: (a) `docs/resultados.md` com a tabela
CSU → teste → resultado (CSU01–CSU24) e as tabelas RNF → teste → evidência de Segurança e de
Confiabilidade; (b) `RF44_IgrejaTests.Atualizar_CampoOmitido_ViraNulo` e os xUnit que as evidências
faltantes do inventário da 7.1 exigiam (a regra existia, o teste não); (c) roteiro de cópia e
restauração (seção 6.17); (d) roteiros das evidências de Segurança faltantes. Nenhuma regra de
negócio, rota ou tela mudou. Levantamento: inventário dos 151 xUnit, 119 Vitest e 18 Playwright,
dos testes manuais e dos 104 prints; cada citação da tabela foi conferida no arquivo real.

**Validação automatizada (19/10/2026, ambiente do autor):** `dotnet build` limpo e `dotnet test` =
**168 aprovados** (151 + 17: `RF44_IgrejaTests.Atualizar_CampoOmitido_ViraNulo` — PUT só com o Nome
deixa Cidade, Estado, Email e Telefone nulos; `RF1_CadastroInicialTests.RegistrarAdmin_GravaSenhaSoComoHashBCrypt`
— 1.2/2.2; `RF13_UsuarioTests.Criar_ComSenhaInformada_GravaSoHashBCrypt` — 13.2, hash `$2x$`, 60
caracteres, custo ≥ 10, sem a senha, `Verify` e login; `RF13_UsuarioTests.RotasDeUsuarios_ComoProfessor_Retorna403`
— GET, POST e PATCH de `/api/usuarios` como Professor → 403 sem gravar (13.1/14.1/16.1);
`RF2_HttpsTests.RequisicaoHttp_RedirecionaParaHttps_Retorna307` — http → 307 com `Location`
https (1.4/2.3/40.4/42.4/44.4); `RF21_PerfilOperacionalTests.CriarAula_ResponsavelComAtribuicaoDeAuxiliar_Retorna400`
— Auxiliar como responsável → 400 sem gravar, Professor → 201 (21.3);
`RF39_RF40_ConviteTests.ListarEObter_AposGerarConvite_NaoDevolvemToken` — 39.2;
`RF39_RF40_ConviteTests.Ativar_ContaInativa_Retorna400` — 40.2, nada consumido nem gravado; e,
apontados pela verificação do `resultados.md`: `RF13_UsuarioTests.ListarUsuarios_ComoPastorOuSuperintendente_Retorna403`,
`RF33_ReabrirAulaTests.Reabrir_ComoPastorOuSuperintendente_Retorna403` e
`RF44_IgrejaTests.Atualizar_ComoPastorOuSuperintendente_Retorna403` (2 casos cada: "restrita ao
Administrador" vale também contra os outros perfis administrativos — 13.1/16.1, 33.5, 44.1) e
`RF21_PerfilOperacionalTests.OperarTurma_ContaProfessorComAtribuicaoDeAuxiliar_Permitido` — conta
Professor de função Auxiliar lista e cadastra aula, primeira parte da 21.3). Todos verdes na
primeira execução: nenhum revelou defeito.

**Corretiva de 20/10/2026 (decisões do autor sobre as divergências da tabela CSU → teste):**
Histórico de presenças do aluno na aba Alunos da Página da Turma (CSU14), aviso "Este recurso é
restrito a administradores." na guarda `requerAdmin` (CSU24 FA2), convite pendente descartado ao
inativar a conta (RNF 40.2) e 403 por perfil com corpo `{ mensagem }` ("Você não tem permissão para
esta operação."). Validação: `dotnet build` limpo e `dotnet test` = **169 aprovados** (170 após a revisão, abaixo; 168 + 1:
`Ativar_ContaInativa_Retorna400` deu lugar a `Inativar_DescartaConvitePendente_EReativarNaoORecria`
— gera convite → inativa → hash e validade nulos, tag some → reativa → link antigo recusado como
inválido → convite novo funciona — e a `Ativar_ContaInativadaForaDaApi_Retorna400`, conta inativada
direto no banco, defesa em profundidade; `RF13_UsuarioTests` passou a conferir a mensagem do 403)
· `vue-tsc -b` limpo · `npm run test` = **24 arquivos, 126 casos** (119 + `DialogHistoricoPresencas.spec`
4: carrega ao abrir e conta só Consolidadas; cartões no celular do mais recente ao mais antigo;
tabela no desktop; fechado não carrega + `PaginaMatriculasTurma.spec` +1: Histórico pelo ⋮ abre o
diálogo e carrega pela pessoa do aluno, inclusive matrícula inativa + `guardas.spec` +2: Professor
em `/igreja` volta ao Painel com um único aviso; Admin abre sem aviso) · `npm run build` ok ·
`npm run test:e2e` = **18 aprovados** (10 `chromium` + 8 `mobile`). Revisão independente do diff da
corretiva apontou dois defeitos, corrigidos antes do fechamento: reativar uma conta
inativada por outro caminho (ou antes da corretiva) revivia o convite antigo — agora o convite só
sobrevive quando a conta estava ativa e continua ativa (`Reativar_ContaInativadaForaDaApi_DescartaConviteAntigo`,
xUnit **170**); e Esc fechava o diálogo do histórico durante o carregamento, deixando a resposta em
voo preencher o histórico da próxima pessoa — agora `closeOnEscape` acompanha o carregamento e uma
resposta atrasada é descartada.

### Roteiro C — Cópia e restauração dos dados (seção 6.17; monografia 5.1, Confiabilidade)

Faz uma cópia do banco de desenvolvimento `koinoniahub` no formato custom do `pg_dump`, restaura
num banco novo `koinoniahub_restaurado`, sobe a API apontada para ele e confere login, listagem e
escrita. **Nenhuma senha vai para arquivo**: ela é digitada uma vez por terminal e fica só na
variável de ambiente da sessão. **O arquivo da cópia fica fora do repositório e fora do OneDrive**
(`%LOCALAPPDATA%`), porque contém dados pessoais e os hashes das senhas.

**Preparação.** Feche a API e o front se estiverem rodando (a API restaurada usa a mesma porta).
Se o banco de desenvolvimento tiver nomes reais de membros, borre-os nos prints 5 e 6 (o Plano
pede evidência só com dados fictícios).

**C1 — Terminal A (PowerShell): variáveis, senha e contagens na origem.**

```powershell
$pg    = "C:\Program Files\PostgreSQL\18\bin"
$pasta = Join-Path $env:LOCALAPPDATA "koinoniahub-copia"
New-Item -ItemType Directory -Force $pasta | Out-Null
$dump  = Join-Path $pasta ("koinoniahub-" + (Get-Date -Format "yyyyMMdd-HHmm") + ".dump")
$sql   = Join-Path $pasta "contagens.sql"
$seg   = Read-Host "Senha do usuario postgres" -AsSecureString
$env:PGPASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($seg))
@'
SELECT current_database() AS banco,
       (SELECT count(*) FROM "Igrejas")       AS igrejas,
       (SELECT count(*) FROM "Pessoas")       AS pessoas,
       (SELECT count(*) FROM "Usuarios")      AS usuarios,
       (SELECT count(*) FROM "Departamentos") AS turmas,
       (SELECT count(*) FROM "Aulas")         AS aulas,
       (SELECT count(*) FROM "Presencas")     AS presencas,
       (SELECT count(*) FROM "AceitesTermo")  AS aceites;
SELECT max("MigrationId") AS ultima_migration FROM "__EFMigrationsHistory";
SELECT count(*) AS pessoa_restauracao_73 FROM "Pessoas" WHERE "Nome" = 'Restauracao 7.3';
'@ | Set-Content -Encoding ascii $sql
& "$pg\psql.exe" -h localhost -p 5432 -U postgres -d koinoniahub -f $sql
```

Esperado: `banco = koinoniahub`, as sete contagens, a última migration aplicada e
`pessoa_restauracao_73 = 0`. **Print 1:** `Confiabilidade-restauracao-1-contagens-origem.png`.

**C2 — Cópia (`pg_dump`, formato custom).**

```powershell
& "$pg\pg_dump.exe" -h localhost -p 5432 -U postgres -d koinoniahub -F c -f $dump
"pg_dump terminou com codigo $LASTEXITCODE"
Get-Item $dump | Select-Object Name, Length, LastWriteTime
& "$pg\pg_restore.exe" --list $dump | Select-Object -First 12
```

Esperado: código `0`; o arquivo `.dump` com tamanho maior que zero; no cabeçalho da listagem,
`Format: CUSTOM` e `dbname: koinoniahub`. **Print 2:** `Confiabilidade-restauracao-2-pg-dump-custom.png`.

**C3 — Banco novo e restauração (`pg_restore`).**

```powershell
& "$pg\dropdb.exe"   -h localhost -p 5432 -U postgres --if-exists koinoniahub_restaurado
& "$pg\createdb.exe" -h localhost -p 5432 -U postgres koinoniahub_restaurado
& "$pg\pg_restore.exe" -h localhost -p 5432 -U postgres -d koinoniahub_restaurado --no-owner --exit-on-error $dump
"pg_restore terminou com codigo $LASTEXITCODE"
```

Esperado: nenhuma mensagem de erro e código `0`. **Print 3:** `Confiabilidade-restauracao-3-pg-restore.png`.

**C4 — Contagens no banco restaurado.**

```powershell
& "$pg\psql.exe" -h localhost -p 5432 -U postgres -d koinoniahub_restaurado -f $sql
```

Esperado: `banco = koinoniahub_restaurado` e **os mesmos números do C1**, a mesma última migration
e `pessoa_restauracao_73 = 0`. **Print 4:** `Confiabilidade-restauracao-4-contagens-restaurado.png`
(de preferência ao lado do print 1).

**C5 — Terminal B: API apontada para o banco restaurado.** A variável de ambiente
`ConnectionStrings__Postgres` vale só neste terminal e tem precedência sobre os *user-secrets* do
ambiente Development; nenhum `appsettings` é tocado.

```powershell
cd C:\Users\burst\OneDrive\Desktop\TCC-KOINONIAHUB\api\KoinoniaHub.API
$seg = Read-Host "Senha do usuario postgres" -AsSecureString
$s   = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($seg))
$env:ConnectionStrings__Postgres = "Host=localhost;Port=5432;Database=koinoniahub_restaurado;Username=postgres;Password=$s"
Remove-Variable s, seg
dotnet run --launch-profile https
```

Esperado: `Now listening on: https://localhost:7054`.

**C6 — Terminal C: front, login e listagem.** `cd web; npm run dev`. No navegador,
http://localhost:5173 → entrar com o Admin de desenvolvimento (o mesmo de sempre). Esperado: o
Painel abre. Menu **Pessoas**. Esperado: a mesma lista do sistema antes da cópia. **Print 5:**
`Confiabilidade-restauracao-5-login-listagem.png` (Pessoas com o usuário logado no topo); e o
console do Terminal B com a API no ar apontada para o banco restaurado:
`Confiabilidade-restauracao-5-api-no-banco-restaurado.png`.

**C7 — Escrita vai para o banco restaurado.** Pessoas → **Nova Pessoa** → Nome
`Restauracao 7.3` (sem acento) → **Salvar**. Esperado: aviso de sucesso e a linha na lista. No
Terminal A:

```powershell
& "$pg\psql.exe" -h localhost -p 5432 -U postgres -d koinoniahub_restaurado -f $sql
& "$pg\psql.exe" -h localhost -p 5432 -U postgres -d koinoniahub -f $sql
```

Esperado: no restaurado, `pessoas` = C4 + 1 e `pessoa_restauracao_73 = 1`; no original, os números
do C1 e `pessoa_restauracao_73 = 0`. Prova que a API lê e grava no banco restaurado. **Print 6:**
`Confiabilidade-restauracao-6-escrita-no-restaurado.png` (as duas saídas).

**C8 — Limpeza.** Terminal B: Ctrl+C e `Remove-Item Env:ConnectionStrings__Postgres`. Terminal A:
`Remove-Item Env:PGPASSWORD`; opcional `& "$pg\dropdb.exe" -h localhost -U postgres koinoniahub_restaurado`.
Apague o `.dump` quando os prints estiverem salvos (`Remove-Item $dump`), ou guarde-o fora de
pastas sincronizadas. Nunca coloque o `.dump` no repositório.

### Roteiros D1 a D10 — Evidências faltantes (Segurança, D1–D8, do inventário da 7.1; corretiva de 20/10, D9–D10)

**Preparação comum.** API: `cd api/KoinoniaHub.API; dotnet run --launch-profile https` (banco de
desenvolvimento). Front: `cd web; npm run dev`. Contas: o Admin de desenvolvimento e um Professor
com o termo já aceito. pgAdmin → Query Tool no banco `koinoniahub` (ou `psql`). Os links de
convite são abertos numa **janela anônima** (Ctrl+Shift+N), porque `/primeiro-acesso` não abre
com sessão. Os links levam o token em claro: **borre o trecho depois de `token=`** em todo print.
Os roteiros reaproveitam contas uns dos outros; execute na ordem.

**D1 — Conta por convite e token exibido uma vez (CSU19; RNFs 39.2 e 39.3).**
1. Admin → **Pessoas** → **Nova Pessoa** → Nome `Teste 7.3 Convite`, E-mail
   `teste73.convite@exemplo.test` → **Salvar**. Esperado: a pessoa na lista.
2. **Usuários** → **Novo Usuário** → Pessoa `Teste 7.3 Convite`; E-mail em branco (usa o da
   pessoa); Perfil `Usuario`; **sem** marcar "Definir a senha manualmente" → **Salvar**. Esperado:
   aviso "Usuário criado. Envie o link de convite para a pessoa definir a senha." e o diálogo
   **Convite de primeiro acesso** com o link, o botão de copiar, **Enviar pelo WhatsApp** e o aviso
   "…Por segurança, ele é exibido apenas agora…". Copie o link (ícone de copiar) para o Bloco de
   Notas como **Link 1**. **Print:** `RNF-39.2-token-exibido-uma-vez.png`.
3. **Fechar** → **Recarregar**. Esperado: a linha com as tags "Ativo" e "Convite pendente"; o link
   não aparece em lugar nenhum da tela.
4. F12 → **Console**:
   `console.table((await (await fetch('/api/usuarios')).json()).map(u => ({ id: u.id, perfil: u.perfil, convitePendente: u.convitePendente, conviteToken: u.conviteToken })))`
   Esperado: `conviteToken` = `null` em todas as linhas; `convitePendente` = `true` na conta nova.
   **Print:** `RNF-39.2-listagem-sem-token.png`.
5. pgAdmin:
   `SELECT "Id", left("ConviteTokenHash", 8) AS inicio_hash, length("ConviteTokenHash") AS tamanho, "ConviteTokenHash" ~ '^[0-9a-f]{64}$' AS sha256_hex, "ConviteExpiraEm" - now() AS validade FROM "Usuarios" WHERE "ConviteTokenHash" IS NOT NULL ORDER BY "Id";`
   Esperado: `tamanho = 64`, `sha256_hex = true`, validade de cerca de 7 dias; o início do hash não
   aparece no link. **Print:** `RNF-39.3-hash-convite-no-banco.png`.

**D2 — Convite novo invalida o anterior (CSU20; RNFs 15.2 e 39.4).**
1. Usuários → linha `Teste 7.3 Convite` → botão de convite (ícone de envio). Esperado: diálogo com
   um link **diferente** do Link 1. Copie como **Link 2** → Fechar.
2. Janela anônima → cole o **Link 1**. Esperado: "Convite inválido ou já utilizado. Solicite um
   novo link ao administrador." e o botão **Ir para o login**. **Print:**
   `RNF-39.4-convite-anterior-invalidado.png`.
3. Na mesma janela, cole o **Link 2**. Esperado: "Olá, Teste 7.3 Convite!" com os campos de senha
   e o termo. Não conclua (é o D3).

**D3 — Convite já utilizado e convite de conta inativa (RNFs 40.1 e 40.2).**
1. Na janela anônima, com o Link 2: Nova senha `Teste@2026`, confirmação `Teste@2026`, marque o
   aceite do termo → **Definir senha e concluir**. Esperado: "Senha definida com sucesso!".
2. Cole o **Link 2** de novo. Esperado: "Convite inválido ou já utilizado. Solicite um novo link
   ao administrador.". **Print:** `RNF-40.2-convite-ja-utilizado.png`.
3. Admin → Usuários → botão de convite da conta `Teste 7.3 Convite` → copie como **Link 3** →
   Fechar.
4. Lápis (Editar) da mesma conta → Status **Inativo** → **Salvar**. Esperado: "Usuário atualizado
   com sucesso." e a tag "Inativo".
5. Janela anônima → cole o **Link 3**. Esperado (corretiva de 20/10: inativar descarta o convite):
   "Convite inválido ou já utilizado. Solicite um novo link ao administrador.". **Print:**
   `RNF-40.2-convite-conta-inativa.png`. Em Usuários, a linha da conta ficou sem a tag "Convite
   pendente" e com o botão de convite desabilitado (`RNF-40.2-conta-inativa-sem-convite-pendente.png`).
6. Editar → Status **Ativo** → Salvar. Esperado: a conta volta a Ativo **sem** a tag "Convite
   pendente" (`RNF-40.2-conta-reativada-sem-convite.png`); o Link 3 continua recusado (cole-o de
   novo na janela anônima: `RNF-40.2-convite-apos-reativar-recusado.png`). Para a pessoa definir a
   senha, o Admin gera um convite novo pelo botão de convite.

**D4 — Senha só como hash BCrypt (RNFs 1.2, 2.2, 13.2, 15.4, 40.3; seção 4.8).**
1. Pessoas → Nova Pessoa → `Teste 7.3 Senha`, E-mail `teste73.senha@exemplo.test` → Salvar.
2. Usuários → Novo Usuário → Pessoa `Teste 7.3 Senha`; Perfil `Usuario`; **marque** "Definir a
   senha manualmente (em vez de gerar link de convite)"; Senha inicial `Teste@2026` → **Salvar**.
   Esperado: "Usuário criado com sucesso.".
3. pgAdmin, consulta A (sem e-mail e sem o hash inteiro):
   `SELECT "Id", "Perfil", left("SenhaHash", 7) AS prefixo, split_part("SenhaHash", '$', 3) AS custo, length("SenhaHash") AS tamanho FROM "Usuarios" ORDER BY "Id";`
   Esperado em todas as linhas — inclusive o Admin do cadastro inicial (1.2/2.2), a conta do passo
   2 (13.2) e a conta do D3, que definiu a senha pelo convite (15.4/40.3): prefixo `$2a$11$` (ou
   `$2b$11$`), custo `11` (≥ 10) e tamanho `60`.
   Consulta B:
   `SELECT count(*) FILTER (WHERE "SenhaHash" !~ '^\$2[aby]\$[0-9]{2}\$.{53}$') AS fora_do_formato_bcrypt, count(*) FILTER (WHERE position('Teste@2026' in "SenhaHash") > 0) AS contem_a_senha FROM "Usuarios";`
   Esperado: `0` e `0`. **Prints:** `RNF-2.2-senhahash-bcrypt-no-banco.png` (consulta A) e
   `RNF-2.2-senhahash-bcrypt-contagem.png` (consulta B).
4. Sair → entrar com `teste73.senha@exemplo.test` / `Teste@2026`. Esperado: abre a tela do termo
   (aceite pendente, RNF 13.4) — o hash confere a senha. **Print:**
   `RNF-13.4-login-senha-manual-termo-pendente.png`. Sair.

**D5 — 403 de Professor em `/api/usuarios` (RNFs 13.1, 14.1, 16.1).**
1. Entrar como Professor. Esperado: menu sem "Usuários".
2. F12 → **Network** (limpar) → **Console**:
   `const r = await fetch('/api/usuarios'); console.log(r.status, await r.json())`
   Esperado: `403 {mensagem: 'Você não tem permissão para esta operação.'}` (corretiva de 20/10:
   o 403 por perfil traz `{ mensagem }`) e a linha `usuarios` com Status 403 na aba Network.
   **Prints:** `RNF-13.1-403-professor-api-usuarios.png` (Console) e
   `RNF-13.1-403-professor-api-usuarios-network.png` (aba Network).
3. Opcional (14.1):
   `const p = await fetch('/api/usuarios/1', { method: 'PATCH', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ Perfil: 'Admin' }) }); console.log(p.status, await p.json())`
   Esperado: `403` com a mesma mensagem. Sair.

**D6 — Auxiliar não pode ser o professor responsável (RNF 21.3).**
1. Admin → Pessoas → Nova Pessoa `Teste 7.3 Auxiliar` (sem e-mail) → Salvar.
2. **Turmas EBD** → abrir uma turma de teste que tenha Professor e matéria ativa → aba
   **Atribuições** → **Nova Atribuição** → Pessoa `Teste 7.3 Auxiliar`, Função `Auxiliar` →
   **Salvar**. Esperado: a linha com a função Auxiliar (e a tag "Sem conta", RNF 21.5). Anote o id
   da turma na URL (`/departamentos/<id>/atribuicoes`).
3. Aba **Aulas** → **Nova Aula** → abra o seletor **Professor**. Esperado: só pessoas com
   atribuição de Professor; o Auxiliar não aparece. **Print:** `RNF-21.3-seletor-so-professores.png`
   (seletor aberto). Cancelar.
4. Console (troque `<id>` pelo id da turma):
   `const t = <id>; console.table((await (await fetch('/api/materias?departamentoId=' + t)).json()).map(m => ({ id: m.id, nome: m.nome }))); console.table((await (await fetch('/api/atribuicoes/departamento/' + t)).json()).map(a => ({ pessoaId: a.pessoaId, nome: a.pessoaNome, funcao: a.funcao })))`
   Anote o id da matéria e o `pessoaId` do Auxiliar.
5. Console:
   `const r = await fetch('/api/aulas', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ Data: new Date().toISOString(), MateriaId: <id da matéria>, ProfessorId: <pessoaId do Auxiliar>, Tema: 'Teste RNF 21.3' }) }); console.log(r.status, await r.json())`
   Esperado: `400 {mensagem: 'Professor não encontrado ou sem atribuição ativa de Professor neste departamento.'}`.
   **Print:** `RNF-21.3-400-auxiliar-responsavel.png` (Console e Network).
6. Aba Aulas → **Recarregar**. Esperado: nenhuma aula "Teste RNF 21.3". Limpeza: aba Atribuições
   → encerrar a atribuição de Auxiliar.

**D7 — Redirecionamento para HTTPS (RNFs 1.4, 2.3, 40.4, 42.4, 44.4).**
1. Com a API no perfil `https` (escuta em `https://localhost:7054` e `http://localhost:5202`), num
   PowerShell: `curl.exe -i http://localhost:5202/api/termo/vigente` (use `curl.exe`, não o alias
   `curl`, e sem `-L`). Esperado: primeira linha `HTTP/1.1 307 Temporary Redirect` e o cabeçalho
   `Location: https://localhost:7054/api/termo/vigente`. **Print:** `RNF-2.3-redirecionamento-https.png`.
2. Opcional: logado no front, F12 → **Application** → Cookies → `http://localhost:5173` →
   `kh_token`: HttpOnly e Secure marcados (borre a coluna Value, que é o JWT). **Print:**
   `RNF-2.3-cookie-secure-httponly.png`.

**D8 — Sem "Excluir aula" quando há presença (RNF 45.5; print citado na 6.5 e ausente da pasta).**
1. Professor ou Admin → turma de teste → aba Aulas → **Nova Aula** (data de hoje, matéria,
   professor) → Salvar → **Fazer chamada** → marque um aluno → **Salvar chamada**.
2. Voltar à aba Aulas → ⋮ da aula. Esperado: "Ver presenças registradas", "Editar aula",
   "Consolidar chamada" e "Marcar como Não realizada", **sem** "Excluir aula". **Print:**
   `RNF-45.5-sem-excluir-com-presenca.png` (menu aberto).

**D9 — Histórico de presenças do aluno pelo Professor, na aba Alunos (CSU14; corretiva de 20/10).**
1. Entrar como Professor → Minhas Turmas → **Abrir turma** → aba **Alunos**. Na linha de um aluno
   que tenha presenças em aula Consolidada, clique no botão de calendário ("Histórico de
   presenças"). Esperado: diálogo "Histórico de Presenças - <nome>" com os cinco indicadores
   (Aulas consolidadas, Presenças, Faltas, Frequência, Registros no histórico), a nota do CSU07 e
   a tabela com Data, Turma, Matéria, Presença, Aula e Observação; só aulas das turmas do
   Professor aparecem (RNF 34.3). **Print:** `CSU14-historico-aluno-professor-desktop.png`.
2. Fechar. F12 → Ctrl+Shift+M → Responsive 360×640 → recarregue a aba Alunos. No cartão do mesmo
   aluno, ⋮ → **Histórico de presenças** (menu aberto: `CSU14-historico-menu-360.png`). Esperado:
   o mesmo diálogo com os registros em cartões (data · turma no título; tags Presente/Ausente e
   situação da aula; matéria e observação), "Mostrar mais" quando houver mais de dez, e nada
   rolando para o lado. **Print:** `CSU14-historico-aluno-professor-360.png`.
3. Opcional: num cartão de matrícula inativa, o ⋮ oferece só o histórico, sem "Remover matrícula".

**D10 — Rota só do Admin digitada por outro perfil (CSU24 FA2; corretiva de 20/10).**
1. Logado como Professor (desktop), digite `http://localhost:5173/igreja` na barra de endereço e
   dê Enter. Esperado: volta ao Painel e aparece um único aviso "Sem permissão — Este recurso é
   restrito a administradores.". **Print:** `CSU24-FA2-aviso-restrito-admin.png` (com o aviso
   visível).
2. Repita com `/usuarios`. Esperado: o mesmo aviso. Entre como Admin e abra Igreja pelo menu.
   Esperado: abre sem aviso.

### Resultado dos roteiros (22/10/2026)

C ✔ · D1 ✔ · D2 ✔ · D3 ✔ · D4 ✔ · D5 ✔ · D6 ✔ · D7 ✔ (com o opcional do cookie) · D8 ✔ · D9 ✔ ·
D10 ✔ — executados pelo autor em 22/10/2026, todos com o resultado esperado. Prints em
`docs/evidencias/`: os nomes dos roteiros (30 arquivos no total) mais os extras
`RNF-2.2-senhahash-bcrypt-contagem.png` (D4, consulta B), `RNF-13.4-login-senha-manual-termo-pendente.png`
(D4, passo 4), `RNF-13.1-403-professor-api-usuarios-network.png` (D5),
`RNF-40.2-conta-inativa-sem-convite-pendente.png`, `RNF-40.2-conta-reativada-sem-convite.png` e
`RNF-40.2-convite-apos-reativar-recusado.png` (D3, passos 5 e 6), `CSU14-historico-menu-360.png`
(D9), `RNF-2.3-cookie-secure-httponly.png` (D7, opcional) e
`Confiabilidade-restauracao-5-api-no-banco-restaurado.png` (C5). Em `docs/resultados.md`, a coluna
Evidência passou a ✔, o CSU19 passou a Aprovado (15 Aprovados e 9 Parciais) e a cópia e restauração,
a Aprovado (Confiabilidade 20 de 20).

---

## SonarCloud — primeira análise (22 e 23/10/2026)

**Escopo.** Próximo passo fora das etapas (Plano v1.9, seção 5): análise estática da API e do front
no SonarCloud, por GitHub Actions. Nenhuma regra de negócio, rota, contrato ou tela mudou, e nenhum
arquivo de código foi alterado. Monorepo com dois projetos na organização `dannkes1`
(`dannkes1_tcc-koinoniahub_api` e `dannkes1_tcc-koinoniahub_web`, públicos, análise automática
desligada). Arquivos: `.github/workflows/sonarcloud.yml` (jobs `api` e `web`, em push e pull request
para a main), `web/sonar-project.properties`, `coverlet.collector` 10.1.0 no projeto de testes e
`TestResults/` e `.sonarqube/` no `.gitignore` (commits `d5d556b` e `c482d3d`). Migrations fora da
análise da API (decisão do autor): **a cobertura reportada é a do código de autoria**. Decisões em
`docs/decisoes/sonarcloud.md`. A correção do que o Sonar apontou é a entrega seguinte, com plano antes.

**Validação local antes do push (22 e 23/10/2026, comandos exatos do workflow, em bash):**
`dotnet build KoinoniaHub.API.sln --no-incremental` limpo (0 avisos, 0 erros) · `dotnet test --no-build
--logger trx --collect:"XPlat Code Coverage" --results-directory TestResults -- …Format=opencover` =
**170 aprovados**, com `TestResults/<guid>/coverage.opencover.xml` e `TestResults/*.trx` (contadores
170/170) nos caminhos que o `begin` lê · `npm run test:cov -- --coverage.reporter=lcov
--coverage.reporter=text-summary` = **24 arquivos, 126 casos**, com `TZ=UTC` (fuso do runner) e com
`TZ=America/Porto_Velho` (o do job), e `coverage/lcov.info` com caminhos `SF:src/...` · `npx vue-tsc -b`
limpo · `npm run build` ok · YAML lido por parser estrito e `actionlint` 1.7.12 sem erros. Medida
local do OpenCover que motivou a exclusão: 28,3% dos pontos de sequência com as Migrations (7.603 dos
12.025 pontos são delas, todos a 0%) e 77,0% sem elas (3.404 de 4.422). A verificação do código
antes do push (cultura, fuso, maiúsculas nos imports, arquivos ignorados, certificado, user-secrets)
mostrou que nenhum teste dependia do Windows do autor.

**Primeira análise (run #1 do workflow SonarCloud, commit `c482d3d`, 23/10/2026, 00h44 locais):** os
dois jobs verdes no `ubuntu-24.04` — API (.NET) em 1min42s e Web (Vue) em 1min16s (1min46s de ponta a
ponta). Foi a primeira execução das suítes fora do Windows: **170 xUnit** e **24 arquivos / 126
Vitest** aprovados no Linux, com cultura invariante e fuso UTC na API.

| Métrica | API (`dannkes1_tcc-koinoniahub_api`) | Web (`dannkes1_tcc-koinoniahub_web`) |
| --- | --- | --- |
| Linhas de código | 6.742 (sem Migrations) | 13.627 |
| Quality Gate | não computado (primeira análise) | não computado (primeira análise) |
| Security | 3 — 2 Blocker, 1 Medium · nota **E** | 0 · **A** |
| Reliability | 12 — 1 High, 11 Medium · **C** | 85 — todas Medium · **C** |
| Maintainability | 45 · **A** | 128 · **A** |
| Security Hotspots | 0 · **A** | 0 · **A** |
| Coverage | **74,5%** (linhas 77,0%; condições 60,5%; 4.422 linhas a cobrir, 1.018 descobertas) | **48,0%** (linhas 41,1%; condições 63,3%; 2.542 a cobrir, 1.498 descobertas) |
| Duplications | 3,0% | 0,9% |
| Testes | 170 · 0 erros · 0 falhas · 100% de sucesso (pelo `.trx`) | não importados (só o lcov) |
| Avisos da análise | nenhum | 1: o lcov cita `src/assets/LOGOIPB2.png` |

**Como ler os números.**
- *Cobertura.* A do SonarCloud combina linhas e condições; por isso a API fica em 74,5% com 77,0% das
  linhas. As 4.422 linhas a cobrir são o mesmo universo medido localmente sem as Migrations. No web,
  o Sonar conta com 0% os arquivos que nenhum spec importa (38 dos 74 `.ts`/`.vue` de `src`, fora os
  specs), enquanto o resumo do Vitest no log do job (71,7% das linhas) só considera os 36 carregados
  pelos testes. **O número oficial para o texto é o do SonarCloud.**
- *Notas.* A organização está no modo clássico (Standard Experience): a nota de cada qualidade segue
  o apontamento mais grave do tipo clássico (VULNERABILITY para Security, BUG para Reliability), não
  a quantidade. Security E na API pelos dois Blocker; Reliability C nos dois projetos porque o pior
  BUG é Major (o S8949, "High" na visão por qualidade, é Major como BUG). Na API, só 2 dos 12
  apontamentos de Reliability são BUG (S8949 e S8733); no web, 12 dos 85 (os 72 S6853 e o S6845 são
  CODE_SMELL e não entram na nota).
- *Security da API.* Dois dos três apontamentos estão no `appsettings.E2E.json` versionado (senha do
  PostgreSQL local de teste e chave JWT do ambiente E2E, valores descartáveis): o scanner .NET indexa
  também os JSON versionados de `api/`. O terceiro está em `Aplicacao` (`TokenServico.cs`). O
  workflow não cita esse arquivo.
- *Annotations do run.* Os 10 avisos do job API em "Annotations" são avisos dos analisadores do Sonar
  na compilação (o GitHub mostra no máximo 10 por passo); não são falhas.
- *Quality Gate.* As condições do Sonar way valem para o código novo, e na primeira análise da main
  ainda não há código novo de referência; os checks "SonarCloud Code Analysis" saíram como neutros.

**Triagem dos apontamentos.** Security (3) e Reliability (API 12, Web 85) foram agrupados por regra
em 23/10/2026, com uma proposta por grupo (corrigir, aceitar com justificativa ou adiar) levada ao
autor. Nada foi corrigido nesta entrega.

**Prints (`docs/evidencias/`, 17 arquivos):** `SONAR-actions-verde.png`, `SONAR-api-log.png`,
`SONAR-api-log-testes.png`, `SONAR-api-overview.png`, `SONAR-api-overview-reliability.png`,
`SONAR-api-overview-maintainability.png`, `SONAR-api-overview-hotspots.png`, `SONAR-api-coverage.png`,
`SONAR-api-testes.png`, `SONAR-api-sem-migrations.png`, `SONAR-web-log.png`, `SONAR-web-log-testes.png`,
`SONAR-web-overview.png`, `SONAR-web-overview-reliability.png`, `SONAR-web-overview-maintainability.png`,
`SONAR-web-overview-hotspots.png` e `SONAR-web-coverage.png`. Prefixo `SONAR-` porque a análise não
corresponde a um RF ou RNF.

**Registro para o Plano (v1.10):** seção 5 (linha nova "SonarCloud" na tabela de estado, fora das
etapas), 7.4 (o CI e a reprodução local da cobertura) e 7.5 (a análise estática como evidência
complementar), mais a pendência de texto da monografia na seção 9 (4.10 e 5.1 não preveem análise
estática). Texto completo em `docs/decisoes/sonarcloud.md`, seção 4.

**Segunda análise (23/10/2026, commit `1d380b4`, só documentação):** Quality Gate **OK** nos dois
projetos (as condições do código novo passaram a ser avaliadas); demais números iguais aos da
primeira, salvo Maintainability do web, de 128 para 131, sem mudança de código (atualização do lado
do SonarCloud).

---

## SonarCloud — correção dos apontamentos (23/10/2026)

**Escopo.** Correção da primeira análise com as decisões do autor de 23/10/2026
(`docs/decisoes/sonarcloud.md`, seções 5 e 6). Nenhuma regra da monografia nem rota mudou; o
contrato da API só muda para requisição malformada, que passa a receber o 400 `ValidationProblemDetails`
(decisão do autor). Blocos de commit: api · front · maintainability-api · maintainability-web · docs.

**O que mudou.**
- *API (Reliability).* `Data` obrigatória no cadastro e na edição de aula (sem o campo, a aula era
  gravada com 0001-01-01); ids de aula, matéria, matrícula e usuário com `[Range(1, …)]` (id ausente
  deixa de responder "não encontrado" e passa a dizer qual campo falta); item da chamada sem
  `presente` → 400, sem gravar nada do lote (antes virava ausência; RNF 32.6); `await app.RunAsync()`
  (S6966); `AsSingleQuery()` na consulta de uma pessoa (S8733, aceito e registrado no código).
- *Front (Reliability e acessibilidade).* 72 rótulos ligados aos campos em 12 telas (17 desses campos
  não tinham nome acessível nenhum); Cadastro Inicial com rótulos visíveis acima dos campos, com os
  placeholders mantidos; rótulo do Conteúdo da aula ligado ao campo; `aria-label` no arquivo da
  importação; `void` no logout em segundo plano (S9383); Minha Frequência sem avaliação dupla (S6523);
  Meus Dados com o perfil administrativo da store (o Superintendente via a orientação errada).
- *Maintainability, sem mudança de comportamento.* API: 8 apontamentos (S112 em `Program.cs`, S1118,
  ASP0027, S108, S1125, CA1822, S1192, S9022). Front: 20 (S7924, S4666 ×2, S9449 ×3, S3863 ×5, S6644
  ×3, S7770, S7762 ×2, S4624, S6551, S5976), mais os `!important` que estavam dentro dos blocos de CSS
  removidos. Aceitos com justificativa: 18 regras (`docs/decisoes/sonarcloud.md`, seção 5).

**Validação automatizada (23/10/2026, ambiente do autor):** `dotnet build` com 0 avisos e 0 erros ·
`dotnet test` = **174 aprovados** (170 + `RF30_CadastrarAulaTests` 2: aula sem data → 400 com
`errors.Data` e nada gravado; sem matéria → 400 com `errors.MateriaId`, antes 404 · `RF45_EditarAulaTests`
+1: editar sem data → 400 e a data não muda · `RF32_ChamadaTests` +1: item sem `presente` → 400 com
`errors["Itens[1].Presente"]`, sem gravar o item válido nem os visitantes) · `vue-tsc -b` limpo ·
`npm run test` = **26 arquivos, 131 casos** (126 + `meusDadosServico.spec` 3 + `PaginaCadastroInicial.spec`
1 + `PaginaAulasTurma.spec` +1: cada rótulo do diálogo aponta para o seu campo e os Dropdowns são
nomeados pelo rótulo) · `npm run build` ok · `npm run test:e2e` = **18 aprovados** (10 `chromium` +
8 `mobile`), com o E2E-01 conferindo o rótulo "Nome da igreja *" e, no celular, `semRolagemHorizontal`
estrito no Cadastro Inicial (verificação nova nessa tela) e o print; o E2E-02, o rótulo "Senha". Cada
bloco de commit foi validado no seu estado.

**CSS conferido no navegador.** Painel, Aulas da turma, Pessoas e Usuários, a 1366×768 e 360×640,
antes e depois das mudanças de CSS: **0 pixel diferente** nos 8 prints; estilos calculados de
BarraTopo (cabeçalho, marca, botão do menu, logo, textos, usuário) e das 21 tags idênticos. A única
diferença é a declarada pelo S9449: a transição dos links do menu passou de `all` para
`background-color, color, font-weight, box-shadow`, e a do Sair para `border-color, color`.

**Testes manuais (perfil `https`):** M1 ✔ · M2 ✔ · M3 ✔ · M4 ✔ · M5 ✔ · M6 ✔ · M7 ✔ · M8 ✔,
executados pelo autor em 24/10/2026 (commit `1043a5d`): `RF1-cadastro-inicial-labels.png`,
`RF1-cadastro-inicial-labels-360.png` e `RNF-30.2-400-aula-sem-data.png`.

**Análise depois da correção (run #3 do workflow, commit `1043a5d`, 24/10/2026):** os dois jobs
verdes. Números antes → depois, com a triagem aplicada na interface em 24/10 (CA1862 como False
positive; as demais regras da seção 5 do `sonarcloud.md` como Accept):

| Métrica | API: antes → depois | Web: antes → depois |
| --- | --- | --- |
| Linhas de código | 6.742 → 6.750 | 13.627 → 13.774 |
| Apontamentos (total) | 60 → 41 (19 fechados); após a triagem: **7 abertos**, 19 aceitos, 15 falsos positivos | 212 → 106 (109 fechados); após a triagem: **90 abertos**, 16 aceitos |
| Security | 3 (E) → 0 abertos, 3 aceitos · **A** | 0 · A |
| Reliability | 12 (C) → 1 (S8949, aceito) · **A**; 11 fechados | 85 (C) → 1 (S6845, aceito) · **A**; 84 fechados |
| Maintainability | 45 → 37 (8 fechados) · A; abertos: 7 × S3776 | 131 → 106 (25 fechados) · A; abertos: S9423 ×88, S3776, S3358 |
| Security Hotspots | 0 | 0 |
| Coverage | 74,5% → **74,5%** (linhas 77,0%; condições 60,5%) | 48,0% → **46,3%** (linhas 41,1% → 42,3%; condições 63,3% → 53,0%; a cobrir 2.542 → 2.709; descobertas 1.498 → 1.563) |
| Cobertura do código novo | 76,92% em 18 linhas | 82,35% em 27 linhas |
| Duplications | 3,0% | 0,9% |
| Testes importados | 170 → 174 | — |
| Quality Gate | não computado → **FAILED** (cobertura do código novo 76,92% < 80%) | não computado → **PASSED** |

- *Quality Gate da API.* Das 18 linhas novas, 5 ficaram sem cobertura (os quatro `return BadRequest`
  da constante `MensagemPeriodoInvalido` em `RelatoriosController` e o `catch` interno de
  `RelogioIgreja.ResolverFuso`), mais uma condição (o `?? throw` da chave JWT em `Program.cs`).
  Nenhum teste exercitava o período invertido dos relatórios. **Fechado em 24/10/2026:**
  `RF35_RelatorioTests.Relatorios_PeriodoInvertido_Retorna400` (`[Theory]`, 4 casos, um por
  relatório de período: frequência da turma, acompanhamento, ranking de faltas e minha frequência;
  `dataFim` anterior a `dataInicio` → 400 com "dataFim não pode ser menor que dataInicio."). `dotnet
  build` limpo e `dotnet test` = **178 aprovados**; o OpenCover local confirma as quatro linhas
  visitadas. Cobertura esperada do código novo na próxima análise: 24 de 26 = 92,3%. Ficam sem
  cobertura, por decisão: o `catch` do `RelogioIgreja` (inalcançável com ICU e tzdata) e a condição
  da chave JWT ausente (exigiria subir um host sem a chave).
- *Cobertura do web.* A queda de 48,0% para 46,3% é efeito dos templates novos: os rótulos e
  atributos acrescentados em páginas que não têm spec entram como linhas a cobrir sem teste
  (a cobrir +167; descobertas +65). O código novo medido pelo Sonar ficou em 82,35%.
- *`npm audit` (24/10/2026):* 15 vulnerabilidades (1 low, 5 moderate, 9 high), em cadeias de
  dependências de desenvolvimento (vite, vitest, playwright) e nos pacotes `axios` e `vue`
  (este último por `@vue/server-renderer`, que a SPA não usa). Fica como item para entrega
  própria, com plano antes: atualização de versões e conferência de cada aviso contra o uso real.

**Prints (`docs/evidencias/`, 11 arquivos):** `SONAR-correcao-actions-verde.png`,
`SONAR-correcao-api-overview.png`, `-api-summary.png`, `-api-reliability.png`,
`-api-maintainability.png`, `-api-issues-abertas.png`, `SONAR-correcao-web-overview.png`,
`-web-summary.png`, `-web-reliability.png`, `-web-maintainability.png`, `-web-issues-abertas.png`;
mais `RNF-responsividade-cadastro-inicial.png` (E2E-01, projeto `mobile`) e os três dos testes manuais.

**Registro para o Plano (v1.10):** `docs/decisoes/sonarcloud.md`, seção 4, itens 11 a 21 (seções 2.3,
2.4, 6.1, 7.1 a 7.5 e 9).

## Aderência à monografia — Entrega A: seis correções de interface e validação (28 e 29/10/2026)

**Escopo.** Auditoria da monografia contra o sistema (28/10/2026) e primeira das três entregas em
que o código passou a seguir o texto: RF36 (limiar crítico ajustável na tela), RF43 / RNF 43.1
(data e hora do aceite na lista de Usuários), RNF 42.1 / CSU01 FA2 / CSU22 FA2 (aviso de aceite
obrigatório), RNF 4.3 (validação de Meus Dados, 400 por campo), CSU11 FA2 / RNF 33.7 ("Marcar
como Não realizada" só sem presenças) e RF29 / CSU12 FA1 (verbo "inativar"). Decisões, critério
e lista do que existe no código sem RF em `docs/decisoes/aderencia-monografia.md` (seções 1, 2 e
5). Nenhuma rota, DTO de resposta ou migration alterados; o contrato de `PUT /api/meus-dados`
passa a responder 400 `ValidationProblemDetails` por campo a e-mail inválido e UF fora de 2 letras,
com e-mail vazio aceito como nulo. Blocos de commit: api · front · e2e · docs · prints.

**Validação automatizada (29/10/2026, ambiente do autor):** `dotnet build` com 0 avisos e 0 erros ·
`dotnet test` = **182 aprovados** (178 + `RF4_MeusDadosTests` 4: e-mail inválido → 400 com
`errors.Email` e nada gravado; UF "ROO" → 400 com `errors.Estado`; e-mail vazio → 204 e nulo
gravado, com a prova de que `[EmailAddress]` sozinho recusa a string vazia; PUT válido → 204 e o
GET reflete) · `vue-tsc -b` limpo · `npm run test` = **28 arquivos, 139 casos** (131 +
`termoUsuario.spec` 4 + `PaginaRelatoriosEbd.spec` 2: a aba Acompanhamento expõe os três campos
com 75/50/3 e envia 80/40/3 depois de alterados + `TermoUsoSigilo.spec` +2: aviso com a caixa
desmarcada e ausente depois de marcar; sem a prop, sem aviso; `PaginaAulasTurma.spec` e
`PaginaMatriculasTurma.spec` com os casos existentes ajustados) · `npm run build` ok ·
`npm run test:e2e` = **18 aprovados** (10 `chromium` + 8 `mobile`; o E2E-01 confere o aviso
"O aceite do termo é obrigatório para concluir." antes do aceite e a ausência dele depois; a API
sobe pelo config com `E2E_INICIAR_API=1`). Suíte completa executada antes (178 / 131 / 18) e
depois da entrega.

**Testes manuais (perfil `https` + `npm run dev`, desktop e 360 × 640), executados pelo autor em
29/10/2026:** T1 ✔ Relatórios › Acompanhamento com os três limiares (Atenção, Crítico e faltas
seguidas), o campo Crítico enviado na consulta e a classificação mudando com ele · T2 ✔ Usuários
com a data e hora do aceite abaixo da tag "Termo", também nos cartões do celular · T3 ✔ aviso de
aceite obrigatório no Cadastro Inicial e no Primeiro Acesso, sumindo ao marcar a caixa · T4 ✔
Meus Dados: e-mail inválido e UF com três letras barrados no campo; UF minúscula gravada em
maiúsculas; e-mail vazio salvo como vazio; o 400 por campo da API visto no console do navegador ·
T5 ✔ aba Aulas: aula Em aberto com presença não oferece "Marcar como Não realizada" no ⋮; sem
presença, oferece e a situação muda · T6 ✔ aba Alunos: "Inativar matrícula" no ⋮, no botão e na
confirmação, com o texto de preservação das presenças; depois de inativar, o histórico da aluna
continua acessível à gestão. Observação do T6, registrada para a Entrega B: a Professora da turma
recebe 403 ao abrir o histórico da matrícula recém-inativada (CSU12 FA1 diz que o histórico fica
preservado; o guarda exige matrícula ativa).

**Prints (`docs/evidencias/`, 19 arquivos):** `RF36-limiar-critico-tela.png`,
`RF36-limiar-critico-360.png`, `RF36-limiar-critico-rede.png`, `RNF-43.1-usuarios-data-aceite.png`,
`RNF-43.1-usuarios-data-aceite-360.png`, `RNF-42.1-aviso-aceite-cadastro.png`,
`RNF-42.1-aviso-aceite-primeiro-acesso.png`, `RNF-42.1-aviso-aceite-primeiro-acesso-360.png`,
`RNF-4.3-meus-dados-email-invalido.png`, `RNF-4.3-meus-dados-email-invalido-360.png`,
`RNF-4.3-meus-dados-uf-invalida.png`, `RNF-4.3-meus-dados-uf-normalizada.png`,
`RNF-4.3-meus-dados-email-vazio.png`, `RNF-4.3-400-uf-api.png` (400 por campo visto no console do
front), `RNF-33.7-sem-nao-realizada-com-presenca.png`, `RNF-33.7-nao-realizada-sem-presenca.png`,
`RF29-inativar-matricula-confirmacao.png`, `RF29-inativar-matricula-confirmacao-360.png`,
`CSU12-historico-matricula-inativa-professor.png` (o 403 da observação do T6).

**Registro para o Plano (v1.11):** `docs/decisoes/aderencia-monografia.md`, seção 8.
