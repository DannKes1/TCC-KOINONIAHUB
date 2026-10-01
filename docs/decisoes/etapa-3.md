# Etapa 3 — Regras de aula (RF31, RF32, RF33) · resumo de decisões

Entregas 3.1 (API, 29/09/2026) e 3.2 (front, 29/09/2026). Plano de Desenvolvimento, seções 5, 6.2, 7.1 e 7.2. Escrito conforme a regra "Resumo de decisões por etapa" (Plano 1.1).

## 1. Decisões tomadas dentro da margem de julgamento

### 1.1 API (3.1)

**"Matrícula ativa" na consolidação = `AlunoDepartamento.Ativo` no momento da operação**, o mesmo filtro que `ObterChamadaCompletaAsync` usa para montar a tela de chamada. Assim, a lista de "alunos sem registro" é sempre um subconjunto do que o professor vê na tela. A monografia (32.6/33.3) fala em "alunos com matrícula ativa na turma", no presente; se um dia a regra passar a considerar a data da aula, muda nos dois lugares.

**`ChamadaIncompletaException` em vez de mudar o retorno de `ConsolidarAsync`.** O Plano 6.2 pede "400 com a lista dos alunos sem registro". Uma exceção que deriva de `InvalidOperationException` mantém o `catch` genérico dos controllers funcionando (400 com `mensagem`) e permite ao `AulasController.Consolidar` acrescentar `alunosSemRegistro[]` (`AlunoDepartamentoId` + `NomeAluno`, para a tela destacar as linhas). Primeira exceção própria do projeto, na pasta nova `Aplicacao/Excecoes/`.

**Idempotência da consolidação removida.** Consolidar aula já Consolidada devolvia sucesso; pela RNF 33.2 só aulas Em aberto são consolidadas → 400. Era comportamento do código, não da monografia. O front já desabilitava o botão.

**`ReabrirAsync` não chama o guard de atribuição**: a RNF 33.5 restringe ao Admin, que é perfil administrativo e passaria de qualquer forma; a restrição está na rota (`[Authorize(Roles = "Admin")]`), como o Plano 6.2 desenha, e o isolamento por igreja continua no `igrejaId` da consulta (aula de outra igreja → 404).

**Reabertura vale para Consolidada e Não realizada** (CSU11, fluxo alternativo 3). Nenhum registro de presença é tocado: os existentes ficam preservados (33.6); aula Não realizada reaberta continua sem registros.

**`QuantidadeVisitantes` não é zerada ao marcar Não realizada**: a monografia só fala em registros de presença (33.4/33.7). O resumo do dia (Etapa 4) soma visitantes só de aulas Consolidadas, então o valor não contamina nada. Zerar seria regra nova.

**Testes de integração em vez de unitários puros** para 32.2/32.6/33.x: passam pelo guard de atribuição e pelo controller, logo provam a RNF de Segurança (33.1, 33.5) junto com a de Confiabilidade. O único unitário é o do índice único de presença (32.3), que é do banco.

### 1.2 Front (3.2)

**Vitest instalado na abertura da etapa** (decisão 5.1 da 2.4): `vitest`, `@vue/test-utils`, `jsdom`, `@vitest/coverage-v8`; bloco `test` no `vite.config.ts`, `vitest/globals` nos tipos do `tsconfig.app.json`, scripts `test`, `test:watch`, `test:cov`; specs ao lado do código (`*.spec.ts`), como o Plano 7.2 pede.

**Regras de exibição em um módulo puro** (`aplicacao/dominio/situacaoAula.ts`): `ehPendenteFechamento`, `rotuloSituacaoAula`, `severidadeSituacaoAula`, `aulaAtendeFiltro`. A API continua sendo a fonte da verdade para `pendenteFechamento` (Plano 6.2); a função local aplica a mesma regra (Em aberto e data anterior a hoje, comparando datas em UTC) e só é usada quando a resposta não trouxer o campo. Foi o jeito de ter a função testável que o Plano 7.2 lista sem duplicar a decisão de negócio no caminho normal.

**Componente `TagSituacaoAula`** (uma tag por situação + "Pendente" quando Em aberto com data já ocorrida) substituiu as três versões diferentes de tag que existiam nas telas (aulas, chamada, presenças, painel), todas derivadas de um booleano `consolidada`. O campo derivado `AulaVM.consolidada`, mantido desde a Etapa 1 "até a Etapa 3 trocar as telas pelas tags", foi removido.

**Filtro por situação na listagem** com as três situações e o recorte "Pendentes de fechamento" (RNF 31.2/31.3), mais um aviso com a contagem de pendentes no topo da tela.

**Botões condicionados à situação e ao perfil** (Plano 6.2): Consolidar e Marcar como Não realizada só em aula Em aberto; Reabrir só fora de Em aberto e só para Admin (`autenticacao.isAdmin`). Todos com diálogo de confirmação, como o CSU11 descreve para as três operações. A tela de chamada manteve apenas Consolidar (CSU11: "na tela da chamada, o usuário aciona Consolidar"); Não realizada e Reabrir ficam na listagem de aulas, onde o Plano as coloca.

**Chamada incompleta na tela**: quando a API responde 400 com `alunosSemRegistro[]`, a tela de chamada destaca as linhas (classe na `<tr>` + tag "Sem registro") e orienta a marcar e salvar; a listagem de aulas mostra os nomes no aviso. O destaque é limpo ao salvar a chamada.

**Somente leitura para Consolidada e Não realizada** na tela de chamada (RNF 32.2 / CSU10 FA1), com mensagem específica para cada situação e indicação de que só o Administrador reabre.

**Painel**: "aulas abertas" passou de `!consolidada` para `situacao === "EmAberto"` — uma aula Não realizada não é uma aula aberta. A conferência dos indicadores de gestão frente às aulas não consolidadas continua na Etapa 4, como o Plano 4.1 (RF3) prevê.

**Menu lateral por perfil** (Plano 7.2, linha movida da Etapa 2 para a 3 na decisão 5.1): Relatórios EBD e Turmas EBD passam a exigir `isGestor` (Admin, Pastor, Superintendente, Professor); o perfil Usuario vê Painel, Minhas Turmas e Meus Dados. É consistente com as precondições do CSU15 (relatórios: gestão ou atribuição) e do CSU17 (turmas: gestão). É restrição de interface: a API já negava o acesso.

## 2. Arquivos criados ou alterados

API — `Aplicacao/DTOS/Respostas/AlunoSemRegistroRespostaDto.cs` (novo); `Aplicacao/Excecoes/ChamadaIncompletaException.cs` (novo); `Aplicacao/Servicos/Interfaces/IAulaServico.cs`; `Aplicacao/Servicos/Implementacoes/AulaServico.cs`; `Controllers/AulasController.cs`.

Testes xUnit — `Infraestrutura/CenarioAula.cs` (novo); `RF33_ConsolidarAulaTests` (5); `RF33_NaoRealizadaTests` (4); `RF33_ReabrirAulaTests` (5); `RF32_ChamadaTests` (5 execuções). Total do projeto: 70.

Front — `package.json`, `vite.config.ts`, `tsconfig.app.json`, `.gitignore`; `aplicacao/dominio/situacaoAula.ts` (+ spec, 12 casos); `components/ui/TagSituacaoAula.vue` (+ spec, 5 casos); `components/layout/MenuLateral.vue` (+ spec, 4 casos); `aplicacao/modelos/dtos.ts`; `aplicacao/servicos/aulasServico.ts`; `paginas/privado/aulas/PaginaAulasTurma.vue`; `paginas/privado/chamada/PaginaChamadaAula.vue`; `paginas/privado/chamada/PaginaPresencasAula.vue`; `paginas/privado/PaginaPainel.vue`. Vitest: 21 casos.

Evidências — `docs/evidencias/RNF-33.5-403-reabrir-professor.png`.

## 3. Ocorrência registrada

O `CenarioAula.cs` entregue na 3.1 não adicionava a matéria ao contexto do EF (nada a referenciava no grafo, pois as aulas são criadas por teste): os 19 testes falhariam por chave estrangeira. Corrigido pelo autor na aplicação com `db.Add(materia)`. Lição: toda entidade da semente que não esteja ligada a outra por navegação precisa do `Add` explícito.

## 4. O que o autor deve saber explicar na banca

- O ciclo de vida da aula (Em aberto → Consolidada / Não realizada → Em aberto só pelo Admin) e por que "pendente de fechamento" é um cálculo, não um estado.
- Por que a consolidação exige registro explícito de todos os alunos ativos (a ausência de registro não é falta) e como a API devolve a lista para a tela.
- A diferença entre Não realizada (sem registros, fora dos cálculos) e Consolidada (registros bloqueados, dentro dos cálculos), e por que uma aula com chamada lançada não pode virar Não realizada.
- Por que a reabertura é exclusiva do Administrador e preserva as presenças (correção auditável, sem perda de dados).
- Como os testes de integração provam as RNFs de Segurança 33.1 e 33.5 (403) e as de Confiabilidade 33.2/33.3/33.6/33.7 (400/204 + estado no banco), e o que o Vitest cobre no front (regra de exibição, componente de tag, menu por perfil).
