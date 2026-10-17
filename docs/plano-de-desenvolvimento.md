# KoinoniaHub · Plano de Desenvolvimento e Testes do TCC 2 · v1.5

**KoinoniaHub — Plano de Desenvolvimento e Testes do TCC 2**

*Base de conhecimento para o projeto de codificação assistida*

Referência normativa: Monografia — versão de 10/10/2026 (RF1 a RF45, CSU01 a CSU24, com RF44, RF45, RNFs 12.3/12.4 e 21.3–21.5). Cópia de leitura em `docs/monografia.md`; a fonte oficial é o .docx do autor. Correções de redação ainda não aplicadas no .docx estão na seção 9.

Estado do código analisado: repositório TCC-KOINONIAHUB, branch `main` após o fechamento da 6.5 (RF45, commitada em 16/10/2026). Última migration: PessoaEnxuta (26/09/2026) — nenhuma depois. Testes: **123 xUnit · 105 Vitest (20 arquivos) · 10 Playwright (3 desktop + 7 celular)**.

Autor: Daniel Rocha · IFRO Campus Ji-Paraná · CST-ADS

**Versão 1.5 — 16 de outubro de 2026**

| Versão | Data | Mudanças |
| --- | --- | --- |
| 1.0 | 24/09/2026 | Versão inicial. |
| 1.1 | 28/09/2026 | Seção 1.1: novos itens "Julgamento dentro do escopo", "Documento vivo" e "Resumo de decisões por etapa". Seção 6: exceções explícitas ao "não reabrir discussão". Seção 7.5: pasta docs/decisoes/. |
| 1.2 | 28/09/2026 | Matriz da seção 4 e estado das etapas (seção 5) atualizados após verificação do código: etapas 0 e 1 concluídas, etapa 2 em andamento. Versões de pacotes resolvidas registradas nas seções 2.2 e 9. |
| 1.3 | 10/10/2026 | Retrato do código após a 6.4-A. Aplicados os registros acumulados: avaliação pré-Etapa 7 (8.1–8.5), recorte da 6.4 (7.1–7.4), LEIA-ME da 6.3 (5.1–5.7) e da 6.4-A (4.1–4.5). Baseline RF1–RF45 / CSU01–CSU24. Seções 2.3, 2.4, 3 e 4 refeitas contra o código; seção 5 com as etapas 0–5 concluídas, Etapa 6 desdobrada (6.1 a 6.6) e Etapa 7 redefinida (7.1 a 7.3); seção 6 com as especificações concluídas marcadas e as novas (RF44, responsividade, Página da Turma, RF45, parentesco derivado, perfil × atribuição, cópia e restauração); seção 7 com o inventário real de testes e a configuração real do Playwright (http, ambiente E2E, PostgreSQL local, dois projetos); seção 9 sem os itens já aplicados na monografia. Cópia oficial passa a ser `docs/plano-de-desenvolvimento.md` no repositório. |
| 1.4 | 14/10/2026 | Fechamento da Etapa 6.4 (6.4-B de 10/10, corretiva de 12/10, ajuste de 13/10 e 6.4-C de 14/10). Aplicados os registros dos quatro LEIA-MEs. Contrato: `DepartamentoId`/`NomeDepartamento` no `AulaRespostaDto` e `Registrado` no item da chamada. Seção 6.12 com a chamada nos dois layouts, barra `sticky`, confirmações no tema, "Sem registro" desde a carga e o CSU11 passo 2 novo. Seções 7.1–7.3 com os totais 114 / 102 / 3 + 7, E2E-M2 e o modo estrito. Seção 9 sem os itens resolvidos. A partir da 6.5 o código é desenvolvido no Claude Code, com este Plano e `docs/monografia.md` como contexto; o projeto do assistente web continua para monografia, Plano e decisões de requisito. |
| 1.5 | 16/10/2026 | Fechamento da Etapa 6.5 (RF45, 15–16/10/2026; primeira entrega feita no Claude Code). Contrato: PUT e DELETE /api/aulas/{id}; `Conteudo` e `PossuiPresencas` no `AulaRespostaDto` (aditivos, decisão do autor em 15/10). Seção 6.14 atendida (9 casos; exclusão → 204). Seções 7.1–7.2 com os totais 123 / 105 (20 arquivos). Monografia revisada em 15/10 (CSU11 passos 2–6 reorganizados, CSU12 aba Alunos, maiúsculas das situações): seção 9 sem esses itens e com a proposta do CSU09 passo 4. Rastros "CSU11 passo 2" → "passos 2–4". |

---

## 1. Como usar este documento

Este documento acompanha a monografia dentro de um projeto de codificação assistida. Ele registra o que o código faz hoje, o que falta para ficar igual ao que a monografia promete, a ordem em que o trabalho será feito e como cada entrega será testada. Ele não substitui a monografia: os requisitos funcionais (RF1 a RF45), os requisitos não funcionais (RNFs numerados por RF), os casos de uso (CSU01 a CSU24) e o modelo de dados (DER) continuam sendo definidos exclusivamente pela monografia.

A cópia oficial deste documento é `docs/plano-de-desenvolvimento.md` no repositório; a cópia anexada ao projeto do assistente é substituída a cada nova versão.

### 1.1 Regras para o assistente do projeto

- **Fonte da verdade.** Em qualquer divergência entre este documento, o código e a monografia, a monografia vence. Se uma mudança de código exigir mudar a monografia, registrar a pendência na seção 9 em vez de silenciosamente divergir. RF novo só é codificado depois que o texto estiver aplicado na monografia.
- **Uma etapa por vez.** Trabalhar na ordem da seção 5. Não iniciar a etapa seguinte sem a definição de pronto da etapa atual.
- **Julgamento dentro do escopo.** No que este documento e a monografia não fixam — nomes internos, organização de um método, mensagens não especificadas, estrutura dos testes além do exigido — o assistente decide sozinho pela melhor prática, sem perguntar nem listar alternativas. No que os dois fixam, não muda sem avisar (seção 6). Melhorias fora do escopo da monografia (funcionalidades novas, refatorações amplas) não são propostas durante as etapas; se valerem registro, viram uma linha na seção 9 marcada "trabalho futuro".
- **Entregas completas.** Ao alterar um arquivo, entregar o arquivo inteiro ou o trecho com contexto suficiente para aplicar sem adivinhar. Nunca entregar pseudocódigo.
- **Convenções do repositório.** Nomes de classes, métodos, rotas, DTOs e mensagens em português, como já está no código (PessoaServico, GarantirAcessoDepartamentoAsync, api/departamentos). Camadas: Controllers → Aplicacao/Servicos → Dominio → Infraestrutura.
- **Sem dependências novas sem justificativa.** As adições feitas até aqui foram as dos testes (xUnit, Vitest, Playwright) e as versões 10.x dos pacotes já usados.
- **Toda etapa termina com** os comandos da seção 7.4 verdes (dotnet build e test; vue-tsc -b, npm run test e npm run build; npm run test:e2e quando a etapa toca tela ou fluxo) e com os testes manuais do fluxo tocado.
- **Testes manuais descritos clique a clique.** Cada entrega traz, para o autor executar: preparação (quais terminais, qual perfil de execução da API, qual usuário), passos numerados com o menu, botão e valor digitado, o resultado esperado de cada passo e os prints a tirar (nome do arquivo no padrão da seção 7.5). Para celular: DevTools (F12) → Ctrl+Shift+M → Responsive 360 × 640.
- **Commits em blocos.** O autor faz todos os commits. A entrega propõe blocos que compilam sozinhos (API · testes · front · E2E · docs), com os comandos `git add` / `git commit` prontos e mensagens sem acentos citando RF/RNF/CSU. Nunca incluir `api/KoinoniaHub.API/appsettings.E2E.json` (connection string local do autor).
- **Evidências.** Para cada RNF de Segurança implementada, guardar um print ou uma requisição/resposta para o capítulo de Resultados do TCC 2 (ver seção 7.5).
- **Documento vivo.** Ao concluir uma entrega, o assistente fecha o LEIA-ME com um "Registro para o Plano" no formato texto atual → texto novo (seções 4, 5, 7 e 9). O autor aplica os registros na revisão seguinte do Plano. A matriz da seção 4 é um retrato do código na data do cabeçalho: em divergência, o código vence para "o que existe hoje" e a monografia continua vencendo para "o que deve existir".
- **Resumo de decisões por etapa.** Ao concluir uma entrega, o assistente acrescenta a seção dela em `docs/decisoes/etapa-N.md` com: as decisões de implementação tomadas dentro da sua margem de julgamento e o porquê de cada uma (2 a 4 linhas por decisão), os arquivos criados ou alterados, e o que o autor deve conseguir explicar sobre essa etapa numa arguição de banca. O autor lê o código da etapa junto com esse resumo antes de autorizar a entrega seguinte. Avaliações e recortes aprovados ficam em `docs/decisoes/` com nome próprio (ex.: `avaliacao-pre-etapa7.md`, `recorte-6.4-celular.md`).

## 2. Contexto do projeto

### 2.1 O produto

Sistema web de diário de classe da Escola Bíblica Dominical (EBD) para uma igreja local (~200 pessoas, ~172 alunos em ~10 turmas). Multi-igreja por isolamento de dados (IgrejaId em todas as entidades e no token). Perfis de acesso: **Admin**, **Pastor**, **Superintendente** (administrativos), **Professor** e **Usuario**. Funções de atribuição na turma: **Professor** e **Auxiliar**. Visitantes não são cadastrados; são apenas contados na chamada. Uso cotidiano do professor é no celular (seção 6.12).

### 2.2 Stack (alvo definido na monografia)

| **Camada** | **Tecnologia prometida** | **Estado do código em 14/10/2026** |
| --- | --- | --- |
| API | .NET 10 (LTS), ASP.NET Core Web API, EF Core 10, PostgreSQL via Npgsql | Conforme: net10.0; JwtBearer, EF Tools e EF Design em 10.0.12; Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3; Swashbuckle.AspNetCore 10.2.3; BCrypt.Net-Next 4.0.3 |
| Front | Vue 3, TypeScript, Vite, PrimeVue, axios | Conforme: vue 3.5, primevue 4.5, vite 7, vue-router 5, TypeScript 5.9, axios, chart.js |
| Autenticação | JWT (8 h) em cookie HttpOnly + Secure + SameSite=None; CORS com credenciais para origens configuradas | Conforme (AuthController, TokenServico, Program.cs), com verificação de Origin (RNF 2.6), token fora do corpo das respostas e filtro global do termo (seção 6.3) |
| Senhas | BCrypt com fator de trabalho ≥ 10 | Conforme (BCrypt.Net-Next, fator padrão 11) |
| Convites | Token aleatório, só hash SHA-256 no banco, 7 dias, uso único | Conforme (AuthServico, UsuarioServico.GerarConviteAsync); único meio de redefinir acesso (RF15) |
| Testes | xUnit, Vitest, Playwright | xunit 2.9.3 + Mvc.Testing e EF Sqlite 10.0.12; vitest 5, @vue/test-utils 2.5, jsdom 30; @playwright/test 1.63 |
| Banco local | PostgreSQL | Desenvolvimento e E2E no PostgreSQL 18 instalado na máquina do autor (bancos separados; o E2E usa `koinoniahub_e2e`) |

### 2.3 Estrutura do repositório

```
TCC-KOINONIAHUB/
  api/KoinoniaHub.API.sln
  api/KoinoniaHub.API/
    Controllers/                 (Auth, Termo, Usuarios, Pessoas, Parentescos, PresencasPessoa, Departamentos,
                                  Matriculas, Atribuicoes, Materias, Aulas, Chamadas, Relatorios, MeusDados, Igrejas)
    Aplicacao/DTOS/Requisicoes/  Aplicacao/DTOS/Respostas/
    Aplicacao/Servicos/Implementacoes/  (AuthServico, UsuarioServico, PessoaServico, PessoaImportacaoServico,
                                  AulaServico, ChamadaServico, MatriculaServico, RelatorioEbdServico,
                                  AceiteTermoServico, IgrejaServico, TokenServico ...)
    Aplicacao/Seguranca/         (UsuarioAutenticado, AutorizacaoEbdServico — GarantirAcessoDepartamento/Materia/
                                  Aula/PessoaAsync; ExigeAceiteTermoFiltro; PermitirSemAceiteTermoAttribute)
    Aplicacao/Excecoes/          (ChamadaIncompletaException)
    Dominio/Entidades/Models/    (Igreja, Usuario, Pessoa, Parentesco, Departamento, Materia, Aula, SituacaoAula,
                                  AlunoDepartamento, Presenca, Atribuicao, AceiteTermo)
    Dominio/Termos/              (TermosDeUso — versões e texto do termo)
    Dominio/Interfaces/Repositorios/   Infraestrutura/Dados/ (KoinoniaHubDbContext)   Infraestrutura/Repositorios/
    Migrations/                  (Inicial ... SituacaoAula 25/09, AceitesTermo 26/09, PessoaEnxuta 26/09)
    Program.cs (public partial class Program), appsettings.json, appsettings.Development.json,
    appsettings.E2E.json (local, nunca commitado com a string do autor), Properties/launchSettings.json (perfis https e e2e)
  api/KoinoniaHub.API.Tests/     (xUnit; Infraestrutura/ com KoinoniaHubWebApplicationFactory, ContextoDeTeste,
                                  sementes; 21 classes — seção 7.1)
  web/
    src/aplicacao/               (rotas/ com guards; armazenamentos/ (store de autenticação); servicos/; dominio/
                                  (situacaoAula, indicadoresPresenca, chamada); composables/ (usarTelaCompacta,
                                  usarMostrarMais, usarAcaoUnica); servicos/notificacoes.ts (toasts e confirmarAsync))
    src/components/              (layout/: LayoutPrincipal, MenuLateral, BarraTopo; ui/: CartaoItem, MenuAcoes, BarraAcoesFixa,
                                  TagSituacaoAula, BotoesExportacao, PageHeader ...; termo/: TermoUsoSigilo;
                                  chamada/: LinhaChamada); App.vue com ConfirmDialog e Toast globais no tema
    src/paginas/                 (publico/: PaginaLogin, PaginaCadastroInicial, PaginaPrimeiroAcesso; privado/:
                                  PaginaPainel, PaginaMinhasTurmas, PaginaTermo, pessoas/, usuarios/, igreja/,
                                  departamentos/ (PaginaTurma),
                                  matriculas/, atribuicoes/, materias/, aulas/, chamada/, relatorios/, meus-dados/,
                                  minha-frequencia/)
    e2e/                         (apoio/ (ambiente, api, semente, celular — semRolagemHorizontal), global-setup.ts, .auth/ (fora do git),
                                  e2e-01..03-*.spec.ts, e2e-m1-celular.spec.ts, e2e-m2-largura-administrativas.spec.ts)
    playwright.config.ts, vite.config.ts, package.json, tsconfig*.json
  docs/
    plano-de-desenvolvimento.md  (este documento — cópia oficial)
    monografia.md                (cópia de leitura gerada do .docx)
    testes.md                    (registro das execuções por etapa)
    decisoes/                    (etapa-0.md a etapa-6.md; avaliacao-pre-etapa7.md; recorte-6.4-celular.md)
    evidencias/                  (RNF-*.png, RF*-*.png, CSU*-*.png, RNF-responsividade-*.png)
```

### 2.4 Endpoints existentes

Todos exigem autenticação, salvo os marcados como públicos. Restrições de perfil por atributo `[Authorize(Roles=...)]` ou por verificação no serviço (`GarantirAcessoDepartamentoAsync` / `GarantirAcessoMateriaAsync` / `GarantirAcessoAulaAsync` / `GarantirAcessoPessoaAsync`, que aceitam perfis administrativos ou atribuição ativa na turma). **Filtro global** `ExigeAceiteTermoFiltro`: toda requisição autenticada de conta sem aceite da versão vigente do termo recebe 403 `{ mensagem, termoPendente: true }`, exceto endpoints `[AllowAnonymous]` e `[PermitirSemAceiteTermo]` (`api/termo/*`, `api/auth/logout`). "Gestão" abaixo = Admin, Pastor, Superintendente.

| **Rota** | **Verbos** | **Restrição atual** |
| --- | --- | --- |
| api/auth/registrar-admin, api/auth/login, api/auth/primeiro-acesso/{token}, api/auth/primeiro-acesso | POST, POST, GET, POST | Públicos (`[AllowAnonymous]` explícito). registrar-admin e primeiro-acesso exigem o aceite da versão vigente; login devolve `termoPendente`. Token só no cookie. |
| api/auth/logout | POST | Autenticado; dispensado do termo |
| api/termo/vigente, api/termo/aceitar | GET, POST | vigente: público (com token de convite ou sessão, inclui igreja e contato); aceitar: autenticado, dispensado do termo |
| api/usuarios, api/usuarios/{id}, api/usuarios/{id}/convite | POST/GET, GET/PATCH, POST | Admin (classe). Sem rota de reset com senha (removida; RNF 15.3, teste de 404). |
| api/igrejas, api/igrejas/{id} | POST, GET/PUT | GET: autenticado, só a própria igreja. PUT: Admin e só a igreja do token (RF44); substitui os cinco campos (Nome, Cidade, Estado, Email, Telefone) — campo omitido vira nulo. POST: Admin, existente desde o início, sem uso pela SPA e sem RF (ver seção 9). |
| api/pessoas, api/pessoas/importar, api/pessoas/{id} | POST/GET, POST, GET/PUT | Escrita e listagem: gestão. GET /{id}: gestão completo; Professor via GarantirAcessoPessoaAsync com DTO reduzido (11.3). |
| api/pessoas/{id}/parentescos, .../{parentescoId} | POST/GET, DELETE | Escrita: gestão; GET: gestão e Professor (via GarantirAcessoPessoaAsync) |
| api/pessoas/{id}/presencas | GET | Gestão; Professor limitado às turmas com atribuição ativa (34.3); próprio usuário |
| api/departamentos, /{id} | POST/GET, GET/PUT | Escrita: gestão |
| api/departamentos/{id}/matriculas, /alunos, /pessoas-disponiveis, /matriculas/{matriculaId} | POST, GET, GET, DELETE | Gestão ou atribuição ativa; pessoas-disponiveis com DTO reduzido para Professor (28.3) |
| api/atribuicoes, /departamento/{id}, /{id}, /{id}/encerrar | POST/GET, GET, GET/PUT, PATCH | Escrita: gestão; leitura: autenticado (23.1) |
| api/materias, /{id} | POST/GET, GET/PUT | Gestão ou atribuição ativa |
| api/aulas, /{id}, /{id}/consolidar, /{id}/nao-realizada, /{id}/reabrir | POST/GET, GET/PUT/DELETE, PATCH, PATCH, PATCH | Gestão ou atribuição ativa; reabrir: Admin. `AulaRespostaDto` traz `DepartamentoId` e `NomeDepartamento` (aditivo, 6.4-B) para o "voltar" da Chamada e das Presenças. PUT /{id} (200 com `AulaRespostaDto`) e DELETE /{id} (204) — RF45, 6.5. `AulaRespostaDto` traz também `Conteudo` e `PossuiPresencas` (aditivos, 6.5): o primeiro preenche o diálogo de edição, o segundo faz a tela não oferecer Excluir quando já há chamada (CSU09 FA3). |
| api/aulas/{aulaId}/presencas, api/aulas/{aulaId}/chamada | POST/GET, GET | Gestão ou atribuição ativa na aula. Cada item da chamada traz `Registrado` (aditivo, 6.4-C): existe presença ou ausência lançada para a matrícula. |
| api/relatorios/ebd/frequencia-turma, /acompanhamento, /ranking-faltas, /resumo-dia, /minha-frequencia | GET | Turma: gestão ou atribuição; resumo-dia: gestão; minha-frequencia: próprio usuário. Só aulas Consolidadas + `aulasPendentes[]` (seção 6.9). |
| api/meus-dados, /minhas-turmas | GET/PUT, GET | Próprio usuário; GET inclui `aceiteTermo` (RF43) |

Observação para a 7.1: "atribuição ativa" hoje basta, qualquer que seja o perfil da conta; a RNF 21.4 exige perfil Professor ou administrativo (seção 6.16). Endpoints sem atributo dependem do `[Authorize]` de classe; a FallbackPolicy da 7.1 torna a autenticação o padrão.

## 3. O que já está conforme (não refazer)

Itens verificados no código e que já atendem à monografia. Servem para o assistente não propor reescrita do que funciona.

- Isolamento por igreja em todas as consultas (IgrejaId lido do token via UsuarioAutenticado).
- JWT de 8 horas em cookie HttpOnly + Secure + SameSite=None; CORS com credenciais lido de Cors:OrigensPermitidas; UseHttpsRedirection; verificação de Origin para requisições de escrita (RNF 2.6, com evidência); token ausente do corpo das respostas de login e cadastro inicial (`[JsonIgnore]`, RF2_TokenSomenteNoCookieTests).
- Senhas com BCrypt (fator padrão 11). Senha mínima de 6 caracteres na ativação.
- Convite de primeiro acesso: 32 bytes aleatórios, hash SHA-256 no banco, 7 dias, uso único, hash zerado ao consumir, novo convite invalida o anterior; botões de copiar e enviar por WhatsApp no front. "Redefinir acesso" = novo convite; a senha atual vale até o uso (RF15, CSU20).
- Último administrador ativo protegido contra inativação e troca de perfil (RNF 14.3).
- Termo de Uso e Sigilo v1.0 em código (`TermosDeUso`), hash SHA-256 do texto normalizado, aceite imutável com versão, hash, data, meio e IP; gates no cadastro inicial (em transação), na ativação e após o login; filtro global na API; tela `/termo` fora do layout; bloco em Meus Dados e coluna em Usuários (RF42, RF43).
- Cadastro inicial (igreja + admin + aceite), login, logout, Meus Dados, Minhas Turmas, Minha Frequência, Painel por perfil (indicadores do aluno só com aulas Consolidadas, janela de 90 dias).
- Dados da igreja editáveis pelo Admin (RF44): tela `/igreja`, cinco campos, cabeçalho do termo lido a cada exibição sem tocar os aceites.
- Pessoa enxuta conforme o DER; CRUD; situação e DataInativacao; parentescos; listagens com paginação, busca e filtro.
- Importação CSV com o modelo enxuto e sinalização `paraConferencia` (RNF 41.3) exibida no diálogo.
- Restrições de leitura do Professor com GarantirAcessoPessoaAsync e DTOs reduzidos (RF11, RF28, RF34), com testes e evidências.
- Departamentos com tipo; atribuições (Professor/Auxiliar) com início, fim e encerramento; matérias com ordem; matrícula com índice único de matrícula ativa e inativação com data de saída.
- Aula com três situações (Em aberto / Consolidada / Não realizada) e pendente de fechamento calculado; chamada só em Em aberto; consolidação exige registro de todos os alunos ativos (400 com `alunosSemRegistro[]`); Não realizada só sem presenças; reabrir só Admin, preservando presenças; tags, filtro e destaque "Pendente" na listagem; dica de observação (32.5).
- Relatórios (frequência da turma, acompanhamento, ranking, resumo do dia, minha frequência) só com aulas Consolidadas, `aulasPendentes[]` no período, resumo do dia com situações por turma; exportação CSV e impressão só para a gestão (`BotoesExportacao`).
- Responsividade (6.3): ponto de corte 768 px, menu em Drawer abaixo dele, fluxo do professor a 360 px sem rolagem horizontal da página.
- Página da Turma (RF20, 6.4): `/departamentos/:id` com cabeçalho (contadores recontados a partir das abas, equipe) e abas Alunos · Matérias · Aulas · Atribuições como rotas filhas, URLs antigas preservadas; no celular, cartões com ação nomeada e ⋮ em Painel, Minhas Turmas e nas abas Alunos, Matérias e Aulas; ⋮ também no desktop da aba Aulas.
- Chamada e Presenças (6.4-B, corretiva e 6.4-C): mesma tela nos dois layouts — linha tocável, observação sob demanda com a orientação 32.5, "Presentes N de M", ⋮ com Recarregar e Limpar, uma mensagem por vez, "Sem registro" desde a carga, barra Salvar · Consolidar `sticky` que nunca cobre o rodapé (Consolidar nomeia quem será gravado como ausente e salva antes; segundo toque ignorado por 500 ms), confirmação ao sair ou recarregar com alterações não salvas; no desktop, alunos em grade de cartões e resumo na barra; Presenças em cartões com `CardIndicador`.
- Confirmações e avisos globais (`App.vue`): tema do projeto, dentro da tela a 360 px com botões empilhados, lista de itens com rolagem própria; severidade `warn` (PrimeVue 4) em todas as etiquetas âmbar.
- Testes: 114 xUnit, 102 Vitest, 10 Playwright; `docs/evidencias/`, `docs/testes.md` e `docs/decisoes/` em uso.

## 4. Matriz requisito × código

Legenda: **OK** = faz o que a monografia diz · **Parcial** = existe, falta regra ou restrição · **Falta** = não existe. Os números entre parênteses são as RNFs da monografia. Retrato de 10/10/2026. Nas RNFs "a interface deve ser responsiva", OK = atende ao critério da seção 6.12 vigente para aquela tela.

### 4.1 Acesso e autosserviços

| **RF** | **Estado** | **Observação** |
| --- | --- | --- |
| RF1 Cadastro inicial | OK | Aceite do termo obrigatório e gravado na mesma transação (1.5). |
| RF2 Login e logout | OK | Gate do termo (2.5) no front e filtro global na API; Origin (2.6) com evidência; token só no cookie. |
| RF3 Painel inicial | OK | Indicadores só com Consolidadas. Celular: painéis do professor e do aluno em cartões (6.4); atribuição em conta Usuário sem ação (21.4); tabela vazia com mensagem e sem paginador. |
| RF4 Meus Dados | OK | Bloco de aceite do termo (RF43). |
| RF5 Minhas Turmas | OK | Cartões com "Abrir turma" no celular; "Minha frequência" só para o vínculo Aluno (6.4). |
| RF6 Minha Frequência | OK | Só Consolidadas, pendentes listadas. Cartões no celular; "voltar" para Minhas Turmas (6.4). |

### 4.2 Pessoas

| **RF** | **Estado** | **Observação** |
| --- | --- | --- |
| RF7 Cadastrar pessoa | OK | Conjunto do DER (7.5). |
| RF8 Editar pessoa | OK | Idem RF7. |
| RF9 Inativar/Reativar | OK | — |
| RF10 Pesquisar e listar | OK | Client-side (volume de ~200 pessoas). Celular: utilizável com rolagem (administrativa). |
| RF11 Visualizar pessoa | OK | DTO reduzido para Professor (11.3); testes e evidências. |
| RF12 Parentescos | Parcial | Adicionar, listar e remover OK. Falta o vínculo derivado na leitura (12.3) e a recusa de remover pelo lado derivado (12.4) — 6.6, seção 6.15. |
| RF41 Importar CSV | OK | Sinalização para conferência (41.3) exibida (6.1). |

### 4.3 Usuários, acesso e igreja

| **RF** | **Estado** | **Observação** |
| --- | --- | --- |
| RF13 Cadastrar usuário | OK | Senha opcional ou convite; conta com senha nasce sem aceite e passa pelo gate (13.4). |
| RF14 Editar usuário | OK | Último Admin ativo protegido (14.3); RF14_UltimoAdminTests e evidência. |
| RF15 Resetar senha | OK | Só por convite; rota antiga devolve 404 (15.3), com evidência. |
| RF16 Listar usuários | OK | Coluna "Termo" (RF43). |
| RF39 Gerar convite | OK | — |
| RF40 Ativar conta | OK | Aceite na ativação (40.5). |
| RF44 Editar dados da igreja | OK | PUT /api/igrejas/{id} (Admin, própria igreja); tela /igreja; RF44_IgrejaTests (6.2). |

### 4.4 Turmas, atribuições, matérias e matrículas

| **RF** | **Estado** | **Observação** |
| --- | --- | --- |
| RF17–RF19 Departamentos | OK | Tipo, isolamento por igreja, lista com filtros; "Abrir" no ⋮ de Turmas EBD leva à Página da Turma. |
| RF20 Detalhes da turma | OK | /departamentos/{id}: cabeçalho (alunos ativos, aulas em aberto, matérias, equipe) e abas Alunos · Matérias · Aulas · Atribuições (gestão) como rotas filhas; PaginaTurma.spec (6.4-A). Abas Alunos e Matérias em cartões no celular; contadores recontados a partir das abas (6.4-C). |
| RF21 Atribuir professor/auxiliar | Parcial | Cadastro, encerramento e 21.3 OK (só atribuição Professor é responsável de aula). Faltam 21.4 (conta com perfil Usuário não opera turma, mesmo com atribuição — hoje opera pela API) e 21.5 (listagem sinaliza "Sem conta" / "Conta com perfil Usuário") — 7.1, seção 6.16. |
| RF22–RF23 Atribuições | OK | — |
| RF24–RF26 Matérias | OK | Autorização por turma e ordem de exibição. |
| RF27 Matricular | OK | Índice único de matrícula ativa (27.2); RF27_MatriculaTests. |
| RF28 Alunos e disponíveis | OK | DTO reduzido para Professor (28.3). |
| RF29 Inativar matrícula | OK | — |

### 4.5 Aulas e chamada

| **RF** | **Estado** | **Observação** |
| --- | --- | --- |
| RF30 Cadastrar aula | OK | Professor responsável precisa de atribuição de Professor (21.3). |
| RF31 Listar aulas | OK | Tags, filtro e "Pendente" (31.2/31.3); cartões no celular e ⋮ nos dois layouts (6.4-A). |
| RF32 Lançar chamada | OK | Só Em aberto (32.2), índice único (32.3), dica de observação (32.5). Chamada tocável nos dois layouts com barra Salvar · Consolidar (6.4-B); grade de cartões no desktop (corretiva); "Sem registro" desde a carga (campo Registrado) e confirmação ao sair/recarregar com alterações não salvas (6.4-C). |
| RF33 Consolidar e reabrir | OK | 32.6, 33.2–33.7 com testes; RNF-33.5 com evidência. Consolidar com alteração pendente ou aluno sem registro nomeia quem será gravado como ausente e grava antes de consolidar (CSU11 passos 2–4, 6.4-C); FA1 mantido para a API e o cancelamento; segundo toque ignorado. |
| RF34 Presenças do aluno | OK | Restrição do Professor (34.3); `SituacaoAula` em cada registro. Presenças da aula em cartões nos dois layouts (6.4-B e corretiva). |
| RF45 Editar aula | OK | 45.1–45.5 com testes (RF45_EditarAulaTests, 9 casos). Editar e Excluir no ⋮ da aba Aulas, só Em aberto; Excluir só sem registro de presença (campo `PossuiPresencas`); diálogo em modo edição com Conteúdo. Atendido na 6.5 (16/10/2026). |

### 4.6 Relatórios

| **RF** | **Estado** | **Observação** |
| --- | --- | --- |
| RF35 Frequência da turma | OK | Só Consolidadas, pendentes (35.5), exportação só gestão (35.4). |
| RF36 Painel de acompanhamento | OK | Limiares e faltas consecutivas; Consolidadas + pendentes; gate de exportação (36.4). |
| RF37 Ranking de faltas | OK | 37.4, 37.5. |
| RF38 Resumo do dia | OK | Situações por turma, totais só de Consolidadas, listas de pendentes e Não realizadas (38.4). |

### 4.7 Termo de uso e sigilo

| **RF** | **Estado** | **Observação** |
| --- | --- | --- |
| RF42 Aceitar termo | OK | TermosDeUso v1.0, hash, registro imutável com meio e IP, gates e filtro global; RF42_TermoTests e evidências. |
| RF43 Consultar aceite | OK | Meus Dados e coluna em Usuários. |

## 5. Ordem de execução e definição de pronto

**Estado em 10/10/2026** (verificado contra o código):

| **Etapa** | **Estado** |
| --- | --- |
| 0. Migração para .NET 10 | **Concluída** |
| 1. Migrations | **Concluída** — SituacaoAula, AceitesTermo, PessoaEnxuta. |
| 2. Segurança de leitura e escrita | **Concluída** (2.1–2.4) — RF11/RF28/RF34, Origin (2.6), último Admin (14.3), reset só por convite (RF15), token só no cookie; resumos etapa-0 a etapa-2. |
| 3. Regras de aula | **Concluída** (29/09/2026) — RF31–RF33 completos, Vitest instalado. |
| 4. Relatórios | **Concluída** (30/09/2026) — só Consolidadas em cinco relatórios, pendentes, gate de exportação, painel do aluno corrigido. |
| 5. Termo de uso | **Concluída** (5.1 em 01/10, 5.2 e 5.3 em 03/10/2026) — RF42/RF43, gates, filtro global na API (decisão do autor), Playwright E2E-01 a E2E-03. |
| 6. Acabamento, RF44, responsividade e turma | **Em andamento** — 6.1 (06/10), 6.2 RF44 (07/10), 6.3 responsividade (08/10) **6.4 concluída em 14/10/2026** (A em 09/10, B em 10/10, corretiva de desktop em 12/10, ajuste em 13/10, C em 14/10: xUnit 114; Vitest 102; Playwright 3 + 7) e **6.5 concluída em 16/10/2026** (RF45, no Claude Code: xUnit 123; Vitest 105; Playwright 3 + 7); 6.6 aberta. A etapa foi reaberta em 08/10/2026 pela avaliação pré-Etapa 7 (`docs/decisoes/avaliacao-pre-etapa7.md`): responsividade e RF20 eram requisitos escritos e não atendidos; RF45 e RNF 12.3 entraram na monografia. |
| 7. Autorização, E2E e Resultados | **Pendente** — 7.1, 7.2 e 7.3 abaixo. |

| **Etapa** | **Conteúdo** | **Definição de pronto** |
| --- | --- | --- |
| 0. Migração para .NET 10 | TargetFramework → net10.0; JwtBearer, EF Tools e Npgsql em 10.x; Swashbuckle atualizado; SDK 10 instalado. | dotnet build sem avisos novos; API sobe; login e uma listagem funcionam; projeto de testes xUnit com teste de fumaça verde. |
| 1. Migrations | Situacao em Aulas; AceitesTermo; Pessoas sem CPF, Categoria, DataBatismo, DataMembresia, Telefone e FotoUrl. | Modelo igual ao DER; database update limpo; DTOs, telas e CSV sem os campos removidos; testes verdes. |
| 2. Segurança de leitura e escrita | RF11, RF34, RF28; Origin (2.6); último Admin (14.3); remoção do reset com senha (RF15). | Testes de integração para cada 403/400; evidências; resumos das etapas 0 a 2. |
| 3. Regras de aula | RF33 completo; RF32 usando Situacao; RF31 com pendentes. | Testes de 32.2, 32.6, 33.3–33.7; tela de aulas com tags. |
| 4. Relatórios | Consolidadas e pendentes em RF6, RF35–RF38; gates de exportação. | Testes de RelatorioEbdServico; botões ausentes para Professor/Usuario. |
| 5. Termo de uso | RF42 + RF43 + gates em RF1, RF2, RF40 (e 13.4). | E2E de cadastro inicial, login com termo pendente e ativação por convite verdes; registro imutável verificado por teste. |
| 6.1 Acabamento | Dica na observação (32.5); conferência da importação (41.3); tela do termo (42.7). | Vitest dos componentes tocados; revisão visual. |
| 6.2 RF44 | Editar dados da igreja (seção 6.11). | RF44_IgrejaTests; tela /igreja. |
| 6.3 Responsividade | RNFs "interface responsiva"; critério: 768 px; fluxo do professor — Login, Termo, Painel, Minhas Turmas, Aulas, Chamada, Presenças, Minha Frequência, Meus Dados — a 360 px sem rolagem horizontal da página, alvos de toque ≥ 40 px, Aluno e Presente visíveis na chamada; menu em Drawer; telas administrativas apenas utilizáveis com rolagem; projeto mobile do Playwright com E2E-01, E2E-02 e E2E-M1. | Projeto mobile verde; prints RNF-responsividade-*.png. |
| 6.4 Página da Turma e celular | Página da Turma (RF20: /departamentos/:id com cabeçalho, equipe e abas Alunos · Matérias · Aulas · Atribuições como rotas filhas, URLs preservadas) e fluxo operacional no celular (seção 6.12, critério estrito). Entregas: **6.4-A** peças reutilizáveis, Página da Turma, aba Aulas, Minhas Turmas e painel do professor em cartões; **6.4-B** chamada tocável com barra fixa Salvar · Consolidar (Consolidar salva as pendentes), chamada enxuta nos dois layouts, Presenças, painel do aluno e Minha Frequência em cartões, "voltar" hierárquico, "Minha frequência" só para o vínculo Aluno, mensagem de tabela vazia no painel; **corretiva (12/10)** Chamada e Presenças no desktop em grade de cartões, barra `sticky` sem cobrir o rodapé, severidade `warn`, clique duplo ignorado; **ajuste (13/10)** bloqueio de 500 ms após a gravação, `overflow` do `<main>` retirado só na rota da chamada, E2E-M2; **6.4-C** abas Alunos e Matérias em cartões, contadores do cabeçalho recontados a partir das abas, "Sem registro" visível desde a carga (campo `Registrado`), Consolidar nomeando quem será gravado como ausente (CSU11 passos 2–4), confirmação ao sair ou recarregar com alterações não salvas, confirmações e avisos globais no tema, E2E-M1 em modo estrito, prints, descrição da Tela de Turma para a figura do protótipo. | Critério da seção 6.12 a 360 × 640; desktop inalterado nas tabelas existentes, salvo Chamada e Presenças; Vitest dos componentes novos; E2E-M1 estrito e E2E-M2 verdes; prints regenerados. **Atendida em 14/10/2026** (`docs/testes.md`, Etapa 6.4). |
| 6.5 RF45 Editar aula | Seção 6.14 (editar e excluir aula Em aberto; exclusão só sem registro de presença, RNF 45.5). Primeira entrega feita no Claude Code. | RF45_EditarAulaTests; Editar e Excluir no ⋮ da aba Aulas nos dois layouts; confirmação de exclusão pelo `confirm.require` comum (tema e encaixe em 360 px vêm do App.vue, sem `acceptClass`); E2E-M1 continua verde no modo estrito. **Atendida em 16/10/2026** (docs/testes.md, Etapa 6.5; docs/decisoes/etapa-6.md, 1.9). |
| 6.6 Parentesco derivado | Seção 6.15, recorte mínimo (decisão 4a do recorte da 6.4). | Testes da tabela de inversos; tag "derivado" sem botão de remover. Se o calendário apertar: reverter 12.3/12.4 e o passo do CSU13 na monografia antes da defesa (nunca deixar texto sem implementação). |
| 7.1 Autorização | FallbackPolicy; perfil × atribuição (RNF 21.4) em GarantirAcessoDepartamentoAsync; `perfilConta` na listagem de atribuições (21.5); inventário das evidências capturadas × faltantes; decisão sobre POST /api/igrejas; decisão sobre o GET /api/aulas/{id} de aula inexistente pelo Professor (hoje 400, porque `GarantirAcessoAulaAsync` lança antes do serviço; para a gestão é 404). | RF21_PerfilOperacionalTests; tag na tela de Atribuições; inventário em docs/testes.md. |
| 7.2 E2E | E2E-04 a E2E-08 no desktop e E2E-04 também no projeto mobile (seção 7.3). | npm run test:e2e verde com os novos cenários. |
| 7.3 Resultados | Tabela CSU → teste → resultado (CSU01–CSU24); procedimento de cópia e restauração executado uma vez com evidência (seção 6.17); teste `Atualizar_CampoOmitido_ViraNulo` do RF44; evidências de Segurança completas (2.6, 11.3, 14.3, 15.3, 21.4, 28.3, 33.5, 34.3, 35.4, 42.x); prints de responsividade. | Material pronto para o capítulo de Resultados (seção 7.5). |

## 6. Especificações fechadas (decisões já tomadas)

Decisões discutidas e aprovadas. O assistente deve implementá-las como estão, sem reabrir a discussão por conta própria. Duas exceções: (a) impedimento técnico — apontar antes de mudar o desenho; (b) problema relevante que o desenho aprovado cause ou deixe passar — segurança, perda de dados, manutenção, conflito com outra etapa ou com a monografia — apontar em no máximo um parágrafo, com a alternativa sugerida, e seguir com o desenho aprovado até ordem em contrário. Avisar não é reabrir a discussão; a decisão de mudar é sempre do autor. As seções marcadas CONCLUÍDA ficam como registro do que foi aprovado; o detalhe de implementação está em `docs/decisoes/etapa-N.md`.

### 6.1 Migração para .NET 10 — CONCLUÍDA (etapa 0)

Versões resolvidas e fixadas no csproj: net10.0; Microsoft.AspNetCore.Authentication.JwtBearer 10.0.12; Microsoft.EntityFrameworkCore.Tools 10.0.12; Microsoft.EntityFrameworkCore.Design 10.0.12 (referência explícita obrigatória a partir do EF Tools 10.0.6 — dotnet/efcore#38124); Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3; Swashbuckle.AspNetCore 10.2.3; BCrypt.Net-Next 4.0.3. `public partial class Program { }` ao final de Program.cs para os testes de integração. Levar essas versões para o texto da monografia (seção 9).

### 6.2 Situação da aula (RF31, RF32, RF33) — CONCLUÍDA (etapas 1 e 3)

`Situacao` com três valores fixos: EmAberto, Consolidada, NaoRealizada (classe SituacaoAula). "Pendente de fechamento" não é situação armazenada: Situacao == EmAberto && Data < hoje (UTC), devolvido nos DTOs como PendenteFechamento.

| **Regra** | **RNF** | **Onde** |
| --- | --- | --- |
| Chamada só pode ser lançada ou alterada com Situacao == EmAberto | 32.2 | ChamadaServico.RegistrarAsync |
| Consolidar exige registro explícito para todos os alunos com matrícula ativa; caso contrário, 400 com `alunosSemRegistro[]`; só Em aberto consolida | 32.6 / 33.2 / 33.3 | AulaServico.ConsolidarAsync (ChamadaIncompletaException) |
| Marcar Não realizada só sem presenças; aulas Não realizadas não contam para frequência | 33.4 / 33.7 | AulaServico.MarcarNaoRealizadaAsync; filtros em RelatorioEbdServico |
| Reabrir: só Admin; preserva presenças; volta para EmAberto (vale para Consolidada e Não realizada) | 33.5 / 33.6 | AulaServico.ReabrirAsync; `[Authorize(Roles="Admin")]` |

Endpoints: PATCH /api/aulas/{id}/consolidar, /nao-realizada (gestão ou atribuição ativa) e /reabrir (Admin). Na interface, as ações por aula ficam numa lista única (`itensAcoes(aula)`) exibida no ⋮ dos dois layouts, com visibilidade por situação e perfil.

### 6.3 Termo de Uso e Sigilo (RF42, RF43, RNFs 1.5, 2.5, 13.4, 40.5) — CONCLUÍDA (etapa 5)

Entidade AceiteTermo (tabela AceitesTermo), imutável — sem endpoint de edição ou exclusão.

```csharp
public class AceiteTermo
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }      public Usuario Usuario { get; set; } = null!;
    public int IgrejaId { get; set; }       public Igreja Igreja { get; set; } = null!;
    public string TermoVersao { get; set; } = string.Empty;   // ex.: "1.0"
    public string TermoHash { get; set; } = string.Empty;     // SHA-256 (hex) do texto fixo da versão
    public DateTime AceitoEm { get; set; }
    public string? Ip { get; set; }                            // só auditoria (RNF 42.8)
    public string Meio { get; set; } = string.Empty;          // CadastroInicial | PrimeiroAcesso | Login
    public DateTime CriadoEm { get; set; }
}
```

Textos em código, na classe estática TermosDeUso (Versao, VigenteDesde, Texto; propriedade Vigente). Hash sobre o texto fixo normalizado (`\r\n` → `\n`, Trim), nunca sobre o texto renderizado com nome da igreja (RNF 42.7). Versão 1.0 vigente desde 01/10/2026.

| **Endpoint** | **Quem** | **Comportamento** |
| --- | --- | --- |
| GET /api/termo/vigente | Público | Versão, texto e, com sessão ou token de convite, nome e contatos da igreja. |
| POST /api/termo/aceitar { versao } | Autenticado | Grava AceiteTermo com Meio = Login, IP e hash; rejeita versão que não seja a vigente; idempotente por versão. |
| POST /api/auth/registrar-admin e POST /api/auth/primeiro-acesso | Público | Recebem AceiteTermoVersao; sem aceite da vigente, 400 antes de qualquer efeito; registro na mesma operação (CadastroInicial em transação; PrimeiroAcesso). |
| POST /api/auth/login | Público | Resposta inclui termoPendente. |
| GET /api/meus-dados, GET /api/usuarios | Próprio usuário / Admin | aceiteTermo: { versao, aceitoEm, vigente } ou null (RF43). |

**Acréscimo aprovado na 5.2:** filtro global `ExigeAceiteTermoFiltro` na API (403 com `termoPendente: true` para conta sem aceite vigente, exceto `[AllowAnonymous]` e `[PermitirSemAceiteTermo]`), porque o gate só no front deixaria a API aberta a uma conta pendente. No front: componente único `TermoUsoSigilo` (embutido no cadastro inicial e no primeiro acesso; com botão em `/termo`), rota `/termo` fora do layout, guard global com `redirecionar`, 403 `termoPendente` tratado no cliente HTTP.

### 6.4 Pessoa enxuta (RF7, RNF 7.5, RF41) — CONCLUÍDA (etapa 1)

Campos da entidade exatamente como no DER: Id, Nome, Situacao, Sexo, DataNascimento, EstadoCivil, Email, Celular, Endereco, Bairro, Cidade, Estado, CEP, DataInativacao, CriadoEm, AtualizadoEm, IgrejaId. Modelo CSV: Nome;Sexo;DataNascimento;EstadoCivil;Email;Celular;Endereco;Bairro;Cidade;Estado;CEP. Mensagem da linha de nome repetido sem e-mail: "Já existe uma pessoa com este nome; linha ignorada para conferência. Se for outra pessoa, cadastre manualmente." (RNF 41.3), sinalizada por `paraConferencia` (6.1).

### 6.5 Restrições de leitura para o perfil Professor (RNFs 11.3, 28.3, 34.3) — CONCLUÍDA (etapa 2)

| **Endpoint** | **Regra para Professor** | **DTO** |
| --- | --- | --- |
| GET /api/pessoas/{id} | Só se a pessoa tiver matrícula ativa em turma com atribuição ativa do professor; senão 403. | PessoaTurmaRespostaDto: Id, Nome, Situacao, Celular, Parentescos[] { ParenteNome, TipoRelacionamento, ParenteCelular }. |
| GET /api/departamentos/{id}/pessoas-disponiveis | Já exige atribuição; muda só o retorno. | PessoaDisponivelRespostaDto: Id, Nome, Situacao. |
| GET /api/pessoas/{id}/presencas | Só com matrícula ativa em turma com atribuição ativa; resultado filtrado a essas turmas. | Mesmo DTO, filtrado. |

### 6.6 Defesa em profundidade contra CSRF (RNF 2.6) — CONCLUÍDA (etapa 2)

Middleware entre `UseCors` e `UseAuthentication`: POST, PUT, PATCH e DELETE com `Origin` fora de Cors:OrigensPermitidas (ou sem Origin) → 403 "Origem não autorizada.". Texto da RNF 2.6, seção 4.8 e referência OWASP já na monografia. Evidências RNF-2.6-*.png.

### 6.7 Último administrador (RNF 14.3) — CONCLUÍDA (etapa 2)

Inativar ou trocar o perfil do único Admin ativo da igreja → 400 "Não é possível inativar ou alterar o perfil do único administrador ativo da igreja." (UsuarioServico.AtualizarAsync + ContarAdminsAtivosAsync). Evidência RNF-14.3.

### 6.8 Reset de senha só por convite (RF15) — CONCLUÍDA (etapa 2)

Rota, serviço, DTO e botão de reset com senha removidos; "Redefinir acesso" chama POST /api/usuarios/{id}/convite; a senha atual vale até o uso (CSU20). Teste de 404 e evidência RNF-15.3.

### 6.9 Relatórios só com aulas Consolidadas (RNFs 6.x, 35.5, 36, 37.5, 38.4) — CONCLUÍDA (etapa 4)

Todas as consultas de frequência, acompanhamento, ranking, resumo do dia e minha frequência filtram Situacao == Consolidada. Cada resposta traz `aulasPendentes[]` (`AulaResumidaRespostaDto`: Id, Data, Materia, Professor, DepartamentoId, Departamento) com as Em aberto já ocorridas **dentro do período consultado**; o resumo do dia lista também as Não realizadas, separadas dos totais. Exportação e impressão só para Admin, Pastor e Superintendente (restrição de interface, como a monografia descreve).

### 6.10 Detalhes de interface — CONCLUÍDA (etapa 6.1)

- Chamada (32.5): o texto "Use este campo apenas para justificativas administrativas (atraso, dispensa, saída antecipada). Não registre informações de saúde ou outros dados sensíveis." aparece **uma vez, acima da lista**, só com a aula editável (não abaixo de cada campo, que se repete por aluno). Na 6.4-B, no layout enxuto da chamada, o aviso aparece quando o professor abre "+ observação".
- Aulas (31.3), Resumo do dia (38.4) e tela do termo (42.7): conforme as etapas 3, 4 e 5.

### 6.11 RF44 — Editar Dados da Igreja — CONCLUÍDA (etapa 6.2)

PUT /api/igrejas/{id}, `[Authorize(Roles = "Admin")]` (44.1), só a igreja do token (44.2), validação por DTO com 400 por campo (44.3). Cinco campos: Nome, Cidade, Estado (maiúsculas), Email, Telefone; Endereco, CEP, CNPJ e LogoUrl continuam sem funcionalidade. O cabeçalho do termo lê os dados a cada exibição; os aceites não são tocados (44.5). PUT substitui os cinco campos: campo omitido vira nulo (comportamento correto de PUT; a tela sempre envia os cinco). Front: rota `/igreja` (requerAdmin), item "Igreja" no menu só para Admin.

### 6.12 Responsividade e uso no celular (RNFs "a interface deve ser responsiva": 3.2, 5.3, 6.4, 20.2, 26.2, 28.2, 35.3, 36.3 e outras)

A monografia não define medida; este é o critério operacional aprovado.

**Base (6.3, CONCLUÍDA):** ponto de corte 768 px; abaixo dele a barra lateral sai do fluxo e o mesmo `MenuLateral` abre num `Drawer` pelo botão ☰ da `BarraTopo` (a instância de desktop continua no DOM, oculta por CSS); conteúdo com respiro de 16 px; grades fluidas.

**Telas operacionais — critério estrito (6.4):** Painel (professor e aluno), Minhas Turmas, Página da Turma (abas Alunos, Matérias, Aulas), Chamada, Presenças da Aula, Minha Frequência. A 360 × 640:

1. Nenhuma rolagem horizontal — nem da página, nem de qualquer container (só a faixa de abas pode rolar).
2. Cada item de lista é um cartão (`CartaoItem`) com uma ação principal com rótulo, ≥ 44 px de altura, visível sem rolar para o lado; ações secundárias no ⋮ (`MenuAcoes`). O interruptor entre tabela e cartões é único: `usarTelaCompacta` (`matchMedia("(max-width: 768px)")`). Listas longas: dez cartões e "Mostrar mais (N restantes)" (`usarMostrarMais`), sem paginador numérico. Confirmações e avisos (`ConfirmDialog` e `Toast` globais no `App.vue`): dentro da tela a 360 px, botões empilhados, lista de itens com rolagem própria; botão de confirmação no verde do projeto; nenhuma página define `acceptClass`.
3. Chamada: marcar presença = um toque na linha (linha de 56 px, caixa de 40 px, fundo indicando presente); contador "Presentes: N de M" correto antes de salvar; barra `Salvar chamada` · `Consolidar` visível em qualquer ponto da rolagem enquanto a aula está Em aberto, `sticky` no fim da página: nunca cobre o rodapé (com lista curta fica encostada nele); Salvar e Consolidar ignoram um segundo toque durante a requisição e por 500 ms depois; observação só quando o professor toca "+ observação"; Recarregar e Limpar no ⋮; visitantes numa linha; uma mensagem de cada vez. Alunos sem registro ficam destacados desde a carga (campo `Registrado` da API) e deixam de estar quando o professor os toca. **Consolidar com alteração pendente ou aluno sem registro nomeia quem será gravado como ausente, grava e só então consolida** (CSU11 passos 2–4); sem pendência, confirmação simples. Sair, recarregar a página ou "Recarregar" do ⋮ com alterações não salvas pede confirmação (alunos sem registro não contam como alteração).
4. Navegação de "voltar" hierárquica: Chamada e Presenças voltam para a aba Aulas da turma (`/departamentos/{id}/aulas`, com o nome da turma); Minha Frequência volta para Minhas Turmas.
5. Desktop idêntico nas tabelas e cabeçalhos das telas existentes, salvo o ⋮ da aba Aulas (decisão 2) e a Chamada e as Presenças (6.4-B e corretiva: alunos em grade de cartões — 3 colunas a 1366, 5 a 1920 —, resumo "Presentes N de M" e "Alterações não salvas" na barra; Presenças em cartões com `CardIndicador`); a única tela nova no desktop é a Página da Turma.

**Telas administrativas** (Pessoas, Usuários, Igreja, Relatórios, Turmas EBD, Importação, aba Atribuições): utilizáveis com rolagem, sem polimento; fora do critério estrito. A 360 px a largura do documento é a da janela (a página não desliza; o que for mais largo rola dentro do `<main>`, que mantém `overflow-x: auto`); a exceção de `overflow` vale só para a rota com barra de ações (meta `barraAcoes`). Medido pelo E2E-M2.

**Cores:** a severidade âmbar do PrimeVue 4 é `warn` (não `warning`); `help` só existe em `Button`.

**Verificação:** projeto `mobile` do Playwright (emulação Pixel 7 com viewport 360 × 640); `semRolagemHorizontal` é **estrito por padrão desde a 6.4-C**: compara o `scrollWidth` do documento com a largura configurada da janela e reprova qualquer container interno que role para o lado (só a faixa de abas pode) — uma tabela nova numa tela operacional reprova no projeto `mobile`; `estrito: false` devolve o critério da 6.3. Toda entrega que toca tela traz prints a 1366, 1920 e 360 tirados antes da entrega.

### 6.13 Página da Turma (RF20) — CONCLUÍDA (6.4-A e 6.4-C)

`/departamentos/:id` → `PaginaTurma.vue` (layout) com cabeçalho — nome, chips de tipo e situação, contadores (alunos ativos, aulas em aberto; no desktop também matérias e equipe), linha de equipe ativa — e abas (`Tabs` do PrimeVue) ligadas às rotas filhas `…/matriculas` (Alunos), `…/materias`, `…/aulas`, `…/atribuicoes`. Sem aba na URL, redireciona para `…/aulas` (decisão 1). A aba e a rota Atribuições existem só para a gestão (`requerAdministrativo`; RNF 21.1); o professor vê a equipe no cabeçalho (RF20/RF23). As quatro páginas existentes viraram conteúdo das abas pela meta `abaDaTurma` (o `PageHeader` delas mostra só as ações); o cabeçalho da turma usa `modo="completo"`. Contadores vêm das listagens existentes (nenhum endpoint novo); as abas Alunos, Matérias e Aulas recarregam o cabeçalho por `provide("turmaRecarregarResumo")` depois de matricular, remover, criar, editar, ativar/inativar ou consolidar (6.4-C). "Abrir turma" (Painel, Minhas Turmas, ⋮ de Turmas EBD) leva à página; o aluno não tem acesso a ela (31.1). Voltar: gestão → Turmas EBD; professor → Minhas Turmas.

### 6.14 RF45 — Editar e excluir aula Em aberto — ATENDIDA (6.5, 16/10/2026)

| Camada | Conteúdo |
|---|---|
| Endpoints | `PUT /api/aulas/{id}` e `DELETE /api/aulas/{id}` — mesma autorização do cadastro (`GarantirAcessoAulaAsync`: gestão ou atribuição ativa na turma). |
| DTO | `AulaAtualizarRequisicaoDto { Data, MateriaId, ProfessorId (obrigatórios), Tema?, Conteudo? }` — os mesmos campos do cadastro; sem Situacao, QuantidadeVisitantes ou Observacoes. Resposta: `AulaRespostaDto` + `Conteudo` e `PossuiPresencas` (aditivos). |
| Regras de edição | (1) aula existe na igreja; (2) Situacao == EmAberto, senão 400 "Somente aulas Em aberto podem ser editadas." (45.2); (3) a matéria pertence à mesma turma da aula, senão 400 "A matéria informada não pertence à turma desta aula." (45.4); (4) o professor tem atribuição ativa de Professor na turma (mesma regra do cadastro, 21.3); (5) AtualizadoEm automático; (6) retorno 200 com a aula atualizada. |
| Regras de exclusão | Só Em aberto (400 "Somente aulas Em aberto podem ser excluídas.") **e** sem nenhum registro de presença (400 "Uma aula com registros de presença não pode ser excluída.", 45.5, mesma condição da 33.7); sucesso → 204; exclusão física (a 45.5 garante que não há presença dependente). |
| Testes `RF45_EditarAulaTests` | Editar Em aberto → 200 e GET reflete; Consolidada → 400; Não realizada → 400; matéria de outra turma → 400; professor sem atribuição de Professor → 400; Professor de outra turma → 403; Excluir sem presenças → 204 e some da listagem; Excluir com presenças → 400 e `possuiPresencas` verdadeiro na listagem; Excluir Consolidada → 400 (9 casos). |
| Front | Editar e Excluir entram em `itensAcoes(aula)` (⋮ dos dois layouts), visíveis só em Em aberto (Excluir só sem presenças, pelo `possuiPresencas`; resposta sem o campo vale true); Editar reabre o diálogo de nova aula em modo edição, com o campo "Conteúdo (opcional)" nos dois modos (`aulasServico.atualizarAula`); Excluir com confirmação pelo `confirm.require` comum (o tema e o encaixe em 360 px vêm do App.vue; sem `acceptClass`). Nenhuma tabela nova nas telas operacionais: o E2E-M1 é estrito. |

### 6.15 Parentesco derivado na leitura (RF12, RNFs 12.3/12.4) — PENDENTE (6.6, recorte mínimo)

O vínculo é gravado uma vez; na leitura do cadastro do parente, o sistema apresenta o grau inverso, sem segundo registro, sem migration e sem mudar o DER. Semântica: o tipo descreve o parente em relação à pessoa ("Parente: Maria — Relacionamento: Mãe" = Maria é mãe da pessoa).

| Tipo registrado (parente → pessoa) | Vínculo derivado no cadastro do parente | Regra |
|---|---|---|
| Pai, Mãe | Filho(a) | fixo |
| Filho(a) | Pai / Mãe / "Pai ou mãe" | pelo sexo da pessoa em cujo cadastro o vínculo foi registrado (Masculino → Pai; Feminino → Mãe; sem sexo → "Pai ou mãe") |
| Irmão(ã) | Irmão(ã) | simétrico |
| Cônjuge | Cônjuge | simétrico |
| Responsável | Dependente | fixo (rótulo só derivado) |
| Outro ou texto livre | mesmo texto, marcado "(informado no cadastro de X)" | sem inversão |

A linha derivada vem com `derivado: true` e não pode ser removida por esse lado (12.4: remove-se no cadastro de origem; a tela esconde o botão e explica). Se já existir registro explícito no sentido inverso para o mesmo par, a derivada é omitida. Entra em `ParentescoRespostaDto` (gestão) e em `PessoaTurmaRespostaDto.Parentescos` (Professor, 11.3). Recorte mínimo: derivação e recusa na API + tag "derivado" e botão oculto no front.

### 6.16 Perfil × atribuição (RNFs 21.4, 21.5; seção 4.9) — PENDENTE (7.1)

Desenho: **perfil = área do sistema que a conta alcança; atribuição ativa = sobre quais turmas ela age** (autorização por papéis para as áreas + autorização baseada em recurso para cada turma, como a 4.9 descreve).

- **Um lugar só:** em `GarantirAcessoDepartamentoAsync` (reaproveitado por matéria e aula), se o perfil não é administrativo nem Professor → 403 "Esta operação exige perfil Professor ou administrativo." antes de consultar a atribuição. `GarantirAcessoPessoaAsync` já trata o Usuario à parte e não muda.
- **FallbackPolicy** em Program.cs: `RequireAuthenticatedUser()` como política padrão, para que um endpoint novo sem atributo nasça protegido; os públicos já têm `[AllowAnonymous]`.
- **`perfilConta` no `AtribuicaoRespostaDto`** (`null` = sem conta; senão o perfil) e tag na tela de Atribuições: "Sem conta" ou "Conta com perfil Usuário — não acessa a turma" (21.5). Pela API, porque Pastor e Superintendente gerem atribuições mas não listam usuários.
- **Testes `RF21_PerfilOperacionalTests`:** conta Usuario com atribuição ativa → 403 em GET /api/aulas?departamentoId e em POST .../presencas; Professor com atribuição → 200; (Professor sem atribuição → 403 já coberto em RF11/RF34). O caso "Usuário com atribuição" é a evidência da 5.1 ("controle por perfil e atribuição").
- **Dados existentes:** antes de subir, consultar no banco de demonstração as atribuições ativas cujo usuário tem perfil Usuario e ajustar os perfis.

### 6.17 Cópia e restauração dos dados (seção 5.1 da monografia, Confiabilidade) — PENDENTE (7.3)

Executar uma vez, com evidência, o procedimento: `pg_dump` do banco (formato custom), restauração com `pg_restore` em um banco novo, API apontada para ele, login e uma listagem conferidos. Registrar os comandos em `docs/testes.md` e os prints em `docs/evidencias/`. Sem dados reais de membros na evidência.

## 7. Estratégia de testes

Três ferramentas, três responsabilidades. Cada etapa da seção 5 só está pronta quando os testes indicados aqui existem e passam.

| **Ferramenta** | **Camada** | **O que testa** | **O que não testa** |
| --- | --- | --- | --- |
| xUnit (.NET) | API | Regras de negócio e autorização por endpoint (403/400) com WebApplicationFactory. | Telas; comportamento do navegador. |
| Vitest + @vue/test-utils | Front | Funções puras e composables, stores, guards de rota, componentes isolados com props. | PrimeVue internamente; chamadas reais à API (mockadas). |
| Playwright | Ponta a ponta | Os fluxos que a banca vai ver, no desktop e no celular (360 × 640). | Regras já cobertas por xUnit; combinações de dados. |

### 7.1 xUnit — projeto de testes da API

Projeto KoinoniaHub.API.Tests (Microsoft.AspNetCore.Mvc.Testing 10.0.12, Microsoft.EntityFrameworkCore.Sqlite 10.0.12, xunit 2.9.3), ambiente `Testing`, **SQLite em memória** (conexão aberta durante o teste, EnsureCreated) — o provedor InMemory não aplica índices únicos nem chaves estrangeiras, e 27.2 e 32.3 são exatamente o que se quer provar. Sementes criam o aceite do termo por padrão (`aceitarTermo: false` para contas pendentes). Uma classe por RF ou regra, métodos Metodo_Cenario_ResultadoEsperado em português; testes de integração (login por cookie, asserção de status) preferidos quando a regra também é de Segurança.

**Existentes — 21 classes, 114 casos verdes:**

| **Classe** | **RNFs / foco** |
| --- | --- |
| Etapa0_FumacaTests | API sobe, login, listagem |
| Etapa1_SituacaoAulaTests, Etapa1_AceiteTermoTests, Etapa1_PessoaEnxutaTests | Modelo conforme o DER |
| RF2_OrigemTests | 2.6 |
| RF2_TokenSomenteNoCookieTests | 4.8 (token fora do corpo) |
| RF2_GateTermoTests | 2.5, 13.4 (filtro global) |
| RF11_PessoaAcessoTests | 11.3 |
| RF14_UltimoAdminTests | 14.3 |
| RF27_MatriculaTests | 27.2 |
| RF28_DisponiveisTests | 28.3 |
| RF32_ChamadaTests | 32.2, 32.3; `Registrado` por matrícula na chamada completa (6.4-C) |
| RF33_ConsolidarAulaTests | 32.6, 33.1, 33.2, 33.3 |
| RF33_NaoRealizadaTests | 33.1, 33.2, 33.4, 33.7 |
| RF33_ReabrirAulaTests | 33.5, 33.6 |
| RF34_PresencasPessoaTests | 34.3 |
| RF35_RelatorioTests | 35.5, 37.5, 38.1, 38.4 (e RF6) |
| RF39_RF40_ConviteTests | 15.3, 39.1, 39.3, 39.4, 40.1–40.3 (e CSU20) |
| RF41_ImportacaoTests | 41.2–41.4 |
| RF42_TermoTests | 1.5, 2.5, 40.5, 42.1–42.8 |
| RF44_IgrejaTests | 44.1–44.3, 44.5 |
| RF45_EditarAulaTests | 45.1, 45.2, 45.4, 45.5 (9 casos) |

**A criar:**

| **Classe** | **Casos** | **RNF** | **Etapa** |
| --- | --- | --- | --- |
| RF12_ParentescoDerivadoTests | Tabela de inversos (Pai/Mãe, Filho(a) por sexo, simétricos, Responsável, texto livre), omissão quando há registro inverso, recusa de remover derivado | 12.3, 12.4 | 6.6 |
| RF21_PerfilOperacionalTests | Seção 6.16 (3 casos) + endpoint sem atributo exige autenticação (FallbackPolicy) | 21.4 | 7.1 |
| RF21 (listagem) | `perfilConta` nulo sem conta e igual ao perfil com conta | 21.5 | 7.1 |
| RF44_IgrejaTests | `Atualizar_CampoOmitido_ViraNulo` | 44.3 | 7.3 |

### 7.2 Vitest — testes de front

Instalado na etapa 3 (`vitest`, `@vue/test-utils`, `jsdom`, `@vitest/coverage-v8`; bloco `test` no vite.config.ts; scripts `test`, `test:watch`, `test:cov`). Arquivos `*.spec.ts` ao lado do código. Chamadas HTTP mockadas (`vi.mock`); nada de subir a API.

**Existentes — 20 arquivos, 105 casos verdes:**

| **Alvo (arquivo)** | **Casos** | **Etapa** |
| --- | --- | --- |
| Situação da aula (`situacaoAula.spec`) | pendente de fechamento; rótulo e cor das três situações; filtro por situação | 3 |
| Tag de situação (`TagSituacaoAula.spec`) | três estados; destaque "Pendente" com classe `p-tag-warn` | 3, 6.4 |
| Menu lateral por perfil (`MenuLateral.spec`) | Admin vê tudo inclusive Igreja; Pastor/Superintendente sem Usuários e Igreja; Professor sem Pessoas; Usuario só Painel, Minhas Turmas, Meus Dados | 3, 6.2 |
| Botões de exportação (`BotoesExportacao.spec`) | por perfil; eventos | 4 |
| Store de autenticação (`autenticacaoStore.spec`) | termoPendente do login, persistência, sair limpa | 5 |
| Guards de rota (`guardas.spec`) | termo pendente redireciona para /termo com destino; /termo abre; liberação após aceite | 5 |
| Componente do termo (`TermoUsoSigilo.spec`) | igreja e contato fora do texto; não inventa contato; botão desabilitado até marcar | 5 |
| Indicadores do aluno (`indicadoresPresenca.spec`) | só Consolidadas; percentual nulo sem aula; janela de 90 dias | 6.1 |
| Resultado da importação (`pessoasServico.spec`) | totais, itens, `paraConferencia`, resposta vazia | 6.1 |
| Layout principal (`LayoutPrincipal.spec`) | menu de desktop no DOM e Drawer fechado; botão abre com os itens do perfil; navegar fecha; classe `conteudo-com-barra` só na rota com `barraAcoes` | 6.3, 6.4 |
| Interruptor de layout compacto (`usarTelaCompacta.spec`) | ≤ 768 px → compacto; acompanha a mudança; sem matchMedia assume amplo | 6.4-A |
| Menu de ações ⋮ (`MenuAcoes.spec`) | filtra itens não visíveis; executa o comando; some sem itens | 6.4-A |
| Página da Turma (`PaginaTurma.spec`) | abre em Aulas; cabeçalho com contadores e equipe; Atribuições só para gestão; aba navega para a rota filha; voltar por perfil | 6.4-A |
| Regras da chamada (`chamada.spec`) | contador; alteração não salva por presença, observação e visitantes; lote com observação vazia → null; `alunosSemRegistro` | 6.4-B, 6.4-C |
| Linha da chamada (`LinhaChamada.spec`) | toque em qualquer ponto alterna (regressão da faixa de baixo); teclado; observação abre com a orientação 32.5 sem alternar; remover; somente leitura; sem registro | 6.4-B |
| Ação única (`usarAcaoUnica.spec`) | segundo acionamento ignorado durante a execução e por 500 ms depois; liberação após erro | 6.4 corretiva/ajuste |
| Chamada — página (`PaginaChamadaAula.spec`) | dois acionamentos de Salvar no mesmo tick e toque duplo após gravação concluída → uma gravação, um aviso; sem registro desde a carga e baixa ao tocar; Consolidar com nomes (gravar → consolidar); confirmação simples sem gravar; sair pela rota (recusa/aceite); `beforeunload`; Recarregar com confirmação | 6.4 ajuste, 6.4-C |
| Aba Alunos em cartões (`PaginaMatriculasTurma.spec`) | ordem, chips, Responsáveis, ⋮ só com matrícula ativa; remover reconta o cabeçalho | 6.4-C |
| Aba Matérias em cartões (`PaginaMateriasTurma.spec`) | ordem, chips, Editar, ⋮; criar e inativar recontam o cabeçalho | 6.4-C |
| Aba Aulas — Editar e Excluir (`PaginaAulasTurma.spec`) | Editar só Em aberto e Excluir só sem presenças (três estados); Editar abre o diálogo preenchido e Salvar chama atualizarAula e reconta o cabeçalho; Excluir pela confirmação comum chama excluirAula e reconta | 6.5 |

**A criar:**

| **Alvo** | **Casos** | **Etapa** |
| --- | --- | --- |
| Ações da aula (`itensAcoes`) | Editar/Excluir só em Em aberto | 6.5 |
| Parentesco derivado | tag "derivado" sem botão de remover | 6.6 |
| Tag de conta na atribuição | "Sem conta" / "Conta com perfil Usuário" | 7.1 |

### 7.3 Playwright — fluxos ponta a ponta

Configuração real (etapa 5.3 e 6.3):

- `baseURL` **http://localhost:5173** (Vite de desenvolvimento, sem certificado próprio; `/api` encaminhado para https://localhost:7054). O cookie `Secure` funciona porque o navegador trata `localhost` como contexto seguro. A semente e as chamadas diretas falam com a API em https, com `Origin: http://localhost:5173` e `ignoreHTTPSErrors`.
- API no ambiente **E2E** (não `Testing`, que é o dos testes xUnit): `cd api/KoinoniaHub.API; dotnet run --launch-profile e2e`, com `appsettings.E2E.json` apontando, pela chave `ConnectionStrings:Postgres`, para o banco `koinoniahub_e2e` no **PostgreSQL local** (o `docker-compose.e2e.yml`, porta 5433, ficou como alternativa). Esse arquivo guarda a string local do autor e nunca é commitado com ela. Opcional: `E2E_INICIAR_API=1` faz o Playwright subir a API.
- **Semente pela própria API**: cada execução cria uma igreja nova (`registrar-admin`) e, como Admin, pessoas, turma, atribuição, matrículas, matéria e usuários (Professor, aluno com perfil Usuario, contas pendentes `pendente` e `pendenteMobile`). Não há SQL nem limpeza: as igrejas "E2E …" acumulam no banco de teste.
- `storageState` gravado pelo `global-setup` em `e2e/.auth/` (`admin.json`, `professor.json`, `aluno.json`; fora do git).
- **Dois projetos:** `chromium` (desktop; todos os `e2e-NN-*`) e `mobile` (emulação Pixel 7 com viewport 360 × 640; E2E-01, E2E-02 e os `e2e-mN-*`). Apoio em `e2e/apoio/celular.ts`: `semRolagemHorizontal` (estrito por padrão), `alvoDeToque`, `larguraDoDocumento`, `evidenciaCelular`. E2E-03 não roda no celular (o convite da pessoa semeada só pode ser gerado uma vez). Execução sequencial (`workers: 1`).
- Seletores por `data-testid`. Prints do projeto mobile em `web/test-results/evidencias/RNF-responsividade-*.png` (pasta apagada a cada execução; copiar para `docs/evidencias/`).

| **Cenário** | **Passos** | **Verifica** | **CSU** | **Estado** |
| --- | --- | --- | --- | --- |
| E2E-01 Cadastro inicial com termo | Preencher igreja e admin; tentar concluir sem o termo; marcar; concluir | Botão desabilitado sem aceite; após concluir, painel e aceite em Meus Dados | CSU01, CSU23 | ✔ desktop e celular |
| E2E-02 Login com termo pendente | Conta sem aceite faz login | Redireciona para /termo; nenhuma outra rota abre; após aceitar, painel | CSU02, CSU23 | ✔ desktop e celular |
| E2E-03 Convite e ativação | Admin gera convite; abrir link; definir senha e aceitar | Conta ativa; segundo uso do link recusado | CSU19, CSU22 | ✔ desktop |
| E2E-M1 Fluxo do professor e do aluno no celular (3 casos) | Projeto mobile: login; professor — Painel (cartão "Fazer chamada") → Drawer → Minhas Turmas (cartão, "Abrir turma") → Página da Turma (cabeçalho; abas Alunos e Matérias em cartões, alvos ≥ 40 px, ⋮) → aba Aulas (cartão, "Fazer chamada", ⋮) → Chamada (voltar com o nome da turma; "Sem registro" desde a carga e baixa ao tocar; toque no meio da linha; "+ observação"; dois toques crus em Salvar → um POST e um aviso; Consolidar com os nomes dentro da tela → "Salvar e consolidar"; barra some; confirmação ao sair com "Continuar na chamada") → Presenças em cartões → Meus Dados; aluno — Painel → Minha Frequência | `semRolagemHorizontal` estrito em cada tela (nenhum container interno rola); ação principal nomeada em cada cartão; barra visível; aula consolidada ao fim; 15 prints RNF-responsividade-*.png | CSU07, CSU09, CSU10, CSU11, RNFs de responsividade | ✔ 6.4-C (14/10/2026) |
| E2E-M2 Largura do documento nas telas administrativas (2 casos) | Projeto mobile, Admin: Pessoas, Usuários, Igreja, Relatórios, Turmas EBD, Importação aberta; abas da turma, Chamada e Presenças | `scrollWidth` do documento e do `body` = 360 em todas | RNFs de responsividade (seção 6.12, administrativas) | ✔ 6.4 ajuste (13/10/2026) |
| E2E-04 Chamada até relatório | Professor cria aula; lança chamada incompleta; tenta consolidar; completa; consolida; abre relatório | Consolidação bloqueada com lista de faltantes; depois liberada; relatório reflete a aula | CSU08, CSU10, CSU11, CSU12 | 7.2 (desktop e mobile) |
| E2E-05 Restrições do Professor | Login como Professor | Menu sem Pessoas/Usuários; GET /api/pessoas/{id} fora da turma retorna 403; sem botões CSV/imprimir; conta Usuario com atribuição recebe 403 (21.4) | CSU16, CSU12, CSU18 | 7.2 |
| E2E-06 Reabertura pelo Admin | Admin reabre aula consolidada; corrige presença; consolida | Presenças preservadas; situação volta e fecha de novo | CSU11 | 7.2 |
| E2E-07 Importação CSV | Admin envia arquivo com e-mail repetido e nome repetido sem e-mail | Criadas, ignoradas e sinalizadas para conferência | CSU16 | 7.2 |
| E2E-08 Último administrador | Admin único tenta inativar a própria conta / trocar o próprio perfil | Mensagem de bloqueio | CSU19 | 7.2 |

### 7.4 Comandos de verificação (definição de pronto)

```powershell
# API
cd api; dotnet build; dotnet test                  # 123

# Front
cd web; npx vue-tsc -b; npm run test               # 105 (20 arquivos)
cd web; npm run build

# E2E (API no ar no perfil e2e e banco koinoniahub_e2e)
cd api/KoinoniaHub.API; dotnet run --launch-profile e2e      # terminal 1
cd web; npm run test:e2e                                      # terminal 2 — 10 (3 + 7)
```

Se o E2E falhar por conexão ("transient failure", porta 5433), o `appsettings.E2E.json` voltou ao original: recolocar a string local em `ConnectionStrings:Postgres`.

### 7.5 Ligação com a monografia (seção 5.1 e objetivo específico 4)

A monografia avalia o sistema pelas características Adequação Funcional, Usabilidade, Confiabilidade e Segurança da ISO/IEC 25010.

| **Característica** | **Evidência produzida por este plano** | **Forma no capítulo de Resultados** |
| --- | --- | --- |
| Adequação Funcional | Um cenário Playwright ou teste xUnit por caso de uso (CSU01–CSU24), com resultado esperado e obtido | Tabela CSU → teste → resultado (Aprovado/Reprovado) → observação (7.3) |
| Confiabilidade | Índices únicos e regras de estado provados por xUnit (27.2, 32.2, 32.3, 33.x, 45.x); comportamento com dados inválidos; **procedimento de cópia e restauração executado** (seção 6.17) | Tabela RNF → teste → resultado |
| Segurança | Testes de integração de 403/400, Origin, filtro do termo, perfil × atribuição; prints das respostas | Tabela RNF → teste → evidência |
| Usabilidade | Não automatizada; prints a 360 px como evidência das RNFs de responsividade | Avaliação com o professor e o PO, conforme 5.1, feita no celular sobre as telas da 6.4 |

Convenções de evidência em `docs/evidencias/`: `RNF-<n.m>-<descricao>.png`, `RF<n>-<descricao>.png`, `CSU<nn>-<descricao>.png`; responsividade: `RNF-responsividade-<tela>.png`. Já capturadas: RNF-2.6 (2), RNF-11.3 (2), RNF-14.3, RNF-15.3, RNF-28.3, RNF-33.5, RNF-34.3 (2), RNF-35.4, RNF-35.5 e 33.6 (antes/depois da reabertura), RNF-38.4, RNF-42.1, RNF-44.1/44.2/44.3, RF42 (registro de aceite), RF43 (2), RF44 (2), RF3, RF20, RF31/CSU09, RF32 (desktop 1920, alterações não salvas, toque duplo, recarregar), RF33 (consolidada), RF34 (presenças desktop), RNF-32.5, RNF-32.6 (2), CSU11-FA1 (2), CSU11 passos 2–4 (nomes a 360), RNF-dialogos-tema (2), RF45 (nova aula com conteúdo, editar desktop e 360, confirmar excluir desktop e 360), RNF-45.2 (PUT e DELETE em Consolidada → 400), RNF-45.5 (sem Excluir com presença), RNF-responsividade usuarios/pessoas a 360 e os 15 RNF-responsividade gerados pelo projeto mobile na 6.4-C. `docs/testes.md` guarda a saída das validações de cada entrega; `docs/decisoes/` guarda os resumos por etapa. Todas as evidências usam a semente de teste (dados fictícios), nunca dados reais de membros.

## 8. Como pedir ajuda ao assistente do projeto

| **Situação** | **Pedido** |
| --- | --- |
| Iniciar uma entrega (no Claude Code, a partir da 6.5) | "Etapa 6.6 do Plano. Implemente a seção 6.15 (RF12, RNFs 12.3/12.4) conforme docs/monografia.md (RF12, CSU13). Antes de codar, liste os arquivos e os testes. Ao final, validações da seção 7.4, testes manuais clique a clique, commits em blocos e registro para o Plano." |
| Revisar código existente contra a monografia | "Compare AulaServico com o RF45 e as RNFs 45.1 a 45.5. Liste divergências e proponha o diff mínimo." |
| Escrever testes | "Escreva RF21_PerfilOperacionalTests conforme a seção 6.16, usando a KoinoniaHubWebApplicationFactory e login por cookie." |
| Verificar antes de avançar | "Antes de eu iniciar a 6.6, confira se a 6.5 atende à definição de pronto da seção 5 e liste o que falta." |
| Quando o código exigir mudar o texto | "Isto muda o que a monografia diz no RF/CSU X? Se sim, escreva a alteração no formato texto atual → texto novo e registre na seção 9." |

## 9. Pendências no texto da monografia

Itens decididos e ainda não aplicados no .docx, ou que só entram na conversão para TCC 2. (Já aplicados e retirados desta lista: 4.8 com as duas medidas contra CSRF, RNF 2.6 e referência OWASP [2026]a; RF44/CSU24 e figuras; RF45 e CSU09, com as RNFs 45.2/45.4/45.5 em Confiabilidade; RNFs 12.3/12.4 e CSU13; RNFs 21.3–21.5 e a frase da 4.9; em 15/10/2026: CSU11 passos 2–6 reorganizados numa única confirmação (FA1 no passo 4), CSU12 passo 1 "abre a aba Alunos (matrículas da turma)" — ajuste de requisito decorrente da decisão da 6.4, figura 3.16 mantida como protótipo — e as situações com inicial maiúscula em CSU10/CSU11/CSU15 e "O Administrador" no CSU24.)

| **Onde** | **O que** | **Quando** |
| --- | --- | --- |
| CSU09, passo 4 | "O usuário informa a data, a matéria, o professor responsável e o tema." → "O usuário informa a data, a matéria, o professor responsável e, opcionalmente, o tema e o conteúdo." (RF45 e CSU09 FA1 declaram o conteúdo editável; a 6.5 o expôs no diálogo, nos dois modos) | Próxima revisão do .docx |
| RF20 / protótipo | Figura "Tela de Turma" (abas) feita a partir da tela real (`RF20-pagina-turma-desktop.png`), com a descrição da seção 1.8 de `docs/decisoes/etapa-6.md`; renumeração das figuras seguintes | Antes da defesa |
| RF12 | Se a 6.6 for cortada: reverter RNFs 12.3/12.4 e o passo do CSU13 ao texto anterior antes da defesa | Só se a 6.6 cair |
| 4.5 / 4.7 / Apêndice | Versões resolvidas na etapa 0: .NET 10 (net10.0); JwtBearer, EF Core Tools e EF Core Design 10.0.12; Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3; Swashbuckle.AspNetCore 10.2.3; BCrypt.Net-Next 4.0.3. | Pronto para entrar |
| Implantação | IP do aceite = `RemoteIpAddress`; atrás de proxy reverso exige `ForwardedHeaders` (RNF 42.8). | Ao descrever a implantação |
| API sem RF | `POST /api/igrejas` (Admin) existe desde o início, não é usado pela SPA e não corresponde a RF: decidir na 7.1 entre remover (com teste de 404) ou justificar. | 7.1 |
| Todo o texto | Conversão para TCC 2: folha de rosto ("Trabalho de Conclusão de Curso"), verbos no passado, cronograma sai, Reuniões das Sprints com datas reais, 5.1 com o número real de participantes. | Após a 7.1 |
| Novos capítulos | Resultados (arquitetura implementada, telas reais, tabela CSU → teste, avaliação ISO 25010 com os participantes, cópia e restauração), Considerações finais, Resumo/Abstract. | Após a 7.3 |
| Trabalhos futuros | Fluxo guiado de configuração da turma (wizard); responsividade polida das telas administrativas (os filtros de Pessoas, Usuários e Atribuições são mais largos que 360 px e rolam dentro do conteúdo); "Limpar chamada" que remove os registros de presença de aula Em aberto (hoje uma chamada salva na aula errada só sai por consolidação ou reabertura). | Seção de trabalhos futuros do TCC 2 |
| Capa e listas | Banca, processo SEI, data, Lista de Figuras. | Versão final |

## 10. Referências das ferramentas de teste

- Vitest — https://vitest.dev/
- Vue Test Utils — https://test-utils.vuejs.org/
- Testes unitários em C# com xUnit — https://learn.microsoft.com/pt-br/dotnet/core/testing/unit-testing-csharp-with-xunit
- Testes de integração no ASP.NET Core (WebApplicationFactory) — https://learn.microsoft.com/pt-br/aspnet/core/test/integration-tests
- EF Core: testes com SQLite em memória — https://learn.microsoft.com/pt-br/ef/core/testing/testing-without-the-database
- Playwright — https://playwright.dev/
- OWASP Cross-Site Request Forgery Prevention Cheat Sheet — https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html
- PostgreSQL — pg_dump / pg_restore — https://www.postgresql.org/docs/current/app-pgdump.html
