<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import { useRouter } from "vue-router";

import { usarAutenticacaoStore } from "../../../aplicacao/armazenamentos/autenticacaoStore";

import type { DepartamentoVM } from "../../../aplicacao/modelos/dtos";
import {
  listarDepartamentos,
  criarDepartamento,
  atualizarDepartamento,
} from "../../../aplicacao/servicos/departamentosServico";

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
import InputText from "primevue/inputtext";
import Checkbox from "primevue/checkbox";
import Menu from "primevue/menu";
import SelectButton from "primevue/selectbutton";
import Tag from "primevue/tag";

import { useConfirm } from "primevue/useconfirm";

const autenticacao = usarAutenticacaoStore();
const router = useRouter();
const confirm = useConfirm();

const { carregando, erro, fieldErrors, run, clearErrors } = useAsync();

const podeGerenciarTurma = computed(() => {
  const perfil = (autenticacao.perfil || "").trim().toLowerCase();
  return (
    perfil === "admin" || perfil === "pastor" || perfil === "superintendente"
  );
});

const departamentos = ref<DepartamentoVM[]>([]);
const dialogAberto = ref(false);
const editandoId = ref<number | null>(null);

const filtroStatus = ref<"ativas" | "inativas" | "todas">("ativas");
const busca = ref("");

const departamentosFiltrados = computed(() => {
  const termo = busca.value.trim().toLowerCase();

  return departamentos.value.filter((d) => {
    const bateStatus =
      filtroStatus.value === "todas" ||
      (filtroStatus.value === "ativas" ? d.ativo : !d.ativo);

    const bateBusca =
      !termo ||
      d.nome.toLowerCase().includes(termo) ||
      String(d.tipo ?? "")
        .toLowerCase()
        .includes(termo);

    return bateStatus && bateBusca;
  });
});

const formulario = reactive({
  nome: "",
  tipo: "EBD",
  ativo: true,
});

const menuAcoes = ref();
const turmaMenuSelecionada = ref<DepartamentoVM | null>(null);

const itensMenu = computed(() => [
  {
    label: "Alunos",
    icon: "pi pi-users",
    command: () =>
      turmaMenuSelecionada.value && abrirMatriculas(turmaMenuSelecionada.value),
  },
  {
    label: "Matérias",
    icon: "pi pi-book",
    command: () =>
      turmaMenuSelecionada.value && abrirMaterias(turmaMenuSelecionada.value),
  },
  {
    label: "Aulas",
    icon: "pi pi-calendar",
    command: () =>
      turmaMenuSelecionada.value && abrirAulas(turmaMenuSelecionada.value),
  },
  {
    label: "Atribuições",
    icon: "pi pi-id-card",
    command: () =>
      turmaMenuSelecionada.value &&
      abrirAtribuicoes(turmaMenuSelecionada.value),
  },
]);

function abrirMenu(event: Event, dep: DepartamentoVM) {
  turmaMenuSelecionada.value = dep;
  menuAcoes.value.toggle(event);
}

function abrirMatriculas(dep: DepartamentoVM) {
  router.push(`/departamentos/${dep.id}/matriculas`);
}
function abrirMaterias(dep: DepartamentoVM) {
  router.push(`/departamentos/${dep.id}/materias`);
}
function abrirAulas(dep: DepartamentoVM) {
  router.push(`/departamentos/${dep.id}/aulas`);
}
function abrirAtribuicoes(dep: DepartamentoVM) {
  router.push(`/departamentos/${dep.id}/atribuicoes`);
}

function limparFormulario() {
  formulario.nome = "";
  formulario.tipo = "EBD";
  formulario.ativo = true;
  editandoId.value = null;
}

function abrirNovo() {
  clearErrors();
  limparFormulario();
  dialogAberto.value = true;
}

function abrirEdicao(dep: DepartamentoVM) {
  clearErrors();
  editandoId.value = dep.id;
  formulario.nome = dep.nome ?? "";
  formulario.tipo = dep.tipo ?? "EBD";
  formulario.ativo = Boolean(dep.ativo);
  dialogAberto.value = true;
}

async function carregarLista() {
  await run(async () => {
    departamentos.value = await listarDepartamentos();
  }, "Não foi possível carregar os departamentos.");
}

async function salvar() {
  if (!formulario.nome.trim()) {
    toastWarn("Informe o nome do departamento/turma.");
    return;
  }

  const payload = {
    Nome: formulario.nome.trim(),
    Tipo: (formulario.tipo || "EBD").trim(),
    Ativo: Boolean(formulario.ativo),
  };

  await run(async () => {
    if (editandoId.value === null) {
      await criarDepartamento(payload);
      toastSuccess("Turma criada com sucesso.", "Criado");
    } else {
      await atualizarDepartamento(editandoId.value, payload);
      toastSuccess("Turma atualizada com sucesso.", "Salvo");
    }

    dialogAberto.value = false;
    await carregarLista();
  }, "Não foi possível salvar o departamento.");
}

function confirmarAlternarAtivo(dep: DepartamentoVM) {
  const novoAtivo = !dep.ativo;

  confirm.require({
    header: novoAtivo ? "Ativar turma" : "Inativar turma",
    message: novoAtivo
      ? `Deseja ativar a turma "${dep.nome}"?`
      : `Deseja inativar a turma "${dep.nome}"?`,
    icon: "pi pi-exclamation-triangle",
    acceptLabel: novoAtivo ? "Ativar" : "Inativar",
    rejectLabel: "Cancelar",
    acceptClass: novoAtivo ? "p-button-success" : "p-button-danger",
    accept: async () => {
      await run(
        async () => {
          await atualizarDepartamento(dep.id, {
            Nome: dep.nome,
            Tipo: dep.tipo ?? "EBD",
            Ativo: novoAtivo,
          });

          toastSuccess(
            novoAtivo ? "Turma ativada." : "Turma inativada.",
            "Concluído",
          );
          await carregarLista();
        },
        novoAtivo
          ? "Não foi possível ativar a turma."
          : "Não foi possível inativar a turma.",
      );
    },
  });
}

onMounted(carregarLista);
</script>

<template>
  <div class="page-container">
    <PageHeader
      titulo="Turmas EBD (Departamentos)"
      subtitulo="Cadastro e organização das turmas da Escola Bíblica Dominical"
    >
      <template #acoes>
        <Button
          v-if="podeGerenciarTurma"
          label="Nova Turma"
          icon="pi pi-plus"
          @click="abrirNovo"
        />
        <Button
          label="Recarregar"
          icon="pi pi-refresh"
          severity="secondary"
          :loading="carregando"
          @click="carregarLista"
        />
      </template>
    </PageHeader>

    <InlineMessage :texto="erro" tipo="erro" />

    <div style="display: flex; align-items: center; gap: 12px; flex-wrap: wrap">
      <span style="font-size: 13px; opacity: 0.8">Mostrar:</span>
      <SelectButton
        v-model="filtroStatus"
        :options="[
          { label: 'Ativas', value: 'ativas' },
          { label: 'Inativas', value: 'inativas' },
          { label: 'Todas', value: 'todas' },
        ]"
        optionLabel="label"
        optionValue="value"
      />

      <InputText
        v-model="busca"
        placeholder="Buscar por nome ou tipo..."
        style="min-width: 260px; margin-left: auto"
      />
    </div>

    <LoadingOverlay :loading="carregando" texto="Carregando turmas...">
      <DataTable
        :value="departamentosFiltrados"
        paginator
        :rows="10"
        rowHover
        sortField="nome"
        :sortOrder="1"
        dataKey="id"
        responsiveLayout="scroll"
      >
        <Column field="nome" header="Nome da turma" sortable />

        <Column field="tipo" header="Tipo" sortable style="width: 130px">
          <template #body="{ data }">
            <Tag :value="data.tipo || '-'" severity="secondary" />
          </template>
        </Column>

        <Column header="Ativo" style="width: 120px">
          <template #body="{ data }">
            <Tag
              :severity="data.ativo ? 'success' : 'danger'"
              :value="data.ativo ? 'Ativo' : 'Inativo'"
            />
          </template>
        </Column>

        <Column header="Ações" style="width: 160px">
          <template #body="{ data }">
            <div style="display: flex; gap: 6px; align-items: center">
              <Button
                v-if="podeGerenciarTurma"
                icon="pi pi-pencil"
                severity="secondary"
                size="small"
                v-tooltip.top="'Editar'"
                :disabled="carregando"
                @click="abrirEdicao(data)"
              />
              <Button
                v-if="podeGerenciarTurma"
                :icon="data.ativo ? 'pi pi-ban' : 'pi pi-check'"
                :severity="data.ativo ? 'danger' : 'success'"
                size="small"
                v-tooltip.top="data.ativo ? 'Desativar' : 'Ativar'"
                :disabled="carregando"
                @click="confirmarAlternarAtivo(data)"
              />
              <Button
                icon="pi pi-ellipsis-v"
                severity="secondary"
                size="small"
                text
                v-tooltip.top="'Mais opções'"
                @click="abrirMenu($event, data)"
              />
            </div>
          </template>
        </Column>
        <template #empty>
          <div style="padding: 14px; opacity: 0.7">
            Nenhuma turma encontrada para os filtros aplicados.
          </div>
        </template>
      </DataTable>
    </LoadingOverlay>

    <!-- Menu popup de ações extras (fora do Dialog) -->
    <Menu ref="menuAcoes" :model="itensMenu" :popup="true" />

    <Dialog
      v-model:visible="dialogAberto"
      modal
      :closable="!carregando"
      :dismissableMask="!carregando"
      header="Departamento / Turma"
      style="width: 520px; max-width: 92vw"
    >
      <div style="display: flex; flex-direction: column; gap: 10px">
        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Nome</label>
          <InputText
            v-model="formulario.nome"
            placeholder="Ex.: Adolescentes, Jovens, Adultos..."
          />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Nome') ||
              firstFieldError(fieldErrors, 'nome')
            "
          />
        </div>

        <div style="display: flex; flex-direction: column; gap: 6px">
          <label>Tipo</label>
          <InputText v-model="formulario.tipo" placeholder="Ex.: EBD" />
          <FieldError
            :texto="
              firstFieldError(fieldErrors, 'Tipo') ||
              firstFieldError(fieldErrors, 'tipo')
            "
          />
          <small style="opacity: 0.7"
            >Dica: se deixar vazio, a API tende a assumir "EBD".</small
          >
        </div>

        <div
          style="display: flex; align-items: center; gap: 10px; margin-top: 4px"
        >
          <Checkbox
            v-model="formulario.ativo"
            :binary="true"
            inputId="depAtivo"
          />
          <label for="depAtivo">Ativo</label>
        </div>
        <FieldError
          :texto="
            firstFieldError(fieldErrors, 'Ativo') ||
            firstFieldError(fieldErrors, 'ativo')
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
          :label="editandoId === null ? 'Criar' : 'Salvar'"
          icon="pi pi-check"
          :loading="carregando"
          @click="salvar"
        />
      </template>
    </Dialog>
  </div>
</template>
