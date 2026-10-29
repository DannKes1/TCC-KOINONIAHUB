# Aderência da monografia ao sistema — revisão de 28/10/2026 e entregas corretivas

Antes da conversão do texto para o TCC 2 (Plano v1.10, seção 9), o .docx foi confrontado com o
sistema implementado: RF1 a RF45 com as RNFs numeradas, CSU01 a CSU24 passo a passo (fluxo
principal e alternativos), o DER e o diagrama de casos de uso, a seção 4.8 (JWT, cookie, CORS,
Origin, BCrypt), as telas do protótipo contra as rotas do front e as versões das ferramentas
contra os manifestos. A verificação mostrou que nenhuma regra de negócio central diverge
(situações da aula, perfil × atribuição, termo, isolamento por igreja, LGPD). Sobraram onze
pontos em que o texto deve descrever o que o sistema faz (seção 1), oito em que o código deve
seguir o texto (seções 2 a 4) e uma lista do que existe no código sem constar da monografia
(seção 5). Decisão do autor em 28/10/2026: código ajustado nos oito itens, em três entregas (A,
B e C), cada uma com plano prévio, suíte completa antes e depois e testes manuais; o texto é
alterado pelo autor no .docx; nenhum RF novo.

## 1. Correções de texto aprovadas (o autor aplica no .docx)

| Onde | Texto atual → texto novo | Por quê |
| --- | --- | --- |
| CSU01, passo 5 | "O sistema direciona o usuário para a tela de login." → "O sistema inicia a sessão do Administrador e exibe o Painel." | Desde a Etapa 2.4 o cadastro inicial grava o cookie de sessão e o front entra no Painel (`PaginaCadastroInicial.vue`); o E2E-01 afirma isso. Já constava da seção 9 do Plano. |
| CSU09, passo 4 | "…o professor responsável e o tema." → "…o professor responsável e, opcionalmente, o tema e o conteúdo." | Tema e conteúdo são opcionais na API e no diálogo (6.5). Já constava da seção 9. |
| RF17, descrição | "definindo nome, tipo e descrição" → "definindo nome e tipo" | A tela e a resposta da API não têm descrição; o CSU17 passo 4 já diz "nome e tipo". |
| CSU08, passo 4 | "O usuário informa nome, descrição e ordem de exibição." → "O usuário informa nome e ordem de exibição." | Mesmo caso; o RF24 já diz só isso. |
| RF4, descrição | "consultar e atualizar os seus dados pessoais e de contato" → "consultar os seus dados e atualizar os dados de contato e endereço" | Nome, nascimento, sexo e estado civil não são editáveis pelo próprio usuário (alinha com CSU05 e 4.9). |
| RF14, descrição | "Permite alterar os dados de acesso e o perfil de um usuário." → "Permite alterar o perfil e a situação (ativo/inativo) de um usuário; o e-mail não é alterado após a criação e a senha só é redefinida por convite (RF15)." | O DTO de edição só tem perfil e situação; o e-mail fica desabilitado no diálogo. |
| RF7 / CSU16, passo 4 | "…o celular, o e-mail, o endereço." → "…o celular, o e-mail e o endereço (logradouro, bairro, cidade, UF e CEP)." | O endereço é decomposto em cinco campos na entidade, no DTO, na tela e no modelo CSV. A RNF 7.5 continua válida: só o nome é obrigatório. |
| RF11, descrição | "incluindo dados pessoais, vínculos e situação" → "incluindo dados pessoais, situação e vínculos de parentesco" | Matrículas e atribuições da pessoa não são exibidas em Pessoas. |
| CSU07, passo 1 | "O usuário acessa a opção Minha Frequência." → "O usuário aciona Minha frequência na turma desejada, a partir de Minhas Turmas ou do Painel." | Não existe item de menu; o acesso é por turma. |
| CSU04, passo 3 | "o histórico dos últimos três meses" → "o histórico dos últimos 90 dias" | Janela fixa de 90 dias no Painel e em Minha Frequência. |
| RNFs 10.2, 16.2, 19.2, 23.2 e 31.2 | "A listagem deve ser paginada e permitir busca e filtros." → "A listagem deve ser paginada em tela e permitir busca e filtros." | A paginação é feita em tela (PrimeVue, dez linhas; "Mostrar mais" no celular); a API devolve a lista inteira da igreja. Para a banca: o porte da base (cerca de 200 pessoas e 8 turmas) dispensa paginação no servidor. |

## 2. Entrega A — seis correções de aderência (28/10/2026)

**Critério.** A monografia é o contrato. Nos seis itens o texto pede algo razoável e a
diferença era acabamento, validação ou consistência, não decisão de produto; corrigir o código
mantém a tabela CSU → teste do capítulo de Resultados sem ressalva. Escopo fechado nos seis
itens; nenhuma rota, DTO de resposta ou migration alterados.

**Item 1 — RF36, limiar crítico ajustável na tela.** O texto diz "os limiares e o número de
faltas consecutivas para situação crítica são ajustáveis na tela"; a tela só expunha o limiar
de atenção e as faltas seguidas, e o crítico ficava fixo em 50 % pelo padrão da API. Entrou o
campo "Crítico: freq. ≤ (%)" ao lado dos existentes, enviado como `limiarCritico` (parâmetro que
a API já aceitava). O limiar de atenção passou a se chamar "Atenção: freq. ≤ (%)", para os dois
ficarem distinguíveis e com o mesmo sinal da regra (≤). Padrões iguais aos da API: 75, 50 e 3.
Prints anteriores a esta entrega mantêm o rótulo antigo.

**Item 2 — RF43 / RNF 43.1, data e hora do aceite na lista de Usuários.** A coluna "Termo"
mostrava a versão na tag e deixava a data só em tooltip, que não existe no celular. A lógica de
rótulo, severidade, data e dica saiu da página para a função pura `resumirTermoUsuario`
(`aplicacao/dominio/termoUsuario.ts`, o mesmo padrão de `atribuicoes.ts`), e a coluna passou a
mostrar a tag e, abaixo, "em dd/mm/aaaa hh:mm". O tooltip continua. Meus Dados já mostrava a
data no corpo e não mudou.

**Item 3 — RNF 42.1 / CSU01 FA2 / CSU22 FA2, aviso de aceite obrigatório.** O botão de
conclusão ficava desabilitado sem o aceite, mas nada "informava que o aceite do termo é
obrigatório" no cadastro inicial e no primeiro acesso; só a tela /termo avisava. O componente
`TermoUsoSigilo` ganhou a prop `avisoAceite` (padrão falso): ligada, mostra "O aceite do termo é
obrigatório para concluir." sob a caixa enquanto ela está desmarcada. As duas telas públicas
ligam a prop; /termo mantém o aviso próprio para não duplicar. É também a boa prática de
explicar por que um botão está desabilitado.

**Item 4 — RNF 4.3, validação de Meus Dados.** O DTO de atualização não tinha nenhuma anotação:
e-mail inválido era gravado e UF com três letras virava 500 pelo tamanho da coluna. O DTO
recebeu as mesmas anotações do DTO de Pessoa (`EmailAddress`, `StringLength` por campo, UF com
2), o que devolve 400 `ValidationProblemDetails` por campo — o contrato já usado pelo RF44 e já
mapeado pela tela nos `FieldError`. O e-mail continua opcional: o `[EmailAddress]` do .NET só
aceita nulo ou texto com um único "@" fora das pontas e recusaria a string vazia, então o DTO
normaliza vazio ou espaços para nulo antes da validação (provado no teste). O front valida antes
(celular, e-mail e UF) com a mensagem no campo, usando para o e-mail exatamente a regra da API
(um "@", fora das pontas; vazio aceito), e envia a UF em maiúsculas. Nenhum campo é obrigatório
em Meus Dados, então a RNF se aplica ao formato e ao tamanho.

**Item 5 — CSU11 FA2 / RNF 33.7, "Marcar como Não realizada" só sem presenças.** O ⋮ da aba
Aulas oferecia a opção para qualquer aula Em aberto e a API recusava com 400 quando havia
presenças; o usuário confirmava e recebia erro. A opção passou a usar a mesma condição de
"Excluir aula" (`emAberto && !possuiPresencas`, campo aditivo da 6.5). A recusa da API continua
para quem chama a rota diretamente.

**Item 6 — RF29 / CSU12 FA1, verbo "inativar".** A tela chamava a operação de "Remover
matrícula" (⋮, botão, confirmação, toast), o que sugere exclusão; o requisito é "Inativar
matrícula", com data de saída e presenças preservadas. Só rótulos, mensagens e ícone mudaram
(`pi-user-minus` no lugar da lixeira); a confirmação passou a dizer que as presenças ficam
preservadas. Nomes internos (`confirmarRemover`, `removerMatricula`) e a rota `DELETE` não
mudaram, conforme a regra de não renomear o que existe.

**Testes.** xUnit `RF4_MeusDadosTests` (4: e-mail inválido → 400 com `errors.Email` e nada
gravado; UF "ROO" → 400 com `errors.Estado`; e-mail vazio → 204 e nulo gravado, com a prova de
que o atributo sozinho recusa ""; PUT válido → 204 e o GET reflete). Vitest:
`termoUsuario.spec` (4), `TermoUsoSigilo.spec` (+2: aviso com a caixa desmarcada e sumindo ao
marcar; sem a prop, sem aviso), `PaginaRelatoriosEbd.spec` (novo, 2: a aba Acompanhamento expõe
os três campos com 75/50/3 e a busca automática envia os três; alterar atenção e crítico e
buscar envia 80/40), `PaginaAulasTurma.spec` (caso existente com as asserções de "Marcar como
Não realizada" nos três estados), `PaginaMatriculasTurma.spec` (rótulos "Inativar matrícula" e
`acceptLabel` "Inativar"). E2E-01: duas asserções no bloco do FA2 (aviso visível antes do aceite
e ausente depois). Totais depois da entrega: xUnit 182 · Vitest 139 (28 arquivos) · Playwright 18.

## 3. Entrega B — RF21, função da atribuição em lista fechada (planejada)

Registro ao fechar a entrega.

## 4. Entrega C — RF35, percentual por aula sobre os registros efetivos (planejada)

Registro ao fechar a entrega.

## 5. Existe no código e não consta da monografia (sem RF novo; decisão do autor, 28/10/2026)

Itens para o capítulo de Resultados ou para Trabalhos futuros, conforme o autor decidir.

**Campos de entidade sem uso nas telas:** `Aula.Observacoes` (aceito no DTO de criação, nunca
exibido nem editado); `Usuario.UltimoAcesso` (nunca preenchido); `Departamento.ImagemUrl`,
`Materia.ImagemUrl`, `Igreja.LogoUrl`, `Igreja.CNPJ`, `Igreja.Endereco`, `Igreja.CEP` (aceitos
na API, fora das telas); `Departamento.Descricao` e `Materia.Descricao` (aceitos na entrada, não
devolvidos nem editáveis; a tela de Matérias envia `null`); `AlunoDepartamento.Observacao`
(campo "Observação (opcional)" do diálogo de matrícula; ausente do DER).

**Regras de negócio além do texto:** e-mail de pessoa único por igreja (cadastro, edição,
importação e Meus Dados); nome e tipo de turma únicos por igreja; uma conta de acesso por
pessoa; o usuário não desativa a própria conta (avaliada antes da RNF 14.3); e-mail de usuário
único em toda a base, não por igreja (índice único; a mensagem "Já existe um usuário com este
e-mail" vale entre igrejas); atribuição ativa não duplica a mesma função; atribuição pode ser
reativada pela edição (apaga a data de fim) e criada já inativa; parentesco único por (pessoa,
parente, tipo); matéria pode ser inativada; pessoa inativa não aparece entre as disponíveis
para matrícula; pessoa pode ser criada já "Inativo".

**Rotas e respostas além do texto:** `GET /api/usuarios/{id}` (sem consumidor no front);
`GET /api/igrejas/{id}` aberto a qualquer perfil da própria igreja; `GET /api/termo/vigente?token=`
público, identificando a igreja pelo convite; `GET /api/aulas/{aulaId}/presencas` e a Tela de
Presenças da Aula (figura 30 do protótipo, sem RF ou CSU que a cite); `GET /api/aulas/{aulaId}/chamada`
e `GET /api/aulas/{id}`; `GET /api/atribuicoes`, `GET /api/atribuicoes/{id}` e
`GET /api/atribuicoes/departamento/{id}` sem o guarda de turma (qualquer conta autenticada da
igreja lista); `GET /api/materias/{id}`; `PATCH /api/aulas/{id}/reabrir` devolve 404 sem corpo
para aula inexistente; Minha Frequência aceita `dataInicio`/`dataFim` sem seletor na tela;
campos aditivos nas respostas (`PendenteFechamento`, `PossuiPresencas`, `Registrado`,
`ConvitePendente`, `PerfilConta`, `Derivado`, `AulasPendentes[]`, `AulasNaoRealizadas[]`,
`Motivos[]`, `DataUltimaPresenca`, `NaoRegistrado`, `AceiteTermo.Vigente`).

**Telas e ações além do texto:** consolidar, marcar Não realizada e reabrir também pelo ⋮ da
aba Aulas; "Marcar todos presentes", "Limpar presenças", indicador "Tudo salvo / Alterações não
salvas" e confirmação ao sair com alterações na chamada; busca local e filtro por situação com
"Pendentes de fechamento" na aba Aulas; painel de gestão com atalhos e tabelas "Últimas aulas" e
"Resumo por turma"; painel do professor com "Aulas em aberto (fazer chamada)"; "Abrir turma" em
Minhas Turmas para vínculo de atribuição; tela /termo consultável depois do aceite; botões
"Descartar" e "Recarregar"; filtros de Pessoas por e-mail e celular e de Turmas por tipo; diálogo
"Responsáveis" na aba Alunos; modelo CSV com apelidos de cabeçalho, detecção de separador,
limite de 2 MB, aceitação de `.txt` e quatro formatos de data; mensagem de WhatsApp
pré-formatada no convite; sessão (só metadados) em `localStorage` para detectar expiração.

**Observações de implantação e de critério, já na seção 9 do Plano:** IP do aceite atrás de
proxy (`ForwardedHeaders`); HSTS; relatórios RF35/RF36 consideram só matrículas ativas (aluno
que saiu da turma some do período); o histórico do aluno (CSU14) calcula o percentual sem
janela e Minha Frequência usa 90 dias; o Resumo do dia conta aulas de turmas inativas nos
totais de aulas, mas não nos de presença; o Professor recebe 403 ao abrir o histórico de
matrícula inativa.

## 6. Arquivos (Entrega A)

API — `Aplicacao/DTOS/Requisicoes/MeusDadosAtualizarRequisicaoDto.cs` (anotações).

Testes xUnit — `RF4_MeusDadosTests.cs` (novo, 4). Total: 182.

Front — `components/termo/TermoUsoSigilo.vue` (prop `avisoAceite`) e `TermoUsoSigilo.spec.ts` (+2);
`paginas/publico/PaginaCadastroInicial.vue` e `PaginaPrimeiroAcesso.vue` (prop ligada);
`aplicacao/dominio/termoUsuario.ts` (novo) e `termoUsuario.spec.ts` (novo, 4);
`paginas/privado/usuarios/PaginaUsuariosLista.vue` (coluna Termo com a data);
`paginas/privado/meus-dados/PaginaMeusDados.vue` (validação prévia e UF em maiúsculas);
`paginas/privado/relatorios/PaginaRelatoriosEbd.vue` (campo do limiar crítico) e
`PaginaRelatoriosEbd.spec.ts` (novo, 2); `paginas/privado/aulas/PaginaAulasTurma.vue`
(condição de Não realizada) e `PaginaAulasTurma.spec.ts`;
`paginas/privado/matriculas/PaginaMatriculasTurma.vue` (rótulos) e `PaginaMatriculasTurma.spec.ts`.
Vitest: 139 casos (28 arquivos).

E2E — `e2e/e2e-01-cadastro-inicial.spec.ts` (+2 asserções). Playwright: 18.

Docs — este arquivo (novo); `docs/testes.md` (bloco "Aderência — Entrega A").

## 7. O que o autor deve saber explicar na banca

- Por que a conferência foi feita antes da conversão para o TCC 2: o capítulo de Resultados
  descreve o sistema entregue; cada frase do Apêndice A que não corresponde à tela vira uma
  pergunta da banca. Ler RF por RF e CSU passo a passo encontrou o que os testes por requisito
  não acusavam (mesma lição da 7.3).
- O critério usado para decidir entre mudar o texto e mudar o código: o texto muda quando a
  implementação é uma decisão melhor e deliberada (seção 1); o código muda quando a diferença
  é acabamento, validação ou consistência (seções 2 a 4).
- RNF 4.3: por que a validação está nas duas pontas. A anotação no DTO é a garantia (vale para
  qualquer cliente da API); a validação no front é conveniência (resposta imediata, sem ida ao
  servidor). O 400 por campo é o mesmo contrato dos demais formulários.
- RF36: a API sempre aceitou os três parâmetros; a correção foi expor na tela o que o texto
  prometia. Os valores padrão (75 %, 50 %, 3 faltas) vêm da regra do RF36.
- RNF 42.1: desabilitar o botão impede o erro; informar o motivo é o que torna a tela
  utilizável para quem tem pouca familiaridade com tecnologia (problemática, seção 1.2).
- RF29: "remover" e "inativar" são operações diferentes para o usuário; a tela precisa dizer o
  que o sistema faz (data de saída, presenças preservadas), que é o que o requisito especifica.

## 8. Registro para o Plano (v1.11)

Seção 5: acrescentar a linha "Aderência — Entrega A (28/10/2026): seis correções (RF36, RF43,
RNF 42.1, RNF 4.3, CSU11 FA2, RF29); registro em `docs/decisoes/aderencia-monografia.md`".
Seção 7.1: linha `RF4_MeusDadosTests | RNF 4.3: 400 por campo (e-mail, UF), e-mail vazio opcional e 204 válido (4 casos)`.
Seção 7.2: `termoUsuario.spec` (4), `PaginaRelatoriosEbd.spec` (2), `TermoUsoSigilo.spec` (7 → 9).
Seção 7.4: "178 aprovados" → "182"; "131 (26 arquivos)" → "139 (28 arquivos)".
Seção 9: retirar as linhas CSU01 passo 5 e CSU09 passo 4 quando o autor aplicar o .docx;
acrescentar as nove correções de texto restantes da seção 1 deste arquivo até serem aplicadas.
