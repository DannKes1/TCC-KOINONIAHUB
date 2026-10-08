# KoinoniaHub · Avaliação dos cinco pontos antes da Etapa 7

Data: 08/10/2026 · Base: monografia "corrigido (9)" (RF1–RF44, CSU01–CSU24), Plano v1.2, código no `main` após a 6.2 (113 xUnit · 54 Vitest · 3 Playwright). Critério que usei em todos: **o que a monografia já promete entra antes da banca; o que é lacuna real de requisito entra se couber e com texto antes do código; o que é melhoria entra na seção 9 como trabalho futuro.**

Resumo das recomendações:

| # | Ponto | Veredito | Onde | Esforço |
|---|---|---|---|---|
| 1 | Responsividade | **Obrigatório** — RNF existente em ao menos oito RFs (3.2, 5.3, 6.4, 20.2, 26.2, 28.2, 35.3, 36.3) e a avaliação de Usabilidade da 5.1 será feita por um professor, provavelmente no celular | **6.3**, antes da 7.2 | 1 entrega (shell) + ajustes em ~6 telas |
| 5 | Página central da turma | **Obrigatório, e não é melhoria: é o RF20** ("Exibe os detalhes da turma, com alunos matriculados, professores atribuídos e matérias"), que o Plano marcou OK "via matrículas, matérias, aulas e atribuições" — o PO acabou de mostrar que a delegação não cumpre o requisito | **6.4** | 1 entrega |
| 4 | Perfil Professor × atribuição | **Corrigir, caminho mínimo** — a 5.1 promete testar "o controle por perfil e atribuição"; hoje a API testa só atribuição | **7.1**, como você propôs | ½ entrega + textos |
| 2 | Editar aula em aberto | **Lacuna real; incluir** como RF45, texto antes do código | **6.5**, depois que o RF estiver na monografia | 1 entrega |
| 3 | Parentesco recíproco | **Concordo com derivar na leitura**; prioridade baixa (RF12 é "Baixa") — última da fila, corta se apertar | **6.6** ou seção 9 | ½ entrega |

Mais um item que a monografia promete e nenhum de nós listou: **5.1, Confiabilidade: "procedimento de cópia e restauração dos dados"**. Entra na 7.3 (seção 7 abaixo).

---

## 1. Responsividade para celular

**O que a monografia diz.** "A interface deve ser responsiva e seguir o padrão visual do sistema (PrimeVue)" é RNF em RF3, RF5, RF6, RF20, RF26, RF28, RF35, RF36 e outros. A 5.1 diz que a Usabilidade será avaliada com "um professor da EBD e o Product Owner" executando tarefas — o uso cotidiano do diário de classe é no celular, e o PO já reclamou do fluxo "principalmente pensando no celular". Não é escopo novo: é requisito escrito e não atendido.

**O que o código faz.** `LayoutPrincipal` é `flex` com `MenuLateral` de 272 px fixos e sem `@media`; só login, primeiro acesso, termo, relatórios e Minha Frequência têm media queries. Em 360 px a barra lateral consome 75 % da tela. As tabelas já usam `responsiveLayout="scroll"` (rolagem interna, aceitável); os formulários usam majoritariamente `auto-fit` (adaptam sozinhos); restam grades fixas em Painel (professor), Presenças da Aula, Meus Dados, Pessoas (diálogo) e Cadastro Inicial; diálogos já têm `max-width: 96vw`.

**Recomendação: Etapa 6.3 "Responsividade", antes da 7.2** (para os E2E novos nascerem com viewport de celular). Critério de pronto mensurável:

- Ponto de corte **768 px**. Abaixo dele: menu lateral vira **Drawer** (PrimeVue `Drawer`, já disponível no pacote) aberto por botão na `BarraTopo`; conteúdo com `padding` 16 px; cabeçalho de página com ações quebrando linha.
- Em **360 × 640** (classe Galaxy/Pixel), as telas do **fluxo do professor** não têm rolagem horizontal da página: Login, Termo, Painel, Minhas Turmas, Aulas da turma, Chamada, Presenças da Aula, Minha Frequência, Meus Dados. Tabelas podem rolar internamente; na Chamada, as colunas Aluno e Presente ficam visíveis sem rolar (a Observação pode ficar à direita).
- Alvos de toque ≥ 40 px nos botões de ação das linhas (Chamada, Aulas).
- Telas administrativas (Pessoas, Usuários, Relatórios, Importação) ficam **utilizáveis com rolagem**, sem polimento — o critério não as inclui, e isso fica escrito no Plano.
- Verificação: um segundo *project* do Playwright, `mobile` (`devices["Pixel 7"]`), executando E2E-04 (chamada) e E2E-02 (termo); prints a 360 px das nove telas em `docs/evidencias/RNF-responsividade-*.png`. Vitest não muda.

**Cabe no cronograma?** Sim, com esse recorte. É uma entrega para a casca (Drawer + BarraTopo + `.conteudo`) e ajustes pontuais nas cinco telas com grade fixa; o que não está no critério não entra. A alternativa — fazer tudo responsivo — não cabe e não é o que a banca vai testar.

---

## 2. Edição de aula em aberto (lacuna de requisito)

**O que a monografia diz.** RF30 "Cadastrar Aula", RF31 "Listar Aulas", CSU09 "Permite cadastrar e consultar as aulas". Nenhum RF edita. Compare com os pares existentes: Pessoa (RF7/RF8), Departamento (RF17/RF18), Usuário (RF13/RF14), Matéria (RF24/RF25), Atribuição (RF21/RF22) — aula é a única entidade operacional sem "Editar". **O código** confirma: `AulasController` tem `POST`, `GET`, `GET/{id}` e os três `PATCH` de situação; não há `PUT` nem `DELETE`. Hoje uma aula com data ou matéria errada só tem duas saídas, ambas ruins: marcá-la Não realizada (polui a lista e o resumo do dia) ou corrigir no banco.

**Avaliação.** É lacuna real e a banca pode perguntar ("e se o professor errar a data?"). Prioridade média: não bloqueia a chamada, mas é operação do dia a dia. Concordo com a regra: **só Em aberto**; Consolidada e Não realizada ficam bloqueadas (coerente com 32.2/33.x — a aula fechada é um registro imutável até o Admin reabrir).

**Recorte técnico (para você redigir o RF):**

| Camada | Conteúdo |
|---|---|
| Endpoint | `PUT /api/aulas/{id}` — mesma autorização do cadastro (`GarantirAcessoAulaAsync`: gestão ou atribuição ativa na turma). |
| DTO | `AulaAtualizarRequisicaoDto { Data (obrigatória), MateriaId (obrigatória), ProfessorId (obrigatório), Tema?, Conteudo? }` — os mesmos campos do cadastro; sem `Situacao`, `QuantidadeVisitantes` ou `Observacoes` (a chamada cuida deles). |
| Regras no serviço | (1) aula existe na igreja; (2) `Situacao == EmAberto`, senão 400 "Somente aulas Em aberto podem ser editadas."; (3) a matéria nova pertence **à mesma turma** da aula (trocar de turma moveria presenças de matrículas de outra turma — proibido); (4) o professor tem atribuição ativa de Professor na turma (mesma regra do `CriarAsync`); (5) `AtualizadoEm` automático. |
| Testes `RF45_EditarAulaTests` | Editar Em aberto → 200 e `GET` reflete; Consolidada → 400; Não realizada → 400; matéria de outra turma → 400; professor sem atribuição de Professor → 400; Professor de outra turma → 403. (6 casos.) |
| Front | Botão "Editar" nas linhas Em aberto de `PaginaAulasTurma`, reabrindo o diálogo de nova aula em modo edição; `aulasServico.atualizarAula`. Em Consolidada/Não realizada o botão não aparece (e a API recusa de qualquer jeito). |
| Monografia | **RF45 — Editar Aula** (padrão dos pares): "Permite alterar a data, a matéria, o professor responsável e o tema de uma aula enquanto ela estiver na situação Em aberto." RNFs: 45.1 = texto da 30.1; 45.2 "Somente aulas na situação Em aberto podem ser editadas; aulas Consolidadas ou Não realizadas permanecem bloqueadas até eventual reabertura pelo Administrador (RF33)" (Confiabilidade); 45.3 = texto da 30.2; 45.4 "A matéria informada deve pertencer à mesma turma da aula" (Confiabilidade). **CSU09**: descrição passa a "cadastrar, consultar e editar"; "Atende aos requisitos RF30, RF31 e RF45"; novo **Fluxo Alternativo 1: Editar aula em aberto** (seleciona a aula Em aberto → aciona Editar → altera data, matéria, professor ou tema → o sistema valida → salva) e **Fluxo Alternativo 2: Aula fechada** ("o sistema informa que aulas Consolidadas ou Não realizadas não podem ser editadas"). Diagrama: "Gerir Aulas" já cobre; nada muda. |

**Uma pergunta para você decidir junto:** aula **duplicada** por engano. Editar não resolve; marcar Não realizada polui. A saída limpa é **excluir aula Em aberto sem nenhum registro de presença** (`DELETE /api/aulas/{id}`, 400 se tiver presenças ou não estiver Em aberto; 2 testes; botão só nessas condições). Cabe no mesmo RF45 como FA3, custa pouco e fecha o caso de uso de verdade. Minha recomendação é incluir; se preferir manter o RF só com edição, tudo bem — é a sua chamada.

---

## 3. Parentesco recíproco (lacuna de requisito)

**O que a monografia diz.** RF12 "adicionar, listar e remover vínculos de parentesco entre pessoas"; CSU13 "informando a pessoa relacionada e o tipo de parentesco"; RNF 11.3 dá ao Professor "vínculos de parentesco pertinentes e informações de contato" dos alunos da turma. **O código** grava `Parentesco { PessoaId, ParenteId, TipoRelacionamento }` uma vez, só no cadastro de quem registrou; o tipo é lista editável (Pai, Mãe, Filho(a), Irmão(ã), Cônjuge, Responsável, Outro **ou texto livre**).

**Avaliação.** Concordo em **derivar na leitura, sem gravar os dois lados**: uma fonte de verdade, sem risco de o par ficar inconsistente ao remover um lado, sem migration e sem mudar o DER. O ganho mais importante não é cosmético: pela 11.3, o professor vê os vínculos do **aluno**; se a mãe foi cadastrada e o vínculo registrado no cadastro dela ("Filho(a): João"), hoje o professor abre o João e não vê a mãe — exatamente o contato de que ele precisa. Com a derivação, vê.

**Semântica que precisa ficar escrita** (hoje o RF não define): na tela, o tipo descreve **o parente em relação à pessoa** ("Parente: Maria — Relacionamento: Mãe" = Maria é mãe da pessoa). Assumo isso; se a leitura na igreja for a oposta, a tabela inverte.

| Tipo registrado (parente → pessoa) | Vínculo derivado no cadastro do parente | Regra |
|---|---|---|
| Pai, Mãe | Filho(a) | fixo |
| Filho(a) | Pai / Mãe / "Pai ou mãe" | pelo **sexo da pessoa** (Masculino → Pai; Feminino → Mãe; sem sexo → "Pai ou mãe") |
| Irmão(ã) | Irmão(ã) | simétrico |
| Cônjuge | Cônjuge | simétrico |
| Responsável | Dependente | fixo (rótulo novo, só derivado) |
| Outro ou texto livre | mesmo texto, marcado "(informado no cadastro de X)" | sem inversão |

Regras de exibição: a linha derivada vem com `derivado: true` e **sem botão de remover** (remove-se no cadastro de origem, com tooltip dizendo isso); se já existir um registro explícito no sentido inverso para o mesmo par, a derivada é omitida (sem duplicar). Entra nos dois DTOs: `ParentescoRespostaDto` (gestão) e `PessoaTurmaRespostaDto.Parentescos` (Professor, 11.3).

**Texto:** RNF nova no RF12 — "12.3 O vínculo é registrado uma única vez e apresentado nos cadastros das duas pessoas; no cadastro do parente, o sistema exibe o grau inverso, derivado do tipo registrado e do sexo da pessoa, sem criar um segundo registro." (Confiabilidade). CSU13: um passo "O sistema apresenta também, como derivados, os vínculos registrados a partir de outras pessoas". Esforço: ½ entrega (serviço + 4 testes unitários da tabela de inversos + tela). Prioridade: a mais baixa dos cinco — se o calendário apertar, vai para a seção 9 como trabalho futuro, com a tabela acima já pronta.

---

## 4. Perfil Professor × atribuição (inconsistência)

**O que a monografia diz.** As RNFs operacionais (24.1, 27.1, 30.1–33.1, 35.1…) dizem "perfil administrativo **ou** atribuição ativa na turma (Professor ou Auxiliar)" — lidas ao pé da letra, a atribuição basta. Mas a 4.9 fala em acesso "por perfil e por atribuição", a 5.1 promete testar "o controle por perfil e atribuição", a 11.3 e o CSU14 falam de "o perfil Professor", e o front já se comporta assim: `isGestor` (menu Turmas EBD, Relatórios) exige perfil Professor. **O código da API** (`GarantirAcessoDepartamentoAsync`) libera qualquer perfil com atribuição ativa — uma conta "Usuário" com atribuição de Auxiliar lança chamada e consolida pela API (não pela tela, que não mostra os menus). O seu diagnóstico está certo: a API ficou atrás do front e do texto.

**Avaliação do caminho mínimo: concordo, é o certo.** Renomear ou remover o perfil tocaria a lista de perfis em dezenas de RNFs, nos atores do diagrama e no CSU19 — custo alto para um ganho que o caminho mínimo já entrega: **perfil = área do sistema que a conta alcança; atribuição = sobre quais turmas ela age**. É um desenho defensável na banca (RBAC para as áreas + autorização por recurso para a turma), e é o que a 4.9 já descreve.

Refinamentos que sugiro ao seu recorte:

- **Um lugar só.** A verificação entra em `GarantirAcessoDepartamentoAsync` (que `GarantirAcessoMateriaAsync` e `GarantirAcessoAulaAsync` já reaproveitam): se o perfil não é administrativo **nem Professor**, 403 com mensagem própria ("Esta operação exige perfil Professor ou administrativo.") antes de consultar a atribuição. `GarantirAcessoPessoaAsync` já trata o Usuário comum à parte e não muda.
- **Testes** (`RF13_PerfilOperacionalTests`, 3): conta Usuário com atribuição ativa → 403 em `GET /api/aulas?departamentoId` e em `POST .../presencas`; conta Professor sem atribuição → 403 (já existe em RF11/RF34, só referenciar); conta Professor com atribuição → 200. O caso "Usuário com atribuição" passa a ser **a evidência da 5.1** ("perfil e atribuição").
- **Aviso na tela de Atribuições — pela API, não pelo front.** Pastor e Superintendente gerem atribuições mas não podem listar usuários (RF16 é Admin); então o `AtribuicaoRespostaDto` ganha `perfilConta` (`null` sem conta, senão o perfil), e a tela mostra a tag "Conta com perfil Usuário — não acessa a turma" ou "Sem conta" ao lado do nome. Aditivo, uma consulta a mais na listagem.
- **Dados existentes:** antes de subir, uma consulta no banco de demonstração (atribuições ativas cujo usuário tem perfil Usuário) para ajustar os perfis; a tag acima mostra os casos restantes.
- **Textos** — os seus dois estão bons; dois ajustes de precisão: na RNF nova do RF21, "somente a pessoa com atribuição de **Professor** pode constar como professor responsável de uma aula (RF30)" é exatamente o que `AulaServico.CriarAsync` já exige, então o texto passa a descrever código existente (ótimo). E vale repetir a frase-chave na **RNF 13.x** em vez de só na descrição do RF13, porque RNF é o que vira linha na tabela de testes de Segurança: "13.5 O perfil Professor habilita a área de operação da EBD; as turmas sobre as quais a conta atua são definidas exclusivamente pelas atribuições ativas (RF21). Contas com perfil Usuário não operam turmas, ainda que possuam atribuição." As RNFs x.1 "perfil administrativo ou atribuição ativa" podem ficar como estão: com a 13.5, "atribuição ativa" pressupõe o perfil.
- **4.8/4.9:** nomear o desenho como você propôs — "autorização por papéis para as áreas administrativas e autorização baseada em recurso (atribuição ativa) para a operação de cada turma". Uma frase, sem reescrever a seção.

Local: **7.1**, junto com a FallbackPolicy, como você pediu — mesma camada, mesma entrega, mesma tabela de evidências.

---

## 5. Fluxo de configuração da turma (feedback do PO)

**O que a monografia diz.** **RF20 — Visualizar Detalhes da Turma: "Exibe os detalhes da turma, com alunos matriculados, professores atribuídos e matérias."** RNF 20.2: responsiva. O Plano (4.4) deu RF17–RF20 como OK com a nota "detalhes via matrículas, matérias, aulas e atribuições" — foi uma leitura generosa minha na montagem do Plano: quatro telas separadas, cada uma com "Voltar" para a lista de turmas, não são "os detalhes da turma". O PO descreveu o sintoma exato dessa lacuna. Além disso, a 5.2 (Scrum) diz que "itens novos sugeridos pelos stakeholders entram no Backlog priorizados conforme o seu valor de uso" — o feedback do PO é matéria-prima do capítulo de Resultados, não um desvio.

**Recomendação: a página central é o RF20, entra antes da banca (6.4); o fluxo guiado (wizard) vai para a seção 9.** Desenho de menor custo, sem tocar a API:

- Rota `/departamentos/:id` → `PaginaTurma.vue`: cabeçalho com nome, tipo, status e quatro contadores (alunos ativos, professores/auxiliares, matérias, aulas Em aberto pendentes) a partir dos endpoints que já existem; abaixo, **abas** (PrimeVue `Tabs`) **Alunos · Matérias · Atribuições · Aulas**, cada aba renderizando a página atual como rota filha (`/departamentos/:id/matriculas`, `/materias`, `/atribuicoes`, `/aulas` — as URLs atuais continuam válidas, nada quebra nos E2E nem nos links do painel).
- A lista de turmas passa a abrir a turma (uma ação por linha, não quatro); "Voltar" das abas leva à turma, não à lista. A aba Atribuições só aparece para gestão (RF21 é Admin/Pastor/Superintendente); para o professor, três abas.
- No celular (6.3), as abas viram a navegação natural da turma — é por isso que 6.3 e 6.4 andam juntas.
- Figura da tela: mockup no Figma como o RF44 (Tela de Turma), inserida entre as de turma; renumera as seguintes.
- O wizard ("criar turma → matricular → matéria → atribuição → primeira aula" em passos) é melhoria real, mas custa uma entrega e meia e não tem requisito; seção 9 com uma frase e, se der tempo depois da 7.3, volta.

---

## 6. A observação do `PUT /api/igrejas/{id}` (corpo parcial zera campos)

É o comportamento correto de `PUT` (substituição integral do recurso) e a tela sempre manda os cinco campos; o que zeraria seria um `PUT` parcial feito à mão. Não recomendo `[Required]` nos opcionais — Cidade, Estado, E-mail e Telefone são opcionais pelo RF44, e exigir todos quebraria a tela de uma igreja que não tem telefone. Se quiser deixar documentado: um teste `Atualizar_CampoOmitido_ViraNulo` (3 linhas) e uma frase na 2.4 do Plano ("`PUT` substitui os cinco campos"). Fica para a 7.3, como você já anotou.

---

## 7. Sequência proposta daqui até a banca

| Ordem | Entrega | Conteúdo | Depende de |
|---|---|---|---|
| **6.3** | Responsividade | Drawer + BarraTopo + `.conteudo`; cinco telas com grade fixa; critério da seção 1; `project` mobile no Playwright (E2E-02 por enquanto) | — |
| **6.4** | Turma (RF20) | `PaginaTurma` com cabeçalho e abas; rotas filhas; lista de turmas com uma ação; mockup da tela | 6.3 (abas no celular) |
| **6.5** | RF45 Editar aula | Recorte da seção 2 (com ou sem exclusão de aula vazia, conforme você decidir) | RF45/CSU09 na monografia |
| **6.6** | Parentesco recíproco | Seção 3 | RNF 12.3 na monografia; **corta se apertar** |
| **7.1** | Autorização | FallbackPolicy + perfil × atribuição (seção 4) + `perfilConta` na tela de Atribuições + inventário das evidências já capturadas × faltantes | RNF 13.5 / 21.x na monografia |
| **7.2** | E2E | E2E-04 a E2E-08 (desktop) + E2E-04 em mobile; `storageState` já gravados | 6.3, 6.4, 7.1 |
| **7.3** | Resultados | Tabela CSU → teste → resultado; **procedimento de cópia e restauração** (pg_dump/pg_restore executado uma vez, com evidência — a 5.1 promete); nota do `PUT`; material do capítulo | tudo acima |

Ritmo atual (uma entrega a cada 1–2 dias com a sua validação): 6.3 a 7.3 são sete entregas, fim de outubro. Novembro fica para o texto do TCC 2, a avaliação de Usabilidade com o professor e o PO (que precisa das 6.3 e 6.4 prontas) e a preparação da defesa. Cabe, desde que 6.3 fique no recorte da seção 1 e a 6.6 seja a primeira a cair se algo atrasar.

**O que preciso de você para começar a 6.3:** nada além do ok — não depende de texto. Para a 6.5 e a 7.1, os textos (RF45/CSU09; RNF 13.5, RNF 21.x, frase da 4.8/4.9), no mesmo rito do RF44: você redige, eu reviso, você aplica, eu codo.

---

## 8. Registro para o Plano (texto atual → texto novo)

**8.1 Seção 4.4.** `RF17–RF20 Departamentos | OK | …detalhes via matrículas, matérias, aulas e atribuições.` → `RF17–RF19 Departamentos | OK | — ` e nova linha `RF20 Detalhes da turma | Parcial | Não há tela de detalhes; as quatro telas separadas não cumprem "exibe os detalhes da turma" (feedback do PO, 07/10). Página /departamentos/{id} com cabeçalho e abas — 6.4.`

**8.2 Seção 5, Etapa 6 (conteúdo e estado).** Acrescentar `6.3 Responsividade (RNFs x.y "interface responsiva"; critério: 768 px, fluxo do professor a 360 px sem rolagem horizontal, Drawer no menu, project mobile no Playwright) · 6.4 Página da turma (RF20) · 6.5 RF45 Editar aula (após texto) · 6.6 Parentesco recíproco (RNF 12.3; opcional).` Estado: `Concluída` → `Em andamento — 6.1 e 6.2 concluídas; 6.3 a 6.6 abertas em 08/10/2026 (seção 9 explica a reabertura).`

**8.3 Seção 5, Etapa 7.** `7.1 FallbackPolicy + perfil × atribuição (RNF 13.5) + inventário · 7.2 E2E-04 a E2E-08 e E2E-04 mobile · 7.3 tabela CSU → teste → resultado, procedimento de cópia e restauração (5.1), material de Resultados.`

**8.4 Seção 7.3.** Linha nova: `| E2E-04m Chamada no celular | E2E-04 no project mobile (Pixel 7) | Chamada concluída sem rolagem horizontal; menu em Drawer | CSU10, RNFs x.y responsividade |`.

**8.5 Seção 9.** Novas linhas: `| RF45 | Editar Aula (só Em aberto; opcional: excluir aula Em aberto sem presenças) — RF, RNFs 45.1–45.4 e FA no CSU09 | Antes da 6.5 |` · `| RF12 | RNF 12.3 (vínculo derivado na leitura) e passo no CSU13 | Antes da 6.6 |` · `| RF13 / RF21 / 4.8–4.9 | RNF 13.5 (perfil Professor habilita a operação; turmas pelas atribuições), RNF 21.x (Professor e Auxiliar com as mesmas permissões; só Professor é responsável de aula), frase sobre RBAC + autorização por recurso | Antes da 7.1 |` · `| RF20 | Figura da Tela de Turma (abas) no protótipo; renumeração | Com a 6.4 |` · `| Trabalho futuro | Fluxo guiado de configuração da turma (wizard); responsividade polida das telas administrativas | Seção de trabalhos futuros do TCC 2 |`.
