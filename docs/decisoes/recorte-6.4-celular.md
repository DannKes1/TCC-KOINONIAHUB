# KoinoniaHub · Recorte da 6.4 — Página da Turma + fluxo operacional no celular

Para aprovação antes de codar. Acompanha `wireframes-360px.png` (seis telas, baixa fidelidade, só para julgar estrutura e hierarquia — não é o visual final, que segue o PrimeVue e a identidade atual).

---

## 0. Conferência

Zip 123dbbd9: 6.3 exatamente como entregue + os onze prints em `docs/evidencias/`. Só uma observação: `docs/testes.md` ainda não tem o bloco da 6.3 (57 / 8 / 113 e os cinco manuais) — vale acrescentar antes do próximo commit, para o inventário da 7.1 não ter lacuna.

---

## 1. Monografia (10) — revisão dos textos novos

Li os três blocos inteiros, mais RF44, CSU24, figuras e a padronização das situações.

| Bloco | Veredito | Observações |
|---|---|---|
| **RNF 21.3** (Professor = Auxiliar nas operações; só atribuição Professor é responsável de aula) | **OK** | Bate com o código: `AulaServico` já exige `Funcao == "Professor"` para o `ProfessorId`. |
| **RNF 21.4** (atribuição só produz efeito para conta Professor ou administrativa; conta Usuário não opera turma) | **OK** | É exatamente a regra que a 7.1 implementa em `GarantirAcessoDepartamentoAsync`: administrativo passa; Professor exige atribuição ativa; Usuário → 403 mesmo com atribuição. Colocar em 21.4 (efeito da atribuição) em vez de 13.5 (cadastro de conta) foi a escolha certa. |
| **RNF 21.5** (listagem sinaliza pessoa sem conta ou com perfil Usuário) | **OK** | `perfilConta` no `AtribuicaoRespostaDto` + tag na tela de Atribuições (7.1). |
| **Seção 4.9**, frase das duas camadas | **OK** | "papéis para as áreas, recurso para as turmas" — é o que a banca precisa ler para entender o 403 por turma. |
| **RF45 + RNFs 45.1–45.4, CSU09 FA1–FA3** | **OK** | FA3 cobre a chamada direta à API ("recusa e informa que a situação atual da aula não permite a alteração") — é o que o teste `Editar_AulaConsolidada_Retorna400` vai provar. |
| **RNF 45.5** | **Corrigir** | Segunda oração contradiz a **RNF 33.7**: "havendo registros, a aula deve ser marcada como Não realizada", mas a 33.7 diz que Não realizada só é permitida *sem* registros. Pelo código, uma aula com chamada salva não pode ser excluída nem marcada como Não realizada — só consolidada. Texto sugerido: `45.5 — A exclusão é permitida somente para aula sem nenhum registro de presença, mesma condição que a RNF 33.7 impõe à marcação como Não realizada; aula com registros permanece Em aberto até ser consolidada.` Categoria: Confiabilidade (como 32.2/33.2/33.7), não Conformidade Legal. |
| **RNF 45.2** | Ajuste menor | Categoria Segurança → **Confiabilidade**, por coerência com 32.2 e 33.2 (mesma regra de estado). |
| **RNF 12.3 / 12.4, CSU13** | **OK** | "derivado do tipo registrado e do sexo da pessoa" = sexo da pessoa em cujo cadastro o vínculo foi registrado — é a tabela de inversos aprovada. FA1 de remoção correto. |
| **RF44 / CSU24 / `<<include>>` / Figuras 34–36** | **OK** | — |
| **Padronização Em aberto / Consolidada / Não realizada** | Quase | Ficaram três minúsculas: CSU10 FA1 ("consolidada ou não realizada"), CSU11 passo 2 ("se a aula está em aberto") e FA2/FA3 ("aula em aberto", "para em aberto"). Cosmético. |

**Lacuna que o 45.5 expôs (não é erro do texto, é do produto):** uma chamada salva na aula errada não tem saída — não dá para excluir (45.5), nem marcar Não realizada (33.7), nem limpar (o "Limpar" grava ausências; nada apaga registros). Hoje o caminho é consolidar dado errado ou pedir reabertura. Sugiro registrar na **seção 9** como trabalho futuro: "Limpar chamada" que remove os registros de presença enquanto a aula está Em aberto (mudança de regra: hoje nenhum endpoint apaga presença). Não entra agora.

**Decisão necessária — 6.6 × texto já na monografia.** Você aplicou RNF 12.3/12.4 e o CSU13 e, ao mesmo tempo, mandou a 6.6 para a seção 9. Se a 6.6 não for implementada, a monografia passa a especificar um comportamento que o sistema não tem, e a banca vê isso na tabela CSU → teste. Opções: **(a)** manter a 6.6 como última entrega antes da 7.1, no menor recorte (só API: derivação na leitura + `derivado: true` + recusa de remoção; front só mostra a tag "derivado" e esconde o botão) — é meio dia, sem migration; **(b)** reverter 12.3/12.4 e o passo do CSU13 para o texto anterior; **(c)** manter o texto e declarar "previsto, não implementado" nos Resultados — a pior. Recomendo **(a)**; se apertar, **(b)** antes da defesa. Com a correção do 45.5 e essa decisão, **7.1, 6.5 e 6.6 ficam destravadas** pelos textos.

---

## 2. O problema e o princípio da solução

A 6.3 cumpriu o critério que fixamos (nada transborda a página), mas o que ficou é uma tela de computador espremida: a informação está lá, a ação não. Nas quatro telas que você citou o padrão de falha é o mesmo — **tabela com a coluna de ação no fim**. Em 360 px a ação sempre cai fora.

Princípio para o celular: **cada item é um cartão com um botão nomeado**, e a ação principal da tela fica onde o dedo está. Concretamente:

1. **Lista → cartões** abaixo de 768 px; a `DataTable` continua intacta no desktop. Um cartão = título, uma linha de contexto, chips de situação e uma linha de ações: botão principal com nome (`Fazer chamada`, `Abrir turma`, `Minha frequência`) e, quando houver mais de duas ações, um **⋮** com o resto.
2. **Chamada** vira uma lista de alunos em que **tocar na linha marca a presença**, com uma **barra fixa** no rodapé (`Salvar chamada` · `Consolidar`) e um contador "Presentes: 12 de 20". A observação só aparece quando o professor pede ("+ observação").
3. **Página da Turma (RF20)** é o centro: cabeçalho com nome, situação, dois contadores, equipe (professores e auxiliares — RF20 pede "professores atribuídos") e as abas Alunos · Matérias · Aulas · Atribuições. É para onde "Abrir turma" leva, no Painel, em Minhas Turmas e em Turmas EBD.

Fazer junto com a 6.4 é o certo: a aba Aulas, Minhas Turmas e o Painel são os mesmos cartões, e separar criaria dois retrabalhos.

---

## 3. Desenho (ver wireframes)

### 3.1 Página da Turma — `/departamentos/:id`

- **Rotas filhas** mantendo as URLs atuais: `…/matriculas` (aba Alunos), `…/materias`, `…/aulas`, `…/atribuicoes`. `/departamentos/:id` redireciona para `…/aulas` (centro operacional; a gestão toca em Alunos quando está configurando). Nada que já aponte para as quatro URLs quebra — o ⋮ de Turmas EBD continua funcionando e passa a abrir dentro da página.
- **Cabeçalho:** voltar (← Turmas EBD para gestão, ← Minhas Turmas para o professor), nome, chips Tipo e Ativa/Inativa, contadores **Alunos ativos** e **Aulas em aberto** (no desktop, quatro: + Matérias e Equipe), linha "Professora: … · Auxiliar: …". Dados vêm dos endpoints que já existem (alunos, matérias, atribuições, aulas) — nenhum endpoint novo.
- **Abas:** `Tabs` do PrimeVue ligado à rota (rolável no celular). **Atribuições** só para gestão (RNF 21.1); o professor vê a equipe no cabeçalho (RF20/RF23) e não a aba.
- **As quatro páginas atuais viram o conteúdo das abas** sem reescrita: ganham um modo "dentro da turma" (meta da rota) que esconde o próprio cabeçalho e o link de voltar, mantendo os botões (`Nova aula`, `Matricular`, `Nova matéria`).
- **Minha Frequência** continua rota própria (o aluno não tem acesso à página da turma — 31.1).
- **Desktop:** mesma página com abas; dentro das abas, as tabelas de hoje, sem alteração.

### 3.2 Telas do fluxo, no celular

| Tela | Cartão (título · contexto · chips) | Ação principal | Secundárias |
|---|---|---|---|
| **Painel do professor** | Aulas em aberto: data · turma e matéria · Pendente | `Fazer chamada` | — |
|  | Minhas turmas: nome · "N aulas · N em aberto · última dd/mm" | `Abrir turma` | — |
| **Minhas Turmas** | nome · tipo e responsável · chip do vínculo | professor: `Abrir turma`; aluno: `Minha frequência` | professor: `Minha frequência` (texto) |
| **Aba Aulas** | data por extenso · matéria e professor · tema · situação (+ Pendente). (O "15 presentes, 3 faltas" do wireframe **não entra**: a listagem de aulas não devolve totais e não vou criar endpoint para isso.) | Em aberto: `Fazer chamada`; fechada: `Ver chamada` | ⋮ Ver presenças · Consolidar · Marcar não realizada · Reabrir (Admin) — e na 6.5 Editar · Excluir entram aqui, sem mais um ícone |
| **Chamada** | ver 3.3 | barra fixa `Salvar chamada` | `Consolidar` na barra; `Marcar todos presentes` e `Limpar` como botões de texto acima da lista; `Recarregar` no cabeçalho |
| **Presenças da Aula** | nome · observação e "registrado em" · chip Presente/Ausente | `Ver chamada` (topo) | — |
| **Aba Alunos** | nome · chips Matrícula ativa / Pessoa ativa | `Histórico` | `Inativar` (contorno vermelho) |
| **Aba Matérias** | nome · ordem · chip Ativa | `Editar` | — |
| **Painel do aluno** | Minhas turmas: nome · tipo · chip Aluno; histórico: data · turma e matéria · chip presença | `Minha frequência` | — |
| **Minha Frequência** | histórico: data · tema · situação · chip Presente/Falta; pendentes num aviso | — | — |

Listas longas (Aulas, histórico): dez cartões e `Mostrar mais (N restantes)` — sem paginador numérico no celular. Filtros e busca ficam, em largura total.

### 3.3 Chamada no celular (CSU10 + CSU11)

- Cabeçalho curto: "Chamada — 11/10/2026", matéria e professor, chips de situação.
- Linha de resumo: **Presentes: 2 de 4** e **Visitantes [ 0 ]** compacto (o texto explicativo encurta para uma linha).
- Aviso da RNF 32.5 permanece, compacto.
- `✓ Marcar todos presentes` · `Limpar` (texto).
- **Lista de alunos:** linha de 56 px, nome à esquerda, caixa de 40 px à direita; **tocar em qualquer ponto da linha alterna presente/ausente**; fundo verde-claro quando presente; "Sem registro" (retorno do 400 da consolidação) como chip na linha, como hoje. Sob o nome, `+ observação` abre o campo na própria linha; com texto, mostra "Obs.: …".
- **Barra fixa** no rodapé enquanto a aula estiver Em aberto: `Salvar chamada` (verde, largura maior) e `Consolidar` (contorno vermelho). Em aula fechada a barra some e a lista fica somente leitura, como hoje. Implementação: `position: fixed` no celular com respiro no fim da lista (o `overflow` da área de conteúdo impede `sticky`).
- Toast e diálogo de confirmação como hoje.
- **Desktop: tabela, cabeçalho e botões como estão.**

### 3.4 Peças reutilizáveis (front)

- `usarTelaCompacta()` — composable com `matchMedia("(max-width: 768px)")`; é o único interruptor entre tabela e cartões.
- `CartaoItem.vue` — título, contexto, chips e linha de ações (slots).
- `MenuAcoes.vue` — botão ⋮ de 44 px + `Menu` popup do PrimeVue com itens `{ label, icon, command, visible }` (o mesmo `Menu` que Turmas EBD já usa).
- `BarraAcoesFixa.vue` — a barra da chamada.
- `LinhaChamada.vue` — a linha tocável da chamada (caixa, observação expansível, estado somente leitura).
- `PaginaTurma.vue` + rotas filhas; nas quatro páginas existentes, só o modo "dentro da turma".

Sem biblioteca nova, sem endpoint novo, sem mudança de contrato da API, sem migration.

---

## 4. Critério de pronto (mensurável, substitui o da 6.3 para as telas operacionais)

Telas operacionais = Painel (professor e aluno), Minhas Turmas, Página da Turma (abas Alunos, Matérias, Aulas), Chamada, Presenças da Aula, Minha Frequência. A 360 × 640:

1. **Nenhuma rolagem horizontal — nem da página, nem de qualquer container** (modo estrito do `semRolagemHorizontal`: só a faixa de abas pode rolar). Na 6.3 a rolagem interna das tabelas era aceita; aqui não.
2. **Cada item de lista tem uma ação principal com rótulo**, ≥ 44 px de altura, visível sem rolar para o lado.
3. **Chamada:** marcar presença = um toque na linha; `Salvar chamada` e `Consolidar` visíveis em qualquer ponto da rolagem; contador de presentes correto antes de salvar.
4. **Desktop idêntico** nas tabelas e cabeçalhos das telas existentes (conferência visual + E2E-01..03 verdes); a única tela nova no desktop é a Página da Turma.
5. **Telas administrativas** (Pessoas, Usuários, Igreja, Relatórios, Turmas EBD, Importação, aba Atribuições): como na 6.3, utilizáveis com rolagem; não entram no modo estrito.

Verificação: **E2E-M1 reescrito** — professor: Painel (cartão "Fazer chamada") → Minhas Turmas (`Abrir turma`) → Página da Turma (abas Alunos e Matérias abrem; aba Aulas) → aula criada pela API → Chamada (toque na linha marca; `+ observação`; `Salvar chamada` na barra; `Consolidar` → confirmação → situação Consolidada e barra some) → Presenças; aluno: Painel → Minha Frequência. Prints `RNF-responsividade-*.png` regenerados (+ `turma-aulas`, `turma-alunos`, `turma-materias`, `chamada-consolidada`). Vitest: `usarTelaCompacta`, `MenuAcoes`, `LinhaChamada` (toque alterna, observação, somente leitura), `PaginaTurma` (aba Atribuições por perfil, redirecionamento) — cerca de dez casos. xUnit: inalterado (113).

---

## 5. Entregas e estimativa

Três entregas em ~3 dias de calendário, cada uma validável sozinha e sem quebrar a anterior.

| Entrega | Conteúdo | Você valida |
|---|---|---|
| **6.4-A** (dia 1) | Peças reutilizáveis; **Página da Turma** com abas e rotas filhas (desktop e celular); aba **Aulas** em cartões com ⋮; **Minhas Turmas** e **Painel do professor** em cartões; "Abrir turma" apontando para a página; Turmas EBD com `Abrir` no ⋮. | Desktop: tabelas iguais, página da turma nova. Celular: Painel → Minhas Turmas → turma → Aulas sem rolar para o lado, ações nomeadas. |
| **6.4-B** (dia 2) | **Chamada** (lista tocável, observação expansível, contador, barra fixa) e **Presenças** em cartões; Painel do aluno e **Minha Frequência** em cartões. | Fazer uma chamada inteira no celular, salvar e consolidar sem procurar botão; aluno vê a frequência. |
| **6.4-C** (dia 3) | Abas **Alunos** e **Matérias** em cartões; `Mostrar mais`; **E2E-M1 reescrito** com modo estrito; prints; `docs/decisoes/etapa-6.md` (1.4); registro para o Plano; descrição da Tela de Turma para o seu mockup (Figura nova do protótipo). | `npm run test:e2e` = 8 (3 + 5; 9 se o teste do professor for dividido em dois), Vitest ≈ 67, prints copiados; mockup feito a partir da tela real. |

Riscos conhecidos: `Tabs` ligado a rotas filhas (resolvido com `router-link` dentro das abas — padrão documentado do PrimeVue); a barra fixa × `overflow` da área de conteúdo (testo no sandbox a 360 px antes de entregar); E2E-M1 fica mais longo (um teste do professor com ~12 passos — divido em dois se passar de 40 s).

Se o orçamento apertar, a ordem de corte é: `Mostrar mais` (fica tudo numa lista) → cartões de Matérias (fica tabela "utilizável") → cartões do histórico do aluno. A Chamada e a Página da Turma não se cortam — são o que a 5.1 vai avaliar.

---

## 6. Decisões suas (com a minha recomendação)

1. **Aba padrão de `/departamentos/:id`:** `Aulas` para todos (recomendo) ou `Alunos` para gestão e `Aulas` para professor?
2. **⋮ no desktop da aba Aulas, já agora?** Seu limite é "desktop não muda", e eu respeito. Mas a 6.5 acrescenta Editar e Excluir: seis ícones por linha. Recomendo trocar os ícones secundários pelo ⋮ **no desktop também, nesta entrega** (fica `Fazer/Ver chamada` + ⋮) — uma mudança pequena e única, em vez de mexer na tabela duas vezes. Se preferir, fica para a 6.5.
3. **Consolidar com alterações não salvas.** Hoje Consolidar não salva o que está na tela; no desktop o professor salva e depois consolida. No celular isso vira uma armadilha. Opções: **(a)** Consolidar salva as alterações pendentes antes e a confirmação diz "A chamada será salva e consolidada" (recomendo; vale nos dois layouts; CSU10 passo 5 continua acontecendo, só que junto); **(b)** `Consolidar` desabilitado enquanto houver alteração não salva, com a dica "Salve a chamada antes de consolidar".
4. **6.6:** (a) último item antes da 7.1, recorte mínimo; ou (b) reverter o texto. (Seção 1.)
5. **45.5 e 45.2:** aplicar as redações da seção 1? E a lacuna "chamada na aula errada" vai para a seção 9 como trabalho futuro?

Se você só responder "1 Aulas, 2 sim, 3 a, 4 a, 5 sim", começo a 6.4-A.

---

## 7. Registro para o Plano (texto atual → texto novo) — a aplicar na v1.2 junto com o 8.x da avaliação

**7.1 Seção 5, Etapa 6, conteúdo.** `6.4 Página da turma (RF20)` → `6.4 Página da Turma (RF20: /departamentos/:id com cabeçalho, equipe e abas Alunos · Matérias · Aulas · Atribuições como rotas filhas, URLs preservadas) e fluxo operacional no celular (critério estrito: Painel, Minhas Turmas, Página da Turma, Chamada, Presenças, Minha Frequência a 360 px sem rolagem horizontal de nenhum container, ação principal nomeada ≥ 44 px em cada item, chamada com toque na linha e barra fixa Salvar/Consolidar; desktop inalterado; administrativas utilizáveis com rolagem). Entregas 6.4-A, B e C.` Retirar `6.6 Parentesco recíproco (RNF 12.3; opcional)` da Etapa 6 **ou** mantê-la como última, conforme a decisão 4.

**7.2 Seção 7.2, tabela.** Novas linhas: `| Interruptor de layout compacto (usarTelaCompacta) | ≤ 768 px → compacto; acima → tabela; reage à mudança | 6 |`, `| Linha da chamada no celular | Toque alterna presença; "+ observação" abre o campo; somente leitura desabilita | 6 |`, `| Página da Turma | Aba Atribuições só para gestão; /departamentos/:id redireciona para a aba padrão | 6 |`.

**7.3 Seção 7.3, linha E2E-M1.** Passos: `… → Página da Turma (abas Alunos, Matérias, Aulas) → Chamada (toque na linha, observação, Salvar chamada e Consolidar na barra fixa) → Presenças …`; Verifica: `nenhuma rolagem horizontal, nem interna; ação principal nomeada em cada cartão; barra fixa visível; aula consolidada ao fim`.

**7.4 Seção 9.** `| RF45 | … | Antes da 6.5 |` → `| RF45 | Texto aplicado em 09/10/2026; corrigir RNF 45.5 (contradição com 33.7) e categoria da 45.2 | Antes da 6.5 |`; `| RF12 | … | Antes da 6.6 |` → `| RF12 | Texto aplicado em 09/10/2026 (RNF 12.3/12.4, CSU13). Implementação: decisão 4 (6.6 mínima ou reverter o texto) |`; `| RF13 / RF21 / 4.8–4.9 | … |` → `| RF21 / 4.9 | Texto aplicado em 09/10/2026 (RNF 21.3–21.5; 4.9) | Resolvido |`; nova: `| RF32/RF33 | Trabalho futuro: "Limpar chamada" que remove registros de presença de aula Em aberto (hoje chamada salva na aula errada só sai por consolidação) | Trabalhos futuros do TCC 2 |`; nova: `| RF20 | Figura "Tela de Turma" (abas) no protótipo, feita a partir da tela da 6.4-C; renumeração | Com a 6.4 |`.

---

## 8. Fora do recorte

Pessoas, Usuários, Igreja, Relatórios EBD, Turmas EBD (lista), Importação e a aba Atribuições continuam "utilizáveis com rolagem"; nenhuma figura nova de celular no protótipo (o capítulo de Resultados pode usar os prints a 360 px como evidência de RNF 3.2/6.4/20.2/28.2); wizard de configuração da turma segue na seção 9; nenhuma mudança na API.
