<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import { useRoute, useRouter } from "vue-router";

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

import DataTable from "primevue/datatable";
import Column from "primevue/column";
import Button from "primevue/button";
import Dialog from "primevue/dialog";
import Dropdown from "primevue/dropdown";
import InputText from "primevue/inputtext";
import Calendar from "primevue/calendar";

import { useConfirm } from "primevue/useconfirm";

import { obterDepartamento } from "../../../aplicacao/servicos/departamentosServico";
import { listarMaterias } from "../../../aplicacao/servicos/materiasServico";
import { listarAtribuicoesPorDepartamento } from "../../../aplicacao/servicos/atribuicoesServico";
import {
  listarAulasPorDepartamento,
  criarAula,
  consolidarAula,
} from "../../../aplicacao/servicos/aulasServico";

import type {
  AulaVM,
  DepartamentoVM,
  MateriaVM,
} from "../../../aplicacao/modelos/dtos";

const route = useRoute();
const router = useRouter();
const confirm = useConfirm();

const { carregando, erro, fieldErrors, run, clearErrors } = useAsync();

const departamentoId = computed(() => Number(route.params.departamentoId));

const turma = ref<DepartamentoVM | null>(null);
const aulas = ref<AulaVM[]>([]);
const materias = ref<MateriaVM[]>([]);
const professores = ref<{ id: number; nome: string }[]>([]);

const dialogAberto = ref(false);

const form = reactive({
  data: null as Date | null,
  tema: "",
  materiaId: null as number | null,
  professorId: null as number | null,
});

function abrirChamada(aula: AulaVM) {
  router.push(`/aulas/${aula.id}/chamada`);
}

function abrirPresencas(aula: AulaVM) {
  router.push(`/aulas/${aula.id}/presencas`);
}

function limparForm() {
  form.data = null;
  form.tema = "";
  form.materiaId = null;
  form.professorId = null;
}

function abrirNovo() {
  clearErrors();
  limparForm();
  dialogAberto.value = true;
}

async function carregarTudo() {
  await run(async () => {
    if (!departamentoId.value) throw new Error("Departamento inválido.");

    turma.value = await obterDepartamento(departamentoId.value);

    const [listaAulas, listaMaterias, listaAtribuicoes] = await Promise.all([
      listarAulasPorDepartamento(departamentoId.value),
      listarMaterias(departamentoId.value),
      listarAtribuicoesPorDepartamento(departamentoId.value, {
        funcao: "Professor",
        ativo: true,
      }),
    ]);

    aulas.value = listaAulas;
    materias.value = listaMaterias;
    professores.value = listaAtribuicoes.map((a) => ({
      id: a.pessoaId,
      nome: a.pessoaNome,
    }));
  }, "Não foi possível carregar as aulas.");
}

function validarRapido(): string {
  if (!form.data) return "Informe a data da aula.";
  if (!form.materiaId) return "Selecione a matéria.";
  if (!form.professorId) return "Selecione o professor.";
  return "";
}

async function salvar() {
  const msg = validarRapido();
  if (msg) {
    toastWarn(msg);
    return;
  }

  await run(async () => {
    const dataIso = (form.data as Date).toISOString();

    await criarAula({
      Data: dataIso,
      Tema: form.tema?.trim() || null,
      MateriaId: form.materiaId as number,
      ProfessorId: form.professorId as number,
    });

    toastSuccess("Aula criada com sucesso.", "Criada");

    dialogAberto.value = false;
    aulas.value = await listarAulasPorDepartamento(departamentoId.value);
  }, "Não foi possível criar a aula.");
}

function confirmarConsolidar(aula: AulaVM) {
  if (aula.consolidada) return;

  confirm.require({
    header: "Consolidar aula",
    message:
      "Ao consolidar, a chamada não poderá mais ser alterada. Deseja continuar?",
    icon: "pi pi-exclamation-triangle",
    acceptLabel: "Consolidar",
    rejectLabel: "Cancelar",
    acceptClass: "p-button-danger",
    accept: async () => {
      await run(async () => {
        await consolidarAula(aula.id);

        toastSuccess("Aula consolidada com sucesso.", "Consolidada");

        aulas.value = await listarAulasPorDepartamento(departamentoId.value);
      }, "Não foi possível consolidar a aula.");
    },
  });
}

function formatarData(iso: string) {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleDateString();
}

onMounted(carregarTudo);
</script>

<template>
  <div class="page-container">
    <PageHeader
      :titulo="turma ? `Aulas — ${turma.nome}` : 'Aulas'"
      subtitulo="Cadastre e gerencie as aulas desta turma"
      voltarPara="/departamentos"
      voltarLabel="Turmas EBD"
    >
      <template #acoes>
        <Button label="Nova Aula" icon="pi pi-plus" @click="abrirNovo" />
        <Button
          label="Recarregar"
          icon="pi pi-refresh"
          severity="secondary"
          :loading="carregando"
          @click="carregarTudo"
        />
      </template>
    </PageHeader>

    <InlineMessage :texto="erro" tipo="erro" />

    <LoadingOverlay :loading="carregando" texto="Carregando aulas.">
      <DataTable
        :value="aulas"
        paginator
        :rows="10"
        rowHover
        sortField="data"
        :sortOrder="-1"
        dataKey="id"
        responsiveLayout="scroll"
      >
        <Column header="Data" style="width: 130px" sortable>
          <template #body="{ data }">
            {{ formatarData(data.data) }}
          </template>
        </Column>

        <Column field="nomeMateria" header="Matéria" sortable />
        <Column field="nomeProfessor" header="Professor" sortable />
        <Column field="tema" header="Tema" />

        <Column header="Consolidada" style="width: 140px">
          <template #body="{ data }">
            {{ data.consolidada ? "Sim" : "Não" }}
          </template>
        </Column>

        <Column header="Ações" style="width: 320px">
          <template #body="{ data }">
            <div style="display: flex; gap: 8px">
              <Button
                icon="pi pi-clipboard"
                severity="info"
                v-tooltip.top="'Fazer chamada'"
                :disabled="carregando"
                @click="abrirChamada(data)"
              />
              <Button
                icon="pi pi-list-check"
                severity="help"
                v-tooltip.top="'Ver presenças registradas'"
                :disabled="carregando"
                @click="abrirPresencas(data)"
              />
              <Button
                icon="pi pi-lock"
                severity="danger"
                v-tooltip.top="
                  data.consolidada
                    ? 'Aula já consolidada'
                    : 'Consolidar chamada (impede alterações futuras)'
                "
                :disabled="carregando || data.consolidada"
                @click="confirmarConsolidar(data)"
              />
            </div>
          </template>
        </Column>
      </DataTable>
    </LoadingOverlay>

    <Dialog
      v-model:visible="dialogAberto"
      modal
      :closable="!carregando"
      :dismissableMask="!carregando"
      header="Nova aula"
      style="width: 600px; max-width: 92vw"
    >
      <div class="page-container">
        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Data</label>
          <Calendar v-model="form.data" dateFormat="dd/mm/yy" showIcon />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Data') ||
              firstFieldError(fieldErrors, 'data')
            "
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Matéria</label>
          <Dropdown
            v-model="form.materiaId"
            :options="materias"
            optionLabel="nome"
            optionValue="id"
            filter
            placeholder="Selecione a matéria."
          />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'MateriaId') ||
              firstFieldError(fieldErrors, 'materiaId')
            "
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Professor</label>
          <Dropdown
            v-model="form.professorId"
            :options="professores"
            optionLabel="nome"
            optionValue="id"
            filter
            placeholder="Selecione o professor."
          />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'ProfessorId') ||
              firstFieldError(fieldErrors, 'professorId')
            "
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Tema (opcional)</label>
          <InputText v-model="form.tema" placeholder="Ex.: Fé, Salvação." />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Tema') ||
              firstFieldError(fieldErrors, 'tema')
            "
          />
        </div>

        <FieldError
          :texto="
            firstFieldError(fieldErrors, 'DepartamentoId') ||
            firstFieldError(fieldErrors, 'departamentoId')
          "
        />
      </div>

      <template #footer>
        <Button
          label="Cancelar"
          severity="secondary"
          :disabled="carregando"
          @click="dialogAberto = false"
        />
        <Button
          label="Salvar"
          icon="pi pi-check"
          :loading="carregando"
          @click="salvar"
        />
      </template>
    </Dialog>
  </div>
</template>
