<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import { useRoute } from "vue-router";


import { usarAutenticacaoStore } from "../../../aplicacao/armazenamentos/autenticacaoStore";

import PageHeader from "../../../components/ui/PageHeader.vue";
import InlineMessage from "../../../components/ui/InlineMessage.vue";
import LoadingOverlay from "../../../components/ui/LoadingOverplay.vue";
import FieldError from "../../../components/ui/FieldError.vue";


import { useAsync } from "../../../aplicacao/composables/useAsync";


import { toastSuccess } from "../../../aplicacao/servicos/notificacoes";

import { firstFieldError } from "../../../aplicacao/servicos/apiError";

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
import Calendar from "primevue/calendar";
import Tag from "primevue/tag";

import { useConfirm } from "primevue/useconfirm";

const autenticacao = usarAutenticacaoStore();
const route = useRoute();
const confirm = useConfirm();

const { carregando, erro, fieldErrors, run, clearErrors } = useAsync();

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
  if (valor === "auxiliar") return "help";
  if (valor === "lider") return "warning";

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
    erro.value = msg;
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
    erro.value = msg;
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
    acceptClass: "p-button-danger",
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
      voltarPara="/departamentos"
      voltarLabel="Turmas EBD"
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
        grid-template-columns: 220px 180px auto auto;
        gap: 10px;
        align-items: end;
      "
    >
      <div style="display: flex; flex-direction: column; gap: 6px">
        <label>Função</label>
        <Dropdown
          v-model="filtroFuncao"
          :options="opcoesFuncao"
          optionLabel="label"
          optionValue="value"
          showClear
          placeholder="Todas"
        />
      </div>

      <div style="display: flex; flex-direction: column; gap: 6px">
        <label>Status</label>
        <Dropdown
          v-model="filtroAtivo"
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
        :value="atribuicoes"
        paginator
        :rows="10"
        rowHover
        sortField="pessoaNome"
        :sortOrder="1"
        dataKey="id"
        responsiveLayout="scroll"
      >
        <Column field="pessoaNome" header="Pessoa" sortable />

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
          <label>Pessoa *</label>
          <Dropdown
            v-model="formularioCriacao.pessoaId"
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
            <label>Função *</label>
            <Dropdown
              v-model="formularioCriacao.funcao"
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
            <label>Data de início</label>
            <Calendar
              v-model="formularioCriacao.dataInicio"
              dateFormat="dd/mm/yy"
              showIcon
              iconDisplay="input"
              showButtonBar
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label>Status inicial</label>
            <Dropdown
              v-model="formularioCriacao.ativo"
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
          <label>Função *</label>
          <Dropdown
            v-model="formularioEdicao.funcao"
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
            <label>Status</label>
            <Dropdown
              v-model="formularioEdicao.ativo"
              :options="[
                { label: 'Ativo', value: true },
                { label: 'Inativo', value: false },
              ]"
              optionLabel="label"
              optionValue="value"
            />
          </div>

          <div style="display: flex; flex-direction: column; gap: 6px">
            <label>Data fim</label>
            <Calendar
              v-model="formularioEdicao.dataFim"
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
