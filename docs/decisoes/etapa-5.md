# Etapa 5 — Termo de Uso e Sigilo (RF42, RF43; gates em RF1, RF2, RF13, RF40) · resumo de decisões

Entregas 5.1 (API, 01/10/2026), 5.2 (front + filtro global na API, 03/10/2026) e 5.3 (Playwright, 03/10/2026). Plano de Desenvolvimento, seções 5, 6.3, 7.1, 7.2 e 7.3. Escrito conforme a regra "Resumo de decisões por etapa" (Plano 1.1).

## 1. Decisões tomadas dentro da margem de julgamento

### 1.1 Texto e versão do termo (5.1)

**Texto v1.0 em código, vigente desde 01/10/2026**, redigido pelo autor e revisado em quatro pontos antes de entrar: menção nominal à Lei nº 13.709/2018 (LGPD), item 8 sobre dados de crianças e adolescentes, cláusula de descumprimento e direitos dos titulares em redação genérica. Hash SHA-256 do texto: `9833a0509a1ec51b421e9ddb53d0d05f0dd338bcf8533628ce1d025ddc61ab92`.

**Fora do texto fixo (e, portanto, fora do hash):** o número da versão e a data de vigência (são metadados de `VersaoTermo`, RNF 42.6), a frase do checkbox "Li e aceito…" (é a interface, CSU23) e a identificação da igreja com o canal de contato (RNF 42.7). O texto diz "ao marcar a opção de aceite", não "abaixo", para não depender do layout.

**O texto informa o que o sistema registra** — versão, hash, data/hora, meio e IP de origem só para auditoria (RNFs 42.2, 42.5, 42.8). Não é interpretação jurídica nova: é transparência sobre o que o código faz.

**Hash sobre o texto normalizado** (`\r\n` → `\n`, `Trim`), para que salvar o arquivo com CRLF no Windows não mude o hash. Há teste disso (`TermosDeUso_HashIndependeDaQuebraDeLinha`).

**Cabeçalho dinâmico sem coluna nova no DER:** "Instituição" = `Igreja.Nome`; "Canal para assuntos relacionados a dados pessoais" = `Igreja.Email` e/ou `Igreja.Telefone`. Sem nenhum dos dois a tela escreve "[contato a ser definido pela igreja]" — nunca inventa um contato. (Pendência registrada: não existe tela para editar esses dados depois do cadastro inicial; ver seção 4.)

### 1.2 API (5.1)

**Validação do aceite antes de qualquer efeito.** No cadastro inicial, antes de checar e-mail e de criar a igreja; na ativação, antes de trocar a senha e consumir o convite. Um 400 nunca deixa rastro.

**Cadastro inicial em transação explícita.** Igreja, pessoa, usuário e aceite (Meio `CadastroInicial`) num `BeginTransactionAsync`, como o Plano 6.3 pede ("na mesma transação"). Na ativação bastou um único `SaveChanges`.

**`Montar` × `RegistrarAsync`.** Os fluxos públicos gravam o aceite junto com outras entidades, então `AceiteTermoServico.Montar` devolve o registro validado e quem grava é o fluxo; o aceite pós-login (`RegistrarAsync`) grava sozinho. A regra "é a versão vigente?" vive em um lugar.

**Aceite idempotente por versão.** Dois cliques não geram dois registros; uma nova versão do texto gera novo registro e `termoPendente` volta a `true` até o aceite. O registro é imutável (RNF 42.3): não há endpoint de edição ou exclusão.

**`Vigente` no DTO do aceite** (além de `versao` e `aceitoEm` do Plano) para a tela de Usuários distinguir "aceitou a atual" de "aceitou uma antiga" quando existir v1.1.

**IP = `RemoteIpAddress`.** Atrás de proxy reverso seria o IP do proxy; corrigir exige `ForwardedHeaders` — anotado para a implantação (Plano, seção 9).

### 1.3 Filtro global na API (decisão do autor, 5.2)

O Plano 6.3 colocava a exigência "antes de liberar qualquer outra tela" (RNF 2.5) só no front. Avisado (Plano 6, exceção b) que, sem bloqueio na API, uma conta pendente alcançaria os dados chamando a API por fora da tela, o autor aprovou o filtro global: `ExigeAceiteTermoFiltro` devolve **403 `{ mensagem, termoPendente: true }`** para toda requisição autenticada de conta sem aceite vigente, exceto endpoints `[AllowAnonymous]` e os marcados com `[PermitirSemAceiteTermo]` (`api/termo/*` e `api/auth/logout`). Uma consulta por requisição autenticada.

**`[AllowAnonymous]` explícito nos endpoints públicos do `AuthController`** (registrar-admin, login, primeiro-acesso GET/POST). Correção feita pelo autor na conferência da 5.2: as actions eram públicas por omissão (sem `[Authorize]`) e por isso não tinham o metadado que o filtro usa para dispensá-las; o teste `Login_ComCookieDeContaPendente_NaoEhBloqueado` falhou na primeira execução e passou depois do atributo. Sem ele, um navegador que ainda carregasse o cookie de uma conta pendente receberia 403 ao tentar fazer login de novo e ficaria preso entre `/login` e `/termo`. Conferido depois: nenhum outro controller tem endpoint público por omissão (`IgrejasController` tem `[Authorize]` nas duas actions; os demais, na classe).

**Sementes dos testes criam o aceite por padrão** (`SementeTermo.AceiteDe`); os cenários que precisam de conta pendente pedem `aceitarTermo: false`. O alvo do convite nunca recebe aceite na semente — o primeiro acesso é que o registra.

### 1.4 Front (5.2)

**Componente único `TermoUsoSigilo`** usado em três lugares: cadastro inicial e primeiro acesso (embutido, sem botão — o aceite vai com o formulário, cujo botão de concluir fica desabilitado até marcar, RNF 42.1) e na tela `/termo` após o login (com botão "Aceitar e continuar"). No cadastro inicial, o cabeçalho acompanha o que está sendo digitado em "Nome da Igreja" e "E-mail da igreja"; no primeiro acesso, vem de `GET /api/termo/vigente?token=` (igreja do convite).

**`/termo` fora do `LayoutPrincipal`** (sem menu): com menu a pessoa veria links que a guarda devolveria para a própria tela. Mostra só o termo, "Sair" e, se já aceito, "Voltar".

**Guard global com `redirecionar`:** autenticado + `termoPendente` + rota que não é a do termo → `/termo?redirecionar=<destino>`; após o aceite volta ao destino (sanitizado como no login). Vem depois das checagens de visitante/autenticação e antes das de perfil.

**403 com `termoPendente` tratado no `clienteHttp`** por `window.location` (como o 401 no mesmo interceptor), sem o toast "peça a um Admin". **`termoPendente` persistido** com a sessão no `localStorage`, para a guarda segurar a conta em `/termo` após F5 sem chamada; a API continua sendo a autoridade — um storage desatualizado é corrigido pelo 403.

**RF43:** "Versão X, aceito em DD/MM/AAAA HH:MM (no cadastro inicial / no primeiro acesso / após o login)" em Meus Dados; coluna "Termo" em Usuários com "Aceito vX" / "Pendente" / "Versão anterior (vX)". O rótulo do meio é texto de tela; o valor gravado continua `CadastroInicial | PrimeiroAcesso | Login`.

### 1.5 Playwright (5.3)

**Semente pela própria API, não por SQL.** Cada execução cria uma igreja nova com `POST /api/auth/registrar-admin` (isolamento por locatário, RNF 1.1) e, autenticada como Admin, cria pessoas, turma, atribuição, matrículas, matéria e usuários. Não há tabela, hash BCrypt nem conexão com o banco no código de teste, e o banco não precisa ser limpo entre execuções — as igrejas "E2E …" só se acumulam no banco de teste. A conta pendente do E2E-02 é uma conta com senha definida pelo Admin (`POST /api/usuarios` com `Senha`), que por desenho nasce sem aceite (RNF 13.4); o Professor do `storageState` recebe o aceite por `POST /api/termo/aceitar` na semente.

**`baseURL` é `http://localhost:5173`, não `https://…`** como o esboço do Plano 7.3 trazia: o Vite de desenvolvimento não tem certificado próprio e encaminha `/api` para `https://localhost:7054` (vite.config.ts). O cookie `Secure` funciona porque o navegador trata `localhost` como contexto seguro. A semente e as chamadas diretas dos testes falam com a API em https, com `Origin: http://localhost:5173` (RNF 2.6) e `ignoreHTTPSErrors`.

**Ambiente da API chama-se `E2E`, não `Testing`**, porque `Testing` é o nome que `KoinoniaHubWebApplicationFactory` já usa: um `appsettings.Testing.json` seria carregado também pelos 108 testes xUnit. `appsettings.E2E.json` aponta para o PostgreSQL de teste (`docker-compose.e2e.yml`, porta 5433) e tem chave JWT descartável; perfil `e2e` no `launchSettings.json`. Rodar contra a API de desenvolvimento também funciona (cada execução usa a própria igreja), mas acumula igrejas de teste no banco de desenvolvimento.

**O Playwright sobe o front (`npm run dev`) e, opcionalmente, a API** (`E2E_INICIAR_API=1`, perfil `e2e`); por padrão a API deve estar no ar antes, como o Plano descreve. `E2E_CHROMIUM` permite usar um Chrome/Chromium já instalado em vez de baixar o do Playwright.

**`storageState` de Admin e Professor** gravados em `e2e/.auth/` pelo `global-setup` (login pela tela, uma vez), para os fluxos das próximas etapas que começam autenticados (chamada → consolidar → relatório, restrições do Professor, reabertura, importação). Os três fluxos desta etapa não os usam: cada um começa do zero de propósito.

**Seletores por `data-testid`** nos elementos tocados (login, cadastro inicial, primeiro acesso, componente do termo, bloco de Meus Dados), como o Plano pede; os campos de senha do PrimeVue usam `inputId`.

## 2. Arquivos criados ou alterados

API — `Dominio/Termos/{VersaoTermo, TermosDeUso}.cs` (novos); `Aplicacao/DTOS/Respostas/{AceiteTermo, TermoVigente, MeusDados}RespostaDto.cs` (novos) e `{Login, Auth, Usuario}RespostaDto.cs`; `Aplicacao/DTOS/Requisicoes/TermoAceitarRequisicaoDto.cs` (novo), `RegistrarAdminRequisicaoDto.cs`, `PrimeiroAcessoAtivarRequisicaoDto.cs`; `Aplicacao/Servicos/{Interfaces/IAceiteTermoServico, Implementacoes/AceiteTermoServico}.cs` (novos), `AuthServico.cs`, `UsuarioServico.cs`; `Aplicacao/Seguranca/{ExigeAceiteTermoFiltro, PermitirSemAceiteTermoAttribute}.cs` (novos); `Controllers/TermoController.cs` (novo), `AuthController.cs`, `MeusDadosController.cs`; `Program.cs`; `appsettings.E2E.json` (novo); `Properties/launchSettings.json` (perfil `e2e`).

Testes xUnit — `Infraestrutura/SementeTermo.cs` (novo), `CenarioAcessoPessoa.cs`, `CenarioAula.cs`; `RF42_TermoTests` (17); `RF2_GateTermoTests` (10); ajustes em `Etapa0_FumacaTests`, `RF2_TokenSomenteNoCookieTests`, `RF14_UltimoAdminTests`, `RF39_RF40_ConviteTests`. Total do projeto: 108.

Front — `aplicacao/modelos/dtos.ts`; `aplicacao/servicos/{termoServico (novo), authServico, clienteHttp, meusDadosServico, usuariosServico}.ts`; `aplicacao/armazenamentos/autenticacaoStore.ts` (+ spec, 5); `aplicacao/rotas/{guardas (+ spec, 5), index}.ts`; `components/termo/TermoUsoSigilo.vue` (novo, + spec, 7); `paginas/privado/PaginaTermo.vue` (novo); `paginas/privado/meus-dados/PaginaMeusDados.vue`; `paginas/privado/usuarios/PaginaUsuariosLista.vue`; `paginas/publico/{PaginaLogin, PaginaCadastroInicial, PaginaPrimeiroAcesso}.vue`. Vitest: 45 casos.

Playwright — `playwright.config.ts`; `e2e/apoio/{ambiente, api, semente}.ts`; `e2e/global-setup.ts`; `e2e/e2e-01-cadastro-inicial.spec.ts`, `e2e/e2e-02-login-termo-pendente.spec.ts`, `e2e/e2e-03-convite-ativacao.spec.ts`; `e2e/tsconfig.json`; `package.json` (`test:e2e`, `@playwright/test`); `.gitignore` (`e2e/.auth/`, relatórios); `docker-compose.e2e.yml` (raiz).

Evidências — `RF42-registro-aceite-hash-meio-ip.png`, `RNF-42.1-400-cadastro-sem-aceite.png` (5.1); `RF42-tela-termo.png`, `RF42-primeiro-acesso-termo.png`, `RF43-meus-dados-termo.png`, `RF43-usuarios-coluna-termo.png`, `RNF-2.5-403-termo-pendente.png`, `RNF-2.5-redirecionamento-termo.png`, `RNF-42.1-concluir-desabilitado.png` (5.2); relatório HTML do Playwright com os três fluxos verdes (5.3).

## 3. O que o autor deve saber explicar na banca

- Por que o hash é calculado sobre o texto fixo e não sobre a tela renderizada (RNF 42.7), e o que aconteceria com a prova do aceite se a identificação da igreja estivesse dentro do texto.
- A diferença entre o Termo de Uso e Sigilo (compromisso de quem **acessa** dados, usuários do sistema) e o consentimento dos titulares (seção 4.9 da monografia) — e por que o sistema registra o primeiro e não trata do segundo.
- Os três meios de aceite (cadastro inicial, primeiro acesso, login) e por que uma conta criada com senha definida pelo Admin aceita no primeiro login (RNF 13.4).
- Por que a exigência de aceite está em duas camadas — guard de rota no front **e** filtro global na API — e o que a segunda camada impede que a primeira não impede.
- Por que o login nunca pode ser bloqueado pelo filtro (o caso do cookie de uma conta pendente) e o que `[AllowAnonymous]` explícito garante além de documentar a intenção.
- Como o E2E semeia dados pela própria API e por que isso é preferível a SQL direto num sistema multi-igreja.

## 4. Pendência registrada para decisão do autor (fora da baseline)

**Edição dos dados da igreja após o cadastro inicial.** A monografia não tem RF para alterar nome, e-mail ou telefone da igreja; o cadastro inicial (RF1) coleta nome, cidade, estado e e-mail, e não coleta telefone. Como a RNF 42.7 exibe o canal de contato "a partir do cadastro da igreja", hoje um e-mail errado ou um telefone só podem ser corrigidos direto no banco. Sugestão do autor (03/10/2026): `PUT /api/igrejas/{id}` restrito a Admin e à própria igreja + tela simples, na Etapa 6. Depende de incluir o requisito na monografia (novo RF e caso de uso) antes de implementar.
