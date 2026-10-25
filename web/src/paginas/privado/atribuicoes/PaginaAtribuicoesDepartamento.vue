<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import { useRoute } from "vue-router";

import { usarAutenticacaoStore } from "../../../aplicacao/armazenamentos/autenticacaoStore";

import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";
import FieldError from "../../../components/ui/FieldError.vue";

import { useAsync } from "../../../aplicacao/composables/useAsync";

import {
  toastSuccess,
  toastWarn,
} from "../../../aplicacao/servicos/notificacoes";

import { firstFieldError } from "../../../aplicacao/servicos/apiError";

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

const { carregando, erro, fieldErrors, run, clearErrors } = useAsync();
const compacta = usarTelaCompacta();

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
const filtroAtivo = ref<string | null>(null);
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
  limparCriacao();
  dialogCriacaoAberto.value = true;
}

function textoOuNull(valor?: string | null) {
  const texto = String(valor ?? "").trim();
  return texto ? texto : null;
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
  if (valor === "lider") return "warn";

  return "secondary";
}

function validarCriacao(): string {
  if (!formularioCriacao.pessoaId) {
    return "Selecione a pessoa para a atribuição.";
  }

  if (!formularioCriacao.funcao.trim()) {
    return "Informe a função da atribuição.";
  }

  return "";
}

function validarEdicao(): string {
  if (!formularioEdicao.funcao.trim()) {
    return "Informe a função da atribuição.";
  }

  return "";
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
  const msg = validarCriacao();
  if (msg) {
    toastWarn(msg);
    return;
  }

  await run(async () => {
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
  const msg = validarEdicao();
  if (msg) {
    toastWarn(msg);
    return;
  }

  const id = editandoId.value;
  if (!id) return;

  await run(async () => {
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
  filtroFuncao.value = null;
  filtroAtivo.value = null;
  carregarAtribuicoes();
}

onMounted(carregarTela);
</script>

<template>
  <div class="page-container">
    <PageHeader
      :titulo="`Atribuições - ${departamento?.nome ?? 'Turma'}`"
      subtitulo="Gestão de professores, auxiliares, líderes e demais funções da turma"
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

    <div
      style="
        display: grid;
        grid-template-columns: 1.4fr 220px 180px auto auto;
        gap: 10px;
        align-items: end;
      "
    >
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

      <Button
        label="Aplicar filtros"
        icon="pi pi-filter"
        severity="secondary"
        :loading="carregando"
        @click="carregarAtribuicoes"
      />

      <Button
        label="Limpar"
        icon="pi pi-times"
        severity="secondary"
        :disabled="carregando"
        @click="limparFiltros"
      />
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
      :closable="!carregando"
      :dismissableMask="!carregando"
      style="width: 680px; max-width: 96vw"
    >
      <div class="page-container">
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
            :texto="
              firstFieldError(fieldErrors, 'PessoaId') ||
              firstFieldError(fieldErrors, 'pessoaId')
            "
          />
        </div>

        <div
          style="
            display: grid;
            grid-template-columns: 1fr 180px 140px;
            gap: 12px;
            align-items: start;
          "
        >
          <div style="display: flex; flex-direction: column; gap: 6px">
            <label for="atribuicao-criacao-funcao">Função *</label>
            <Dropdown
              v-model="formularioCriacao.funcao"
              labelId="atribuicao-criacao-funcao"
              :options="opcoesFuncao"
              optionLabel="label"
              optionValue="value"
              editable
              placeholder="Selecione ou digite"
            />
            <FieldError
              :texto="
                firstFieldError(fieldErrors, 'Funcao') ||
                firstFieldError(fieldErrors, 'funcao')
              "
            />
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
          </div>
        </div>
      </div>

      <template #footer>
        <Button
          label="Cancelar"
          severity="secondary"
          :disabled="carregando"
          @click="dialogCriacaoAberto = false"
        />
        <Button
          label="Salvar"
          icon="pi pi-check"
          :loading="carregando"
          @click="salvarCriacao"
        />
      </template>
    </Dialog>

    <Dialog
      v-model:visible="dialogEdicaoAberto"
      modal
      header="Editar atribuição"
      :closable="!carregando"
      :dismissableMask="!carregando"
      style="width: 680px; max-width: 96vw"
    >
      <div class="page-container">
        <div style="display: flex; flex-direction: column; gap: 6px">
          <label for="atribuicao-edicao-funcao">Função *</label>
          <Dropdown
            v-model="formularioEdicao.funcao"
            labelId="atribuicao-edicao-funcao"
            :options="opcoesFuncao"
            optionLabel="label"
            optionValue="value"
            editable
            placeholder="Selecione ou digite"
          />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Funcao') ||
              firstFieldError(fieldErrors, 'funcao')
            "
          />
        </div>

        <div style="display: grid; grid-template-columns: 180px 1fr; gap: 12px">
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
          </div>
        </div>

        <InlineMessage
          texto="Se você marcar como inativa e não informar a data fim, o backend preencherá automaticamente."
          tipo="info"
        />
      </div>

      <template #footer>
        <Button
          label="Cancelar"
          severity="secondary"
          :disabled="carregando"
          @click="dialogEdicaoAberto = false"
        />
        <Button
          label="Salvar"
          icon="pi pi-check"
          :loading="carregando"
          @click="salvarEdicao"
        />
      </template>
    </Dialog>
  </div>
</template>

<style scoped>
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
