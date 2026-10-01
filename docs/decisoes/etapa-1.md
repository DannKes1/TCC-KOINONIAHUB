# Etapa 1 — Migrations (Situacao da aula, AceitesTermo, Pessoa enxuta) · resumo de decisões

Concluída em 26/09/2026 (Plano, seções 5, 6.2, 6.3 e 6.4). Migrations: `20260925041839_SituacaoAula`, `20260926193619_AceitesTermo`, `20260926203116_PessoaEnxuta`. Escrito retroativamente no fechamento da Etapa 2 (Plano 1.1).

## 1. Decisões tomadas dentro da margem de julgamento

### 1.1 Situacao da aula (Plano 6.2)

**`string Situacao` com constantes em `SituacaoAula`** (`EmAberto`, `Consolidada`, `NaoRealizada`), coluna de 20 caracteres, padrão `EmAberto`. String e não `enum` porque o restante do projeto já guarda estados como texto (`Pessoa.Situacao`, `Usuario.Perfil`) e o valor aparece legível no banco e nos relatórios.

**Migration converte os dados**: `Consolidada = true` → `'Consolidada'`, o restante → `'EmAberto'`, e só então remove a coluna booleana. A coluna nasce com `DEFAULT 'EmAberto'` para as linhas existentes; o modelo EF não usa `HasDefaultValue` (a entidade já inicializa em código), como as demais colunas.

**"Pendente de fechamento" não é armazenado**: calculado em `AulaServico.CalcularPendenteFechamento` como `Situacao == EmAberto && Data.Date < DateTime.UtcNow.Date` e devolvido nos DTOs como `PendenteFechamento`. Uma aula do próprio dia não é pendente. A `Data` é `timestamp with time zone`, por isso a comparação é em UTC.

**RNF 32.2 aplicada de imediato** em `ChamadaServico`: `if (aula.Consolidada)` só podia virar `if (aula.Situacao != SituacaoAula.EmAberto)`, com a mensagem da RNF. As demais regras do RF33 (chamada completa, Não realizada, reabertura) ficaram para a Etapa 3, e `ConsolidarAsync` continuou idempotente como antes.

**Front sem tela tocada**: `AulaVM` ganhou `situacao` e `pendenteFechamento`; `consolidada` virou campo derivado (`situacao === "Consolidada"`) até a Etapa 3 trocar as telas pelas tags de situação.

### 1.2 AceitesTermo (Plano 6.3)

**Entidade não herda `EntidadeBase`**: o registro é imutável (RNF 42.3) e não tem `AtualizadoEm`; `CriadoEm` é inicializado com `DateTime.UtcNow` na própria entidade.

**Tamanhos das colunas** (o Plano não fixa): `TermoVersao` 20, `TermoHash` 64 (SHA-256 em hexadecimal tem exatamente 64 caracteres), `Ip` 45 (IPv6 máximo), `Meio` 30. Constantes do meio em `MeioAceiteTermo` (`CadastroInicial`, `PrimeiroAcesso`, `Login`).

**`DeleteBehavior.Restrict` nas FKs** para `Usuario` e `Igreja`: um aceite não pode desaparecer por cascata. O sistema não exclui usuários (só inativa), então nada muda nos fluxos; é a mesma escolha já feita em `Parentesco`.

**Sem navegação de coleção** em `Usuario`/`Igreja` e sem índice adicional: o Plano 6.3 não os prevê e, com ~200 usuários, os índices de FK criados por convenção bastam. O teste `ContextoDeTeste` passou a abrir o SQLite com `Foreign Keys=True` para o caso de exclusão ser verificável.

### 1.3 Pessoa enxuta (Plano 6.4)

**Entidade com exatamente as 17 colunas do DER** (Figura 35 da monografia); removidos CPF, Categoria, DataBatismo, DataMembresia, Telefone, FotoUrl e Observacoes de entidade, DTOs, telas e CSV. O teste `Pessoa_ModeloMapeado_TemExatamenteAsColunasDoDer` lê o modelo EF em tempo de execução e acusa qualquer campo que volte por engano.

**Coluna `Situacao` do CSV deixou de ser lida**: o modelo fechado no Plano 6.4 não a inclui e "colunas fora desse conjunto são ignoradas" (RF41); toda pessoa importada nasce ativa. Decisão confirmada pelo autor.

**Mensagem da RNF 41.3 e arquivo-modelo CSV antecipados** da Etapa 6: o parser e o modelo estavam sendo reescritos e a definição de pronto da Etapa 1 pedia "CSV sem os campos removidos"; tocar os mesmos arquivos duas vezes não fazia sentido. Na Etapa 6 fica só a marcação visual das linhas sinalizadas.

**`RF41_ImportacaoTests` entrou nesta etapa** porque testa o código alterado nela; `NormalizarOpcao` foi removido por ficar sem uso.

**Grade dos diálogos de pessoa** reorganizada (`2fr 1fr` na primeira linha; `1fr 1fr` na linha de contato) só para fechar os espaços dos campos removidos.

## 2. Arquivos criados ou alterados

API: `Dominio/Entidades/Models/{Aula, SituacaoAula, AceiteTermo, MeioAceiteTermo, Pessoa}.cs`; `Infraestrutura/Dados/KoinoniaHubDbContext.cs`; `Aplicacao/DTOS/Requisicoes/{PessoaCriar, PessoaAtualizar, MeusDadosAtualizar}RequisicaoDto.cs`; `Aplicacao/DTOS/Respostas/{AulaResposta, PessoaResposta, ParentescoResposta}Dto.cs`; `Aplicacao/Servicos/Implementacoes/{AulaServico, ChamadaServico, PessoaServico, PessoaImportacaoServico, MatriculaServico, AuthServico}.cs`; `Migrations/` (três migrations com Designer e snapshot gerados pela ferramenta).

Testes: `Infraestrutura/ContextoDeTeste.cs`; `Etapa1_SituacaoAulaTests.cs` (4); `Etapa1_AceiteTermoTests.cs` (3); `Etapa1_PessoaEnxutaTests.cs` (2); `RF41_ImportacaoTests.cs` (5).

Front: `aplicacao/modelos/dtos.ts`; `aplicacao/servicos/{aulasServico, pessoasServico, meusDadosServico, parentescosServico, matriculasServico}.ts`; `paginas/privado/pessoas/PaginaPessoasLista.vue`; `paginas/privado/meus-dados/PaginaMeusDados.vue`; `paginas/privado/matriculas/PaginaMatriculasTurma.vue`; `DialogImportarPessoas.vue`; arquivo-modelo CSV.

## 3. Ocorrências e lições

- Na 1.1, `dotnet ef database update` foi executado antes de o corpo da migration ser colado: a migration gerada automaticamente removeu `Consolidada` e criou `Situacao` vazia, e a base de desenvolvimento precisou ser reconstruída. Por isso a base de desenvolvimento **não** serve como evidência; o fechamento da etapa foi refeito em banco limpo (`dotnet ef database update` do zero), com aulas consolidadas preservadas.
- Na 1.2, `migrations add` rodou antes de os arquivos de entidade serem copiados e gerou migration vazia (`migrations remove` + regeneração). Regra adotada desde então: **`dotnet build` verde antes de `migrations add`**.
- O histórico `__EFMigrationsHistory` tinha um id antigo para `ConvitePrimeiroAcessoUsuario`; corrigido com `UPDATE` na tabela de histórico.

## 4. O que o autor deve saber explicar na banca

- Por que "Situacao" com três valores substitui um booleano e por que "pendente" é calculado e não gravado (uma aula fica pendente pela passagem do tempo, sem nenhum evento que a atualizasse).
- Por que o aceite do termo é uma tabela própria e imutável, com hash do texto (RNF 42.2/42.6) e não uma coluna em `Usuarios`.
- Por que a Pessoa foi reduzida ao DER (LGPD, minimização de dados: nada de CPF, batismo, membresia ou foto) e como o teste de colunas garante isso.
- Como uma migration do EF Core converte dados existentes (SQL de `UPDATE` entre `AddColumn` e `DropColumn`) e por que o histórico de migrations importa.
