# Etapa 2 — Segurança de leitura e escrita · resumo de decisões

Entregas 2.1 (RNFs 11.3/28.3/34.3), 2.2 (RNF 2.6), 2.3 (RNF 14.3) e 2.4 (RF15 e JWT só no cookie), de 27 a 28/09/2026 (Plano, seções 5, 6.5 a 6.8). Escrito conforme a regra "Resumo de decisões por etapa" (Plano 1.1).

## 1. Decisões tomadas dentro da margem de julgamento

### 1.1 Restrições de leitura do Professor (2.1 · Plano 6.5)

**Uma regra, um lugar.** `AutorizacaoEbdServico.GarantirAcessoPessoaAsync` decide para os três endpoints (pessoa, pessoas disponíveis, presenças da pessoa): gestão passa; `Usuario` só o próprio `PessoaId`; `Professor` só se a pessoa tiver matrícula ativa em turma na qual ele tenha atribuição ativa. `ListarDepartamentosComAtribuicaoAtivaAsync` existe porque a RF34 precisa da *lista* de turmas para filtrar o histórico, não só do sim/não; usa a mesma consulta interna.

**`Perfis` como classe de constantes** (`Aplicacao/Seguranca`), no padrão de `SituacaoAula`/`MeioAceiteTermo`, para os controllers escolherem o DTO por perfil sem repetir listas literais.

**DTOs reduzidos** exatamente como o Plano 6.5: `PessoaTurmaRespostaDto` (Id, Nome, Situacao, Celular, parentescos com nome, tipo e celular) e `PessoaDisponivelRespostaDto` (Id, Nome, Situacao). Gestão e `Usuario` (no próprio registro) continuam com o DTO completo.

**Guard antes do 404**: para o Professor, pessoa fora das suas turmas devolve 403 mesmo que o id não exista — ele não descobre se a pessoa existe. Gestão recebe 404 para id inexistente. Professor consultando o próprio registro por `GET /api/pessoas/{id}` recebe 403 (não é aluno de turma sua); o caminho é `GET /api/meus-dados`.

**Parentescos (`GET /api/pessoas/{id}/parentescos`)**: estava aberto ao Professor para qualquer pessoa. Recebeu o mesmo guard. A RNF 12.1, lida à risca, restringiria a operação à gestão; o autor decidiu a opção (a): Professor continua na rota, limitado às suas turmas pelo guard, porque o diálogo "Responsáveis" da tela de Matrículas depende dela. Registrado no Plano 2.4.

### 1.2 Verificação de Origin nas escritas (2.2 · Plano 6.6)

**Código do Plano aplicado como está**, entre `UseCors` e `UseAuthentication`: POST/PUT/PATCH/DELETE com `Origin` fora de `Cors:OrigensPermitidas` → 403 `{ "mensagem": "Origem não autorizada." }`; sem `Origin` também 403, como recomenda o OWASP CSRF Prevention Cheat Sheet (fonte conferida em 27/09/2026). **Sem fallback para `Referer`**: navegadores enviam `Origin` em toda escrita da SPA, e o que chega sem ele (curl, Postman) é justamente o que deve ser recusado.

**Antes da autenticação**, como o Plano manda: escrita de origem estranha recebe 403 mesmo sem token; um teste fixa isso.

**Origens de desenvolvimento**: Vite (`http://localhost:5173`) e os dois perfis do Swagger (`https://localhost:7054`, `http://localhost:5202`). A origem do Playwright entra na Etapa 7.

**Fábrica de testes envia `Origin` autorizado em todo `HttpClient`** — a alternativa seria ajustar os 28 testes existentes um a um; `RF2_OrigemTests` remove ou troca o cabeçalho localmente.

### 1.3 Último administrador (2.3 · Plano 6.7)

**Código do Plano como está**, com `Perfis.Admin` no lugar do literal no serviço (o repositório manteve o literal por estar na camada de Infraestrutura). Ordem das verificações em `AtualizarAsync`: autoinativação → RNF 14.3 → validação do perfil; `<= 1` e não `== 1`, para bloquear também contagem inconsistente.

**Sutileza registrada**: pela API, o único Admin ativo é o único ator capaz de chamar `PATCH /api/usuarios/{id}`; para inativação, a autoproteção anterior ("Você não pode desativar o seu próprio usuário") dispara antes da 14.3. O ramo efetivamente alcançável pela API é o **rebaixamento de perfil**, que a autoproteção não cobria; o ramo "inativar por outro ator" é provado por teste unitário do serviço.

### 1.4 Redefinição de acesso só por convite e JWT só no cookie (2.4 · Plano 6.8)

**Remoção completa do reset com senha**: rota `PATCH /api/usuarios/{id}/resetar-senha`, `UsuarioServico.ResetarSenhaAsync`, `UsuarioResetarSenhaRequisicaoDto`, e no front `resetarSenhaUsuario`, `UsuarioResetarSenhaDTO`, o botão e o diálogo "Resetar senha". "Redefinir acesso" é o mesmo botão de convite, que chama `POST /api/usuarios/{id}/convite` (RF15/CSU20): a senha atual continua válida até o link ser usado, e o administrador nunca digita a senha de outro usuário (RNF 15.3). O teste `ResetarSenha_RotaRemovida_Retorna404` fixa a ausência da rota.

**`[JsonIgnore]` em `Token` nos DTOs de resposta de login e cadastro inicial**, em vez de mudar a assinatura dos serviços. O controller ainda precisa do token para gravar o cookie; o contrato com o cliente não o inclui (monografia 4.8: por ser httpOnly, "o token não fica acessível a scripts"). Foi a menor alteração que fecha a divergência registrada na Etapa 0 sem tocar `AuthServico`, `AuthController` nem o store do front. A alternativa (serviço devolver um par resposta + token) exigiria mais arquivos sem ganho para o cliente; `RF2_TokenSomenteNoCookieTests` prova que o corpo não contém o JWT, nem sob outro nome.

**Cadastro inicial**: a tela decidia entrar no sistema pela presença de `Token` no corpo; passou a decidir por `UsuarioId`. Correção de uma afirmação minha na Etapa 0 ("o front atual não depende do campo") — dependia nesse ponto.

**Testes além dos quatro do Plano 7.1**: `GerarConvite_ComoProfessor_Retorna403` (39.1/15.1), `GerarConvite_SenhaAtualContinuaValidaAteOUso` (CSU20) e `ResetarSenha_RotaRemovida_Retorna404` (15.3), porque cada um prova uma RNF de Segurança com uma linha na tabela de Resultados.

## 2. Arquivos criados ou alterados

API — `Aplicacao/Seguranca/{Perfis, IAutorizacaoEbdServico, AutorizacaoEbdServico}.cs`; `Aplicacao/DTOS/Respostas/{PessoaTurmaResposta, PessoaTurmaParentescoResposta, PessoaDisponivelResposta, LoginResposta, AuthResposta}Dto.cs`; `Aplicacao/Servicos/Interfaces/{IPessoaServico, IMatriculaServico, IPresencaHistoricoServico, IUsuarioServico}.cs`; `Aplicacao/Servicos/Implementacoes/{PessoaServico, MatriculaServico, PresencaHistoricoServico, UsuarioServico}.cs`; `Controllers/{PessoasController, PresencasPessoaController, MatriculasController, ParentescosController, UsuariosController}.cs`; `Dominio/Interfaces/Repositorios/IUsuarioRepositorio.cs`; `Infraestrutura/Repositorios/UsuarioRepositorio.cs`; `Program.cs` (middleware); `appsettings.Development.json`. Removido: `Aplicacao/DTOS/Requisicoes/UsuarioResetarSenhaRequisicaoDto.cs`.

Testes — `Infraestrutura/{KoinoniaHubWebApplicationFactory, CenarioAcessoPessoa}.cs`; `RF11_PessoaAcessoTests` (5); `RF28_DisponiveisTests` (3); `RF34_PresencasPessoaTests` (3); `RF2_OrigemTests` (6 métodos, 8 execuções); `RF14_UltimoAdminTests` (6); `RF39_RF40_ConviteTests` (7); `RF2_TokenSomenteNoCookieTests` (2).

Front — `aplicacao/modelos/dtos.ts`; `aplicacao/servicos/{usuariosServico, authServico}.ts`; `paginas/publico/PaginaCadastroInicial.vue`; `paginas/privado/usuarios/PaginaUsuariosLista.vue`.

Evidências — `docs/evidencias/RNF-11.3*.png`, `RNF-28.3*.png`, `RNF-34.3*.png`, `RNF-2.6-403-origem-nao-autorizada.png`, `RNF-2.6-passa-origem-autorizada.png`, `RNF-14.3-400-ultimo-admin.png`, `RNF-15.3-404-resetar-senha.png`.

Monografia — 4.8 (parágrafo de CSRF), RNF 2.6 no RF2, referências OWASP [2026]a/[2026]b.

## 3. O que o autor deve saber explicar na banca

- A diferença entre autorização por perfil (`[Authorize(Roles)]`) e autorização por atribuição (`GarantirAcesso*Async`), e por que o Professor recebe DTOs reduzidos (minimização de dados, LGPD) em vez de apenas um "permitido/negado".
- Por que CORS sozinho não impede CSRF com `SameSite=None` (requisições simples sem preflight: convite, importação, logout) e como a verificação de `Origin` no servidor cobre esse espaço; por que recusar requisições sem `Origin` é a recomendação da OWASP.
- Por que a RNF 14.3 existe (sem um Admin ativo ninguém redefine acessos) e por que, pela API, o caso real é o rebaixamento de perfil.
- Por que o administrador não pode mais definir a senha de ninguém (RNF 15.3): a senha só existe como hash BCrypt escolhido pelo próprio titular, e a redefinição é um convite de uso único que invalida o anterior e deixa a senha atual válida até o uso.
- Por que o JWT vai apenas no cookie httpOnly e o que muda se ele também fosse devolvido no JSON (exposição a XSS, contra a seção 4.8).
