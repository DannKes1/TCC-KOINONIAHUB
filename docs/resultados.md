# Resultados — KoinoniaHub

Material para o capítulo de Resultados do TCC (registro do projeto, seções 5 e
7.5; monografia, seção 5.1). Etapa 7.3, 19 a 22/10/2026; atualizado em 31/10/2026 com as entregas
de aderência à monografia (A a D, 28 a 31/10/2026; seção 7) e com a análise estática do código
(seção 8), em 02/11/2026 com a implantação em VPS (HTTPS e HSTS na nota 3.1; implantação e
backup na seção 4), em 03/11/2026 com a Corretiva G2 (um e-mail por pessoa: RNFs 7.6, 8.4 e 13.5
na seção 4; RF14 na seção 7) e em 04/11/2026 com a Corretiva G5 (pessoa inativa sem acesso e
sessão encerrada na hora: RNFs 9.3 e 14.4 na seção 7; diálogos e páginas conferidos a 360 px
pelos E2E-M4 e E2E-M5).

A monografia avalia o sistema por quatro características da ISO/IEC 25010. Este documento reúne
a evidência de três delas:

| Característica | Evidência | Seção |
| --- | --- | --- |
| Adequação Funcional | Tabela CSU → teste → resultado (CSU01–CSU24) | 2 |
| Segurança | Tabela RNF → teste → evidência | 3 |
| Confiabilidade | Tabela RNF → teste → resultado, incluindo a cópia e restauração dos dados e a implantação com backup | 4 |

A Usabilidade é avaliada com os participantes (seção 5.1), sobre as telas a 360 px; os prints
`RNF-responsividade-*.png` estão em `docs/evidencias/`. A seção 8 reúne a análise estática do
código no SonarCloud, evidência complementar de Segurança, Confiabilidade e Manutenibilidade.

## 1. Base da avaliação

**Suítes automatizadas** (todas verdes em 04/11/2026):

| Ferramenta | Camada | Casos |
| --- | --- | --- |
| xUnit + WebApplicationFactory + SQLite em memória | API (regras, autorização, integridade) | 217 (35 classes) |
| Vitest + @vue/test-utils | Front (funções, componentes, guardas de rota) | 192 (38 arquivos) |
| Playwright | Fluxos ponta a ponta, desktop e celular 360 × 640 | 25 (11 `chromium` + 14 `mobile`) |

**Testes manuais**: registrados por etapa em `docs/testes.md`, com o passo, o resultado esperado,
a marca ✔ quando executado e o print correspondente em `docs/evidencias/`. Os roteiros da 7.3
(C, cópia e restauração; D1 a D10, evidências) estão em `docs/testes.md`, Etapa 7.3, e foram
executados pelo autor em 22/10/2026, todos ✔. Os roteiros das entregas de aderência (T1 a T10,
29 e 30/10/2026, 38 prints) estão nos blocos "Aderência à monografia — Entrega A" a "C" do
mesmo arquivo, todos ✔; são citados abaixo como "manual aderência Tn". Os da Corretiva G2 (T1 a T8,
03/11/2026, 12 prints) estão no bloco "Corretiva G2" e são citados como "manual G2 Tn"; os da
Corretiva G5 (04/11/2026, 22 prints), no bloco "Corretiva G5", citados como "manual G5".

**Como ler o resultado de cada caso de uso:**

- **Aprovado**: todos os passos do fluxo principal e todos os fluxos alternativos são exercitados
  por teste automatizado (em qualquer camada) ou por teste manual registrado com ✔, e todos os
  testes citados passam. Um dado exibido dentro de um passo exercitado sem asserção própria, ou o
  caminho negativo de uma validação de formulário, não rebaixa o resultado: fica entre as lacunas
  da seção 2.2.
- **Parcial**: todos os testes citados passam, mas algum passo do fluxo principal ou algum fluxo
  alternativo não tem teste, ou o comportamento difere do descrito no caso de uso (seção 6).
- **Não coberto**: nenhum teste exercita o caso de uso.

Nenhum teste citado falha; por isso não há a categoria "Reprovado" prevista na seção 7.5 da
registro do projeto.

**Como os testes são citados.** xUnit: `Classe.Metodo`; `Classe` (n) quando todos os n casos da
classe se aplicam (cada `InlineData` de um `[Theory]` conta um caso); para um recorte, os métodos
pelo nome ou "(k dos n casos: …)". Vitest: `arquivo.spec`, com a mesma regra para (n).
Playwright: E2E-01 a E2E-09 e E2E-M1 a E2E-M5 (seção 7.3 do registro do projeto; E2E-09,
E2E-M4 e E2E-M5 desde a Corretiva G5). Manual: "manual Etapa X,
passo N" (`docs/testes.md`).

## 2. Adequação Funcional — CSU → teste → resultado

### 2.1 Tabela

| CSU | Caso de uso | Testes que o cobrem | Resultado | Observação |
| --- | --- | --- | --- | --- |
| CSU01 | Realizar Cadastro Inicial | E2E-01 (desktop e celular)<br>`Etapa0_FumacaTests.CadastroInicial_Login_EListagem_Funcionam`<br>`RF42_TermoTests.RegistrarAdmin_ComAceite_GravaMeioCadastroInicialESemPendencia`, `RegistrarAdmin_SemAceite_Retorna400`<br>`RF1_CadastroInicialTests` (1: senha só como hash BCrypt)<br>`RF2_TokenSomenteNoCookieTests.RegistrarAdmin_NaoDevolveTokenNoCorpo_SoNoCookie`<br>`PaginaCadastroInicial.spec` (2: rótulos ligados aos campos; aviso de aceite obrigatório até marcar e Concluir enviando igreja, administrador e versão do termo)<br>manual Etapa 5.1, passo 2 (`RNF-42.1-400-cadastro-sem-aceite.png`); manual aderência T3 (`RNF-42.1-aviso-aceite-cadastro.png`) | Parcial | Fluxo principal e FA2 (aviso "O aceite do termo é obrigatório para concluir." e conclusão desabilitada sem o aceite, RNF 42.1) no E2E-01 e no Vitest. O FA1 (dados inválidos, mensagens junto aos campos) não tem teste. O passo 5 (sessão iniciada e Painel) está conforme o texto desde a revisão do .docx de 31/10/2026 (seção 7). |
| CSU02 | Realizar Login | E2E-02 (desktop e celular), E2E-03, E2E-05<br>`RF2_GateTermoTests` (10)<br>`RF42_TermoTests.Login_SemAceiteVigente_RetornaTermoPendente`<br>`RF2_TokenSomenteNoCookieTests.Login_NaoDevolveTokenNoCorpo_SoNoCookie`<br>`Etapa0_FumacaTests.Login_ComSenhaErrada_Retorna400`; `RF39_RF40_ConviteTests.GerarConvite_SenhaAtualContinuaValidaAteOUso` (senha errada de conta existente → 400)<br>`guardas.spec`, `autenticacaoStore.spec`<br>manual Etapas 2.4 e 5.1 | Aprovado | FA2 (termo pendente) no E2E-02, no filtro global da API e nas guardas do front. FA1 (credenciais inválidas) provado na API (400); a mensagem na tela não tem asserção. |
| CSU03 | Realizar Logout | `RF2_GateTermoTests.TermoELogout_SemAceite_Liberados`<br>`autenticacaoStore.spec` (sair limpa a sessão e a pendência) | Parcial | A rota responde 204 e a store limpa o estado. Nenhum teste aciona Sair no menu e confere a volta ao login e o cookie expirado. |
| CSU04 | Visualizar Painel | E2E-M1 (painel do aluno e do professor, celular); E2E-01 (título do painel de gestão)<br>`RF34_PresencasPessoaTests.Presencas_UsuarioComum_SoAsProprias`, `Presencas_ProfessorNaTurma_FiltraPelasSuasTurmas` (situação da aula em cada registro, usada pelo painel)<br>`indicadoresPresenca.spec` (5)<br>manual Etapa 4.2, passo 5 (`RF3-painel-aluno-consolidadas.png`); Etapa 6.1, passo 3 (`RF3-painel-aluno-90-dias.png`) | Parcial | Painel do usuário comum coberto, com o cálculo só de aulas Consolidadas e a janela de 90 dias. Do painel de gestão, só o título é conferido. |
| CSU05 | Gerenciar Meus Dados | E2E-01, E2E-02, E2E-03 (bloco do termo)<br>`RF42_TermoTests.MeusDados_EUsuarios_IncluemAceite`<br>`RF4_MeusDadosTests` (5: e-mail inválido e UF com três letras → 400 com o erro no campo e nada gravado; e-mail diferente do da conta → 400 sem gravar; e-mail vazio → 204 mantendo o da conta; PUT válido → 204 e o GET reflete)<br>`PaginaMeusDados.spec` (5: carga; e-mail da conta bloqueado com a dica; UF e celular inválidos barrados no campo sem chamar a API; envio com o e-mail da conta e UF em maiúsculas; Descartar restaura)<br>manual Etapa 5.2, passo 3 (`RF43-meus-dados-termo.png`); manual aderência T4 (`RNF-4.3-meus-dados-*.png`, 5; `RNF-4.3-400-uf-api.png`) | Aprovado | Passos 1 e 2 (dados e aceite exibidos) no E2E e no xUnit do aceite; passos 3 a 5 (alterar, validar, salvar) no `PUT /api/meus-dados` com 400 por campo e 204, na validação prévia da tela e no manual T4 (Entrega A da aderência, RNF 4.3). |
| CSU06 | Consultar Minhas Turmas | E2E-05, E2E-M1, E2E-04 (navegação)<br>`RF21_PerfilOperacionalTests.MinhasTurmas_UsuarioComAtribuicaoAtiva_SemVinculoDeAtribuicao`<br>manual Etapa 7.1, passos 6 e 10 (`RNF-21.4-minhas-turmas-usuario.png`); Etapa 7.2, T3 (`RNF-21.4-usuario-com-atribuicao-minhas-turmas.png`) | Aprovado | Vínculos Aluno e Professor na API; na tela, o vínculo Aluno é conferido (E2E-05) e o de Professor pela ação Abrir turma (E2E-04, E2E-M1). A conta Usuario com atribuição vê só o vínculo Aluno (RNF 21.4). O responsável de cada turma não tem asserção. |
| CSU07 | Consultar Minha Frequência | `RF35_RelatorioTests.MinhaFrequencia_SoConsolidadas_EListaPendentes`<br>E2E-M1 (aluno)<br>manual Etapas 4.2, passo 4 e 6.3, passo 4 | Aprovado | Cálculo só com Consolidadas e pendentes à parte (xUnit); a tela abre pela turma (E2E-M1) e foi conferida no manual 4.2. |
| CSU08 | Gerir Matérias | `PaginaMateriasTurma.spec` (cartões na ordem de exibição; criação pelo diálogo, API simulada)<br>E2E-M1 (listagem)<br>manual Etapa 6.4, passo 2 | Parcial | Listagem e criação pelo diálogo cobertas no front. Nenhum xUnit de `POST`/`PUT /api/materias`; o FA1 (nome repetido na turma) e a edição não têm teste. |
| CSU09 | Gerir Aulas | E2E-04 (cria a aula pela tela, desktop e celular), E2E-M1<br>`RF45_EditarAulaTests` (9: FA1, FA2, FA3)<br>`RF21_PerfilOperacionalTests.CriarAula_ResponsavelComAtribuicaoDeAuxiliar_Retorna400`<br>`PaginaAulasTurma.spec` (3), `TagSituacaoAula.spec` (6), `situacaoAula.spec`<br>manual Etapa 3.2, passo 1 (`RF31-CSU09-listagem-tres-tags.png`); Etapa 6.5, passos 1 a 10 | Aprovado | Criação pela tela no E2E-04; pela API, responsável com atribuição de Auxiliar é recusado e o Professor é aceito (RNF 21.3). FA1 a FA3 com xUnit, Vitest e manual. |
| CSU10 | Lançar Chamada | E2E-04, E2E-M1, E2E-06<br>`RF32_ChamadaTests` (6)<br>`RF21_PerfilOperacionalTests.RegistrarPresenca_UsuarioComAtribuicaoAtiva_Retorna403ENadaGravado`<br>`chamada.spec`, `LinhaChamada.spec`, `PaginaChamadaAula.spec`<br>manual Etapa 6.4, passos 3 e 7 (`RF32-chamada-desktop-1920.png`, `RF32-chamada-toque-duplo-360.png`); Etapa 6.1, passo 1 (`RNF-32.5-dica-observacao.png`) | Aprovado | Marcar, observar (com a orientação da RNF 32.5) e salvar em lote; FA1 (aula fechada) na API e na tela. Visitantes diferente de zero só no front (`chamada.spec` monta o lote com 3); nenhum teste os grava pela API ou pela tela. |
| CSU11 | Consolidar e Reabrir Aula | E2E-04, E2E-M1, E2E-06<br>`RF33_ConsolidarAulaTests` (5), `RF33_NaoRealizadaTests` (4), `RF33_ReabrirAulaTests` (7)<br>`PaginaChamadaAula.spec`, `LinhaChamada.spec`, `PaginaAulasTurma.spec` (FA2 no ⋮ da aba Aulas)<br>manual Etapa 7.2, T2 e T4 (`CSU11-confirmacao-sem-registro.png`, `CSU11-reabrir-preserva.png`, `CSU11-reconsolidada.png`); Etapas 3.1, 3.2, 4.1 e 6.4 | Aprovado | Passos 2 a 6 no E2E-04 e no E2E-M1: a confirmação nomeia quem fica ausente (os dois cancelam nesse ponto) e o E2E-M1 confirma "Salvar e consolidar" com alteração pendente; gravar os nomeados como ausentes ao confirmar está no `PaginaChamadaAula.spec` (API simulada) e no manual 6.4, passo 4. FA1 na API; FA2 na API (400 com presenças), no `PaginaAulasTurma.spec` (a opção "Marcar como Não realizada" só em aula Em aberto sem presenças, três estados; Entrega A da aderência) e nos manuais 3.2 e aderência T5 (`RNF-33.7-*.png`, 2); FA3 (só o Admin; presenças preservadas) no E2E-06 e no xUnit. |
| CSU12 | Gerir Matrículas | `RF27_MatriculaTests` (3), `RF28_DisponiveisTests` (3)<br>`RF35_RelatorioTests.FrequenciaTurma_MatriculaCriadaDepoisDaAulaConsolidada_NaoAlteraPercentualDaAula` (matrícula pelo `POST /api/departamentos/{id}/matriculas` com sucesso)<br>`RF34_PresencasPessoaTests.Presencas_ProfessorNaTurma_MatriculaInativa_Retorna200ComAsPresencasDaTurma`, `Presencas_ProfessorForaDaTurma_MatriculaInativa_Retorna403`; `RF11_PessoaAcessoTests.ObterPessoa_ProfessorNaTurma_MatriculaInativa_RetornaDtoReduzido` (matrícula inativada com data de saída: o histórico continua gravado e acessível ao Professor da turma)<br>`PaginaMatriculasTurma.spec` (cartões; "Inativar matrícula" pelo ⋮ com a confirmação que anuncia a preservação das presenças, API simulada; histórico pelo ⋮)<br>E2E-M1 (listagem)<br>manual Etapa 6.4, passo 2; Etapa 2.1 (`RNF-28.3-disponiveis-dto-reduzido.png`); manual aderência T6 (`RF29-inativar-matricula-confirmacao.png`, `-360.png`) e T8a a T8c (`CSU12-historico-matricula-inativa-professor-ok.png`, `-360.png`, `RNF-34.3-403-outra-turma.png`) | Aprovado | Fluxo principal: listagem em cartões, matrícula com sucesso pela API (xUnit) e pela tela (manual 6.4, passo 2), regras 27.2 e 28.3. FA1 (Entregas A e B da aderência): verbo "inativar" e confirmação na tela, data de saída e histórico preservado e consultável pelo Professor da turma no xUnit, e o fluxo inteiro contra a API real nos manuais T6 e T8 (o 403 do Professor encontrado no T6 foi corrigido na Entrega B; RNF 34.3). A data da matrícula não tem asserção própria (seção 2.2). |
| CSU13 | Gerir Parentescos | `RF12_ParentescoDerivadoTests` (13)<br>`TabelaParentescos.spec` (6)<br>manual Etapa 6.6, passos 1 a 9 (`RF12-parentesco-origem-explicito.png` e 5 prints `RNF-12.3-*`) | Aprovado | Vínculos explícitos e derivados (grau inverso), inclusão pela tela e FA1 (remoção pelo lado derivado recusada). O caminho negativo da validação não tem teste. |
| CSU14 | Consultar Presenças do Aluno | `RF34_PresencasPessoaTests` (3)<br>`PaginaMatriculasTurma.spec` (Histórico pelo ⋮ abre o diálogo e carrega pela pessoa do aluno); `DialogHistoricoPresencas.spec` (4: carrega ao abrir, totais só de Consolidadas, cartões no celular, tabela no desktop)<br>manual Etapa 2.1 (`RNF-34.3-403-fora-da-turma.png`, `RNF-34.3-200-presencas-filtradas.png`); Etapa 6.1, passo 4 (`RF34-historico-pessoas-consolidadas.png`); Etapa 7.3, D9 ✔ (`CSU14-historico-aluno-professor-desktop.png`, `CSU14-historico-menu-360.png`, `CSU14-historico-aluno-professor-360.png`) | Aprovado | Passo 1 pela aba Alunos da Página da Turma (corretiva de 20/10: o Professor consulta pelo mesmo diálogo de Pessoas); passo 2 na API (403 fora das turmas do Professor); passo 3 com data, turma, matéria e situação na tela e turma e situação conferidas no xUnit. |
| CSU15 | Consultar Relatórios da Turma | `RF35_RelatorioTests.FrequenciaTurma_IgnoraAulasNaoRealizadasEEmAberto`, `FrequenciaTurma_ListaPendentesDeFechamento`, `Acompanhamento_SoConsolidadas_EListaPendentes`, `RankingFaltas_SoConsolidadas_EListaPendentes`, `FrequenciaTurma_MatriculaCriadaDepoisDaAulaConsolidada_NaoAlteraPercentualDaAula`, `FrequenciaTurma_AulaConsolidadaSemRegistro_PercentualZero` (percentual por aula sobre os registros efetivos)<br>`PaginaRelatoriosEbd.spec` (2: limiares Atenção e Crítico e faltas seguidas ajustáveis na tela e enviados na consulta, RF36)<br>`RF35_FusoHorarioTests.FrequenciaTurma_MatriculaAs23hLocaisDoDiaD_EntraComFimD`, `AulaDeHoje_As21hLocais_NaoEhPendente`, `AulaDeOntem_EhPendente`<br>`BotoesExportacao.spec` (7)<br>E2E-04 (Frequência após consolidar, desktop e celular), E2E-05 (sem exportação)<br>manual Etapas 4.1 e 4.2; Etapa 7.2, T1 a T3 (`RF35-relatorio-matricula-noite.png`, `CSU15-frequencia-apos-consolidar.png`, `RNF-35.4-professor-sem-exportacao.png`); manual aderência T1 (`RF36-limiar-critico-*.png`, 3), T9 e T10 (`RF35-percentual-aula-*.png`, 9) | Aprovado | Frequência, Acompanhamento e Ranking só com Consolidadas; período no dia civil da igreja; exportação só para a gestão. Desde a Entrega C da aderência, o percentual de cada aula divide presentes pelos registros efetivos da aula, como o geral e o por aluno (CSU15); os limiares de Acompanhamento são ajustáveis na tela (RF36, Entrega A). |
| CSU16 | Gerir Pessoas | `RF11_PessoaAcessoTests` (4 dos 5 casos: os `ObterPessoa_*`), `RF41_ImportacaoTests` (6), `RF7_RF8_EmailPessoaTests` (4: e-mail único entre pessoas e contas da igreja no cadastro e na edição; pessoa com conta exibe e mantém o e-mail da conta)<br>`pessoasServico.spec` (4), `MenuLateral.spec`<br>E2E-07 (importação), E2E-05<br>manual Etapa 6.1, passo 2 (`RNF-41.3-importacao-conferir.png`); Etapa 7.2, T5 (`RNF-41.3-importacao-conferencia.png`); Etapa 1.3 | Parcial | FA1 (importação CSV com duplicidades, RNF 41.3) e a consulta cobertos; cadastro e edição provados pela API nas regras do e-mail (RNFs 7.6 e 8.4, manual G2 T3). O FA2 (inativar e reativar, RF9) não tem teste. |
| CSU17 | Gerir Turmas | `PaginaTurma.spec` (4)<br>manual Etapa 6.4, passo 1 (`RF20-pagina-turma-desktop.png`) | Parcial | Só a consulta da turma (Página da Turma, RF20). Listagem com filtro, cadastro, edição e inativação (RF17 a RF19) não têm teste. |
| CSU18 | Gerir Atribuições | `RF21_PerfilOperacionalTests.ListarAtribuicoes_PerfilConta_NuloSemContaEIgualAoPerfilComConta` e os 4 casos da RNF 21.4 (seção 3)<br>`RF21_FuncaoAtribuicaoTests` (4: `POST /api/atribuicoes` com Professor e Auxiliar → 200 e função gravada; função fora da lista → 400 em `errors.Funcao` sem gravar, em criar e em editar)<br>`PaginaAtribuicoesDepartamento.spec` (3: Dropdown de função fechado em Professor e Auxiliar nos diálogos de criar e editar; tags de conta), `atribuicoes.spec` (4), `guardas.spec`, `PaginaTurma.spec`<br>E2E-05<br>manual Etapa 7.1, passos 1 a 3 (`RNF-21.5-*.png`, 3); manual aderência T7a a T7d (`RF21-funcao-lista-fechada-*.png`, 3; `RF21-funcao-antiga-*.png`, 2; `RNF-21.2-400-funcao-invalida.png`) | Parcial | Fluxo principal coberto: inclusão pela API (xUnit, Entrega B da aderência) e pela tela (manual 7.1 e T7), função em lista fechada Professor/Auxiliar (RF21), efeito perfil × atribuição (21.4). A edição da função tem xUnit e manual (T7b). O FA1 (encerrar atribuição registrando a data de fim) continua sem teste. |
| CSU19 | Gerir Usuários | `RF13_UsuarioTests` (9: senha só como hash; 403 de Professor em GET, POST e PATCH; 403 de Pastor e Superintendente; criação grava o e-mail da conta no cadastro da pessoa e recusa e-mail de outra; PATCH do e-mail de acesso troca o login, iguala o cadastro e invalida o convite, recusa e-mail em uso e 403 de Professor)<br>`RF14_UltimoAdminTests` (6)<br>`RF39_RF40_ConviteTests.GerarConvite_GuardaSoHash`, `GerarConvite_ComoProfessor_Retorna403`, `GerarNovoConvite_InvalidaAnterior`, `GerarConvite_SenhaAtualContinuaValidaAteOUso`, `ListarEObter_AposGerarConvite_NaoDevolvemToken`<br>`RF42_TermoTests.MeusDados_EUsuarios_IncluemAceite`<br>`PaginaUsuariosLista.spec` (3: coluna Termo com versão e data e hora do aceite, pendente sem data; Novo usuário com o e-mail da pessoa preenchido; Editar usuário com o e-mail editável, enviado só quando muda), `termoUsuario.spec` (4)<br>`MenuLateral.spec`<br>E2E-08, E2E-03, E2E-05<br>manual Etapa 2.3 (`RNF-14.3-400-ultimo-admin.png`); Etapa 5.2, passo 6 (`RF43-usuarios-coluna-termo.png`); manual aderência T2 (`RNF-43.1-usuarios-data-aceite.png`, `-360.png`); Etapa 7.2, T6 (`RNF-14.3-perfil-ultimo-admin.png`); Etapa 7.3, D1 ✔ (`RNF-39.2-token-exibido-uma-vez.png`, `RNF-39.2-listagem-sem-token.png`) e D4 ✔ (`RNF-13.4-login-senha-manual-termo-pendente.png`); manual G2 T1, T2 e T6 (`RF13-conta-preenche-pessoa.png`, `RF13-pessoa-sem-email-recebe-conta.png`, `RF14-email-alterado.png`, `RF14-convite-invalidado.png`) | Aprovado | Listagem com o aceite (versão e, desde a Entrega A da aderência, data e hora, RNF 43.1), edição (perfil, situação e, desde a Corretiva G2, e-mail de acesso), último Admin e convite (FA1) cobertos; criação com senha e com convite provada pela API, e o token fora das leituras. O diálogo Novo Usuário — por convite, com o link exibido uma vez, e com senha definida pelo Admin — foi exercitado pela tela nos manuais D1 e D4 (22/10). |
| CSU20 | Resetar Senha de Usuário | `RF39_RF40_ConviteTests` (6 dos 11 casos: `GerarConvite_GuardaSoHash`, `GerarNovoConvite_InvalidaAnterior`, `GerarConvite_SenhaAtualContinuaValidaAteOUso`, `Ativar_SegundaVez_Retorna400`, `GerarConvite_ComoProfessor_Retorna403`, `ResetarSenha_RotaRemovida_Retorna404`)<br>manual Etapa 2.4, passos 1 e 2; evidência da 2.4 por curl (`RNF-15.3-404-resetar-senha.png`); Etapa 7.3, D2 ✔ (`RNF-39.4-convite-anterior-invalidado.png`) | Aprovado | Redefinir acesso gera um convite de uso único, que invalida o anterior; a senha nunca é digitada pelo Admin (RNF 15.3). |
| CSU21 | Consultar Resumo do Dia | `RF35_RelatorioTests.ResumoDia_SomaSoConsolidadas`, `ResumoDia_EmAbertoDeHoje_NaoEhPendente`, `ResumoDia_ComoProfessor_Retorna403`<br>`RF35_FusoHorarioTests.AulaDeHoje_As21hLocais_NaoEhPendente`, `AulaDeOntem_EhPendente`<br>E2E-05 (aba ausente para o Professor)<br>manual Etapa 4.2, passo 3 (`RNF-38.4-resumo-dia-situacoes.png`) | Aprovado | Totais só de Consolidadas, listas separadas de Não realizadas e pendentes, situação por turma e 403 do Professor. |
| CSU22 | Ativar Primeiro Acesso | E2E-03<br>`RF39_RF40_ConviteTests.Ativar_ConviteExpirado_Retorna400`, `Ativar_SegundaVez_Retorna400`, `Inativar_DescartaConvitePendente_EReativarNaoORecria`, `Ativar_ContaInativadaForaDaApi_Retorna400`, `GerarNovoConvite_InvalidaAnterior`<br>`RF42_TermoTests.PrimeiroAcesso_ComAceite_GravaMeioPrimeiroAcesso`, `PrimeiroAcesso_SemAceite_Retorna400ENaoConsomeConvite`, `Vigente_ComTokenDeConvite_RetornaIgrejaDoConvite`<br>`PaginaPrimeiroAcesso.spec` (3: link sem token; e-mail do convite exibido e aviso de aceite obrigatório até marcar; confirmação de senha diferente recusada e ativação com token, senha e versão do termo)<br>`TermoUsoSigilo.spec` (5 dos 9 casos, os do uso embutido: versão, igreja e contato fora do texto; caixa de aceite sem botão próprio; erro de carregamento; aviso de aceite com a prop e sem ela)<br>manual Etapa 5.1, passo 4; Etapa 5.2, passo 5 (`RF42-primeiro-acesso-termo.png`); manual aderência T3 (`RNF-42.1-aviso-aceite-primeiro-acesso.png`, `-360.png`); Etapa 7.3, D3 ✔ (`RNF-40.2-convite-ja-utilizado.png`, `RNF-40.2-convite-conta-inativa.png`, `RNF-40.2-convite-apos-reativar-recusado.png`) | Aprovado | Tela pública com o termo da igreja, senha em BCrypt, uso único; FA1 (convite inválido, expirado, já usado ou de conta inativa) e FA2 (aceite obrigatório, com o aviso na tela desde a Entrega A da aderência) cobertos. |
| CSU23 | Aceitar Termo de Uso e Sigilo | E2E-01, E2E-02, E2E-03<br>`RF42_TermoTests` (13 dos 17 casos: `Vigente_*` (3), `Login_SemAceiteVigente_RetornaTermoPendente`, `Aceitar_*` (4), `RegistrarAdmin_*` (2), `PrimeiroAcesso_*` (2), `Servico_RegistrarGuardaIpSoParaAuditoria`)<br>`Etapa1_AceiteTermoTests.AceiteTermo_Gravado_PersisteTodosOsCampos`; `RF2_GateTermoTests.AposAceite_RotasLiberadas`<br>`TermoUsoSigilo.spec` (9), `guardas.spec`, `autenticacaoStore.spec`<br>manual Etapa 5.1 (`RF42-registro-aceite-hash-meio-ip.png`); Etapa 5.2 (`RF42-tela-termo.png`, `RNF-42.1-concluir-desabilitado.png`) | Aprovado | Os três caminhos de aceite (cadastro inicial, primeiro acesso, login) de ponta a ponta; registro com versão, hash SHA-256, data, IP e meio. |
| CSU24 | Editar Dados da Igreja | `RF44_IgrejaTests` (8, com `Atualizar_CampoOmitido_ViraNulo` e o 403 de Pastor e Superintendente)<br>`guardas.spec` (Professor em `/igreja` volta ao Painel com o aviso "Este recurso é restrito a administradores."; Admin abre sem aviso)<br>`MenuLateral.spec`, `TermoUsoSigilo.spec`<br>manual Etapa 6.2, passos 1 a 5 (`RF44-tela-igreja.png`, `RF44-termo-contato-atualizado.png`, `RNF-44.1-*`, `RNF-44.2-*`, `RNF-44.3-*`); Etapa 7.3, D10 ✔ (`CSU24-FA2-aviso-restrito-admin.png`) | Aprovado | Fluxo principal e FA1 (dados inválidos) cobertos; o contato novo aparece no termo sem novo aceite (RNF 44.5); o PUT substitui os cinco campos. FA2: a recusa está provada na API (só o Admin, só a própria igreja) e, desde a corretiva de 20/10, a tela informa que o recurso é restrito a administradores. |

**Resumo:** 17 Aprovados, 7 Parciais, nenhum Não coberto. Na 7.3 (22/10/2026) eram 15 e 9; o CSU19
passou a Aprovado com os manuais D1 e D4 (22/10) e, com as entregas de aderência (seção 7), o
CSU05 (edição de Meus Dados validada e testada) e o CSU12 (FA1 com data de saída e histórico
preservado) passaram a Aprovado em 31/10/2026. Continuam Parciais: CSU01, CSU03, CSU04, CSU08,
CSU16, CSU17 e CSU18.

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
- **CSU05** — cenário Playwright da edição (xUnit do `PUT`, Vitest da tela e o manual T4 cobrem o
  fluxo); o 400 da API exibido no campo pela tela só no manual T4.
- **CSU06** — o responsável de cada turma; vínculo Auxiliar para conta Professor.
- **CSU07** — asserção automatizada dos números e do histórico na tela (o E2E-M1 só abre a tela;
  os números estão no xUnit e o manual 4.2, passo 4, conferiu a tela).
- **CSU08** — `POST`/`PUT /api/materias`; FA1 (nome repetido na turma); edição.
- **CSU09** — recusa direta do `DELETE` de aula Não realizada (a tela não oferece a operação).
- **CSU10** — gravação de visitantes diferente de zero pela API ou pela tela; totais da aula na
  API após o lote; FA1 de aula Não realizada pela tela (só manual 3.2).
- **CSU11** — reabertura de aula Não realizada pela tela; FA2 por cenário Playwright (Vitest da
  aba Aulas e manuais 3.2 e T5).
- **CSU12** — data da matrícula sem asserção (o xUnit matricula pela API e confere o relatório);
  xUnit do `DELETE` da matrícula gravando a data de saída (o xUnit inativa a matrícula no banco
  com a data e prova o histórico preservado; a rota foi exercitada nos manuais T6 e T8a).
- **CSU13** — caminho negativo da validação; 403 de perfil não gestor no `POST`/`DELETE` de
  parentesco (RNF 12.1).
- **CSU14** — data e matéria de cada registro na API (o xUnit confere a turma e a situação da
  aula; a tela os exibe, conferidos no Vitest do componente); cenário Playwright do Histórico pela
  aba Alunos, desktop e celular (Vitest com API simulada e o manual D9).
- **CSU15** — 403 de Professor sem atribuição nos relatórios de turma; seleção de período pela
  tela; conteúdo do CSV e da impressão; gráfico de evolução.
- **CSU16** — cadastro e edição por teste automatizado; busca e filtros; FA2 (inativar e
  reativar, RF9).
- **CSU17** — listagem com filtro, cadastro, edição e inativação de turma (RF17 a RF19); 403 de
  perfil não gestor (RNFs 17.1/18.1).
- **CSU18** — FA1 (encerrar atribuição com data de fim); 403 de perfil não gestor (RNF 21.1).
- **CSU19** — validação do formulário Novo Usuário; reativação de conta.
- **CSU20** — cenário Playwright de Redefinir acesso.
- **CSU21** — cenário Playwright da aba Resumo do dia.
- **CSU22** — senha curta na tela; convite expirado pela tela (só na API).
- **CSU23** — data de vigência exibida.
- **CSU24** — cenário Playwright da edição e do aviso do FA2 (Vitest da guarda e o manual D10).

## 3. Segurança — RNF → teste → evidência

Prints em `docs/evidencias/`. Os marcados "D*n*" vieram dos roteiros da Etapa 7.3
(`docs/testes.md`), executados pelo autor em 22/10/2026.

| RNF | Requisito | Testes | Evidência | Situação |
| --- | --- | --- | --- | --- |
| 2.1, 3.1, 4.1, 5.1, 6.1, 9.2, 11.1, 19.1, 20.1, 23.1, 26.1, 34.1 | Acesso só com sessão válida (JWT no cookie httpOnly) | `RF21_PerfilOperacionalTests.EndpointSemAtributo_SemSessaoRetorna401_ComSessaoRetorna200` (FallbackPolicy); `Etapa0_FumacaTests.RotaProtegida_SemCookie_Retorna401`; `RF42_TermoTests.Aceitar_SemSessao_Retorna401`; `RF2_TokenSomenteNoCookieTests` (2); `guardas.spec` | ✔ `RNF-2.1-401-sem-sessao.png` | Coberta |
| 1.2, 2.2, 13.2, 15.4, 40.3 | Senha só como hash BCrypt, custo ≥ 10 (seção 4.8) | `RF1_CadastroInicialTests` (1: 1.2/2.2) e `RF13_UsuarioTests.Criar_ComSenhaInformada_GravaSoHashBCrypt` (13.2), com formato, custo ≥ 10, ausência da senha e `Verify`; `RF39_RF40_ConviteTests.Ativar_SegundaVez_Retorna400` (15.4/40.3: formato `$2…`, ausência da senha e `Verify`; o custo é conferido no manual D4) | ✔ D4: `RNF-2.2-senhahash-bcrypt-no-banco.png` (consulta A: prefixo, custo 11 e tamanho 60), `RNF-2.2-senhahash-bcrypt-contagem.png` (consulta B: zero fora do formato, zero com a senha), `RNF-13.4-login-senha-manual-termo-pendente.png` (a conta criada com senha entra e cai no termo) | Coberta |
| 1.4, 2.3, 40.4, 42.4, 44.4 | Comunicação sobre HTTPS | `RF2_HttpsTests` (1: http → 307 com `Location` https); cookie `Secure` em `RF2_TokenSomenteNoCookieTests` (2); atrás do proxy, `RF2_ProxyReversoTests` (2: `X-Forwarded-Proto` mantém o https sem laço; `X-Forwarded-For` é o IP do aceite do termo, RNF 42.8) | ✔ D7: `RNF-2.3-redirecionamento-https.png` (307 com `Location` https), `RNF-2.3-cookie-secure-httponly.png` (cookie `kh_token` HttpOnly e Secure); ✔ VPS (02/11/2026): `RNF-2.3-saude-hsts-vps.png` (HTTP/2 200 com HSTS), `RNF-2.3-https-celular.png` (cadeado no celular), `RNF-42.8-ip-aceite-vps.png` (IP do navegador no aceite) | Coberta (nota 3.1) |
| 2.6 | Escrita só da origem autorizada | `RF2_OrigemTests` (8) | ✔ `RNF-2.6-403-origem-nao-autorizada.png`, `RNF-2.6-passa-origem-autorizada.png` | Coberta |
| 4.2, 5.2, 6.2, 34.3 | Acesso só aos próprios dados; Professor só às suas turmas | `RF34_PresencasPessoaTests` (5, com a matrícula inativada: 200 na turma do Professor, 403 fora dela — RNF 34.3 com a frase "inclusive os de matrícula já inativada", 31/10/2026); `RF11_PessoaAcessoTests.ObterPessoa_UsuarioOutraPessoa_Retorna403`; `RF35_RelatorioTests.MinhaFrequencia_SoConsolidadas_EListaPendentes`; `RF21_PerfilOperacionalTests.MinhasTurmas_UsuarioComAtribuicaoAtiva_SemVinculoDeAtribuicao`; `RF42_TermoTests.MeusDados_EUsuarios_IncluemAceite` | ✔ `RNF-34.3-403-fora-da-turma.png`, `RNF-34.3-200-presencas-filtradas.png` | Coberta |
| 11.3 | Professor vê de Pessoa só o necessário, e só nas suas turmas | `RF11_PessoaAcessoTests` (6, com o DTO reduzido do ex-aluno da turma); `RF12_ParentescoDerivadoTests.ObterPessoaParaTurma_ProfessorVeADerivada`; E2E-05; `MenuLateral.spec` | ✔ `RNF-11.3-403-fora-da-turma.png`, `RNF-11.3-200-dto-reduzido.png`, `RNF-12.3-responsaveis-professor-derivado.png` | Coberta |
| 28.3 | Pessoas disponíveis em DTO reduzido para o Professor | `RF28_DisponiveisTests.PessoasDisponiveis_ComoProfessor_RetornaSoNomeESituacao`, `PessoasDisponiveis_ComoAdmin_MantemDtoCompleto` | ✔ `RNF-28.3-disponiveis-dto-reduzido.png` | Coberta |
| 21.4 | Perfil define a área; atribuição define as turmas | `RF21_PerfilOperacionalTests.ListarAulas_UsuarioComAtribuicaoAtiva_Retorna403`, `RegistrarPresenca_UsuarioComAtribuicaoAtiva_Retorna403ENadaGravado`, `ListarAulas_ProfessorComAtribuicaoAtiva_Retorna200`, `MinhasTurmas_UsuarioComAtribuicaoAtiva_SemVinculoDeAtribuicao`; E2E-05; `guardas.spec` | ✔ `RNF-21.4-403-post-presencas.png`, `RNF-21.4-minhas-turmas-usuario.png`, `RNF-21.4-usuario-com-atribuicao-minhas-turmas.png` | Coberta |
| 21.3 | Professor e Auxiliar operam igual; só o Professor é responsável da aula | `RF21_PerfilOperacionalTests.OperarTurma_ContaProfessorComAtribuicaoDeAuxiliar_Permitido` (conta Professor de função Auxiliar lista e cadastra aula); `CriarAula_ResponsavelComAtribuicaoDeAuxiliar_Retorna400` (POST); `RF45_EditarAulaTests.Editar_ProfessorSemAtribuicaoDeProfessor_Retorna400` (PUT, responsável sem atribuição) | ✔ D6: `RNF-21.3-seletor-so-professores.png`, `RNF-21.3-400-auxiliar-responsavel.png` | Coberta |
| 21.5 | Sinalizar atribuição sem conta ou com perfil Usuário | `RF21_PerfilOperacionalTests.ListarAtribuicoes_PerfilConta_NuloSemContaEIgualAoPerfilComConta`; `atribuicoes.spec` (4); `PaginaAtribuicoesDepartamento.spec` | ✔ `RNF-21.5-tag-conta-usuario.png`, `RNF-21.5-tag-sem-conta.png`, `RNF-21.5-atribuicoes-360.png` | Coberta (Usabilidade na monografia) |
| 24.1, 25.1, 27.1–33.1, 35.1–37.1, 45.1 | Gestão ou atribuição ativa na turma | Professor sem atribuição → 403: `RF28_DisponiveisTests.PessoasDisponiveis_ProfessorSemAtribuicaoNaTurma_Retorna403` (28.1), `RF33_ConsolidarAulaTests.Consolidar_ProfessorSemAtribuicaoNaTurma_Retorna403` e `RF33_NaoRealizadaTests.MarcarNaoRealizada_ProfessorSemAtribuicaoNaTurma_Retorna403` (33.1), `RF45_EditarAulaTests.Editar_ProfessorDeOutraTurma_Retorna403` (45.1); conta Usuario com atribuição → 403 pelo perfil: `RF21_PerfilOperacionalTests.ListarAulas_UsuarioComAtribuicaoAtiva_Retorna403` (31.1), `RegistrarPresenca_UsuarioComAtribuicaoAtiva_Retorna403ENadaGravado` (32.1); caminhos positivos no E2E-04 | ✔ `RNF-21.4-403-post-presencas.png` (mesmo guarda da turma; exemplo da família) | Parcial: 24.1, 25.1, 27.1, 29.1, 30.1, 35.1, 36.1 e 37.1 sem 403 próprio; 31.1 e 32.1 sem o caso "Professor sem atribuição" |
| 7.1, 8.1, 9.1, 10.1, 12.1, 17.1, 18.1, 21.1, 22.1, 38.1, 41.1 | Operações só da gestão (Admin, Pastor, Superintendente) | `RF35_RelatorioTests.ResumoDia_ComoProfessor_Retorna403` (38.1); E2E-05 (`GET /api/pessoas` → 403, 10.1; sem Resumo do dia); `MenuLateral.spec`; `guardas.spec`; `PaginaTurma.spec` | ✔ `RNF-35.4-professor-sem-exportacao.png` | Parcial: 12.1, 17.1 e 18.1 sem teste; 7.1, 8.1, 9.1, 21.1, 22.1 e 41.1 só com a restrição testada no front |
| 7.2, 8.2, 11.2 | Dados pessoais por perfil e atribuição | `RF11_PessoaAcessoTests` (5, todos de leitura); E2E-05; `MenuLateral.spec` | ✔ `RNF-11.3-403-fora-da-turma.png`, `RNF-11.3-200-dto-reduzido.png` | Parcial: `POST`/`PUT /api/pessoas` com perfil não gestor sem teste |
| 13.1, 14.1, 15.1, 16.1, 39.1 | Gestão de usuários só pelo Administrador | `RF13_UsuarioTests.RotasDeUsuarios_ComoProfessor_Retorna403` (GET, POST, PATCH) e `ListarUsuarios_ComoPastorOuSuperintendente_Retorna403` (2), ambos com o corpo `{ mensagem }` do 403; `RF39_RF40_ConviteTests.GerarConvite_ComoProfessor_Retorna403`; E2E-05; `MenuLateral.spec`; `guardas.spec` (aviso em `/igreja`; a mesma guarda `requerAdmin` protege `/usuarios`, conferido no manual D10, passo 2) | ✔ D5: `RNF-13.1-403-professor-api-usuarios.png` (Console: 403 com `{ mensagem }`), `RNF-13.1-403-professor-api-usuarios-network.png` (aba Network) | Coberta |
| 14.3 | Último Admin ativo não pode ser inativado nem rebaixado | `RF14_UltimoAdminTests` (6); E2E-08 | ✔ `RNF-14.3-400-ultimo-admin.png`, `RNF-14.3-perfil-ultimo-admin.png` | Coberta |
| 15.2, 39.4 | Convite novo invalida o anterior; validade de 7 dias | `RF39_RF40_ConviteTests.GerarNovoConvite_InvalidaAnterior`, `GerarConvite_GuardaSoHash` | ✔ D2: `RNF-39.4-convite-anterior-invalidado.png` | Coberta |
| 15.3 | Nova senha só pelo próprio usuário | `RF39_RF40_ConviteTests.ResetarSenha_RotaRemovida_Retorna404`, `GerarConvite_SenhaAtualContinuaValidaAteOUso` | ✔ `RNF-15.3-404-resetar-senha.png` | Coberta |
| 39.2 | Token do convite aleatório, exibido em claro uma única vez | `RF39_RF40_ConviteTests.ListarEObter_AposGerarConvite_NaoDevolvemToken`; E2E-03 | ✔ D1: `RNF-39.2-token-exibido-uma-vez.png`, `RNF-39.2-listagem-sem-token.png` | Coberta |
| 39.3 | Só o hash SHA-256 do token no banco | `RF39_RF40_ConviteTests.GerarConvite_GuardaSoHash` | ✔ D1: `RNF-39.3-hash-convite-no-banco.png` | Coberta |
| 40.1 | Convite de uso único | `RF39_RF40_ConviteTests.Ativar_SegundaVez_Retorna400`; E2E-03 | ✔ `E2E-termo-3-fluxos-verdes.png` | Coberta |
| 40.2 | Recusar convite expirado, já usado ou de conta inativa, com mensagem clara | `RF39_RF40_ConviteTests.Ativar_ConviteExpirado_Retorna400`, `Ativar_SegundaVez_Retorna400`, `Inativar_DescartaConvitePendente_EReativarNaoORecria` (inativar pela API descarta o convite; o link antigo segue recusado após reativar), `Ativar_ContaInativadaForaDaApi_Retorna400` (conta inativada por outro caminho: "Este usuário está inativo"), `Reativar_ContaInativadaForaDaApi_DescartaConviteAntigo`; E2E-03 | ✔ D3: `RNF-40.2-convite-ja-utilizado.png`, `RNF-40.2-convite-conta-inativa.png` (link recusado após inativar), `RNF-40.2-conta-inativa-sem-convite-pendente.png` (tag "Convite pendente" some), `RNF-40.2-conta-reativada-sem-convite.png`, `RNF-40.2-convite-apos-reativar-recusado.png` (reativar não recria) | Coberta |
| 33.5 | Reabertura só pelo Administrador | `RF33_ReabrirAulaTests.Reabrir_ComoProfessor_Retorna403`, `Reabrir_ComoPastorOuSuperintendente_Retorna403` (2); E2E-06 | ✔ `RNF-33.5-403-reabrir-professor.png` | Coberta |
| 35.4, 36.4, 37.4 | CSV e impressão só para a gestão | `BotoesExportacao.spec` (7); E2E-05 | ✔ `RNF-35.4-professor-sem-exportacao.png` | Coberta |
| 42.3 | Aceite imutável, ligado ao usuário e à igreja | `Etapa1_AceiteTermoTests.AceiteTermo_Gravado_PersisteTodosOsCampos`, `Usuario_ComAceiteRegistrado_NaoPodeSerExcluido`; `RF42_TermoTests.Aceitar_GravaHashDaVersaoEMeio`, `Aceitar_SegundaVezDaMesmaVersao_NaoDuplicaRegistro` | ✔ `RF42-registro-aceite-hash-meio-ip.png` | Coberta |
| 43.2 | O usuário vê o próprio aceite; o Admin, os da igreja | `RF42_TermoTests.MeusDados_EUsuarios_IncluemAceite`; `RF13_UsuarioTests.RotasDeUsuarios_ComoProfessor_Retorna403` (GET) | ✔ `RF43-meus-dados-termo.png`, `RF43-usuarios-coluna-termo.png` | Coberta |
| 44.1, 44.2 | Editar a igreja: só o Admin, só a própria | `RF44_IgrejaTests.Atualizar_ComoProfessor_Retorna403`, `Atualizar_ComoPastorOuSuperintendente_Retorna403` (2), `Atualizar_OutraIgreja_Retorna403`; `guardas.spec` (aviso na rota `/igreja`) | ✔ `RNF-44.1-403-perfil-sem-permissao.png`, `RNF-44.2-403-outra-igreja.png`; ✔ D10: `CSU24-FA2-aviso-restrito-admin.png` | Coberta |
| 1.1, 7.4, 17.3 | Isolamento dos dados entre igrejas | `RF33_ReabrirAulaTests.Reabrir_AulaDeOutraIgreja_Retorna404` (aula); `RF44_IgrejaTests.Atualizar_OutraIgreja_Retorna403` (igreja); `RF21_PerfilOperacionalTests.CriarIgreja_RotaRemovida_Retorna404` (1.1: a igreja só nasce pelo cadastro inicial). Os 3 casos de 404 uniforme do RF21 usam id inexistente e não cruzam igrejas | ✔ `RNF-44.2-403-outra-igreja.png`, `RF1-404-post-igrejas-removido.png` | Parcial: pessoa (7.4) e turma (17.3) de outra igreja sem teste próprio |
| 2.5, 42.1 (Conformidade Legal na monografia) | Termo aceito antes de qualquer operação | `RF2_GateTermoTests` (10); `RF42_TermoTests` (aceite no cadastro inicial e no primeiro acesso); E2E-01 a E2E-03 | ✔ `RNF-2.5-403-termo-pendente.png`, `RNF-2.5-redirecionamento-termo.png`, `RNF-42.1-*.png` | Coberta |

**Resumo:** 28 linhas; 24 cobertas e 4 parciais. As parciais estão na seção 5.

### 3.1 Notas de Segurança

- **HTTPS.** A API redireciona HTTP para HTTPS (307), e o cookie de sessão é `Secure`. Em
  desenvolvimento o navegador fala HTTP com o Vite (`localhost:5173`), e só o trecho Vite → API
  é HTTPS. Na implantação (02/11/2026: VPS Ubuntu 24.04 com Docker Compose e Caddy, domínio
  `koinoniahub.tech`), front e API são servidos em HTTPS na mesma origem pelo Caddy, com
  certificado automático e HSTS (`strict-transport-security: max-age=31536000; includeSubDomains`,
  conferido com `curl -I`: HTTP/2 200); `www` redireciona para o domínio principal e a API só é
  alcançável pelo proxy. A API aplica os cabeçalhos `X-Forwarded-Proto` e `X-Forwarded-For` do
  Caddy (`UseForwardedHeaders` antes do redirecionamento; `RF2_ProxyReversoTests`): o
  redirecionamento não entra em laço, o cookie `Secure` vale atrás do proxy e o IP gravado no
  aceite do termo é o do navegador, não o do contêiner (RNF 42.8, conferido no primeiro acesso de
  uma conta na VPS). Acesso pelo celular em rede móvel com o cadeado do navegador. Prints
  `RNF-2.3-saude-hsts-vps.png`, `RNF-2.3-https-celular.png`, `RNF-42.8-ip-aceite-vps.png`.
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
| 7.6, 8.4 | E-mail, quando informado, único entre as pessoas e as contas de acesso da igreja (cadastro, edição e importação) | `RF7_RF8_EmailPessoaTests.Criar_EmailDeOutraPessoaOuDaContaDeOutra_Retorna400ENaoCria`, `Atualizar_PessoaSemConta_EmailDaContaDeOutra_Retorna400`; `RF41_ImportacaoTests.Importar_EmailDeContaDeOutraPessoa_Ignora`, `Importar_EmailRepetido_Ignora`; E2E-07 | ✔ `RNF-13.5-email-em-uso-pessoas.png`, `RF41-email-de-conta-ignorado.png` | Aprovado |
| 13.5 | E-mail da conta é o da pessoa vinculada: a criação da conta grava o mesmo e-mail no cadastro; pessoa com conta mantém o e-mail da conta (só o Admin o altera, RF14); nenhum e-mail em duas pessoas da igreja | `RF13_UsuarioTests.Criar_PessoaSemEmail_CadastroRecebeOEmailDaConta`, `Criar_PessoaComOutroEmail_CadastroPassaAoEmailDaConta`, `Criar_EmailDeOutraPessoa_Retorna400ENaoCria`, `AtualizarEmail_AlteraOLoginSincronizaOCadastroEInvalidaOConvite`, `AtualizarEmail_EmUso_Retorna400ENadaMuda`; `RF7_RF8_EmailPessoaTests.Atualizar_PessoaComConta_EmailDiferenteDaConta_Retorna400`, `PessoaComConta_ExibeOEmailDaConta_EGravarComEleOuVazio_Retorna204EIguala`; `RF4_MeusDadosTests.Atualizar_EmailDiferenteDaConta_Retorna400ENaoGrava`, `Atualizar_EmailVazio_MantemOEmailDaConta_Retorna204`; `PaginaUsuariosLista.spec` (2 dos 3), `PaginaMeusDados.spec` | ✔ `RF13-conta-preenche-pessoa.png`, `RF13-pessoa-sem-email-recebe-conta.png`, `RNF-13.5-email-em-uso-usuarios.png`, `RF4-email-bloqueado.png`, `RF14-email-alterado.png`, `RF14-email-em-uso-editar.png`, `RF14-convite-invalidado.png` | Aprovado |
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
| 35.5 | Frequência só com Consolidadas; pendentes à parte; período no dia civil; percentual por aula sobre os registros efetivos | `RF35_RelatorioTests.FrequenciaTurma_IgnoraAulasNaoRealizadasEEmAberto`, `FrequenciaTurma_ListaPendentesDeFechamento`, `FrequenciaTurma_MatriculaCriadaDepoisDaAulaConsolidada_NaoAlteraPercentualDaAula`, `FrequenciaTurma_AulaConsolidadaSemRegistro_PercentualZero`, `Acompanhamento_SoConsolidadas_EListaPendentes`, `MinhaFrequencia_SoConsolidadas_EListaPendentes`; `RF35_FusoHorarioTests.FrequenciaTurma_MatriculaAs23hLocaisDoDiaD_EntraComFimD`, `AulaDeHoje_As21hLocais_NaoEhPendente`, `AulaDeOntem_EhPendente`; `indicadoresPresenca.spec` (5); E2E-04 | ✔ `RNF-35.5-*.png` (2), `RF35-relatorio-matricula-noite.png` | Aprovado |
| 37.5 | Ranking só com Consolidadas | `RF35_RelatorioTests.RankingFaltas_SoConsolidadas_EListaPendentes` | — | Aprovado |
| 38.4 | Resumo do dia separa as situações | `RF35_RelatorioTests.ResumoDia_SomaSoConsolidadas`, `ResumoDia_EmAbertoDeHoje_NaoEhPendente`; `RF35_FusoHorarioTests.AulaDeHoje_As21hLocais_NaoEhPendente`, `AulaDeOntem_EhPendente` | ✔ `RNF-38.4-resumo-dia-situacoes.png` | Aprovado |
| 41.3 | Duplicidade na importação CSV | `RF41_ImportacaoTests.Importar_EmailRepetido_Ignora`, `Importar_NomeRepetidoSemEmail_IgnoraESinaliza`; `pessoasServico.spec`; E2E-07 | ✔ `RNF-41.3-importacao-conferir.png`, `RNF-41.3-importacao-conferencia.png` | Aprovado |
| 45.2 | Editar e excluir só aula Em aberto | `RF45_EditarAulaTests.Editar_AulaConsolidada_Retorna400`, `Editar_AulaNaoRealizada_Retorna400`, `Excluir_AulaConsolidada_Retorna400`; `PaginaAulasTurma.spec` | ✔ `RNF-45.2-put-consolidada-400.png`, `RNF-45.2-delete-consolidada-400.png` | Aprovado |
| 45.4 | Matéria da turma; responsável com atribuição de Professor | `RF45_EditarAulaTests.Editar_MateriaDeOutraTurma_Retorna400`, `Editar_ProfessorSemAtribuicaoDeProfessor_Retorna400` (PUT); `RF21_PerfilOperacionalTests.CriarAula_ResponsavelComAtribuicaoDeAuxiliar_Retorna400` (POST) | — | Aprovado |
| 45.5 | Excluir só aula sem presença | `RF45_EditarAulaTests.Excluir_SemPresencas_Retorna204ESomeDaListagem`, `Excluir_ComPresencas_Retorna400`; `PaginaAulasTurma.spec` (2 dos 3 casos: Excluir só sem presenças; Excluir com a confirmação comum) | ✔ `RF45-confirmar-excluir-desktop.png`, `RF45-confirmar-excluir-360.png`; ✔ D8: `RNF-45.5-sem-excluir-com-presenca.png` | Aprovado |
| Seção 5.1 — persistência e integridade do modelo | Modelo conforme o DER; valores padrão; FKs restritivas | `Etapa1_SituacaoAulaTests` (4), `Etapa1_PessoaEnxutaTests` (2), `Etapa1_AceiteTermoTests.AceiteTermo_Gravado_PersisteTodosOsCampos`, `Usuario_ComAceiteRegistrado_NaoPodeSerExcluido`; manual Etapa 1 (banco limpo) | — | Aprovado |
| Seção 5.1 — dados inválidos | Entrada inválida recusada sem gravar; PUT substitui os campos | `RF44_IgrejaTests.Atualizar_NomeVazio_Retorna400ComErroDoCampo`, `Atualizar_EmailInvalido_Retorna400ComErroDoCampo` (400 com o erro no campo), `Atualizar_CampoOmitido_ViraNulo`; `RF4_MeusDadosTests` (4: e-mail e UF inválidos → 400 por campo sem gravar; e-mail vazio → nulo; RNF 4.3); `RF21_FuncaoAtribuicaoTests` (4: função fora da lista → 400 sem gravar); `RF41_ImportacaoTests.Importar_MaisDe1000Linhas_Retorna400` (nada gravado), `Importar_ColunaForaDoModelo_Ignorada`; `RF42_TermoTests.RegistrarAdmin_SemAceite_Retorna400`, `Aceitar_VersaoDiferenteDaVigente_Retorna400` (nada gravado); `Etapa0_FumacaTests.Login_ComSenhaErrada_Retorna400` | ✔ `RNF-44.3-400-email-invalido.png` | Aprovado |
| Seção 5.1 — cópia e restauração dos dados (registro do projeto, 6.17) | `pg_dump` (formato custom) → `pg_restore` num banco novo → API apontada para ele → login, listagem e escrita conferidos | Manual: roteiro C da Etapa 7.3 (`docs/testes.md`), executado em 22/10/2026 ✔ | ✔ `Confiabilidade-restauracao-1-contagens-origem.png`, `-2-pg-dump-custom.png`, `-3-pg-restore.png`, `-4-contagens-restaurado.png`, `-5-api-no-banco-restaurado.png` (API apontada para o banco restaurado), `-5-login-listagem.png`, `-6-escrita-no-restaurado.png` (a pessoa nova só no banco restaurado) | Aprovado |

| Implantação e backup | Sistema em produção com HTTPS; cópia diária automática do banco com retenção de 7 dias | `SaudeTests` (1: `GET /api/saude` 200 sem sessão, healthcheck do contêiner); manual na VPS (02/11/2026): `db` e `api` healthy, `/api/saude` = `{"situacao":"ok"}`, `deploy/backup.sh` gerou `koinoniahub-20261102-2227.dump` (60 KB), cron das 03:00 agendado, base de exemplo restaurada com `pg_restore` a partir do dump local (Entrega F em `docs/testes.md`) | ✔ `RNF-2.3-saude-hsts-vps.png`, `Confiabilidade-backup-cron-vps.png` | Aprovado |

**Resumo:** 23 linhas, 23 aprovadas.

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
- **CSU11, FA1.** Desde a 6.4-C, a tela nomeia na confirmação quem será gravado como ausente e
  consolida; a recusa por chamada incompleta (400 com a lista) continua na API para quem chama a
  rota diretamente.
- **Rótulos de CSU.** O E2E-04 cria a aula (CSU09) e abre a Frequência da turma (CSU15); o E2E-05
  consulta relatórios (CSU15). As colunas CSU dos cenários E2E no registro do projeto (7.3), os
  comentários dos specs e o print `CSU15-frequencia-apos-consolidar.png` (gravado na 7.2 como `CSU12-…`) foram corrigidos na
  7.3.
- **Escala do resultado.** A estratégia de testes do registro do projeto (7.5)
  previa Aprovado/Reprovado; com todos os testes verdes, a distinção útil é entre cobertura completa e parcial do fluxo (seção 1).

## 7. Aderência especificação × sistema

Em 28/10/2026, antes da conversão do texto para TCC 2, o .docx foi confrontado com o sistema
implementado: RF1 a RF45 com as RNFs numeradas, CSU01 a CSU24 passo a passo (fluxo principal e
alternativos), o DER, a seção 4.8 e as versões das ferramentas. Nenhuma regra de negócio central
diverge (situações da aula, perfil × atribuição, termo, isolamento por igreja, LGPD). As
divergências encontradas foram classificadas em **texto** (a monografia descrevia algo diferente
do que o sistema faz e o sistema está certo: o autor corrige o .docx) ou **código** (o texto pede
algo razoável que o sistema não fazia: o código passa a seguir o texto, com teste). Os ajustes de
código saíram em três entregas (A, B e C, 28 a 30/10/2026), cada uma com suíte completa antes e
depois e testes manuais do autor (38 prints); a Entrega D (31/10) só acrescentou specs para a
cobertura do código novo no SonarCloud. As correções de texto foram aplicadas no .docx em
31/10/2026. A conferência RF por RF está no registro do projeto; os testes manuais estão em
`docs/testes.md`, blocos "Aderência à monografia — Entrega A" a "C".

| Divergência | Classificação | Tratamento (entrega e teste) |
| --- | --- | --- |
| CSU01, passo 5: "direciona o usuário para a tela de login"; o sistema inicia a sessão e exibe o Painel | Texto | .docx em 31/10/2026 ("O sistema inicia a sessão do Administrador e exibe o Painel"); E2E-01 afirma o comportamento |
| CSU09, passo 4: tema obrigatório; tema e conteúdo são opcionais (RF45) | Texto | .docx em 31/10 ("e, opcionalmente, o tema e o conteúdo"); `RF45_EditarAulaTests` |
| RF17: "nome, tipo e descrição"; a turma não tem descrição na tela nem na resposta da API | Texto | .docx em 31/10 ("nome e tipo") |
| CSU08, passo 4: "nome, descrição e ordem"; a matéria não tem descrição | Texto | .docx em 31/10 ("nome e ordem de exibição") |
| RF4: "atualizar os seus dados pessoais e de contato"; só contato e endereço são editáveis pelo próprio usuário | Texto | .docx em 31/10 ("consultar os seus dados e atualizar os dados de contato e endereço") |
| RF14: "alterar os dados de acesso e o perfil"; só perfil e situação mudam, e-mail fixo e senha só por convite | Texto | .docx em 31/10; `RF14_UltimoAdminTests`, `RF39_RF40_ConviteTests` |
| RF7 / CSU16, passo 4: "o endereço"; cinco campos (logradouro, bairro, cidade, UF e CEP) | Texto | .docx em 31/10; `RF41_ImportacaoTests` (modelo CSV com os cinco campos) |
| RF11: "vínculos e situação"; Pessoas exibe só os vínculos de parentesco | Texto | .docx em 31/10 ("situação e vínculos de parentesco") |
| CSU07, passo 1: "acessa a opção Minha Frequência"; não há item de menu, o acesso é por turma | Texto | .docx em 31/10 (acesso a partir de Minhas Turmas ou do Painel); E2E-M1 |
| CSU04, passo 3: "últimos três meses"; janela fixa de 90 dias | Texto | .docx em 31/10 ("últimos 90 dias"); `indicadoresPresenca.spec` |
| RNFs 10.2, 16.2, 19.2, 23.2 e 31.2: "paginada"; paginação em tela, a API devolve a lista da igreja | Texto | .docx em 31/10 ("paginada em tela") |
| RNF 34.3: Professor consulta alunos das turmas com atribuição ativa; o texto não dizia se a matrícula inativa continua consultável | Texto (decorrente da Entrega B) | .docx em 31/10 ("inclusive os de matrícula já inativada"); `RF34_PresencasPessoaTests` (5) |
| Tela de Presenças da Aula e `GET /api/aulas/{aulaId}/presencas` sem RF ou CSU que os citasse | Texto (acréscimo) | CSU09, Fluxo Alternativo 4 "Consultar presenças registradas", .docx em 31/10; a tela de Presenças está no manual 6.4-B (`RF34-presencas-desktop.png`) e no E2E-M1; a rota não tem xUnit próprio (seção 2.2, CSU10) |
| Campo Observação da matrícula (`AlunoDepartamento.Observacao`) ausente do DER | Texto (acréscimo) | Coluna Observação no DER, .docx em 31/10 |
| RF36: "limiares ajustáveis na tela"; o limiar crítico ficava fixo em 50 % | Código | Entrega A: campo "Crítico: freq. ≤ (%)" em Relatórios › Acompanhamento; `PaginaRelatoriosEbd.spec` (2); manual T1 |
| RNF 43.1: data e hora do aceite na lista de Usuários; só a versão aparecia, data em tooltip | Código | Entrega A: tag e "em dd/mm/aaaa hh:mm" na coluna Termo; `termoUsuario.spec` (4), `PaginaUsuariosLista.spec` (1); manual T2 |
| RNF 42.1 / CSU01 FA2 / CSU22 FA2: "informa que o aceite é obrigatório"; o botão só ficava desabilitado | Código | Entrega A: aviso sob a caixa no Cadastro Inicial e no Primeiro Acesso; `TermoUsoSigilo.spec` (+2), `PaginaCadastroInicial.spec` (+1), `PaginaPrimeiroAcesso.spec` (3), E2E-01; manual T3 |
| RNF 4.3: validação de Meus Dados; e-mail inválido era gravado e UF com três letras dava 500 | Código | Entrega A: 400 `ValidationProblemDetails` por campo, e-mail vazio como nulo, validação prévia na tela; `RF4_MeusDadosTests` (4), `PaginaMeusDados.spec` (5); manual T4 |
| CSU11 FA2 / RNF 33.7: "Marcar como Não realizada" oferecida com presenças; a API recusava depois da confirmação | Código | Entrega A: opção só em aula Em aberto sem presenças; `PaginaAulasTurma.spec`; manual T5 |
| RF29 / CSU12 FA1: a tela dizia "Remover matrícula"; o requisito é inativar com data de saída | Código | Entrega A: rótulos, confirmação e ícone "Inativar matrícula"; `PaginaMatriculasTurma.spec`; manual T6 |
| RF21: função "Professor, Auxiliar"; a API aceitava qualquer texto e os Dropdowns eram editáveis | Código | Entrega B: `Dominio/Regras/FuncoesAtribuicao`, `[FuncaoAtribuicao]` nos DTOs (400 em `errors.Funcao`), Dropdowns fechados; `RF21_FuncaoAtribuicaoTests` (4), `PaginaAtribuicoesDepartamento.spec` (+2); manual T7 |
| CSU12 FA1 / RNF 34.3: o Professor da turma recebia 403 ao abrir o histórico da matrícula recém-inativada (visto no manual T6) | Código | Entrega B: o guarda de pessoa aceita matrícula ativa ou inativa em turma com atribuição ativa (histórico, pessoa reduzida, parentescos); `RF34_PresencasPessoaTests` (+2), `RF11_PessoaAcessoTests` (+1); manual T8 |
| CSU15: "percentual sobre os registros efetivos"; o percentual por aula dividia pelo total de alunos ativos hoje, e uma matrícula nova rebaixava aulas já consolidadas e o gráfico | Código | Entrega C: denominador = registros da própria aula (`RelatorioEbdServico`), rota e DTO inalterados; `RF35_RelatorioTests` (+2); manual T9 e T10 |
| `Pessoa.Email` e `Usuario.Email` independentes: criar a conta não gravava o e-mail na pessoa, Pessoas e Meus Dados aceitavam o e-mail da conta de outra pessoa e a importação só comparava com pessoas; o texto não tinha a regra | Texto (acréscimo) e código | Corretiva G2 (03/11/2026): RNFs 7.6, 8.4 e 13.5 no .docx em 03/11; `Aplicacao/Regras/EmailDaPessoa`, criação da conta grava o cadastro no mesmo `SaveChanges`, campo bloqueado em Pessoas e Meus Dados; `RF7_RF8_EmailPessoaTests` (4), `RF13_UsuarioTests` (+3), `RF4_MeusDadosTests` (+1), `RF41_ImportacaoTests` (+1); manual G2 T1 a T5 |
| RF14: "o e-mail não é alterado após a criação"; o Administrador precisa corrigir o e-mail de acesso de uma conta | Texto e código (decisão do autor, 03/11/2026) | RF14 reescrito no .docx em 03/11 ("Permite alterar o perfil, a situação (ativo/inativo) e o e-mail de acesso de um usuário; a senha só é redefinida por convite (RF15)."); `PATCH /api/usuarios/{id}` aceita `Email` com a recusa cruzada e a global, iguala o cadastro e invalida o convite pendente; `RF13_UsuarioTests` (+3 de RF14); `PaginaUsuariosLista.spec` (+1); manual G2 T6 |
| CSU16 FA2 / RF9: inativar a pessoa encerrava matrículas e atribuições, mas a conta de acesso continuava ativa e a pessoa inativa ainda entrava no sistema | Texto (acréscimo) e código (decisão do autor, 04/11/2026) | Corretiva G5: RNF 9.3 ("A inativação da pessoa inativa também a sua conta de acesso e descarta convite pendente; a reativação da pessoa não reativa a conta.") e CSU16 FA2, passo 2 ("e inativa a conta de acesso, se houver"), texto pendente no .docx (seção 9 do registro do projeto); `PessoaServico.AtualizarAsync` inativa a conta e descarta o convite na mesma gravação, recusa o próprio cadastro e a pessoa do último Admin ativo (400); `RF9_InativarPessoaContaTests` (4 dos 6 casos), `PaginaPessoasLista.spec` (1), E2E-09; manual G5 (`RF9-*.png`, 9). **Atendida** |
| RF14: só as operações de turma conferiam se a conta continuava ativa; nas demais rotas uma sessão aberta seguia válida até o token expirar | Texto (acréscimo) e código (decisão do autor, 04/11/2026) | Corretiva G5: RNF 14.4 ("A inativação da conta bloqueia o acesso imediatamente, mesmo com sessão aberta."), texto pendente no .docx (seção 9 do registro do projeto); `Aplicacao/Seguranca/ContaAtivaMiddleware` confere a conta em toda requisição autenticada a endpoint não público (401 e cookie apagado), rotas públicas abertas; `RF9_InativarPessoaContaTests.ContaInativadaComTokenValido_Recebe401EmRotaAutenticada`, `ContaInativada_RotaPublicaContinuaRespondendo`; manual G5 (`RNF-14.4-sessao-encerrada.png`). **Atendida** |

Efeito na tabela da seção 2.1: CSU05 e CSU12 passaram de Parcial a Aprovado, e as lacunas de
CSU11, CSU18, CSU22 e CSU23 encolheram (seção 2.2). Efeito nas suítes: 170 → 191 xUnit (31
classes), 126 → 151 Vitest (31 arquivos), 18 Playwright inalterados; o SonarCloud manteve o
Quality Gate Passed nos dois projetos (run #9, 31/10/2026, cobertura do código novo do web 84,8 %).
Decisões do autor registradas para a banca (registro do projeto):
o percentual por aula continua filtrado por matrícula ativa (a saída de um aluno ainda altera as
aulas anteriores, como no geral e no por aluno); a lista fechada de funções não exigiu migration
(o valor antigo "Secretário" foi editado pela tela); nenhuma rota, DTO de resposta ou migration
mudou nas três entregas. A Corretiva G2 acrescentou as duas últimas linhas, com os campos aditivos
`possuiConta` (Pessoas e Meus Dados) e `Email` (PATCH de usuário): 196 → 208 xUnit, 153 → 155 Vitest,
20 Playwright inalterados. A Corretiva G5 acrescentou as RNFs 9.3 e 14.4, sem mudar rota nem DTO:
211 → 217 xUnit (35 classes), 182 → 192 Vitest (38 arquivos), 20 → 25 Playwright (E2E-09, E2E-M4
e E2E-M5).

## 8. Análise estática (SonarCloud)

Os testes provam o comportamento executando o sistema; a análise estática lê o código sem
executá-lo e aponta padrões de defeito, de vulnerabilidade e de manutenção difícil. Uma não
substitui a outra: a monografia avalia Segurança e Confiabilidade por testes técnicos (5.1) e
declara a Manutenibilidade sem medição formal (4.10); os números desta seção entram no texto como
evidência complementar.

**Como a análise roda.** Desde 23/10/2026, o workflow `.github/workflows/sonarcloud.yml` repete
num `ubuntu-24.04`, a cada push e pull request para a main, o build, os testes com cobertura e o
`vue-tsc -b`, e envia dois projetos ao SonarCloud (`dannkes1_tcc-koinoniahub_api` e
`dannkes1_tcc-koinoniahub_web`, organização `dannkes1`). A cobertura da API vem do
`coverlet.collector` 10.1.0 em formato OpenCover, com a contagem dos testes pelo `.trx`; a do
front, do lcov do Vitest. As Migrations ficam fora da análise da API: são código gerado pelo
`dotnet ef` que os testes nunca executam (SQLite com `EnsureCreated`), e com elas a cobertura
seria 28,3 % em vez de 77,0 % das linhas, sem que nenhuma linha de autoria mudasse. O job do
front roda com o fuso da igreja (`TZ=America/Porto_Velho`) e a API no UTC do runner, o que prova
a cada push a convenção "instante em UTC, dia no fuso da igreja". Foi a primeira execução das
suítes fora do Windows do autor: nenhum teste dependia da cultura, do fuso ou do disco local.

**Como ler os números.** A cobertura do SonarCloud combina linhas e condições (por isso a API fica
em 74,5 % com 77,0 % das linhas). No front, o Sonar conta com 0 % os arquivos que nenhum spec
importa, enquanto o resumo do Vitest só considera os arquivos carregados pelos testes (71,7 % das
linhas na primeira análise): o número oficial para o texto é o do SonarCloud. A nota de A a E de
cada qualidade segue o apontamento mais grave, não a quantidade; na organização (Standard
Experience) só os do tipo VULNERABILITY contam para Security e só os do tipo BUG para
Reliability. O Quality Gate (Sonar way) avalia o código novo desde a análise de referência:
cobertura mínima de 80 %, sem issue nova, sem hotspot aberto.

### 8.1 Primeira análise (run #1, commit `c482d3d`, 23/10/2026)

Os dois jobs verdes (API em 1min42s, Web em 1min16s); 170 xUnit e 126 Vitest (24 arquivos)
aprovados no Linux. Nenhum arquivo de código mudou nesta entrega.

| Métrica | API | Web |
| --- | --- | --- |
| Linhas de código | 6.742 (sem Migrations) | 13.627 |
| Quality Gate | não computado (primeira análise) | não computado (primeira análise) |
| Security | 3 (2 Blocker, 1 Medium) · **E** | 0 · **A** |
| Reliability | 12 (1 High, 11 Medium) · **C** | 85 (todas Medium) · **C** |
| Maintainability | 45 · **A** | 128 · **A** |
| Security Hotspots | 0 · **A** | 0 · **A** |
| Cobertura | **74,5 %** (linhas 77,0 %; condições 60,5 %; 4.422 linhas a cobrir, 1.018 descobertas) | **48,0 %** (linhas 41,1 %; condições 63,3 %; 2.542 a cobrir, 1.498 descobertas) |
| Duplicação | 3,0 % | 0,9 % |
| Testes importados | 170 (0 erros, 0 falhas) | não importados (só o lcov) |

Dois dos três apontamentos de Security da API estavam no `appsettings.E2E.json` versionado (senha
do PostgreSQL local de teste e chave JWT do ambiente E2E, valores descartáveis); o terceiro, em
`TokenServico.cs`. Na API, só 2 dos 12 apontamentos de Reliability eram BUG (S8949 e S8733); no
front, 12 dos 85 (os 72 S6853 de rótulo sem campo associado e o S6845 são CODE_SMELL e não entram
na nota). A segunda análise (commit `1d380b4`, só documentação) computou o Quality Gate **OK** nos
dois projetos, com os demais números iguais. Os apontamentos de Security e Reliability foram
agrupados por regra em 23/10/2026 e triados pelo autor: corrigir, aceitar com justificativa ou
adiar.

### 8.2 Correção dos apontamentos (run #3, commit `1043a5d`, 24/10/2026)

**O que mudou.** API (Reliability): `Data` obrigatória no cadastro e na edição de aula (sem o
campo, a aula era gravada com 0001-01-01); ids de aula, matéria, matrícula e usuário com
`[Range(1, …)]` (id ausente passa a dizer qual campo falta); item da chamada sem `presente` → 400
sem gravar nada do lote (antes virava ausência, contra o registro explícito da RNF 32.6);
`await app.RunAsync()`; `AsSingleQuery()` na consulta de uma pessoa. Requisição malformada passa a
receber o 400 `ValidationProblemDetails` padrão (`errors[campo]`); para dados válidos nada muda.
Front (Reliability e acessibilidade): 72 rótulos ligados aos campos em 12 telas (17 campos não
tinham nome acessível nenhum); Cadastro Inicial com rótulos visíveis; `aria-label` no arquivo da
importação; logout em segundo plano com `void`; Meus Dados com o perfil administrativo da store.
Maintainability sem mudança de comportamento: 8 apontamentos na API e 20 no front, mais blocos de
CSS que não casavam com nada (seletores do PrimeVue 3). Nenhuma regra da monografia nem rota mudou.

| Métrica | API: antes → depois | Web: antes → depois |
| --- | --- | --- |
| Linhas de código | 6.742 → 6.750 | 13.627 → 13.774 |
| Apontamentos (total) | 60 → 41 (19 fechados); após a triagem: **7 abertos**, 19 aceitos, 15 falsos positivos | 212 → 106 (109 fechados); após a triagem: **90 abertos**, 16 aceitos |
| Security | 3 (E) → 0 abertos, 3 aceitos · **A** | 0 · **A** |
| Reliability | 12 (C) → 1 aceito (S8949) · **A** | 85 (C) → 1 aceito (S6845) · **A** |
| Maintainability | 45 → 37 · **A** (abertos: 7 × S3776) | 131 → 106 · **A** (abertos: S9423 ×88, S3776, S3358) |
| Security Hotspots | 0 | 0 |
| Cobertura | 74,5 % → **74,5 %** | 48,0 % → **46,3 %** (a cobrir 2.542 → 2.709) |
| Cobertura do código novo | 76,92 % em 18 linhas | 82,35 % em 27 linhas |
| Duplicação | 3,0 % | 0,9 % |
| Testes importados | 170 → 174 | — |
| Quality Gate | **FAILED** (cobertura do código novo < 80 %) | **PASSED** |

**Quality Gate da API.** Das 18 linhas novas, 5 ficaram sem cobertura (os quatro `BadRequest` do
período invertido em `RelatoriosController` e o `catch` interno de `RelogioIgreja.ResolverFuso`),
mais a condição da chave JWT ausente em `Program.cs`. Fechado no mesmo dia com
`RF35_RelatorioTests.Relatorios_PeriodoInvertido_Retorna400` (`[Theory]`, um caso por relatório de
período): `dotnet test` = 178 e o OpenCover local com as quatro linhas visitadas, código novo em
24 de 26 (92,3 %). Ficam sem cobertura, por decisão, o `catch` do `RelogioIgreja` (inalcançável
com ICU e tzdata) e a condição da chave JWT (exigiria subir um host sem a chave). A queda da
cobertura do web (48,0 % → 46,3 %) é efeito dos templates novos: os rótulos acrescentados em
páginas sem spec entram como linhas a cobrir sem teste; nenhuma linha antes coberta deixou de ser.

**Aceitos com justificativa (decisão do autor, 23/10/2026, marcados na interface em 24/10):**
S6418 e S2068 (`appsettings.E2E.json`: configuração do ambiente de teste com valores descartáveis;
os reais nunca são versionados); S6781 (`TokenServico`: chave lida do `IConfiguration`, sem
literal no código); S6845 (`TermoUsoSigilo.vue`: `tabindex="0"` na região rolável do termo, WCAG
2.1.1); S8949 (`RespostaAutorizacaoHandler`: `WriteAsJsonAsync` já usa o `RequestAborted`); S8733
(`AsSingleQuery` registrado no código); S9129 ×8 (SQL idêntico, conferido com `ToQueryString`);
ASP0025; S112; CA1861 ×3 (arrays de `Assert` em teste); S127 (leitor CSV da RFC 4180); S6960
(`PessoasController`, 133 linhas); S9423 ×10 (`@media print`: só `!important` vence o tamanho
inline do Chart.js); S7924 (`BarraTopo`: contraste real 9,69:1); S6564 ×2; S7750 (`findLast`
exige ES2023); S6819 (`LinhaChamada`: a linha inteira é o alvo de toque, `role=button` com
teclado e `aria-pressed`). CA1862 ×15 como **falso positivo**: a comparação está dentro de
consulta `IQueryable` traduzida para `lower(coluna) = @p`, e o `string.Equals` com
`StringComparison` sugerido não é traduzido pelo EF Core 10. Abertos como trabalho futuro:
S3776 (complexidade cognitiva: 7 métodos na API e 1 no front), S3358 (ternário aninhado) e os
S9423 de `style.css` e `LayoutPrincipal.vue`.

**Validação.** `dotnet build` com 0 avisos; `dotnet test` = 174 (depois 178); `vue-tsc -b` limpo;
`npm run test` = 131 (26 arquivos); `npm run build` ok; `npm run test:e2e` = 18, com o E2E-01
conferindo o rótulo do Cadastro Inicial e, no celular, a ausência de rolagem lateral. CSS
conferido no navegador: Painel, Aulas da turma, Pessoas e Usuários a 1366×768 e 360×640, antes e
depois, com **0 pixel diferente** nos 8 prints e os estilos calculados idênticos (salvo a transição
dos links do menu, declarada por propriedade). Testes manuais M1 a M8 ✔, executados pelo autor em
24/10/2026 (`RF1-cadastro-inicial-labels.png`, `RF1-cadastro-inicial-labels-360.png`,
`RNF-30.2-400-aula-sem-data.png`). `npm audit` em 24/10/2026: 15 vulnerabilidades (1 low, 5
moderate, 9 high) nas cadeias de vite, vitest e playwright e nos pacotes `axios` e `vue` (por
`@vue/server-renderer`, que a SPA não usa); fica como entrega própria.

### 8.3 Código novo das entregas de aderência (runs #8 e #9, 31/10/2026)

As Entregas A a C da aderência (seção 7) chegaram ao SonarCloud no run #8 (191 xUnit, 141 Vitest,
18 Playwright): API com Quality Gate Passed e cobertura geral 77,8 %; Web com Quality Gate
**Failed** por uma condição, cobertura do código novo 73,5 % sobre 80 linhas, e o aviso "There
are problems with file encoding in the source code". A conferência dos 136 arquivos de `web/`
rastreados mostrou nenhum fora de UTF-8, sem BOM e com LF; os únicos arquivos sob `sonar.sources`
que não são texto eram as duas imagens de `src/assets`, que o scanner tentava ler, e
`src/assets/**` entrou nas exclusões. As linhas novas descobertas estavam em páginas sem spec
(`PaginaMeusDados.vue`, `PaginaUsuariosLista.vue`, `PaginaPrimeiroAcesso.vue` e os `v-model` do
Cadastro Inicial); a Entrega D acrescentou só specs: `PaginaMeusDados.spec` (5),
`PaginaUsuariosLista.spec` (1), `PaginaPrimeiroAcesso.spec` (3) e `PaginaCadastroInicial.spec`
(+1), sem mudar comportamento (`npm run test` = 151 casos, 31 arquivos).

**Resultado (run #9, 31/10/2026).** Web: Quality Gate **Passed**, cobertura do código novo
84,8 % sobre 89 linhas, 0 issues novas e nenhum aviso de análise (o aviso de encoding desapareceu
com as imagens fora da análise, o que confirma o diagnóstico). API: Quality Gate **Passed**. A
medição local (62 de 62 linhas executáveis novas cobertas, pelo lcov cruzado com o `git diff`) e a
do Sonar (84,8 %) diferem porque o Sonar conta também linhas de template dos `.vue`, que o lcov
não lista. Lição registrada: uma página sem spec conta inteira como a cobrir, então toda página
alterada precisa de spec na mesma entrega.

### 8.4 Prints (`docs/evidencias/`, prefixo `SONAR-`)

- Primeira análise (17): `SONAR-actions-verde.png`, `SONAR-api-log.png`, `SONAR-api-log-testes.png`,
  `SONAR-api-overview.png`, `-reliability.png`, `-maintainability.png`, `-hotspots.png`,
  `SONAR-api-coverage.png`, `SONAR-api-testes.png`, `SONAR-api-sem-migrations.png`,
  `SONAR-web-log.png`, `SONAR-web-log-testes.png`, `SONAR-web-overview.png`, `-reliability.png`,
  `-maintainability.png`, `-hotspots.png`, `SONAR-web-coverage.png`.
- Correção (12): `SONAR-correcao-actions-verde.png`, `SONAR-correcao-api-overview.png`,
  `-summary.png`, `-reliability.png`, `-maintainability.png`, `-issues-abertas.png`,
  `SONAR-correcao-web-overview.png`, `-summary.png`, `-reliability.png`, `-maintainability.png`,
  `-issues-abertas.png`; `SONAR-correcao-api-quality-gate.png`.
- Aderência (2): `SONAR-aderencia-web-quality-gate.png`, `SONAR-aderencia-api-quality-gate.png`.

### 8.5 Leitura para o texto do TCC

- Security, Reliability e Maintainability do SonarCloud conversam com Segurança, Confiabilidade e
  Manutenibilidade da ISO/IEC 25010 (seção 4.10), mas a nota vem do apontamento mais grave, não
  é uma medição na escala da norma.
- Excluir as Migrations não maquia o número: é código gerado que os testes não executam por
  desenho, e a decisão está registrada com os dois valores (28,3 % e 77,0 %).
- Um apontamento não é automaticamente um defeito. Há três destinos, todos registrados: corrigido
  (ex.: `Data` obrigatória), aceito com justificativa (ex.: S8949) ou falso positivo (CA1862, cuja
  sugestão quebraria a tradução para SQL); a dívida mantida aberta (S3776) está em Trabalhos
  futuros.
- O CI num Linux com fuso UTC e cultura invariante prova, a cada push, que a regra de datas
  ("instante em UTC, dia no fuso da igreja") e o CSV não dependem do ambiente do autor.
