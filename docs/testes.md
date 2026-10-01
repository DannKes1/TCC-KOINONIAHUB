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