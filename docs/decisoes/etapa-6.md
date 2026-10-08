# Etapa 6 — Acabamento de UI e RF44 (Editar Dados da Igreja) · resumo de decisões

Entregas 6.1 (acabamento, 06/10/2026) e 6.2 (RF44, 07/10/2026). Plano de Desenvolvimento, seções 5, 6.10 e 7.2; monografia RF44 e CSU24 (incluídos em 06/10/2026 por decisão do autor), RNFs 32.5, 41.3, 42.7, 44.1–44.5; RF3, RF6, RF34; CSU07. Escrito conforme a regra "Resumo de decisões por etapa" (Plano 1.1).

## 1. Decisões tomadas dentro da margem de julgamento

### 1.1 Acabamento (6.1)

**Dica da observação (RNF 32.5) uma vez, acima da tabela.** O Plano 6.10 dizia "abaixo do campo de observação"; na chamada o campo se repete por aluno, e a frase repetida em cada linha poluiria a tela. Texto idêntico ao do Plano, exibido só com a aula editável; `placeholder` "Justificativa administrativa (opcional)" e `maxlength` igual ao limite do DTO (500).

**Sinalização da importação (RNF 41.3) como dado, não como texto.** A mensagem "Já existe uma pessoa com este nome; linha ignorada para conferência…" já era a do Plano 6.4. O que faltava era a *sinalização*: a API ganhou `paraConferencia` no item e no total (aditivo), e o diálogo mostra tag "Conferir", contador, destaque e aviso. Preferido a procurar "conferência" na mensagem: contrato explícito, testável no xUnit, sem acoplamento ao texto. O parser do resultado virou função pura (`normalizarResultadoImportacao`) com o spec que o Plano 7.2 pedia.

**Indicadores do aluno em um módulo só (`indicadoresPresenca.ts`).** Painel do usuário comum (RF3) e diálogo de histórico de Pessoas (RF34) calculavam cada um por si. Agora os dois usam `resumirPresencas` — só aulas Consolidadas contam (CSU07); percentual `null` → "-" quando não há aula, em vez de um "0%" enganoso.

**Janela do painel = janela padrão de Minha Frequência (90 dias), aplicada no front.** O endpoint do RF34 devolve o histórico completo e não filtra período; o painel filtra os registros recebidos com `filtrarJanela`. O rótulo "3 Meses" (que não correspondia a filtro algum) deu lugar a "últimos 90 dias" nos cards e no título, com nota dizendo que o painel soma todas as turmas. Diferença possível só na aula exatamente no limite (comparação por hora contra a `UtcNow.AddDays(-90)` da API); irrelevante para o indicador. Minha Frequência passou a exibir o período que a API usou (os campos já vinham na resposta).

**Tela do termo (42.7):** revisada sem alteração — o cabeçalho Instituição / Canal já vinha do cadastro da igreja desde a 5.2.

### 1.2 RF44 — Editar Dados da Igreja (6.2)

**Origem.** Achado do autor na validação da 5.2: não havia tela para editar os dados da igreja, e o e-mail é justamente o canal exibido no termo (RNF 42.7); o telefone nem era coletado (RF1 não o pede). Sinalizado como fora da baseline; o autor redigiu RF44 e CSU24, inseriu a Figura 34 (Tela de Igreja, mockup no Figma no layout de Meus Dados), acrescentou o caso de uso ao diagrama (Figura 35) e aprovou a implementação na Etapa 6.

**Cinco campos, não nove.** `Igrejas` tem Nome, Endereco, Cidade, Estado, CEP, Telefone, Email, CNPJ e LogoUrl; RF44 expõe Nome, Cidade, Estado, E-mail e Telefone. Endereco, CEP, CNPJ e LogoUrl continuam sem funcionalidade (decisão do autor, pela motivação do RF — o canal de contato do termo). Registrado no Plano, seção 9.

**Caso de uso próprio, não «extend» de CSU01.** Editar depois não insere comportamento na execução do cadastro inicial; a relação é só de dados. No diagrama: associação direta com o Administrador e, por consistência com os demais casos que têm "O sistema valida os dados" + FA "Dados inválidos", `<<include>> Validar Dados`.

**API.** `PUT /api/igrejas/{id}` com `[Authorize(Roles = "Admin")]` (44.1) e a mesma checagem do `GET` — o `id` precisa ser o da igreja do token, senão 403 (44.2); validação pelo DTO (`[Required]` no nome, `[EmailAddress]`, tamanhos) → 400 com os erros por campo (44.3). O serviço altera só os cinco campos: Endereco/CEP/CNPJ/LogoUrl e os registros de aceite não são tocados; o cabeçalho do termo lê Nome/Email/Telefone a cada exibição, então a alteração reflete na próxima tela sem mexer nos aceites gravados — o hash cobre só o texto fixo (44.5). `Estado` é normalizado para maiúsculas (UF); strings vazias viram `null`. `IgrejaRespostaDto` ganhou `Telefone` e `AtualizadoEm` (aditivo). Repositório ganhou `AtualizarAsync`; `AtualizadoEm` é preenchido pelo `SaveChangesAsync` do contexto, como nas demais entidades.

**Front.** Rota `/igreja` com `requerAdmin`; item "Igreja" (ícone `pi-building`) no menu, só para Admin, logo abaixo de Usuários; tela `PaginaIgreja` no layout de Meus Dados (Descartar/Salvar no cabeçalho; aviso sobre o canal do termo; blocos "Dados da igreja" e "Contato"), que carrega a igreja do token (`igrejaId` da sessão). Higienização do telefone e UF em maiúsculas no front, iguais ao que a API faz. O spec do menu lateral passou a cobrir "Igreja" (Admin vê; Pastor, Superintendente, Professor e Usuario não).

## 2. Arquivos criados ou alterados

API — `Aplicacao/DTOS/Respostas/PessoaImportacaoRespostaDto.cs`, `Aplicacao/Servicos/Implementacoes/PessoaImportacaoServico.cs` (6.1); `Aplicacao/DTOS/Requisicoes/IgrejaAtualizarRequisicaoDto.cs` (novo), `Aplicacao/DTOS/Respostas/IgrejaRespostaDto.cs`, `Aplicacao/Servicos/{Interfaces/IIgrejaServico, Implementacoes/IgrejaServico}.cs`, `Dominio/Interfaces/Repositorios/IIgrejaRepositorio.cs`, `Infraestrutura/Repositorios/IgrejaRepositorio.cs`, `Controllers/IgrejasController.cs` (6.2).

Testes xUnit — `RF41_ImportacaoTests` (asserções de `ParaConferencia`); `RF44_IgrejaTests` (5: Admin atualiza e o termo reflete; Professor 403; outra igreja 403; nome vazio 400; e-mail inválido 400). Total do projeto: 113.

Front — `aplicacao/modelos/dtos.ts`; `aplicacao/servicos/pessoasServico.ts` (+ spec, 4); `aplicacao/dominio/indicadoresPresenca.ts` (novo, + spec, 5); `paginas/privado/pessoas/{DialogImportarPessoas, PaginaPessoasLista}.vue`; `paginas/privado/chamada/PaginaChamadaAula.vue`; `paginas/privado/PaginaPainel.vue`; `paginas/privado/minha-frequencia/PaginaMinhaFrequencia.vue`; `style.css` (6.1). `aplicacao/servicos/igrejasServico.ts` (novo); `paginas/privado/igreja/PaginaIgreja.vue` (novo); `aplicacao/rotas/index.ts`; `components/layout/MenuLateral.vue` (+ spec atualizado) (6.2). Vitest: 54 casos.

Evidências — `RNF-32.5-dica-observacao.png`, `RNF-41.3-importacao-conferir.png`, `RF3-painel-aluno-90-dias.png`, `RF34-historico-pessoas-consolidadas.png` (6.1); `RF44-tela-igreja.png`, `RF44-termo-contato-atualizado.png` (6.2).

## 3. O que o autor deve saber explicar na banca

- Por que o campo de observação da chamada pede uma orientação explícita (RNF 32.5, princípio da necessidade da LGPD) e por que ela é texto de tela, não validação — o sistema não tem como saber o conteúdo.
- A diferença, na importação, entre "ignorada por e-mail repetido" (certeza de duplicidade) e "ignorada para conferência" (nome igual sem e-mail: pode ser homônimo), e por que a segunda precisa ser sinalizada e não só ignorada (RNF 41.3).
- Por que os indicadores de frequência do aluno — no painel e no histórico — contam só aulas Consolidadas (CSU07) e por que a janela do painel é a mesma de Minha Frequência.
- Por que RF44 entrou depois da aprovação do TCC 1 (lacuna entre RF1 e RNF 42.7: o canal de contato do termo vinha de um cadastro que não podia ser editado) e o que mudou na monografia por causa disso (RF44, CSU24, Figuras 34–36).
- Por que alterar o e-mail/telefone da igreja não invalida nem altera os aceites já registrados (hash só do texto fixo, RNF 42.7/44.5).
