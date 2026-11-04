<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from "vue";
import { useRoute } from "vue-router";

import { usarAutenticacaoStore } from "../../../aplicacao/armazenamentos/autenticacaoStore";

import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";
import FieldError from "../../../components/ui/FieldError.vue";

import { useAsync } from "../../../aplicacao/composables/useAsync";

import { toastSuccess } from "../../../aplicacao/servicos/notificacoes";

import {
  firstFieldError,
  mensagemForaDosCampos,
} from "../../../aplicacao/servicos/apiError";

// RNF 21.5 (7.1): tag "Sem conta" / "Conta com perfil Usuário — não acessa a turma"
// ao lado da pessoa, só na coluna Pessoa desta tela (decisão do autor). Abaixo de
// 768 px usa o texto curto, para a tag caber em duas linhas.
import { avisoConta } from "../../../aplicacao/dominio/atribuicoes";
import { usarTelaCompacta } from "../../../aplicacao/composables/usarTelaCompacta";

import { obterDepartamento } from "../../../aplicacao/servicos/departamentosServico";
import { listarPessoas } from "../../../aplicacao/servicos/pessoasServico";
import {
  listarAtribuicoesPorDepartamento,
  obterAtribuicao,
  criarAtribuicao,
  atualizarAtribuicao,
  encerrarAtribuicao,
} from "../../../aplicacao/servicos/atribuicoesServico";

import type {
  DepartamentoVM,
  PessoaVM,
  AtribuicaoVM,
} from "../../../aplicacao/modelos/dtos";

import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Button from "primevue/button";
import Dialog from "primevue/dialog";
import Dropdown from "primevue/dropdown";
import InputText from "primevue/inputtext";
import Calendar from "primevue/calendar";
import Tag from "primevue/tag";

import { useConfirm } from "primevue/useconfirm";

const autenticacao = usarAutenticacaoStore();
const route = useRoute();
const confirm = useConfirm();

const { carregando, erro, run, clearErrors } = useAsync();
const compacta = usarTelaCompacta();

// Corretiva G4: o erro do salvar fica no próprio diálogo (uma instância serve aos dois
// diálogos, que nunca ficam abertos ao mesmo tempo) — junto ao campo quando a API
// devolve o campo ou a validação local aponta o campo, senão num aviso no topo — e some
// ao fechar ou ao alterar um campo. A instância da página continua com o erro da lista,
// da carga da atribuição e do encerramento.
const {
  carregando: salvando,
  erro: erroDoDialogo,
  fieldErrors: errosDoDialogo,
  run: executarNoDialogo,
  clearErrors: limparErrosDaApi,
} = useAsync();

// Validação local no próprio campo (chave = nome do campo no DTO).
const errosLocais = reactive<Record<string, string>>({});

function limparErrosDoDialogo() {
  limparErrosDaApi();
  for (const chave of Object.keys(errosLocais)) delete errosLocais[chave];
}

function erroDoCampo(campo: string, camel: string) {
  return (
    errosLocais[campo] ||
    firstFieldError(errosDoDialogo.value, campo) ||
    firstFieldError(errosDoDialogo.value, camel)
  );
}

// Campos com FieldError em cada diálogo: um erro da API fora deles (ex.: DepartamentoId,
// que vem da rota) vai para o aviso do topo.
const CAMPOS_CRIACAO = ["PessoaId", "Funcao", "DataInicio", "Ativo"];
const CAMPOS_EDICAO = ["Funcao", "Ativo", "DataFim"];
const avisoCriacao = computed(() =>
  mensagemForaDosCampos(erroDoDialogo.value, errosDoDialogo.value, CAMPOS_CRIACAO),
);
const avisoEdicao = computed(() =>
  mensagemForaDosCampos(erroDoDialogo.value, errosDoDialogo.value, CAMPOS_EDICAO),
);

const podeGerenciarAtribuicao = computed(() => {
  const perfil = (autenticacao.perfil || "").trim().toLowerCase();
  return (
    perfil === "admin" || perfil === "pastor" || perfil === "superintendente"
  );
});

const departamentoId = Number(route.params.departamentoId ?? 0);

const departamento = ref<DepartamentoVM | null>(null);
const pessoas = ref<PessoaVM[]>([]);
const atribuicoes = ref<AtribuicaoVM[]>([]);

const dialogCriacaoAberto = ref(false);
const dialogEdicaoAberto = ref(false);

const editandoId = ref<number | null>(null);

const filtroFuncao = ref<string | null>(null);
// Entrega G: a lista abre com as atribuições ativas (parâmetro da API); "Todos" é
// escolha explícita. Limpar volta a este padrão.
const FILTRO_ATIVO_PADRAO = "true";
const filtroAtivo = ref<string | null>(FILTRO_ATIVO_PADRAO);
const busca = ref("");

/** Busca local por nome da pessoa ou função (os demais filtros são aplicados pela API). */
const atribuicoesFiltradas = computed(() => {
  const termo = busca.value.trim().toLowerCase();
  if (!termo) return atribuicoes.value;

  return atribuicoes.value.filter(
    (a) =>
      String(a.pessoaNome ?? "")
        .toLowerCase()
        .includes(termo) ||
      String(a.funcao ?? "")
        .toLowerCase()
        .includes(termo),
  );
});

// RF21: função em lista fechada (a API recusa outro valor com 400 em errors.Funcao).
const opcoesFuncao = [
  { label: "Professor", value: "Professor" },
  { label: "Auxiliar", value: "Auxiliar" },
];

const opcoesStatus = [
  { label: "Ativo", value: "true" },
  { label: "Inativo", value: "false" },
];

const formularioCriacao = reactive({
  pessoaId: null as number | null,
  funcao: "Professor",
  dataInicio: null as Date | null,
  ativo: true,
});

const formularioEdicao = reactive({
  funcao: "Professor",
  ativo: true,
  dataFim: null as Date | null,
});

watch([dialogCriacaoAberto, dialogEdicaoAberto], ([criacao, edicao]) => {
  if (!criacao && !edicao) limparErrosDoDialogo();
});
watch(formularioCriacao, limparErrosDoDialogo);
watch(formularioEdicao, limparErrosDoDialogo);

function limparCriacao() {
  formularioCriacao.pessoaId = null;
  formularioCriacao.funcao = "Professor";
  formularioCriacao.dataInicio = null;
  formularioCriacao.ativo = true;
}

function limparEdicao() {
  formularioEdicao.funcao = "Professor";
  formularioEdicao.ativo = true;
  formularioEdicao.dataFim = null;
  editandoId.value = null;
}

function abrirNovo() {
  clearErrors();
  limparErrosDoDialogo();
  limparCriacao();
  dialogCriacaoAberto.value = true;
}

function textoOuNull(valor?: string | null) {
  const texto = String(valor ?? "").trim();
  return texto || null;
}

function dataParaEnvio(data: Date | null) {
  if (!data) return null;

  const ano = data.getFullYear();
  const mes = data.getMonth();
  const dia = data.getDate();

  return new Date(Date.UTC(ano, mes, dia, 12, 0, 0)).toISOString();
}

function formatarData(valor?: string | null) {
  if (!valor) return "-";

  const data = new Date(valor);
  if (Number.isNaN(data.getTime())) return "-";

  return data.toLocaleDateString("pt-BR");
}

function rotuloStatus(ativo: boolean) {
  return ativo ? "Ativa" : "Encerrada";
}

function severityStatus(ativo: boolean) {
  return ativo ? "success" : "danger";
}

function severityFuncao(funcao: string) {
  const valor = String(funcao || "").toLowerCase();

  if (valor === "professor") return "info";
  if (valor === "auxiliar") return "contrast";

  // Valor gravado antes da lista fechada (RF21): só exibido, até ser editado ou encerrado.
  return "secondary";
}

function validarCriacao(): boolean {
  limparErrosDoDialogo();
  if (!formularioCriacao.pessoaId) {
    errosLocais.PessoaId = "Selecione a pessoa para a atribuição.";
  }
  if (!formularioCriacao.funcao.trim()) {
    errosLocais.Funcao = "Informe a função da atribuição.";
  }
  return Object.keys(errosLocais).length === 0;
}

function validarEdicao(): boolean {
  limparErrosDoDialogo();
  if (!formularioEdicao.funcao.trim()) {
    errosLocais.Funcao = "Informe a função da atribuição.";
  }
  return Object.keys(errosLocais).length === 0;
}

const pessoasOrdenadas = computed(() => {
  return [...pessoas.value].sort((a, b) =>
    a.nome.localeCompare(b.nome, "pt-BR"),
  );
});

async function carregarCabecalho() {
  await run(async () => {
    const [dep, listaPessoas] = await Promise.all([
      obterDepartamento(departamentoId),
      listarPessoas(),
    ]);

    departamento.value = dep;
    pessoas.value = listaPessoas;
  }, "Não foi possível carregar os dados da turma.");
}

async function carregarAtribuicoes() {
  await run(async () => {
    atribuicoes.value = await listarAtribuicoesPorDepartamento(departamentoId, {
      funcao: textoOuNull(filtroFuncao.value),
      ativo:
        filtroAtivo.value === "true"
          ? true
          : filtroAtivo.value === "false"
            ? false
            : null,
    });
  }, "Não foi possível carregar as atribuições.");
}

async function carregarTela() {
  await carregarCabecalho();
  await carregarAtribuicoes();
}

async function abrirEdicao(item: AtribuicaoVM) {
  clearErrors();
  limparErrosDoDialogo();

  const atribuicao = await run(
    async () => await obterAtribuicao(item.id),
    "Não foi possível carregar a atribuição.",
  );

  if (!atribuicao) return;

  editandoId.value = atribuicao.id;
  formularioEdicao.funcao = atribuicao.funcao || "Professor";
  formularioEdicao.ativo = Boolean(atribuicao.ativo);
  formularioEdicao.dataFim = atribuicao.dataFim
    ? new Date(atribuicao.dataFim)
    : null;

  dialogEdicaoAberto.value = true;
}

async function salvarCriacao() {
  if (!validarCriacao()) return;

  await executarNoDialogo(async () => {
    await criarAtribuicao({
      PessoaId: Number(formularioCriacao.pessoaId),
      DepartamentoId: departamentoId,
      Funcao: formularioCriacao.funcao.trim(),
      DataInicio: dataParaEnvio(formularioCriacao.dataInicio),
      Ativo: Boolean(formularioCriacao.ativo),
    });

    toastSuccess("Atribuição criada com sucesso.", "Criado");
    dialogCriacaoAberto.value = false;
    await carregarAtribuicoes();
  }, "Não foi possível criar a atribuição.");
}

async function salvarEdicao() {
  if (!validarEdicao()) return;

  const id = editandoId.value;
  if (!id) return;

  await executarNoDialogo(async () => {
    await atualizarAtribuicao(id, {
      Funcao: formularioEdicao.funcao.trim(),
      Ativo: Boolean(formularioEdicao.ativo),
      DataFim: dataParaEnvio(formularioEdicao.dataFim),
    });

    toastSuccess("Atribuição atualizada com sucesso.", "Salvo");
    dialogEdicaoAberto.value = false;
    limparEdicao();
    await carregarAtribuicoes();
  }, "Não foi possível atualizar a atribuição.");
}

function confirmarEncerramento(item: AtribuicaoVM) {
  confirm.require({
    header: "Encerrar atribuição",
    message: `Deseja encerrar a atribuição "${item.funcao}" de "${item.pessoaNome}"?`,
    icon: "pi pi-exclamation-triangle",
    acceptLabel: "Encerrar",
    rejectLabel: "Cancelar",
    accept: async () => {
      await run(async () => {
        await encerrarAtribuicao(item.id);
        toastSuccess("Atribuição encerrada com sucesso.", "Concluído");
        await carregarAtribuicoes();
      }, "Não foi possível encerrar a atribuição.");
    },
  });
}

function limparFiltros() {
  busca.value = "";
  filtroFuncao.value = null;
  filtroAtivo.value = FILTRO_ATIVO_PADRAO;
  carregarAtribuicoes();
}

onMounted(carregarTela);
</script>

<template>
  <div class="page-container">
    <PageHeader
      :titulo="`Atribuições - ${departamento?.nome ?? 'Turma'}`"
      subtitulo="Gestão de professores e auxiliares da turma"
    >
      <template #acoes>
        <Button
          v-if="podeGerenciarAtribuicao"
          label="Nova Atribuição"
          icon="pi pi-plus"
          @click="abrirNovo"
        />
        <Button
          label="Recarregar"
          icon="pi pi-refresh"
          severity="secondary"
          :loading="carregando"
          @click="carregarTela"
        />
      </template>
    </PageHeader>

    <InlineMessage :texto="erro" tipo="erro" />

    <div class="atribuicoes-filtros">
      <div style="display: flex; flex-direction: column; gap: 6px">
        <label for="atribuicao-filtro-busca">Buscar</label>
        <InputText
          id="atribuicao-filtro-busca"
          v-model="busca"
          placeholder="Pesquisar por pessoa ou função..."
        />
      </div>

      <div style="display: flex; flex-direction: column; gap: 6px">
        <label id="atribuicao-filtro-funcao-rotulo" for="atribuicao-filtro-funcao">Função</label>
        <Dropdown
          v-model="filtroFuncao"
          labelId="atribuicao-filtro-funcao"
          ariaLabelledby="atribuicao-filtro-funcao-rotulo"
          :options="opcoesFuncao"
          optionLabel="label"
          optionValue="value"
          showClear
          placeholder="Todas"
        />
      </div>

      <div style="display: flex; flex-direction: column; gap: 6px">
        <label id="atribuicao-filtro-status-rotulo" for="atribuicao-filtro-status">Status</label>
        <Dropdown
          v-model="filtroAtivo"
          labelId="atribuicao-filtro-status"
          ariaLabelledby="atribuicao-filtro-status-rotulo"
          :options="opcoesStatus"
          optionLabel="label"
          optionValue="value"
          showClear
          placeholder="Todos"
        />
      </div>

      <div class="atribuicoes-filtros-acoes">
        <Button
          label="Aplicar filtros"
          icon="pi pi-filter"
          severity="secondary"
          :loading="carregando"
          data-testid="atribuicoes-aplicar-filtros"
          @click="carregarAtribuicoes"
        />

        <Button
          label="Limpar"
          icon="pi pi-times"
          severity="secondary"
          :disabled="carregando"
          data-testid="atribuicoes-limpar-filtros"
          @click="limparFiltros"
        />
      </div>
    </div>

    <LoadingOverlay :loading="carregando" texto="Carregando atribuições...">
      <DataTable
        :value="atribuicoesFiltradas"
        paginator
        :rows="10"
        rowHover
        sortField="pessoaNome"
        :sortOrder="1"
        dataKey="id"
        responsiveLayout="scroll"
      >
        <Column field="pessoaNome" header="Pessoa" sortable style="min-width: 240px">
          <template #body="{ data }">
            <div class="atribuicao-pessoa">
              <span>{{ data.pessoaNome }}</span>
              <Tag
                v-if="avisoConta(data, compacta)"
                :value="avisoConta(data, compacta)"
                severity="warn"
                icon="pi pi-exclamation-circle"
                data-testid="atribuicao-aviso-conta"
              />
            </div>
          </template>
        </Column>

        <Column header="Função" style="width: 160px">
          <template #body="{ data }">
            <Tag :value="data.funcao" :severity="severityFuncao(data.funcao)" />
          </template>
        </Column>

        <Column header="Início" style="width: 120px">
          <template #body="{ data }">
            {{ formatarData(data.dataInicio) }}
          </template>
        </Column>

        <Column header="Fim" style="width: 120px">
          <template #body="{ data }">
            {{ formatarData(data.dataFim) }}
          </template>
        </Column>

        <Column header="Status" style="width: 130px">
          <template #body="{ data }">
            <Tag
              :value="rotuloStatus(data.ativo)"
              :severity="severityStatus(data.ativo)"
            />
          </template>
        </Column>

        <Column
          v-if="podeGerenciarAtribuicao"
          header="Ações"
          style="width: 180px"
        >
          <template #body="{ data }">
            <div style="display: flex; gap: 8px">
              <Button
                icon="pi pi-pencil"
                severity="secondary"
                v-tooltip.top="'Editar atribuição'"
                :disabled="carregando"
                @click="abrirEdicao(data)"
              />
              <Button
                icon="pi pi-ban"
                severity="danger"
                v-tooltip.top="
                  data.ativo ? 'Encerrar atribuição' : 'Atribuição já encerrada'
                "
                :disabled="carregando || !data.ativo"
                @click="confirmarEncerramento(data)"
              />
            </div>
          </template>
        </Column>
        <template #empty>
          <div style="padding: 14px; opacity: 0.7">
            Nenhuma atribuição encontrada para os filtros aplicados.
          </div>
        </template>
      </DataTable>
    </LoadingOverlay>

    <Dialog
      v-model:visible="dialogCriacaoAberto"
      modal
      header="Nova atribuição"
      :closable="!salvando"
      :dismissableMask="!salvando"
      style="width: 680px; max-width: 96vw"
    >
      <div class="page-container">
        <InlineMessage
          :texto="avisoCriacao"
          tipo="erro"
          data-testid="atribuicao-criacao-erro"
        />

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label id="atribuicao-criacao-pessoa-rotulo" for="atribuicao-criacao-pessoa">Pessoa *</label>
          <Dropdown
            v-model="formularioCriacao.pessoaId"
            labelId="atribuicao-criacao-pessoa"
            ariaLabelledby="atribuicao-criacao-pessoa-rotulo"
            :options="pessoasOrdenadas"
            optionLabel="nome"
            optionValue="id"
            filter
            showClear
            placeholder="Selecione a pessoa"
          />
          <FieldError
            data-testid="atribuicao-criacao-pessoa-erro"
            :texto="erroDoCampo('PessoaId', 'pessoaId')"
          />
        </div>

        <div class="atribuicao-form-linha atribuicao-form-linha--tres">
          <div style="display: flex; flex-direction: column; gap: 6px">
            <label for="atribuicao-criacao-funcao">Função *</label>
            <Dropdown
              v-model="formularioCriacao.funcao"
              labelId="atribuicao-criacao-funcao"
              :options="opcoesFuncao"
              optionLabel="label"
              optionValue="value"
              placeholder="Selecione"
            />
            <FieldError :texto="erroDoCampo('Funcao', 'funcao')" />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label for="atribuicao-criacao-data-inicio">Data de início</label>
            <Calendar
              v-model="formularioCriacao.dataInicio"
              inputId="atribuicao-criacao-data-inicio"
              dateFormat="dd/mm/yy"
              showIcon
              iconDisplay="input"
              showButtonBar
            />
            <FieldError :texto="erroDoCampo('DataInicio', 'dataInicio')" />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label id="atribuicao-criacao-status-rotulo" for="atribuicao-criacao-status">Status inicial</label>
            <Dropdown
              v-model="formularioCriacao.ativo"
              labelId="atribuicao-criacao-status"
              ariaLabelledby="atribuicao-criacao-status-rotulo"
              :options="[
                { label: 'Ativo', value: true },
                { label: 'Inativo', value: false },
              ]"
              optionLabel="label"
              optionValue="value"
            />
            <FieldError :texto="erroDoCampo('Ativo', 'ativo')" />
          </div>
        </div>
      </div>

      <template #footer>
        <Button
          label="Cancelar"
          severity="secondary"
          :disabled="salvando"
          data-testid="atribuicao-criacao-cancelar"
          @click="dialogCriacaoAberto = false"
        />
        <Button
          label="Salvar"
          icon="pi pi-check"
          :loading="salvando"
          data-testid="atribuicao-criacao-salvar"
          @click="salvarCriacao"
        />
      </template>
    </Dialog>

    <Dialog
      v-model:visible="dialogEdicaoAberto"
      modal
      header="Editar atribuição"
      :closable="!salvando"
      :dismissableMask="!salvando"
      style="width: 680px; max-width: 96vw"
    >
      <div class="page-container">
        <InlineMessage
          :texto="avisoEdicao"
          tipo="erro"
          data-testid="atribuicao-edicao-erro"
        />

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label for="atribuicao-edicao-funcao">Função *</label>
          <Dropdown
            v-model="formularioEdicao.funcao"
            labelId="atribuicao-edicao-funcao"
            :options="opcoesFuncao"
            optionLabel="label"
            optionValue="value"
            placeholder="Selecione"
          />
          <FieldError :texto="erroDoCampo('Funcao', 'funcao')" />
        </div>

        <div class="atribuicao-form-linha atribuicao-form-linha--duas">
          <div style="display: flex; flex-direction: column; gap: 6px">
            <label id="atribuicao-edicao-status-rotulo" for="atribuicao-edicao-status">Status</label>
            <Dropdown
              v-model="formularioEdicao.ativo"
              labelId="atribuicao-edicao-status"
              ariaLabelledby="atribuicao-edicao-status-rotulo"
              :options="[
                { label: 'Ativo', value: true },
                { label: 'Inativo', value: false },
              ]"
              optionLabel="label"
              optionValue="value"
            />
            <FieldError :texto="erroDoCampo('Ativo', 'ativo')" />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label for="atribuicao-edicao-data-fim">Data fim</label>
            <Calendar
              v-model="formularioEdicao.dataFim"
              inputId="atribuicao-edicao-data-fim"
              dateFormat="dd/mm/yy"
              showIcon
              iconDisplay="input"
              showButtonBar
            />
            <FieldError :texto="erroDoCampo('DataFim', 'dataFim')" />
          </div>
        </div>

        <InlineMessage
          texto="Se você marcar como inativa e não informar a data fim, o sistema preenche a data fim automaticamente."
          tipo="info"
        />
      </div>

      <template #footer>
        <Button
          label="Cancelar"
          severity="secondary"
          :disabled="salvando"
          data-testid="atribuicao-edicao-cancelar"
          @click="dialogEdicaoAberto = false"
        />
        <Button
          label="Salvar"
          icon="pi pi-check"
          :loading="salvando"
          data-testid="atribuicao-edicao-salvar"
          @click="salvarEdicao"
        />
      </template>
    </Dialog>
  </div>
</template>

<style scoped>
/* Filtros (Buscar · Função · Status · botões) e linhas dos diálogos. Abaixo de 576 px os
   campos empilham numa coluna: a 360 px as colunas fixas estouravam a largura do diálogo
   ("Status inicial" cortado) e a dos filtros (Dropdown "Todas" cortado na borda).
   Entrega G: até 1280 px os botões Aplicar filtros e Limpar vão para uma linha própria
   (ao lado dos campos, a coluna `auto` dos botões cresce antes da faixa `fr` da busca:
   a 1024 px o Buscar ficava com 26 px e, com o ponto de quebra em 1100 px, com ~90 px
   logo acima dele; a partir de 1280 px a busca tem mais de 250 px). */
.atribuicoes-filtros {
  display: grid;
  grid-template-columns: minmax(0, 1.4fr) 220px 180px auto;
  gap: 10px;
  align-items: end;
}

.atribuicoes-filtros > * {
  min-width: 0;
}

.atribuicoes-filtros-acoes {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

@media (max-width: 1279.98px) {
  .atribuicoes-filtros {
    grid-template-columns: minmax(0, 1fr) 200px 160px;
  }

  .atribuicoes-filtros-acoes {
    grid-column: 1 / -1;
  }
}

.atribuicao-form-linha {
  display: grid;
  gap: 12px;
  align-items: start;
}

.atribuicao-form-linha--tres {
  grid-template-columns: 1fr 180px 140px;
}

.atribuicao-form-linha--duas {
  grid-template-columns: 180px 1fr;
}

@media (max-width: 575.98px) {
  .atribuicoes-filtros,
  .atribuicao-form-linha--tres,
  .atribuicao-form-linha--duas {
    grid-template-columns: minmax(0, 1fr);
  }

  .atribuicoes-filtros > *,
  .atribuicao-form-linha > * {
    min-width: 0;
  }

  .atribuicoes-filtros :deep(.p-inputtext),
  .atribuicoes-filtros :deep(.p-select),
  .atribuicao-form-linha :deep(.p-select),
  .atribuicao-form-linha :deep(.p-datepicker),
  .atribuicao-form-linha :deep(.p-inputtext) {
    width: 100%;
  }
}

.atribuicao-pessoa {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px 10px;
}

/* A tag ocupa a largura da coluna e quebra o texto em linhas inteiras (no máximo
   duas a 360 px, com o texto curto), em vez de uma palavra por linha. */
.atribuicao-pessoa :deep(.p-tag) {
  max-width: 100%;
  white-space: normal;
  text-align: left;
  line-height: 1.25;
}
</style>
