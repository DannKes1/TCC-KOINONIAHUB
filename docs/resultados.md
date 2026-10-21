# Resultados — KoinoniaHub

Material para o capítulo de Resultados do TCC (Plano de Desenvolvimento v1.8, seções 5 e 7.5;
monografia, seção 5.1). Etapa 7.3, 19 e 20/10/2026.

A monografia avalia o sistema por quatro características da ISO/IEC 25010. Este documento reúne
a evidência de três delas:

| Característica | Evidência | Seção |
| --- | --- | --- |
| Adequação Funcional | Tabela CSU → teste → resultado (CSU01–CSU24) | 2 |
| Segurança | Tabela RNF → teste → evidência | 3 |
| Confiabilidade | Tabela RNF → teste → resultado, incluindo a cópia e restauração dos dados | 4 |

A Usabilidade é avaliada com os participantes (seção 5.1), sobre as telas a 360 px; os prints
`RNF-responsividade-*.png` estão em `docs/evidencias/`.

## 1. Base da avaliação

**Suítes automatizadas** (todas verdes em 20/10/2026):

| Ferramenta | Camada | Casos |
| --- | --- | --- |
| xUnit + WebApplicationFactory + SQLite em memória | API (regras, autorização, integridade) | 170 |
| Vitest + @vue/test-utils | Front (funções, componentes, guardas de rota) | 126 (24 arquivos) |
| Playwright | Fluxos ponta a ponta, desktop e celular 360 × 640 | 18 (10 `chromium` + 8 `mobile`) |

**Testes manuais**: registrados por etapa em `docs/testes.md`, com o passo, o resultado esperado,
a marca ✔ quando executado e o print correspondente em `docs/evidencias/`. Os roteiros da 7.3
(cópia e restauração; evidências D1 a D10) estão em `docs/testes.md`, Etapa 7.3.

**Como ler o resultado de cada caso de uso:**

- **Aprovado**: todos os passos do fluxo principal e todos os fluxos alternativos são exercitados
  por teste automatizado (em qualquer camada) ou por teste manual registrado com ✔, e todos os
  testes citados passam. Um dado exibido dentro de um passo exercitado sem asserção própria, ou o
  caminho negativo de uma validação de formulário, não rebaixa o resultado: fica entre as lacunas
  da seção 2.2.
- **Parcial**: todos os testes citados passam, mas algum passo do fluxo principal ou algum fluxo
  alternativo não tem teste, ou o comportamento difere do descrito no caso de uso (seção 6).
- **Não coberto**: nenhum teste exercita o caso de uso.

Nenhum teste citado falha; por isso não há a categoria "Reprovado" prevista na seção 7.5 do
Plano.

**Como os testes são citados.** xUnit: `Classe.Metodo`; `Classe` (n) quando todos os n casos da
classe se aplicam (cada `InlineData` de um `[Theory]` conta um caso); para um recorte, os métodos
pelo nome ou "(k dos n casos: …)". Vitest: `arquivo.spec`, com a mesma regra para (n).
Playwright: E2E-01 a E2E-08, E2E-M1 e E2E-M2 (seção 7.3 do Plano). Manual: "manual Etapa X,
passo N" (`docs/testes.md`).

## 2. Adequação Funcional — CSU → teste → resultado

### 2.1 Tabela

| CSU | Caso de uso | Testes que o cobrem | Resultado | Observação |
| --- | --- | --- | --- | --- |
| CSU01 | Realizar Cadastro Inicial | E2E-01 (desktop e celular)<br>`Etapa0_FumacaTests.CadastroInicial_Login_EListagem_Funcionam`<br>`RF42_TermoTests.RegistrarAdmin_ComAceite_GravaMeioCadastroInicialESemPendencia`, `RegistrarAdmin_SemAceite_Retorna400`<br>`RF1_CadastroInicialTests` (1: senha só como hash BCrypt)<br>`RF2_TokenSomenteNoCookieTests.RegistrarAdmin_NaoDevolveTokenNoCorpo_SoNoCookie`<br>manual Etapa 5.1, passo 2 (`RNF-42.1-400-cadastro-sem-aceite.png`) | Parcial | Fluxo principal e FA2 (conclusão desabilitada sem o aceite) no E2E-01. O FA1 (dados inválidos, mensagens junto aos campos) não tem teste. O sistema abre a sessão e vai ao Painel, e não à tela de login como diz o passo 5 (seção 6). |
| CSU02 | Realizar Login | E2E-02 (desktop e celular), E2E-03, E2E-05<br>`RF2_GateTermoTests` (10)<br>`RF42_TermoTests.Login_SemAceiteVigente_RetornaTermoPendente`<br>`RF2_TokenSomenteNoCookieTests.Login_NaoDevolveTokenNoCorpo_SoNoCookie`<br>`Etapa0_FumacaTests.Login_ComSenhaErrada_Retorna400`; `RF39_RF40_ConviteTests.GerarConvite_SenhaAtualContinuaValidaAteOUso` (senha errada de conta existente → 400)<br>`guardas.spec`, `autenticacaoStore.spec`<br>manual Etapas 2.4 e 5.1 | Aprovado | FA2 (termo pendente) no E2E-02, no filtro global da API e nas guardas do front. FA1 (credenciais inválidas) provado na API (400); a mensagem na tela não tem asserção. |
| CSU03 | Realizar Logout | `RF2_GateTermoTests.TermoELogout_SemAceite_Liberados`<br>`autenticacaoStore.spec` (sair limpa a sessão e a pendência) | Parcial | A rota responde 204 e a store limpa o estado. Nenhum teste aciona Sair no menu e confere a volta ao login e o cookie expirado. |
| CSU04 | Visualizar Painel | E2E-M1 (painel do aluno e do professor, celular); E2E-01 (título do painel de gestão)<br>`RF34_PresencasPessoaTests.Presencas_UsuarioComum_SoAsProprias`, `Presencas_ProfessorNaTurma_FiltraPelasSuasTurmas` (situação da aula em cada registro, usada pelo painel)<br>`indicadoresPresenca.spec` (5)<br>manual Etapa 4.2, passo 5 (`RF3-painel-aluno-consolidadas.png`); Etapa 6.1, passo 3 (`RF3-painel-aluno-90-dias.png`) | Parcial | Painel do usuário comum coberto, com o cálculo só de aulas Consolidadas e a janela de 90 dias. Do painel de gestão, só o título é conferido. |
| CSU05 | Gerenciar Meus Dados | E2E-01, E2E-02, E2E-03 (bloco do termo)<br>`RF42_TermoTests.MeusDados_EUsuarios_IncluemAceite`<br>manual Etapa 5.2, passo 3 (`RF43-meus-dados-termo.png`); Etapa 1.3, bloco 2, item 5 | Parcial | A exibição do aceite está coberta. A edição (alterar, validar, salvar) só tem o registro "OK" da Etapa 1.3, sem print; nenhum teste chama `PUT /api/meus-dados`. |
| CSU06 | Consultar Minhas Turmas | E2E-05, E2E-M1, E2E-04 (navegação)<br>`RF21_PerfilOperacionalTests.MinhasTurmas_UsuarioComAtribuicaoAtiva_SemVinculoDeAtribuicao`<br>manual Etapa 7.1, passos 6 e 10 (`RNF-21.4-minhas-turmas-usuario.png`); Etapa 7.2, T3 (`RNF-21.4-usuario-com-atribuicao-minhas-turmas.png`) | Aprovado | Vínculos Aluno e Professor na API; na tela, o vínculo Aluno é conferido (E2E-05) e o de Professor pela ação Abrir turma (E2E-04, E2E-M1). A conta Usuario com atribuição vê só o vínculo Aluno (RNF 21.4). O responsável de cada turma não tem asserção. |
| CSU07 | Consultar Minha Frequência | `RF35_RelatorioTests.MinhaFrequencia_SoConsolidadas_EListaPendentes`<br>E2E-M1 (aluno)<br>manual Etapas 4.2, passo 4 e 6.3, passo 4 | Aprovado | Cálculo só com Consolidadas e pendentes à parte (xUnit); a tela abre pela turma (E2E-M1) e foi conferida no manual 4.2. |
| CSU08 | Gerir Matérias | `PaginaMateriasTurma.spec` (cartões na ordem de exibição; criação pelo diálogo, API simulada)<br>E2E-M1 (listagem)<br>manual Etapa 6.4, passo 2 | Parcial | Listagem e criação pelo diálogo cobertas no front. Nenhum xUnit de `POST`/`PUT /api/materias`; o FA1 (nome repetido na turma) e a edição não têm teste. |
| CSU09 | Gerir Aulas | E2E-04 (cria a aula pela tela, desktop e celular), E2E-M1<br>`RF45_EditarAulaTests` (9: FA1, FA2, FA3)<br>`RF21_PerfilOperacionalTests.CriarAula_ResponsavelComAtribuicaoDeAuxiliar_Retorna400`<br>`PaginaAulasTurma.spec` (3), `TagSituacaoAula.spec` (6), `situacaoAula.spec`<br>manual Etapa 3.2, passo 1 (`RF31-CSU09-listagem-tres-tags.png`); Etapa 6.5, passos 1 a 10 | Aprovado | Criação pela tela no E2E-04; pela API, responsável com atribuição de Auxiliar é recusado e o Professor é aceito (RNF 21.3). FA1 a FA3 com xUnit, Vitest e manual. |
| CSU10 | Lançar Chamada | E2E-04, E2E-M1, E2E-06<br>`RF32_ChamadaTests` (6)<br>`RF21_PerfilOperacionalTests.RegistrarPresenca_UsuarioComAtribuicaoAtiva_Retorna403ENadaGravado`<br>`chamada.spec`, `LinhaChamada.spec`, `PaginaChamadaAula.spec`<br>manual Etapa 6.4, passos 3 e 7 (`RF32-chamada-desktop-1920.png`, `RF32-chamada-toque-duplo-360.png`); Etapa 6.1, passo 1 (`RNF-32.5-dica-observacao.png`) | Aprovado | Marcar, observar (com a orientação da RNF 32.5) e salvar em lote; FA1 (aula fechada) na API e na tela. Visitantes diferente de zero só no front (`chamada.spec` monta o lote com 3); nenhum teste os grava pela API ou pela tela. |
| CSU11 | Consolidar e Reabrir Aula | E2E-04, E2E-M1, E2E-06<br>`RF33_ConsolidarAulaTests` (5), `RF33_NaoRealizadaTests` (4), `RF33_ReabrirAulaTests` (7)<br>`PaginaChamadaAula.spec`, `LinhaChamada.spec`<br>manual Etapa 7.2, T2 e T4 (`CSU11-confirmacao-sem-registro.png`, `CSU11-reabrir-preserva.png`, `CSU11-reconsolidada.png`); Etapas 3.1, 3.2, 4.1 e 6.4 | Aprovado | Passos 2 a 6 no E2E-04 e no E2E-M1: a confirmação nomeia quem fica ausente (os dois cancelam nesse ponto) e o E2E-M1 confirma "Salvar e consolidar" com alteração pendente; gravar os nomeados como ausentes ao confirmar está no `PaginaChamadaAula.spec` (API simulada) e no manual 6.4, passo 4. FA1 na API; FA2 na API e no manual 3.2; FA3 (só o Admin; presenças preservadas) no E2E-06 e no xUnit. |
| CSU12 | Gerir Matrículas | `RF27_MatriculaTests` (3), `RF28_DisponiveisTests` (3)<br>`PaginaMatriculasTurma.spec` (cartões; remover pelo ⋮ com confirmação, API simulada; histórico pelo ⋮)<br>E2E-M1 (listagem)<br>manual Etapa 6.4, passo 2; Etapa 2.1 (`RNF-28.3-disponiveis-dto-reduzido.png`) | Parcial | Regras provadas: matrícula ativa única (27.2) e disponíveis em DTO reduzido (28.3). A matrícula com sucesso pela tela só tem o manual 6.4, passo 2 (contador após matricular), sem conferir a data; do FA1, só a confirmação de Remover tem teste — a data de saída e a preservação das presenças, não. |
| CSU13 | Gerir Parentescos | `RF12_ParentescoDerivadoTests` (13)<br>`TabelaParentescos.spec` (6)<br>manual Etapa 6.6, passos 1 a 9 (`RF12-parentesco-origem-explicito.png` e 5 prints `RNF-12.3-*`) | Aprovado | Vínculos explícitos e derivados (grau inverso), inclusão pela tela e FA1 (remoção pelo lado derivado recusada). O caminho negativo da validação não tem teste. |
| CSU14 | Consultar Presenças do Aluno | `RF34_PresencasPessoaTests` (3)<br>`PaginaMatriculasTurma.spec` (Histórico pelo ⋮ abre o diálogo e carrega pela pessoa do aluno); `DialogHistoricoPresencas.spec` (4: carrega ao abrir, totais só de Consolidadas, cartões no celular, tabela no desktop)<br>manual Etapa 2.1 (`RNF-34.3-403-fora-da-turma.png`, `RNF-34.3-200-presencas-filtradas.png`); Etapa 6.1, passo 4 (`RF34-historico-pessoas-consolidadas.png`); roteiro 7.3-D9 | Aprovado | Passo 1 pela aba Alunos da Página da Turma (corretiva de 20/10: o Professor consulta pelo mesmo diálogo de Pessoas); passo 2 na API (403 fora das turmas do Professor); passo 3 com data, turma, matéria e situação na tela e turma e situação conferidas no xUnit. |
| CSU15 | Consultar Relatórios da Turma | `RF35_RelatorioTests.FrequenciaTurma_IgnoraAulasNaoRealizadasEEmAberto`, `FrequenciaTurma_ListaPendentesDeFechamento`, `Acompanhamento_SoConsolidadas_EListaPendentes`, `RankingFaltas_SoConsolidadas_EListaPendentes`<br>`RF35_FusoHorarioTests.FrequenciaTurma_MatriculaAs23hLocaisDoDiaD_EntraComFimD`, `AulaDeHoje_As21hLocais_NaoEhPendente`, `AulaDeOntem_EhPendente`<br>`BotoesExportacao.spec` (7)<br>E2E-04 (Frequência após consolidar, desktop e celular), E2E-05 (sem exportação)<br>manual Etapas 4.1 e 4.2; Etapa 7.2, T1 a T3 (`RF35-relatorio-matricula-noite.png`, `CSU15-frequencia-apos-consolidar.png`, `RNF-35.4-professor-sem-exportacao.png`) | Aprovado | Frequência, Acompanhamento e Ranking só com Consolidadas; período no dia civil da igreja; exportação só para a gestão. |
| CSU16 | Gerir Pessoas | `RF11_PessoaAcessoTests` (4 dos 5 casos: os `ObterPessoa_*`), `RF41_ImportacaoTests` (5)<br>`pessoasServico.spec` (4), `MenuLateral.spec`<br>E2E-07 (importação), E2E-05<br>manual Etapa 6.1, passo 2 (`RNF-41.3-importacao-conferir.png`); Etapa 7.2, T5 (`RNF-41.3-importacao-conferencia.png`); Etapa 1.3 | Parcial | FA1 (importação CSV com duplicidades, RNF 41.3) e a consulta cobertos. Cadastro e edição só têm o registro "OK" da Etapa 1.3; o FA2 (inativar e reativar, RF9) não tem teste. |
| CSU17 | Gerir Turmas | `PaginaTurma.spec` (4)<br>manual Etapa 6.4, passo 1 (`RF20-pagina-turma-desktop.png`) | Parcial | Só a consulta da turma (Página da Turma, RF20). Listagem com filtro, cadastro, edição e inativação (RF17 a RF19) não têm teste. |
| CSU18 | Gerir Atribuições | `RF21_PerfilOperacionalTests.ListarAtribuicoes_PerfilConta_NuloSemContaEIgualAoPerfilComConta` e os 4 casos da RNF 21.4 (seção 3)<br>`PaginaAtribuicoesDepartamento.spec`, `atribuicoes.spec` (4), `guardas.spec`, `PaginaTurma.spec`<br>E2E-05<br>manual Etapa 7.1, passos 1 a 3 (`RNF-21.5-*.png`, 3) | Parcial | Inclusão pela tela (manual 7.1) e efeito perfil × atribuição (21.4) cobertos. O FA1 (encerrar atribuição) não tem teste. |
| CSU19 | Gerir Usuários | `RF13_UsuarioTests` (6: senha só como hash; 403 de Professor em GET, POST e PATCH; 403 de Pastor e Superintendente)<br>`RF14_UltimoAdminTests` (6)<br>`RF39_RF40_ConviteTests.GerarConvite_GuardaSoHash`, `GerarConvite_ComoProfessor_Retorna403`, `GerarNovoConvite_InvalidaAnterior`, `GerarConvite_SenhaAtualContinuaValidaAteOUso`, `ListarEObter_AposGerarConvite_NaoDevolvemToken`<br>`RF42_TermoTests.MeusDados_EUsuarios_IncluemAceite`<br>`MenuLateral.spec`<br>E2E-08, E2E-03, E2E-05<br>manual Etapa 2.3 (`RNF-14.3-400-ultimo-admin.png`); Etapa 5.2, passo 6 (`RF43-usuarios-coluna-termo.png`); Etapa 7.2, T6 (`RNF-14.3-perfil-ultimo-admin.png`); roteiro 7.3-D1 | Parcial (Aprovado após o roteiro D1) | Listagem com o aceite, edição, último Admin e convite (FA1) cobertos; criação com senha e com convite provada pela API, e o token fora das leituras. O diálogo Novo Usuário com o link exibido uma vez é exercitado pelo roteiro D1 da 7.3. |
| CSU20 | Resetar Senha de Usuário | `RF39_RF40_ConviteTests` (6 dos 11 casos: `GerarConvite_GuardaSoHash`, `GerarNovoConvite_InvalidaAnterior`, `GerarConvite_SenhaAtualContinuaValidaAteOUso`, `Ativar_SegundaVez_Retorna400`, `GerarConvite_ComoProfessor_Retorna403`, `ResetarSenha_RotaRemovida_Retorna404`)<br>manual Etapa 2.4, passos 1 e 2; evidência da 2.4 por curl (`RNF-15.3-404-resetar-senha.png`); roteiro 7.3-D2 | Aprovado | Redefinir acesso gera um convite de uso único, que invalida o anterior; a senha nunca é digitada pelo Admin (RNF 15.3). |
| CSU21 | Consultar Resumo do Dia | `RF35_RelatorioTests.ResumoDia_SomaSoConsolidadas`, `ResumoDia_EmAbertoDeHoje_NaoEhPendente`, `ResumoDia_ComoProfessor_Retorna403`<br>`RF35_FusoHorarioTests.AulaDeHoje_As21hLocais_NaoEhPendente`, `AulaDeOntem_EhPendente`<br>E2E-05 (aba ausente para o Professor)<br>manual Etapa 4.2, passo 3 (`RNF-38.4-resumo-dia-situacoes.png`) | Aprovado | Totais só de Consolidadas, listas separadas de Não realizadas e pendentes, situação por turma e 403 do Professor. |
| CSU22 | Ativar Primeiro Acesso | E2E-03<br>`RF39_RF40_ConviteTests.Ativar_ConviteExpirado_Retorna400`, `Ativar_SegundaVez_Retorna400`, `Inativar_DescartaConvitePendente_EReativarNaoORecria`, `Ativar_ContaInativadaForaDaApi_Retorna400`, `GerarNovoConvite_InvalidaAnterior`<br>`RF42_TermoTests.PrimeiroAcesso_ComAceite_GravaMeioPrimeiroAcesso`, `PrimeiroAcesso_SemAceite_Retorna400ENaoConsomeConvite`, `Vigente_ComTokenDeConvite_RetornaIgrejaDoConvite`<br>`TermoUsoSigilo.spec` (3 dos 7 casos, os do uso embutido: versão, igreja e contato fora do texto; caixa de aceite sem botão próprio; erro de carregamento)<br>manual Etapa 5.1, passo 4; Etapa 5.2, passo 5 (`RF42-primeiro-acesso-termo.png`); roteiro 7.3-D3 | Aprovado | Tela pública com o termo da igreja, senha em BCrypt, uso único; FA1 (convite inválido, expirado, já usado ou de conta inativa) e FA2 (aceite obrigatório) cobertos. |
| CSU23 | Aceitar Termo de Uso e Sigilo | E2E-01, E2E-02, E2E-03<br>`RF42_TermoTests` (13 dos 17 casos: `Vigente_*` (3), `Login_SemAceiteVigente_RetornaTermoPendente`, `Aceitar_*` (4), `RegistrarAdmin_*` (2), `PrimeiroAcesso_*` (2), `Servico_RegistrarGuardaIpSoParaAuditoria`)<br>`Etapa1_AceiteTermoTests.AceiteTermo_Gravado_PersisteTodosOsCampos`; `RF2_GateTermoTests.AposAceite_RotasLiberadas`<br>`TermoUsoSigilo.spec`, `guardas.spec`, `autenticacaoStore.spec`<br>manual Etapa 5.1 (`RF42-registro-aceite-hash-meio-ip.png`); Etapa 5.2 (`RF42-tela-termo.png`, `RNF-42.1-concluir-desabilitado.png`) | Aprovado | Os três caminhos de aceite (cadastro inicial, primeiro acesso, login) de ponta a ponta; registro com versão, hash SHA-256, data, IP e meio. |
| CSU24 | Editar Dados da Igreja | `RF44_IgrejaTests` (8, com `Atualizar_CampoOmitido_ViraNulo` e o 403 de Pastor e Superintendente)<br>`guardas.spec` (Professor em `/igreja` volta ao Painel com o aviso "Este recurso é restrito a administradores."; Admin abre sem aviso)<br>`MenuLateral.spec`, `TermoUsoSigilo.spec`<br>manual Etapa 6.2, passos 1 a 5 (`RF44-tela-igreja.png`, `RF44-termo-contato-atualizado.png`, `RNF-44.1-*`, `RNF-44.2-*`, `RNF-44.3-*`); roteiro 7.3-D10 | Aprovado | Fluxo principal e FA1 (dados inválidos) cobertos; o contato novo aparece no termo sem novo aceite (RNF 44.5); o PUT substitui os cinco campos. FA2: a recusa está provada na API (só o Admin, só a própria igreja) e, desde a corretiva de 20/10, a tela informa que o recurso é restrito a administradores. |

**Resumo:** 14 Aprovados, 10 Parciais, nenhum Não coberto. Com o roteiro D1 da 7.3 executado, o
CSU19 passa a Aprovado (15 e 9).

### 2.2 Lacunas por caso de uso

Partes dos fluxos sem teste, para a discussão dos resultados e para os trabalhos futuros. São
comportamentos implementados sem prova registrada; nenhuma é um defeito conhecido.

- **CSU01** — FA1 (dados inválidos com mensagem junto ao campo); precondição "igreja ainda sem
  cadastro".
- **CSU02** — mensagem de credenciais inválidas na tela; login de conta inativa; validade de oito
  horas do token.
- **CSU03** — Sair pelo menu → tela de login; cookie expirado na resposta e 401 na requisição
  seguinte.
- **CSU04** — conteúdo do painel de gestão (cartões de pessoas, turmas e aulas).
- **CSU05** — editar, validar e salvar os dados (`PUT /api/meus-dados`); exibição de contato e
  endereço.
- **CSU06** — o responsável de cada turma; vínculo Auxiliar para conta Professor.
- **CSU07** — asserção automatizada dos números e do histórico na tela (o E2E-M1 só abre a tela;
  os números estão no xUnit e o manual 4.2, passo 4, conferiu a tela).
- **CSU08** — `POST`/`PUT /api/materias`; FA1 (nome repetido na turma); edição.
- **CSU09** — recusa direta do `DELETE` de aula Não realizada (a tela não oferece a operação).
- **CSU10** — gravação de visitantes diferente de zero pela API ou pela tela; totais da aula na
  API após o lote; FA1 de aula Não realizada pela tela (só manual 3.2).
- **CSU11** — FA2 (Marcar como Não realizada) por teste automatizado de tela (só manual 3.2,
  passo 3); reabertura de aula Não realizada pela tela.
- **CSU12** — matrícula com sucesso por teste automatizado, com a data registrada; FA1 (inativar
  com data de saída, preservando presenças) contra a API real.
- **CSU13** — caminho negativo da validação; 403 de perfil não gestor no `POST`/`DELETE` de
  parentesco (RNF 12.1).
- **CSU14** — data e matéria de cada registro na API (o xUnit confere a turma e a situação da
  aula; a tela os exibe, conferidos no Vitest do componente); cenário Playwright do Histórico pela
  aba Alunos, desktop e celular (só Vitest com API simulada e o roteiro D9).
- **CSU15** — 403 de Professor sem atribuição nos relatórios de turma; seleção de período pela
  tela; conteúdo do CSV e da impressão; gráfico de evolução.
- **CSU16** — cadastro e edição por teste automatizado; busca e filtros; FA2 (inativar e
  reativar, RF9).
- **CSU17** — listagem com filtro, cadastro, edição e inativação de turma (RF17 a RF19); 403 de
  perfil não gestor (RNFs 17.1/18.1).
- **CSU18** — FA1 (encerrar atribuição com data de fim); `POST /api/atribuicoes` por xUnit; 403
  de perfil não gestor (RNF 21.1).
- **CSU19** — validação do formulário Novo Usuário; reativação de conta.
- **CSU20** — cenário Playwright de Redefinir acesso.
- **CSU21** — cenário Playwright da aba Resumo do dia.
- **CSU22** — senha curta ou confirmação diferente na tela; e-mail exibido; convite expirado pela
  tela (só na API).
- **CSU23** — data de vigência exibida; mensagem de aceite obrigatório na tela.
- **CSU24** — cenário Playwright da edição e do aviso do FA2 (só Vitest da guarda e o roteiro D10).

## 3. Segurança — RNF → teste → evidência

Prints em `docs/evidencias/`. "Roteiro D*n*" = evidência a capturar pelo roteiro da Etapa 7.3 em
`docs/testes.md`; a coluna Evidência passa a ✔ quando o print for salvo.

| RNF | Requisito | Testes | Evidência | Situação |
| --- | --- | --- | --- | --- |
| 2.1, 3.1, 4.1, 5.1, 6.1, 9.2, 11.1, 19.1, 20.1, 23.1, 26.1, 34.1 | Acesso só com sessão válida (JWT no cookie httpOnly) | `RF21_PerfilOperacionalTests.EndpointSemAtributo_SemSessaoRetorna401_ComSessaoRetorna200` (FallbackPolicy); `Etapa0_FumacaTests.RotaProtegida_SemCookie_Retorna401`; `RF42_TermoTests.Aceitar_SemSessao_Retorna401`; `RF2_TokenSomenteNoCookieTests` (2); `guardas.spec` | ✔ `RNF-2.1-401-sem-sessao.png` | Coberta |
| 1.2, 2.2, 13.2, 15.4, 40.3 | Senha só como hash BCrypt, custo ≥ 10 (seção 4.8) | `RF1_CadastroInicialTests` (1: 1.2/2.2) e `RF13_UsuarioTests.Criar_ComSenhaInformada_GravaSoHashBCrypt` (13.2), com formato, custo ≥ 10, ausência da senha e `Verify`; `RF39_RF40_ConviteTests.Ativar_SegundaVez_Retorna400` (15.4/40.3: formato `$2…`, ausência da senha e `Verify`; o custo é conferido pelo roteiro D4) | Roteiro D4: `RNF-2.2-senhahash-bcrypt-no-banco.png` | Coberta |
| 1.4, 2.3, 40.4, 42.4, 44.4 | Comunicação sobre HTTPS | `RF2_HttpsTests` (1: http → 307 com `Location` https); cookie `Secure` em `RF2_TokenSomenteNoCookieTests` (2) | Roteiro D7: `RNF-2.3-redirecionamento-https.png` | Coberta (nota 3.1) |
| 2.6 | Escrita só da origem autorizada | `RF2_OrigemTests` (8) | ✔ `RNF-2.6-403-origem-nao-autorizada.png`, `RNF-2.6-passa-origem-autorizada.png` | Coberta |
| 4.2, 5.2, 6.2, 34.3 | Acesso só aos próprios dados; Professor só às suas turmas | `RF34_PresencasPessoaTests` (3); `RF11_PessoaAcessoTests.ObterPessoa_UsuarioOutraPessoa_Retorna403`; `RF35_RelatorioTests.MinhaFrequencia_SoConsolidadas_EListaPendentes`; `RF21_PerfilOperacionalTests.MinhasTurmas_UsuarioComAtribuicaoAtiva_SemVinculoDeAtribuicao`; `RF42_TermoTests.MeusDados_EUsuarios_IncluemAceite` | ✔ `RNF-34.3-403-fora-da-turma.png`, `RNF-34.3-200-presencas-filtradas.png` | Coberta |
| 11.3 | Professor vê de Pessoa só o necessário, e só nas suas turmas | `RF11_PessoaAcessoTests` (5); `RF12_ParentescoDerivadoTests.ObterPessoaParaTurma_ProfessorVeADerivada`; E2E-05; `MenuLateral.spec` | ✔ `RNF-11.3-403-fora-da-turma.png`, `RNF-11.3-200-dto-reduzido.png`, `RNF-12.3-responsaveis-professor-derivado.png` | Coberta |
| 28.3 | Pessoas disponíveis em DTO reduzido para o Professor | `RF28_DisponiveisTests.PessoasDisponiveis_ComoProfessor_RetornaSoNomeESituacao`, `PessoasDisponiveis_ComoAdmin_MantemDtoCompleto` | ✔ `RNF-28.3-disponiveis-dto-reduzido.png` | Coberta |
| 21.4 | Perfil define a área; atribuição define as turmas | `RF21_PerfilOperacionalTests.ListarAulas_UsuarioComAtribuicaoAtiva_Retorna403`, `RegistrarPresenca_UsuarioComAtribuicaoAtiva_Retorna403ENadaGravado`, `ListarAulas_ProfessorComAtribuicaoAtiva_Retorna200`, `MinhasTurmas_UsuarioComAtribuicaoAtiva_SemVinculoDeAtribuicao`; E2E-05; `guardas.spec` | ✔ `RNF-21.4-403-post-presencas.png`, `RNF-21.4-minhas-turmas-usuario.png`, `RNF-21.4-usuario-com-atribuicao-minhas-turmas.png` | Coberta |
| 21.3 | Professor e Auxiliar operam igual; só o Professor é responsável da aula | `RF21_PerfilOperacionalTests.OperarTurma_ContaProfessorComAtribuicaoDeAuxiliar_Permitido` (conta Professor de função Auxiliar lista e cadastra aula); `CriarAula_ResponsavelComAtribuicaoDeAuxiliar_Retorna400` (POST); `RF45_EditarAulaTests.Editar_ProfessorSemAtribuicaoDeProfessor_Retorna400` (PUT, responsável sem atribuição) | Roteiro D6: `RNF-21.3-seletor-so-professores.png`, `RNF-21.3-400-auxiliar-responsavel.png` | Coberta |
| 21.5 | Sinalizar atribuição sem conta ou com perfil Usuário | `RF21_PerfilOperacionalTests.ListarAtribuicoes_PerfilConta_NuloSemContaEIgualAoPerfilComConta`; `atribuicoes.spec` (4); `PaginaAtribuicoesDepartamento.spec` | ✔ `RNF-21.5-tag-conta-usuario.png`, `RNF-21.5-tag-sem-conta.png`, `RNF-21.5-atribuicoes-360.png` | Coberta (Usabilidade na monografia) |
| 24.1, 25.1, 27.1–33.1, 35.1–37.1, 45.1 | Gestão ou atribuição ativa na turma | Professor sem atribuição → 403: `RF28_DisponiveisTests.PessoasDisponiveis_ProfessorSemAtribuicaoNaTurma_Retorna403` (28.1), `RF33_ConsolidarAulaTests.Consolidar_ProfessorSemAtribuicaoNaTurma_Retorna403` e `RF33_NaoRealizadaTests.MarcarNaoRealizada_ProfessorSemAtribuicaoNaTurma_Retorna403` (33.1), `RF45_EditarAulaTests.Editar_ProfessorDeOutraTurma_Retorna403` (45.1); conta Usuario com atribuição → 403 pelo perfil: `RF21_PerfilOperacionalTests.ListarAulas_UsuarioComAtribuicaoAtiva_Retorna403` (31.1), `RegistrarPresenca_UsuarioComAtribuicaoAtiva_Retorna403ENadaGravado` (32.1); caminhos positivos no E2E-04 | ✔ `RNF-21.4-403-post-presencas.png` (mesmo guarda da turma; exemplo da família) | Parcial: 24.1, 25.1, 27.1, 29.1, 30.1, 35.1, 36.1 e 37.1 sem 403 próprio; 31.1 e 32.1 sem o caso "Professor sem atribuição" |
| 7.1, 8.1, 9.1, 10.1, 12.1, 17.1, 18.1, 21.1, 22.1, 38.1, 41.1 | Operações só da gestão (Admin, Pastor, Superintendente) | `RF35_RelatorioTests.ResumoDia_ComoProfessor_Retorna403` (38.1); E2E-05 (`GET /api/pessoas` → 403, 10.1; sem Resumo do dia); `MenuLateral.spec`; `guardas.spec`; `PaginaTurma.spec` | ✔ `RNF-35.4-professor-sem-exportacao.png` | Parcial: 12.1, 17.1 e 18.1 sem teste; 7.1, 8.1, 9.1, 21.1, 22.1 e 41.1 só com a restrição testada no front |
| 7.2, 8.2, 11.2 | Dados pessoais por perfil e atribuição | `RF11_PessoaAcessoTests` (5, todos de leitura); E2E-05; `MenuLateral.spec` | ✔ `RNF-11.3-403-fora-da-turma.png`, `RNF-11.3-200-dto-reduzido.png` | Parcial: `POST`/`PUT /api/pessoas` com perfil não gestor sem teste |
| 13.1, 14.1, 15.1, 16.1, 39.1 | Gestão de usuários só pelo Administrador | `RF13_UsuarioTests.RotasDeUsuarios_ComoProfessor_Retorna403` (GET, POST, PATCH) e `ListarUsuarios_ComoPastorOuSuperintendente_Retorna403` (2), ambos com o corpo `{ mensagem }` do 403; `RF39_RF40_ConviteTests.GerarConvite_ComoProfessor_Retorna403`; E2E-05; `MenuLateral.spec`; `guardas.spec` (aviso em `/igreja`; a mesma guarda `requerAdmin` protege `/usuarios`, conferido no roteiro D10, passo 2) | Roteiro D5: `RNF-13.1-403-professor-api-usuarios.png` | Coberta |
| 14.3 | Último Admin ativo não pode ser inativado nem rebaixado | `RF14_UltimoAdminTests` (6); E2E-08 | ✔ `RNF-14.3-400-ultimo-admin.png`, `RNF-14.3-perfil-ultimo-admin.png` | Coberta |
| 15.2, 39.4 | Convite novo invalida o anterior; validade de 7 dias | `RF39_RF40_ConviteTests.GerarNovoConvite_InvalidaAnterior`, `GerarConvite_GuardaSoHash` | Roteiro D2: `RNF-39.4-convite-anterior-invalidado.png` | Coberta |
| 15.3 | Nova senha só pelo próprio usuário | `RF39_RF40_ConviteTests.ResetarSenha_RotaRemovida_Retorna404`, `GerarConvite_SenhaAtualContinuaValidaAteOUso` | ✔ `RNF-15.3-404-resetar-senha.png` | Coberta |
| 39.2 | Token do convite aleatório, exibido em claro uma única vez | `RF39_RF40_ConviteTests.ListarEObter_AposGerarConvite_NaoDevolvemToken`; E2E-03 | Roteiro D1: `RNF-39.2-token-exibido-uma-vez.png`, `RNF-39.2-listagem-sem-token.png` | Coberta |
| 39.3 | Só o hash SHA-256 do token no banco | `RF39_RF40_ConviteTests.GerarConvite_GuardaSoHash` | Roteiro D1: `RNF-39.3-hash-convite-no-banco.png` | Coberta |
| 40.1 | Convite de uso único | `RF39_RF40_ConviteTests.Ativar_SegundaVez_Retorna400`; E2E-03 | ✔ `E2E-termo-3-fluxos-verdes.png` | Coberta |
| 40.2 | Recusar convite expirado, já usado ou de conta inativa, com mensagem clara | `RF39_RF40_ConviteTests.Ativar_ConviteExpirado_Retorna400`, `Ativar_SegundaVez_Retorna400`, `Inativar_DescartaConvitePendente_EReativarNaoORecria` (inativar pela API descarta o convite; o link antigo segue recusado após reativar), `Ativar_ContaInativadaForaDaApi_Retorna400` (conta inativada por outro caminho: "Este usuário está inativo"), `Reativar_ContaInativadaForaDaApi_DescartaConviteAntigo`; E2E-03 | Roteiro D3: `RNF-40.2-convite-ja-utilizado.png`, `RNF-40.2-convite-conta-inativa.png` | Coberta |
| 33.5 | Reabertura só pelo Administrador | `RF33_ReabrirAulaTests.Reabrir_ComoProfessor_Retorna403`, `Reabrir_ComoPastorOuSuperintendente_Retorna403` (2); E2E-06 | ✔ `RNF-33.5-403-reabrir-professor.png` | Coberta |
| 35.4, 36.4, 37.4 | CSV e impressão só para a gestão | `BotoesExportacao.spec` (7); E2E-05 | ✔ `RNF-35.4-professor-sem-exportacao.png` | Coberta |
| 42.3 | Aceite imutável, ligado ao usuário e à igreja | `Etapa1_AceiteTermoTests.AceiteTermo_Gravado_PersisteTodosOsCampos`, `Usuario_ComAceiteRegistrado_NaoPodeSerExcluido`; `RF42_TermoTests.Aceitar_GravaHashDaVersaoEMeio`, `Aceitar_SegundaVezDaMesmaVersao_NaoDuplicaRegistro` | ✔ `RF42-registro-aceite-hash-meio-ip.png` | Coberta |
| 43.2 | O usuário vê o próprio aceite; o Admin, os da igreja | `RF42_TermoTests.MeusDados_EUsuarios_IncluemAceite`; `RF13_UsuarioTests.RotasDeUsuarios_ComoProfessor_Retorna403` (GET) | ✔ `RF43-meus-dados-termo.png`, `RF43-usuarios-coluna-termo.png` | Coberta |
| 44.1, 44.2 | Editar a igreja: só o Admin, só a própria | `RF44_IgrejaTests.Atualizar_ComoProfessor_Retorna403`, `Atualizar_ComoPastorOuSuperintendente_Retorna403` (2), `Atualizar_OutraIgreja_Retorna403`; `guardas.spec` (aviso na rota `/igreja`) | ✔ `RNF-44.1-403-perfil-sem-permissao.png`, `RNF-44.2-403-outra-igreja.png`; roteiro D10: `CSU24-FA2-aviso-restrito-admin.png` | Coberta |
| 1.1, 7.4, 17.3 | Isolamento dos dados entre igrejas | `RF33_ReabrirAulaTests.Reabrir_AulaDeOutraIgreja_Retorna404` (aula); `RF44_IgrejaTests.Atualizar_OutraIgreja_Retorna403` (igreja); `RF21_PerfilOperacionalTests.CriarIgreja_RotaRemovida_Retorna404` (1.1: a igreja só nasce pelo cadastro inicial). Os 3 casos de 404 uniforme do RF21 usam id inexistente e não cruzam igrejas | ✔ `RNF-44.2-403-outra-igreja.png`, `RF1-404-post-igrejas-removido.png` | Parcial: pessoa (7.4) e turma (17.3) de outra igreja sem teste próprio |
| 2.5, 42.1 (Conformidade Legal na monografia) | Termo aceito antes de qualquer operação | `RF2_GateTermoTests` (10); `RF42_TermoTests` (aceite no cadastro inicial e no primeiro acesso); E2E-01 a E2E-03 | ✔ `RNF-2.5-403-termo-pendente.png`, `RNF-2.5-redirecionamento-termo.png`, `RNF-42.1-*.png` | Coberta |

**Resumo:** 28 linhas; 24 cobertas e 4 parciais. As parciais estão na seção 5.

### 3.1 Notas de Segurança

- **HTTPS.** A API redireciona HTTP para HTTPS (307), e o cookie de sessão é `Secure`. Em
  desenvolvimento o navegador fala HTTP com o Vite (`localhost:5173`), e só o trecho Vite → API
  é HTTPS. Na implantação, o front também precisa ser servido em HTTPS. A API não usa HSTS.
- **403 por perfil (`[Authorize(Roles)]`).** Desde a corretiva de 20/10, sai com o corpo
  `{ "mensagem": "Você não tem permissão para esta operação." }` (`RespostaAutorizacaoHandler`),
  como os demais 403 da API (guarda da turma, acesso a pessoas, Resumo do dia, igreja de outro
  locatário). O 401 sem sessão não mudou. O front mostra a mensagem no toast "Sem permissão".
- **Mensagem do convite.** Token inexistente, substituído, já usado ou de conta inativada pela
  API recebem a mesma mensagem ("Convite inválido ou já utilizado…"). A validação (`GET`) responde
  404 e a ativação (`POST`), 400. Desde a corretiva de 20/10, inativar a conta descarta o convite
  pendente e reativá-la não o recria (`Inativar_DescartaConvitePendente_EReativarNaoORecria`); a
  mensagem "Este usuário está inativo…" fica para uma conta inativada por outro caminho
  (`Ativar_ContaInativadaForaDaApi_Retorna400`); reativar uma conta assim também descarta o hash
  antigo (`Reativar_ContaInativadaForaDaApi_DescartaConviteAntigo`), o que cobre as contas
  inativadas antes da corretiva.

## 4. Confiabilidade — RNF → teste → resultado

| RNF | Requisito | Testes | Evidência | Resultado |
| --- | --- | --- | --- | --- |
| 12.3 | Parentesco registrado uma vez; grau inverso derivado | `RF12_ParentescoDerivadoTests` (13); `TabelaParentescos.spec` (6) | ✔ `RNF-12.3-*.png` (5), `RF12-parentesco-origem-explicito.png` | Aprovado |
| 27.2 | Uma matrícula ativa por turma e pessoa (índice único) | `RF27_MatriculaTests` (3, SQLite com índice único filtrado) | — | Aprovado |
| 31.3 | Situação visível; pendência pelo dia civil da igreja | `situacaoAula.spec`; `TagSituacaoAula.spec` (6); `RF35_FusoHorarioTests.AulaDeHoje_As21hLocais_NaoEhPendente`, `AulaDeOntem_EhPendente`; E2E-06 | ✔ `RF31-CSU09-listagem-tres-tags.png`, `RNF-31.3-aula-hoje-nao-pendente.png` | Aprovado |
| 32.2 | Chamada só em aula Em aberto | `RF32_ChamadaTests.Registrar_EmAulaForaDeEmAberto_Retorna400` (2: Consolidada e Não realizada), `Registrar_EmAulaConsolidada_Retorna400`; `LinhaChamada.spec` | ✔ `RF33-chamada-consolidada-desktop.png` | Aprovado |
| 32.3 | Uma presença por aula e matrícula (índice único; toque duplo grava uma vez) | `RF32_ChamadaTests.Registrar_MesmaMatriculaDuasVezes_ViolaIndiceUnico`, `Registrar_MesmaMatriculaDuasVezes_AtualizaEmVezDeDuplicar`; `PaginaChamadaAula.spec`; `usarAcaoUnica.spec` (3); E2E-M1 | ✔ `RF32-chamada-toque-duplo-360.png` | Aprovado |
| 32.6, 33.3 | Registro explícito de todos os alunos ativos antes de consolidar | `RF33_ConsolidarAulaTests.Consolidar_ComChamadaIncompleta_Retorna400ComAlunosSemRegistro`, `Consolidar_ComChamadaCompleta_MudaSituacaoParaConsolidada`; `RF32_ChamadaTests.ChamadaCompleta_InformaRegistrado_PorMatricula`; `chamada.spec`; `LinhaChamada.spec`; `PaginaChamadaAula.spec`; E2E-04, E2E-M1 | ✔ `RNF-32.6-*.png` (2), `CSU11-FA1-*.png` (2), `CSU11-confirmacao-sem-registro.png` | Aprovado |
| 33.2 | Consolidar ou marcar Não realizada só se Em aberto | `RF33_ConsolidarAulaTests.Consolidar_AulaNaoRealizada_Retorna400`, `Consolidar_AulaJaConsolidada_Retorna400`; `RF33_NaoRealizadaTests.MarcarNaoRealizada_AulaConsolidada_Retorna400` | — | Aprovado |
| 33.4 | Não realizada sem presenças e fora dos cálculos | `RF33_NaoRealizadaTests.MarcarNaoRealizada_SemPresencas_MudaSituacao`; `RF32_ChamadaTests.Registrar_EmAulaForaDeEmAberto_Retorna400` (caso Não realizada); `RF35_RelatorioTests.FrequenciaTurma_IgnoraAulasNaoRealizadasEEmAberto`, `ResumoDia_SomaSoConsolidadas` | ✔ `RNF-38.4-resumo-dia-situacoes.png` | Aprovado |
| 33.6 | Reabrir preserva as presenças | `RF33_ReabrirAulaTests.Reabrir_ComoAdmin_VoltaParaEmAbertoEPreservaPresencas`; E2E-06 | ✔ `RNF-35.5-33.6-frequencia-depois-reabertura.png`, `CSU11-reabrir-preserva.png`, `CSU11-reconsolidada.png` | Aprovado |
| 33.7 | Não realizada só sem presenças | `RF33_NaoRealizadaTests.MarcarNaoRealizada_ComPresencas_Retorna400` | — | Aprovado |
| 35.5 | Frequência só com Consolidadas; pendentes à parte; período no dia civil | `RF35_RelatorioTests.FrequenciaTurma_IgnoraAulasNaoRealizadasEEmAberto`, `FrequenciaTurma_ListaPendentesDeFechamento`, `Acompanhamento_SoConsolidadas_EListaPendentes`, `MinhaFrequencia_SoConsolidadas_EListaPendentes`; `RF35_FusoHorarioTests.FrequenciaTurma_MatriculaAs23hLocaisDoDiaD_EntraComFimD`, `AulaDeHoje_As21hLocais_NaoEhPendente`, `AulaDeOntem_EhPendente`; `indicadoresPresenca.spec` (5); E2E-04 | ✔ `RNF-35.5-*.png` (2), `RF35-relatorio-matricula-noite.png` | Aprovado |
| 37.5 | Ranking só com Consolidadas | `RF35_RelatorioTests.RankingFaltas_SoConsolidadas_EListaPendentes` | — | Aprovado |
| 38.4 | Resumo do dia separa as situações | `RF35_RelatorioTests.ResumoDia_SomaSoConsolidadas`, `ResumoDia_EmAbertoDeHoje_NaoEhPendente`; `RF35_FusoHorarioTests.AulaDeHoje_As21hLocais_NaoEhPendente`, `AulaDeOntem_EhPendente` | ✔ `RNF-38.4-resumo-dia-situacoes.png` | Aprovado |
| 41.3 | Duplicidade na importação CSV | `RF41_ImportacaoTests.Importar_EmailRepetido_Ignora`, `Importar_NomeRepetidoSemEmail_IgnoraESinaliza`; `pessoasServico.spec`; E2E-07 | ✔ `RNF-41.3-importacao-conferir.png`, `RNF-41.3-importacao-conferencia.png` | Aprovado |
| 45.2 | Editar e excluir só aula Em aberto | `RF45_EditarAulaTests.Editar_AulaConsolidada_Retorna400`, `Editar_AulaNaoRealizada_Retorna400`, `Excluir_AulaConsolidada_Retorna400`; `PaginaAulasTurma.spec` | ✔ `RNF-45.2-put-consolidada-400.png`, `RNF-45.2-delete-consolidada-400.png` | Aprovado |
| 45.4 | Matéria da turma; responsável com atribuição de Professor | `RF45_EditarAulaTests.Editar_MateriaDeOutraTurma_Retorna400`, `Editar_ProfessorSemAtribuicaoDeProfessor_Retorna400` (PUT); `RF21_PerfilOperacionalTests.CriarAula_ResponsavelComAtribuicaoDeAuxiliar_Retorna400` (POST) | — | Aprovado |
| 45.5 | Excluir só aula sem presença | `RF45_EditarAulaTests.Excluir_SemPresencas_Retorna204ESomeDaListagem`, `Excluir_ComPresencas_Retorna400`; `PaginaAulasTurma.spec` (2 dos 3 casos: Excluir só sem presenças; Excluir com a confirmação comum) | ✔ `RF45-confirmar-excluir-desktop.png`, `RF45-confirmar-excluir-360.png`; roteiro D8: `RNF-45.5-sem-excluir-com-presenca.png` | Aprovado |
| Seção 5.1 — persistência e integridade do modelo | Modelo conforme o DER; valores padrão; FKs restritivas | `Etapa1_SituacaoAulaTests` (4), `Etapa1_PessoaEnxutaTests` (2), `Etapa1_AceiteTermoTests.AceiteTermo_Gravado_PersisteTodosOsCampos`, `Usuario_ComAceiteRegistrado_NaoPodeSerExcluido`; manual Etapa 1 (banco limpo) | — | Aprovado |
| Seção 5.1 — dados inválidos | Entrada inválida recusada sem gravar; PUT substitui os campos | `RF44_IgrejaTests.Atualizar_NomeVazio_Retorna400ComErroDoCampo`, `Atualizar_EmailInvalido_Retorna400ComErroDoCampo` (400 com o erro no campo), `Atualizar_CampoOmitido_ViraNulo`; `RF41_ImportacaoTests.Importar_MaisDe1000Linhas_Retorna400` (nada gravado), `Importar_ColunaForaDoModelo_Ignorada`; `RF42_TermoTests.RegistrarAdmin_SemAceite_Retorna400`, `Aceitar_VersaoDiferenteDaVigente_Retorna400` (nada gravado); `Etapa0_FumacaTests.Login_ComSenhaErrada_Retorna400` | ✔ `RNF-44.3-400-email-invalido.png` | Aprovado |
| Seção 5.1 — cópia e restauração dos dados (Plano 6.17) | `pg_dump` (formato custom) → `pg_restore` num banco novo → API apontada para ele → login, listagem e escrita conferidos | Manual: roteiro C da Etapa 7.3 (`docs/testes.md`) | Roteiro C: `Confiabilidade-restauracao-1` a `-6` | A executar |

**Resumo:** 20 linhas; 19 aprovadas e 1 a executar (cópia e restauração).

## 5. Lacunas conhecidas de Segurança

Requisitos implementados por um mecanismo comum (atributo de perfil, guarda da turma ou filtro
pela igreja do token), sem teste próprio. Nenhum comportamento divergente foi observado; falta só
a prova.

- **12.1** — parentescos restritos à gestão: nenhum `POST`/`DELETE` de parentesco com perfil não
  gestor.
- **17.1, 18.1** — cadastrar e editar turma só pela gestão: sem teste.
- **7.1, 8.1, 9.1, 21.1, 22.1, 41.1** — operações de gestão: a restrição só está testada no front
  (menu e abas); a API usa o mesmo `[Authorize(Roles = "Admin,Pastor,Superintendente")]` do
  `GET /api/pessoas`, cujo 403 ao Professor o E2E-05 confere (10.1). O 38.1 também é testado, mas
  a restrição está no próprio controller, não no atributo.
- **7.2, 8.2** — dados pessoais por perfil e atribuição: só a leitura tem teste
  (`RF11_PessoaAcessoTests`); nenhum `POST`/`PUT /api/pessoas` com perfil não gestor (mesmo
  atributo do `GET /api/pessoas`).
- **24.1, 25.1, 27.1, 29.1, 30.1, 35.1, 36.1, 37.1** — gestão ou atribuição: sem 403 próprio; o
  guarda `GarantirOperacaoNaTurmaAsync` é o mesmo das RNFs 28.1, 33.1 e 45.1, testadas. Em 31.1 e
  32.1 o 403 testado é o da conta Usuario (perfil), não o do Professor sem atribuição.
- **7.4, 17.3** — isolamento por igreja em pessoa e turma: só aula e igreja têm teste cruzando
  igrejas.

## 6. Notas para o texto do TCC

- **Funcionalidade descrita e ausente, corrigida na 7.3 (20/10).** A tabela CSU → teste
  encontrou duas divergências entre código e monografia que os testes por RF não acusavam:
  (1) CSU14, passo 1: o Professor não tinha tela para o histórico de presenças do aluno (a API
  atendia); entrou "Histórico de presenças" na aba Alunos da Página da Turma, pelo mesmo diálogo
  de Pessoas. (2) CSU24, FA2: a rota `/igreja` digitada por outro perfil voltava ao Painel sem
  aviso; entrou o aviso "Este recurso é restrito a administradores.".
- **CSU01, passo 5 (muda a monografia).** O texto diz que o cadastro inicial leva à tela de
  login; desde a Etapa 2.4 o sistema inicia a sessão e exibe o Painel (o E2E-01 afirma isso).
  Proposta para a seção 9 do Plano v1.9 (registro em `docs/decisoes/etapa-7.md`, 1.3): "5. O
  sistema direciona o usuário para a tela de login." → "5. O sistema inicia a sessão do
  Administrador e exibe o Painel."
- **CSU11, FA1.** Desde a 6.4-C, a tela nomeia na confirmação quem será gravado como ausente e
  consolida; a recusa por chamada incompleta (400 com a lista) continua na API para quem chama a
  rota diretamente.
- **Rótulos de CSU.** O E2E-04 cria a aula (CSU09) e abre a Frequência da turma (CSU15); o E2E-05
  consulta relatórios (CSU15). As colunas CSU da seção 7.3 do Plano, os comentários dos specs e o
  print `CSU15-frequencia-apos-consolidar.png` (gravado na 7.2 como `CSU12-…`) foram corrigidos na
  7.3.
- **Escala do resultado.** A seção 7.5 do Plano previa Aprovado/Reprovado; com todos os testes
  verdes, a distinção útil é entre cobertura completa e parcial do fluxo (seção 1).
