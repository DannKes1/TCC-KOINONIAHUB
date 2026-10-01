# Etapa 4 — Relatórios só com aulas Consolidadas (RF6, RF35–RF38) · resumo de decisões

Entregas 4.1 (API, 30/09/2026) e 4.2 (front, 30/09/2026). Plano de Desenvolvimento, seções 5, 6.9, 6.10, 7.1 e 7.2. Escrito conforme a regra "Resumo de decisões por etapa" (Plano 1.1).

## 1. Decisões tomadas dentro da margem de julgamento

### 1.1 API (4.1)

**Um único recorte, aplicado em cinco lugares.** Frequência da turma, painel de acompanhamento, ranking de faltas, resumo do dia e minha frequência filtram `Situacao == Consolidada` nas aulas e, por consequência, nas presenças (RNFs 35.5, 37.5, 38.4; descrição do RF36; CSU07). O serviço anterior não aplicava filtro algum: uma chamada lançada em aula ainda Em aberto já entrava no percentual.

**"Data já ocorrida" = `Data` anterior ao início de hoje em UTC**, a mesma regra de `AulaServico.CalcularPendenteFechamento`. Uma aula Em aberto de hoje não é pendente em nenhum relatório; no resumo do dia de hoje ela aparece como "em aberto", não como pendente.

**`AulasPendentes[]` respeita o período consultado.** Uma aula Em aberto de meses atrás, fora do período, não aparece no relatório — a tela de aulas (RF31) é o lugar de encontrá-la. `ListarAulasPendentesAsync` é um método privado reutilizado pelos quatro relatórios de turma; o resumo do dia monta as listas a partir das aulas do dia que já carregou.

**`AulaResumidaRespostaDto` único** (`Id, Data, Materia, Professor, DepartamentoId, Departamento`) para pendentes e Não realizadas. O Plano 6.9 fixa os quatro primeiros campos; os dois últimos servem ao resumo do dia, que cruza turmas.

**Resumo do dia preserva os campos existentes** (`TemChamada`, totais), agora alimentados só por Consolidadas, e ganha as contagens por situação por turma (`AulasConsolidadas`, `AulasNaoRealizadas`, `AulasEmAberto`, `PendenteFechamento`), os totais por situação e as duas listas — o que o Plano 6.10 pede para a tela.

**Assinaturas intactas.** `IRelatorioEbdServico` e `RelatoriosController` não mudaram; o front da etapa anterior continuou funcionando entre a 4.1 e a 4.2.

**Testes de integração com semente de cinco aulas** (2 Consolidadas, 1 Não realizada, 1 Em aberto já ocorrida *com chamada lançada*, 1 futura): a presença lançada na aula não consolidada é o caso que prova o filtro; a futura prova que "pendente" depende da data. `RF27_MatriculaTests` entrou nesta etapa (proposta da 3.1) porque reaproveita a semente e porque o índice único filtrado precisava de prova no SQLite dos testes.

### 1.2 Front (4.2)

**Gate de exportação em um componente** (`BotoesExportacao`): lê `isAdministrativo` do store e não renderiza nada para Professor e Usuario (RNFs 35.4/36.4/37.4). Substituiu o botão "Imprimir" do cabeçalho e os cinco botões "CSV". A regra fica em um lugar só e o spec do Plano 7.2 a verifica por perfil. É restrição de interface, como a monografia descreve — a API não tem endpoint de exportação.

**`ListaAulasPendentes` reutilizável** (frequência, acompanhamento, ranking, minha frequência e resumo do dia), com a variante "Não realizadas" para o resumo. Não renderiza quando a lista está vazia, para não poluir o relatório de quem está em dia.

**Resumo do dia** (Plano 6.10): coluna "Situação das aulas" com tags por turma (consolidadas / não realizadas / em aberto ou pendentes), colunas numéricas por situação, rodapé "Totais (só aulas Consolidadas)" e as duas listas abaixo da tabela. O rodapé da coluna "Em aberto" soma as Em aberto de todas as turmas (inclui as de hoje), enquanto o card "Pendentes de fechamento" conta só as de data já ocorrida — são números distintos de propósito.

**Rótulos "Aulas consolidadas"** no lugar de "Aulas no período" nos cards de frequência, acompanhamento e minha frequência, porque o número passou a significar isso.

**Painel do usuário comum (RF3) corrigido.** A conferência que o Plano previa para o RF3 ("os indicadores excluem aulas não consolidadas?") encontrou um ponto que o Plano não listava: o painel do aluno calculava "Total de aulas", "Presenças", "Faltas" e "Frequência" sobre **todos** os registros do histórico (RF34), inclusive de aulas Em aberto. RF3 chama isso de "indicadores de frequência", e o CSU07 fixa que frequência considera só Consolidadas. Correção: o histórico (RF34) passou a informar `SituacaoAula` em cada registro — campo aditivo, com asserção em `RF34_PresencasPessoaTests` — e o painel conta só os de aulas Consolidadas; a tabela de histórico continua mostrando todos, com a tag da situação da aula. A leitura do CSU14 ("lista as presenças por aula, com data, turma, matéria e situação") é compatível com o campo novo.

**Deixado para a Etapa 6 (registrado no Plano):** o diálogo de histórico da tela de Pessoas (RF34 para gestão) ainda soma presenças e faltas de todos os registros; não é um indicador de frequência do RF3, mas convém alinhar.

## 2. Arquivos criados ou alterados

API — `Aplicacao/DTOS/Respostas/AulaResumidaRespostaDto.cs` (novo); `Aplicacao/DTOS/Respostas/{FrequenciaTurma, PainelAcompanhamento, RankingFaltas, MinhaFrequenciaTurma, ResumoDia, HistoricoPresenca}RespostaDto.cs`; `Aplicacao/Servicos/Implementacoes/{RelatorioEbdServico, PresencaHistoricoServico}.cs`.

Testes xUnit — `Infraestrutura/CenarioAula.cs` (`CriarAulaEmAsync`, `CriarUsuarioDaMatriculaAsync`); `RF35_RelatorioTests` (8); `RF27_MatriculaTests` (3); `RF34_PresencasPessoaTests` (asserção do campo novo). Total do projeto: 81.

Front — `aplicacao/modelos/dtos.ts`; `aplicacao/servicos/{relatoriosServico, meusDadosServico, presencasPessoaServico}.ts`; `components/ui/BotoesExportacao.vue` (+ spec, 7 casos); `components/ui/ListaAulasPendentes.vue`; `paginas/privado/relatorios/PaginaRelatoriosEbd.vue`; `paginas/privado/minha-frequencia/PaginaMinhaFrequencia.vue`; `paginas/privado/PaginaPainel.vue`. Vitest: 28 casos.

Evidências — `RNF-35.5-frequencia-antes-reabertura.png`, `RNF-35.5-33.6-frequencia-depois-reabertura.png` e, da 4.2, o print dos botões de exportação ausentes para o Professor (`RNF-35.4-professor-sem-exportacao.png`).

## 3. O que o autor deve saber explicar na banca

- Por que um relatório de frequência não pode contar aulas Em aberto (a chamada ainda pode mudar) nem Não realizadas (não houve aula) — e por que a regra está no serviço da API, não nas telas.
- O que é "pendente de fechamento", por que é um cálculo e não um estado, e por que uma aula de hoje não é pendente.
- Por que a exportação é restrita a gestão sendo uma restrição de interface, e o que isso implica (o dado em tela é o mesmo; o que muda é o canal de saída).
- Como o resumo do dia distingue as três situações e por que os totais somam só Consolidadas.
- O caso do painel do aluno: um indicador que estava certo "por acaso" enquanto toda aula era consolidada e passou a precisar do filtro quando o ciclo de vida da aula entrou — exemplo de por que o RF3 pedia conferência depois da Etapa 4.
